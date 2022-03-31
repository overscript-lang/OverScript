
using System.Linq;
using static OverScript.ScriptClass;

namespace OverScript
{
    public class LocMark
    {
        public CodeFile CFile;
        public int Line;
        public int Line2;

        static char[] CharsToTrim = new char[] { '\t', '\r', ' ' };
        const string LineBreak = @"\n";
        public LocMark(CodeFile File, int line, int line2 = -1)
        {
            Line = line;
            Line2 = line;
            CFile = File;

        }
        public string Code
        {
            get
            {
                string code = "";
                for (int i = Line; i <= Line2; i++)
                {
                    string s = CFile.Lines[i];

                    RemoveComments(ref s);
                    code += s.Trim(CharsToTrim) + LineBreak;

                }
                if (code.EndsWith(LineBreak)) code = code.Remove(code.Length - 2, 2);
                return code;
            }
        }
        public string File => CFile.File;

        public LocMark Simplify() => new LocMark(CFile, Line2 < 0 ? Line : Line2);

        public static CodeFile NumToCodeFile(int num, Script script)
        {
            return script.CodeFiles.Where(x => x.Num == num).FirstOrDefault();
        }
    }

}
