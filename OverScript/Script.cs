using System;
using System.Collections.Generic;
using static OverScript.ScriptClass;

namespace OverScript
{
    public class Script
    {
        internal string ScriptDir;
        internal string ScriptFile;
        public string File => ScriptFile;
        public Dictionary<string, string> AppInfo = new Dictionary<string, string>() { { "Name", "App" } };

        internal int LockNum = 0;
        internal int ChainItemNum = 0;
        internal int ClassCount = 0;

        internal List<CodeFile> CodeFiles = new List<CodeFile>();
        internal int FnLayer;
        internal ScriptFunction CurrentBuildFn;
        internal ScriptClass FThis;
        internal CodeUnit CurrentBuildCU;
        internal List<Dictionary<string, VarType>> AvtProcessed = new List<Dictionary<string, VarType>>();

        internal BasicFunctions BFuncs = new BasicFunctions();

        internal Dictionary<string, VarType> AllTypes;



        internal List<string>[] VarNames = new List<string>[MaxTypeId + 1];
        internal ExceptionVarID ExVarID;

        internal ScriptClass RootClass;
        internal EventHandler<int> OnProgress;
        public const int LoadingSteps = 5;
        public Script(string file, EventHandler<int> onProgress = null)
        {
            OnProgress = onProgress;

            VarNames[(int)TypeID.IntArray] = new List<string>();
            VarNames[(int)TypeID.ObjectArray] = new List<string>();
            VarNames[(int)TypeID.LongArray] = new List<string>();
            VarNames[(int)TypeID.FloatArray] = new List<string>();
            VarNames[(int)TypeID.DoubleArray] = new List<string>();
            VarNames[(int)TypeID.DecimalArray] = new List<string>();
            VarNames[(int)TypeID.StringArray] = new List<string>();
            VarNames[(int)TypeID.CharArray] = new List<string>();
            VarNames[(int)TypeID.BoolArray] = new List<string>();
            VarNames[(int)TypeID.ShortArray] = new List<string>();
            VarNames[(int)TypeID.ByteArray] = new List<string>();
            VarNames[(int)TypeID.DateArray] = new List<string>();
            VarNames[(int)TypeID.CustomArray] = new List<string>();

            VarNames[(int)TypeID.Int] = new List<string>();
            VarNames[(int)TypeID.String] = new List<string>();
            VarNames[(int)TypeID.Char] = new List<string>();
            VarNames[(int)TypeID.Double] = new List<string>();
            VarNames[(int)TypeID.Float] = new List<string>();
            VarNames[(int)TypeID.Long] = new List<string>();
            VarNames[(int)TypeID.Decimal] = new List<string>();
            VarNames[(int)TypeID.Bool] = new List<string>();
            VarNames[(int)TypeID.Object] = new List<string>();
            VarNames[(int)TypeID.Short] = new List<string>();
            VarNames[(int)TypeID.Byte] = new List<string>();
            VarNames[(int)TypeID.Date] = new List<string>();
            VarNames[(int)TypeID.Custom] = new List<string>();

            ExVarID = new ExceptionVarID(this);
            AllTypes = new Dictionary<string, VarType>();
            foreach (var item in BasicTypes) AllTypes.Add(item.Key, item.Value);


            ScriptFile = file;
            ScriptDir = System.IO.Path.GetDirectoryName(file);
            string code = System.IO.File.ReadAllText(ScriptFile, System.Text.Encoding.UTF8);

            int i = code.IndexOf("#app ");

            if (i >= 0)
            {

                bool atStart = false;
                if (i > 0)
                {
                    string preText = code.Substring(0, i).Replace("\r", "").Replace("\n", "").Replace("\t", "").Trim();
                    atStart = preText.Length == 0;
                }
                else atStart = true;

                if (atStart)
                {
                    var d = PPDirective.CuteDirectiveLine(ref code, "app");
                    if (d.str != null)
                    {
                        string origDirective = d.str;
                        try { MakePreCode(this, ref d.str, ScriptFile); }
                        catch (Exception ex) { throw new ScriptLoadingException($"Invalid app directive. " + ex.Message); }

                        d.str = d.str.TrimStart();
                        var ppd = PPDirective.Get(d.str, ScriptFile, this);
                        if (ppd.Data == null || !CheckCharsInVarName(ppd.Data))
                            throw new ScriptLoadingException($"Invalid app name '{ppd.Data}'.");

                        AppInfo["Name"] = ppd.Data;
                        if (ppd.Params != null)
                        {
                            foreach (var item in ppd.Params)
                                AppInfo[item.Key] = item.Value;
                        }

                    }
                }



            }

            SetLoadingProgress(0);
            RootClass = new ScriptClass(code, AppInfo["Name"], null, this);
            SetLoadingProgress(-1);

        }

        internal void SetLoadingProgress(int step)
        {
            if (OnProgress != null) OnProgress(this, step);
        }

        internal int GetVarID<T>(string name)
        {
            TypeID tid = GetTypeID(typeof(T));
            return GetVarID(name, tid);


        }
        internal int GetVarID(string name, TypeID tid)
        {
            if (tid == TypeID.CustomArray) tid = TypeID.Custom;
            var names = VarNames[(int)tid];

            int i = names.IndexOf(name);
            if (i < 0) { names.Add(name); i = names.Count - 1; }
            return i;
        }
        internal string VarIdToName<T>(int id) => VarNames[(int)GetTypeID(typeof(T))][id];
        internal int VarNameCount<T>() => VarNames[(int)GetTypeID(typeof(T))].Count;



        internal void ImportFunctions(ref string code, string file)
        {


            var d = PPDirective.CuteDirectiveLine(ref code, "import");
            while (d.str != null)
            {
                var ppd = PPDirective.Get(d.str, file, this);
                BFuncs.Import(ppd);

                d = PPDirective.CuteDirectiveLine(ref code, "import", d.pos);
            }
        }





        public static implicit operator Executor(Script script)
        {
            return new Executor(script);
        }

    }


}
