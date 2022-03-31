using System;
using System.Collections.Generic;
using System.Threading;

namespace OverScript
{
    public partial class Executor : IDisposable
    {
        internal int ExecID;
        public int ID => ExecID;
        internal Script ExecutedScript;
        public Script Script => ExecutedScript;
        internal string[] Args;
        internal ClassInstance[] StaticInstances;
        internal Stack<int> FreeScopes;
        internal object FreeScopesLocker;
        internal int LastScope = 0;

        internal bool Disposed = false, Canceled = false, ForciblyCanceled = false;
        internal ClassInstance AppInstance;
        Dictionary<string, object> GlobalProperties = new Dictionary<string, object>();
        internal object GlobalPropertiesLocker = new object();
        internal ExecutionStatus ExecStatus = ExecutionStatus.None;
        public ExecutionStatus Status => ExecStatus;
        public bool IsValid => !Disposed && (Status == ExecutionStatus.Running || Status == ExecutionStatus.Completed);

        ManualResetEvent CancellationCompleted = new ManualResetEvent(false);
        public bool IgnoreFinalizerExceptions = false;

        public enum ExecutionStatus : byte { None, Running, Completed, Faulted, Canceled }
        public Executor(Script script)
        {
            ExecID = ExecPool.GetVacantID(this);
            ExecutedScript = script;



            SetVarStorage();

        }
        private Executor()
        {
            ExecID = -1;
            ExecutedScript = null;
        }
        internal static Executor GetConstExecutor() => new Executor();



        public void Dispose()
        {
            Disposed = Canceled = ForciblyCanceled = true;
            DropVarStorage();

            AppInstance = null;
            StaticInstances = null;
            ExecutedScript = null;
            FreeScopes = null;
            FreeScopesLocker = null;
            Args = null;
            GlobalProperties = null;
            GlobalPropertiesLocker = null;
            ExecPool.Executors[ExecID] = null;
            ExecID = -1;

        }
        public void Cancel(bool force = false)
        {

            ForciblyCanceled = force;
            Canceled = true;

            if (ExecStatus == ExecutionStatus.Running) CancellationCompleted.WaitOne();
        }
        public void Execute(string[] args = null)
        {
            if (ExecStatus != ExecutionStatus.None) throw new InvalidOperationException("Re-execution is not supported.");
            ExecStatus = ExecutionStatus.Running;
            Args = args;
            FreeScopes = new Stack<int>();
            FreeScopesLocker = new object();
            StaticInstances = new ClassInstance[ExecutedScript.ClassCount];
            try
            {
                AppInstance = new ClassInstance(this, ExecutedScript.RootClass);
            }
            catch
            {
                ExecStatus = Canceled && !Disposed ? ExecutionStatus.Canceled : ExecutionStatus.Faulted;
                CancellationCompleted.Set();
                throw;
            }
            ExecStatus = ExecutionStatus.Completed;
            if (Canceled) CancellationCompleted.Set();
        }
        internal ClassInstance GetStaticInstance(ScriptClass c)
        {
            var si = StaticInstances[c.ID];
            if (si == null)
                StaticInstances[c.ID] = si = new ClassInstance(this, c, null, null, -1, true);

            return si;
        }
        internal void SetStaticInstance(ScriptClass c, ClassInstance si)
        {
            StaticInstances[c.ID] = si;

        }

        public object Call(string fnName, params object[] args)
        {
            if (AppInstance != null)
                return ScriptClass.DynFuncCall(AppInstance, fnName, args);
            else
                throw new InvalidOperationException($"Cannot call a function because the main app instance is null.");

        }

        public object GetGlobal(string key)
        {
            lock (GlobalPropertiesLocker) { return GlobalProperties[key]; }
        }
        public void SetGlobal(string key, object value)
        {
            lock (GlobalPropertiesLocker) { GlobalProperties[key] = value; }
        }
        public Dictionary<string, object>.Enumerator GetGlobalPropertiesEnumerator() => GlobalProperties.GetEnumerator();

        public void ClearGlobalProperties()
        {
            GlobalProperties.Clear();
        }
        public void ClearRootVars()
        {
            ClearVarsInScope(AppInstance.Scope);
            var staticInst = StaticInstances[AppInstance.Class.ID];
            if(staticInst!=null) ClearVarsInScope(staticInst.Scope);
        }

        public object this[string key]
        {
            get { return GetGlobal(key); }
            set { SetGlobal(key, value); }
        }


    }


}
