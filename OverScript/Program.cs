using System;
using System.IO;
using System.Linq;

namespace OverScript
{
    class Program
    {

        static System.Reflection.Assembly ExecutingAssembly = System.Reflection.Assembly.GetExecutingAssembly();
        static public string OverScriptDir = Path.GetDirectoryName(ExecutingAssembly.Location);
        static public string ModulesDir = Path.Combine(Program.OverScriptDir, "modules");

        static int LoadingStatusCurPos = 0;


        static void Main(string[] args)
        {
      
            Console.Title = "OverScript";
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(UnhException);
            ExecPool.Capacity = 1;

            string scriptFile;
            string[] scriptArgs;

            if (args.Length > 0)
            {
                scriptFile = Path.GetFullPath(args[0]);
                scriptArgs = args.Skip(1).ToArray();
            }
            else
            {
                Console.WriteLine("Welcome to OverScript v" + ExecutingAssembly.GetName().Version+ ". See more at overscript.org.");
                Console.Write("Enter app file: ");
                string cl = Console.ReadLine();
                if (cl.Length < 1) throw new FileLoadException("App file not specified.");


                var scriptToRun = ScriptClass.GetFileAndArgs(cl);
                scriptFile = scriptToRun.file;
                scriptArgs = scriptToRun.args;
            }


            string code = System.IO.File.ReadAllText(scriptFile, System.Text.Encoding.UTF8);



            Console.CursorVisible = false;
            Console.Clear();

            string loadingStr = $"{Path.GetFileName(scriptFile)} loading";
            LoadingStatusCurPos = loadingStr.Length + 1;
            if (LoadingStatusCurPos >= Console.WindowWidth) LoadingStatusCurPos = Console.WindowWidth - 1;

            Console.Write(loadingStr + " ...");


            Script script = new Script(scriptFile, LoadingProgressChanged);
            Console.Clear();

            string scriptName = script.AppInfo["Name"];
            Console.Title = scriptName;

            string currentCulture;
            script.AppInfo.TryGetValue("CurrentCulture", out currentCulture);
            if (currentCulture != null)
                System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo(currentCulture);



            Console.CursorVisible = true;

            Executor exec = new Executor(script);
            exec.Execute(scriptArgs);

        }

        public static void LoadingProgressChanged(object sender, int step)
        {
            Console.SetCursorPosition(LoadingStatusCurPos, 0);

            Console.WriteLine(step < 0 ? "completed." : $"[{new string('#', step).PadRight(Script.LoadingSteps, '-')}]");
        }


        static void UnhException(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            string s = "";

            if (e.Data.Contains(ExceptionVarName.TypeName))
                s += (e.Data[ExceptionVarName.TypeName] ?? "Null_exception_name") + ": " + (e.Data[ExceptionVarName.Message] ?? "Null_exception_message") + Environment.NewLine;
            else
            {
                s = e.GetType().Name + ": " + e.Message;
                Exception ie = e.InnerException;
                while (ie != null)
                {
                    s += " ---> " + ie.GetType().Name + ": " + ie.Message;
                    ie = ie.InnerException;
                }
                s += Environment.NewLine;
            }
            if (e.Data.Contains(ExceptionVarName.StackTrace)) s += e.Data[ExceptionVarName.StackTrace] + Environment.NewLine;

            Console.WriteLine(s);
            Console.WriteLine("Interpreter stack trace:");
            Console.WriteLine(e.StackTrace);
            Console.WriteLine("");
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
            Environment.Exit(0);
        }
    }

    public enum TypeID : byte
    {
        None, Void, Bool, Byte, Short, Int, Long, Float, Double, Decimal, String, Char, Date, Object, Custom,
        BoolArray, ByteArray, ShortArray, IntArray, LongArray, FloatArray, DoubleArray, DecimalArray, StringArray, CharArray, DateArray, ObjectArray, CustomArray,

        Type, BasicType, CustomType, Array, OfRefType, Empty, HintType, HintArrayType,

        BoolType, ByteType, ShortType, IntType, LongType, FloatType, DoubleType, DecimalType, StringType, CharType, DateType, ObjectType,
        BoolArrayType, ByteArrayType, ShortArrayType, IntArrayType, LongArrayType, FloatArrayType, DoubleArrayType, DecimalArrayType, StringArrayType, CharArrayType, DateArrayType, ObjectArrayType
    }

    public static class ExecPool
    {
        public static int Capacity = 64;
        internal static Executor[] Executors = new Executor[Capacity];
        internal static int LastID = -1;
        internal static int GetVacantID(Executor exec)
        {
            int n = LastID;
            bool fromStart = false;
            do
            {
                n++;
                if (n >= Capacity) { n = 0; fromStart = true; }
                if (n > LastID && fromStart) throw new InvalidOperationException($"The maximum number of executors is used ({Capacity}).");


            } while (Executors[n] != null);

            LastID = n;
            Executors[n] = exec;
            return n;
        }
    }


    public class CodeFile
    {
        public string File;
        public string[] Lines;
        public string Base;
        public int Num;
        public CodeFile(string file, int num, Script script)
        {
            File = file;
            Lines = new string[0];
            Base = script.ScriptDir;
            Num = num;
        }
    }


    public static class StringExtension
    {

        public static int EIndexOf(this string text, string str, int pos, StringComparison stringComparison = StringComparison.InvariantCulture)
        {
            if (pos < 0 || pos >= text.Length) return -1;
            return text.IndexOf(str, pos, stringComparison);
        }
        public static int EIndexOf(this string text, char str, int pos)
        {
            if (pos < 0 || pos >= text.Length) return -1;
            return text.IndexOf(str, pos);
        }
        public static int ELastIndexOf(this string text, string str, int pos, StringComparison stringComparison = StringComparison.InvariantCulture)
        {
            if (pos < 0 || pos >= text.Length) return -1;
            return text.LastIndexOf(str, pos, stringComparison);
        }
        public static int ELastIndexOf(this string text, char str, int pos)
        {
            if (pos < 0 || pos >= text.Length) return -1;
            return text.LastIndexOf(str, pos);
        }
 

    }



    class ScriptExecutionException : Exception
    {
        public ScriptExecutionException(string message) : base(ScriptClass.RestoreCode(message)) { }

    }
    public class ScriptLoadingException : Exception
    {
        public ScriptLoadingException(string message) : base(ScriptClass.RestoreCode(message)) { }
    }
    class CustomThrownException : Exception
    {
        public CustomThrownException(string message) : base(message) { }
        public CustomThrownException() : base() { }
    }
    class InvalidByRefArgumentException : Exception
    {
        public InvalidByRefArgumentException(string message) : base(ScriptClass.RestoreCode(message)) { }
    }

    class ExecutingCanceledException : Exception
    {
        public ExecutingCanceledException() : base("Executing is canceled.") { }
        public ExecutingCanceledException(string message) : base(message) { }
        public static Exception GetCanceledException(bool forced, string msg = null)
        {
            return forced ? new ExecutingForciblyCanceledException(msg) : new ExecutingCanceledException(msg);
        }
    }
    class ExecutingForciblyCanceledException : ExecutingCanceledException
    {
        public ExecutingForciblyCanceledException() : base("Executing is forcibly canceled.") { }
        public ExecutingForciblyCanceledException(string message) : base(message) { }
    }
    class FinalizationFailedException : Exception
    {
        public FinalizationFailedException(string message, Exception inner) : base(message, inner) { }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ImportAttribute : System.Attribute { }





    public static class ExceptionVarName
    {
        public static string TypeName = "exName";
        public static string Message = "exMessage";
        public static string CustomExObj = "exObject";
        public static string Object = "exception";
        public static string StackTrace = "stackTrace";

        public static string NameVarInCustomExClass = "Name";
        public static string MessageVarInCustomExClass = "Message";
    }


}
