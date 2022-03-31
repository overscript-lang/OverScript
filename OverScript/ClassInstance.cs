using System;

namespace OverScript
{
    public class ClassInstance
    {
        public int Scope;
        public ScriptClass Class;
        public bool IsStatic;
        public CustomObject ThisObj;
        public Executor Exec;

        private ClassInstance()
        {
            Scope = -1;
            Exec = Executor.GetConstExecutor();
        }
        public static ClassInstance GetConstInst() => new ClassInstance();

        public ClassInstance(Executor exec, ScriptClass scriptClass, EvalUnit[] args = null, ClassInstance srcInst = null, int baseScope = -1, bool isStatic = false, bool ignoreConstructor = false, object constrFn = null)
        {
            Class = scriptClass;
            Scope = exec.NewScope();

            IsStatic = isStatic;
            Exec = exec;

            ClassInstance ci = this;

            CallStack cstack = null;
            if (!isStatic)
            {

                if (Class.InstanceFuncs != null)
                    for (int i = 0; i < Class.InstanceFuncs.Length; i++) Class.InstanceFuncs[i](args, baseScope, srcInst, ci, cstack, null);

                if (!ignoreConstructor)
                {
                    if (constrFn != null)
                    {

                        var fn = (ScriptClass.FuncToCall<object>)constrFn;
                        fn(args, baseScope, srcInst, ci, cstack, null);

                    }
                    else
                    {
                        if (Class.CtorFunc != null)
                        {
                            args = null;
                            Class.CtorFunc(args, baseScope, srcInst, ci, cstack, null);

                        }
                    }

                }
            }
            else
            {
                Exec.SetStaticInstance(scriptClass, this);

                if (Class.StaticFuncs != null)
                    for (int i = 0; i < Class.StaticFuncs.Length; i++) Class.StaticFuncs[i](args, baseScope, srcInst, ci, cstack, null);
            }
        }

        ~ClassInstance()
        {
            if (Exec.ForciblyCanceled) return;


            try
            {

                if (Class.FinalizeFuncs != null)
                    for (int i = 0; i < Class.FinalizeFuncs.Length; i++) Class.FinalizeFuncs[i](null, -1, null, this, null, null);


                if (Class.ForCleaning != null) Exec.RemVars(Scope, Class.ForCleaning);
                Exec.FreeScope(Scope);
            }
            catch (Exception ex)
            {
                if (!Exec.IgnoreFinalizerExceptions) throw new FinalizationFailedException("An error occurred during finalization. ", ex);
            }
        }

        public override string ToString()
        {

            return $"Instance{(IsStatic ? " (static)" : "")} of {Class.ClassFullName}";

        }
    }
}
