using System.Collections.Generic;
using System.Linq;
using static OverScript.Literals;
using static OverScript.ScriptClass;

namespace OverScript
{
    public class PPDirective
    {
        public string Code;
        public string CodeFile;

        public string Name;
        public string Path;
        public string Data;

        public Dictionary<string, string> Params;
        public static PPDirective Get(string str, string file, Script script)
        {
            if (!str.StartsWith("#")) return null;
            string baseDir = System.IO.Path.GetDirectoryName(file);
            var d = new PPDirective();
            d.Code = str;
            d.CodeFile = file;
            string[] parts = str.Split(',').Select(x => x.Trim()).ToArray();

            string[] p = parts[0].Split(' ').Select(x => x.Trim()).ToArray();
            d.Name = p[0].Remove(0, 1);

            int dataIndex = 1;
            if (p.Length > 1)
            {
                string p1 = p[1];
                if ((p1.StartsWith(LiteralMark) && GetLitTypeIdByStr(p1) == TypeID.String) || p1.StartsWith('<'))
                {
                    char c = p1[0];

                    string path = p1.Substring(1, p1.Length - 2);
                    if (c == '<')
                        path = System.IO.Path.GetFullPath(path, Program.ModulesDir);
                    else
                    {
                        path = GetLitByStr(path) as string ?? "";
                        if (path.Length > 0)
                        {

                            path = ExpandVars(path, script);
                            if (!System.IO.Path.IsPathFullyQualified(path)) path = System.IO.Path.GetFullPath(path, baseDir);
                            else path = System.IO.Path.GetFullPath(path);
                        }
                    }
                    d.Path = path;
                    dataIndex++;
                }
            }

            for (int n = dataIndex; n < p.Length; n++)
                d.Data += GetParamValue(p[n]) + " ";

            if (d.Data != null) d.Data = d.Data.TrimEnd();

            if (parts.Length > 1) d.Params = new Dictionary<string, string>();
            for (int n = 1; n < parts.Length; n++)
            {
                string s = parts[n];
                int i = s.IndexOf('=');
                if (i >= 0)
                {
                    string key = s.Substring(0, i).Trim();
                    string value = s.Substring(i + 1).Trim();

                    d.Params.Add(key, GetParamValue(value));
                }
                else
                    d.Params.Add(s, "");

            }
            return d;

            string GetParamValue(string value) => value.StartsWith(LiteralMark) ? GetLitByStr(value).ToString() : value;
        }

        public static (string str, int pos) CuteDirectiveLine(ref string code, string name, int start = 0, char endChar = '\n')
        {
            string drc = null;
            int i = code.IndexOf("#" + name, start);
            if (i >= 0)
            {
                int i2 = code.IndexOf(endChar, i);
                if (i2 < 0) i2 = code.Length;

                drc = code.Substring(i, i2 - i);
                drc = drc.TrimEnd('\r');
                code = code.Remove(i, i2 - i);
            }
            return (drc, i);
        }
    }
}
