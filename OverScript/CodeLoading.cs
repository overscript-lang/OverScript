using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static OverScript.Literals;

namespace OverScript
{
    public partial class ScriptClass
    {

        static string ArrowOperatorRaw = "->";
        static string ArrowOperator = "→";




        const char CodeLocationChar = (char)127;


        public static string RemLocationMarks(string code)
        {

            int i2, i = code.IndexOf(CodeLocationChar);
            while (i >= 0)
            {
                i2 = code.IndexOf(CodeLocationChar, i + 1);
                i2 = code.IndexOf(CodeLocationChar, i2 + 1);
                code = code.Remove(i, i2 + 1 - i);
                i = code.IndexOf(CodeLocationChar, i);
            }
            return code;
        }
        public static LocMark GetLocationMark(Script script, string code, bool onlyFirst = false)
        {
            LocMark lm = null, lm2 = null;
            string s;
            int i2 = -1, i = code.IndexOf(CodeLocationChar);
            while (i >= 0)
            {
                i2 = code.IndexOf(CodeLocationChar, i + 1);
                i2 = code.IndexOf(CodeLocationChar, i2 + 1);

                if (i2 + 1 >= code.Length || code[i2 + 1] != CodeLocationChar)
                {
                    s = code.Substring(i, i2 + 1 - i);
                    lm = FromLocationMarkStr(s, script);
                    break;
                }

                i = i2 + 1;
            }
            if (i2 >= 0 && !onlyFirst)
            {
                int i4 = code.LastIndexOf(CodeLocationChar);
                if (i4 != i2)
                {
                    int i3 = code.LastIndexOf(CodeLocationChar, i4 - 1);
                    i3 = code.LastIndexOf(CodeLocationChar, i3 - 1);
                    s = code.Substring(i3, i4 + 1 - i3);
                    lm2 = FromLocationMarkStr(s, script);
                }
            }
            if (lm2 != null && lm2.CFile.Num == lm.CFile.Num) lm.Line2 = lm2.Line;
            return lm;
        }
        public static void AddCodeLocationMarks(ref string code, string file, Script script)
        {
            var cf = script.CodeFiles.Where(x => x.File == file).FirstOrDefault();
            int fileNum;

            if (cf == null)
            {
                cf = new CodeFile(file, script.CodeFiles.Count, script);
                script.CodeFiles.Add(cf);
                fileNum = script.CodeFiles.Count - 1;
            }
            else
                fileNum = script.CodeFiles.IndexOf(cf);

            code = code.Replace(CodeLocationChar.ToString(), "");
            string[] lines = code.Split('\n');

            cf.Lines = (string[])lines.Clone();

            for (int i = 0; i < lines.Length; i++)
            {
                if (RemCRAndTabs(lines[i]).Length > 0) lines[i] = ToLocationMarkStr(fileNum, i) + lines[i];

            }
            code = string.Join('\n', lines);

        }

        const char LM_0 = (char)15;
        const char LM_1 = (char)16;
        public static string ToLocationMarkStr(int file, int line)
        {
            string eFile = Convert.ToString(file, 2).Replace('1', LM_1).Replace('0', LM_0);
            string eLine = Convert.ToString(line, 2).Replace('1', LM_1).Replace('0', LM_0);

            return $"{CodeLocationChar}{eFile}{CodeLocationChar}{eLine}{CodeLocationChar}";
        }
        public static LocMark FromLocationMarkStr(string lm, Script script)
        {
            lm = lm.Trim(CodeLocationChar);
            int i = lm.IndexOf(CodeLocationChar, StringComparison.Ordinal);
            string fileNum = lm.Substring(0, i).Replace(LM_1, '1').Replace(LM_0, '0');
            string line = lm.Substring(i + 1).Replace(LM_1, '1').Replace(LM_0, '0');

            return new LocMark(LocMark.NumToCodeFile(Convert.ToInt32(fileNum, 2), script), Convert.ToInt32(line, 2));
        }
        public static void MakePreCode(Script script, ref string code, string path, bool noMarks = false)
        {
            if (!noMarks) AddCodeLocationMarks(ref code, path, script);
            RemoveComments(ref code);
            code = " " + code;
            ExtractLiterals(ref code, script);
        }
        private static void PasteIncludes(ref string code, string baseFile, Script script)
        {



            MakePreCode(script, ref code, baseFile);


            string content;

            var d = PPDirective.CuteDirectiveLine(ref code, "include");
            while (d.str != null)
            {
                var ppd = PPDirective.Get(d.str, baseFile, script);
                if (!File.Exists(ppd.Path)) throw new ScriptLoadingException($"Couldn't find include file '{ppd.Path}'.");
                content = File.ReadAllText(ppd.Path);




                PasteIncludes(ref content, ppd.Path, script);


                code = code.Insert(d.pos, content);

                d = PPDirective.CuteDirectiveLine(ref code, "include", d.pos);
            }


            script.ImportFunctions(ref code, baseFile);


            d = PPDirective.CuteDirectiveLine(ref code, "primebase");
            if (d.str != null)
            {


                var cf = script.CodeFiles.Where(x => x.File == baseFile).FirstOrDefault();

                var ppd = PPDirective.Get(d.str, baseFile, script);

                cf.Base = TrimLastSlash(ppd.Path) ?? Path.GetDirectoryName(baseFile);
            }

            do
            {
                d = PPDirective.CuteDirectiveLine(ref code, "app");
            } while (d.str != null);



        }
        static string TrimLastSlash(string str) => str == null ? null : (str.TrimEnd('\\').TrimEnd('/'));





        private static string RemCRAndTabs(string code)
        {
            code = code.Replace("\r", "");
            code = code.Replace("\t", "");

            return code;
        }



        private static void PrepareCode(ref string code, Script script)
        {




            PasteIncludes(ref code, script.ScriptFile, script);



            code = RemCRAndTabs(code);
            code = code.Replace("\n", " ");

            script.SetLoadingProgress(1);
            CheckBraces(code, 1, script);
            CheckBraces(code, 2, script);

            while (code.IndexOf("  ", StringComparison.Ordinal) >= 0) code = code.Replace("  ", " ");

            code = code.Replace(" {", "{");

            code = code.Replace(" else if ", " elseif ");
            code = code.Replace("}else if(", "}elseif(");
            code = code.Replace("}else if ", "}elseif ");
            code = code.Replace(" else if(", " elseif(");
            code = code.Replace(CodeLocationChar + "else if(", CodeLocationChar + "elseif(");
            code = code.Replace(CodeLocationChar + "else if ", CodeLocationChar + "elseif ");

            code = code.Trim();

            script.SetLoadingProgress(2);
            ProtectSpaces(ref code);

            code = code.Replace(" else ", " else" + ServiceChar);
            code = code.Replace("}else ", "}else" + ServiceChar);
            code = code.Replace(CodeLocationChar + "else ", CodeLocationChar + "else" + ServiceChar);

            code = code.Replace(" ", "");
            code = code.Replace(ServiceChar, " ");


            ItemizeMultiDeclaring(ref code);
            ProtectInitializers(ref code);

            script.SetLoadingProgress(3);
            AddBraces(ref code, script);


            script.SetLoadingProgress(4);
            ReplaceOperators(ref code);

            ReplaceDoubleDot(ref code);

            ReplaceForEachIn(ref code);

            ApplyTopSyntacticSugar(ref code, script);

            code = code.Remove(0, 1);

            int i = code.IndexOf(';');
            while (i >= 0)
            {
                if (InsideBlock(code, i, "(", ")")) code = code.Substring(0, i) + ServiceChar + code.Substring(i + 1);

                i = code.IndexOf(';', i + 1);
            }

            code = code.Replace(";", $"{UnitSeparator};{UnitSeparator}");

            code = code.Replace("{", $"{@"{"}{UnitSeparator}");
            code = code.Replace("}", $"{UnitSeparator}{@"}"}{UnitSeparator}");


            code = code.Replace(":", ":" + UnitSeparator);


            code = code.Replace(ProtectedOpenBrace, '{').Replace(ProtectedCloseBrace, '}');


            script.SetLoadingProgress(5);
        }

        static void ReplaceForEachIn(ref string code)
        {
            char c;
            int i = code.IndexOf("foreach(");
            while (i >= 0)
            {
                if (i > 0) c = code[i - 1]; else c = default(char);
                if (c != '_' && !Char.IsLetterOrDigit(c))
                {
                    int ii = i + 8;
                    int i2 = FindClosing(code, ii, "(", ")");
                    if (i2 < 0) break;
                    int i3 = code.IndexOf(" in", ii);
                    while (i3 > i && i3 < i2)
                    {
                        c = code[i3 + 3];
                        if (!char.IsLetterOrDigit(c) && c != '_')
                        {
                            code = code.Remove(i3, c == ' ' ? 4 : 3).Insert(i3, ServiceChar);
                            break;
                        }
                        i3 = code.IndexOf(" in", i3 + 1);
                    }
                }
                i = code.IndexOf("foreach(", i + 1);
            }
        }
        private static void ApplyTopSyntacticSugar(ref string code, Script script)
        {
            code = InterpolateStrings(code);
            ReplaceLock(ref code, script);
            ReplaceUsing(ref code, script);
            code = ReplaceOptionalChaining(code, script);
            code = ReplaceTernary(code);


        }


        static void ReplaceDoubleDot(ref string code)
        {
            int i = code.IndexOf("..");
            while (i >= 0)
            {
                if (InsideBlock(code, i, "[", "]"))
                    code = code.Remove(i, 2).Insert(i, ",");

                i = code.IndexOf("..", i + 1);
            }
        }



        const string UnitSeparator = "\xe";
        private CodeUnit[] ParseCode(ScriptFunction fn)
        {
            string code = fn.Code;
            string s;

            string[] lines = code.Split(UnitSeparator);
            CodeUnit[] units = new CodeUnit[lines.Length];

            LocMark lm0 = null;
            for (int i = 0; i < units.Length; i++)
            {

                units[i] = new CodeUnit();
                units[i].Fn = fn;
                LocMark lm = GetLocationMark(CurScript, lines[i]);

                if (lm == null) lm = lm0;
                else
                {
                    if (!lines[i].StartsWith(CodeLocationChar))
                        lm.Line = lm0.Line;
                    lm0 = lm;
                }
                units[i].CodeLocation = lm;

                lines[i] = RemLocationMarks(lines[i]);



            }
            UnitType currentBlockType = 0;

            bool nestingDecrease;
            Stack<UnitType> types = new Stack<UnitType>();
            for (int i = 0; i < lines.Length; i++)
            {
#if EXON
                try
                {
#endif
                    nestingDecrease = false;
                    s = lines[i];
                    if (s == ";") continue;
                    else if (s == "}")
                    {
                        currentBlockType = types.Peek();
                        if (currentBlockType == UnitType.If || currentBlockType == UnitType.ElseIf)
                        {
                            units[i].Type = UnitType.EndIf;
                        }
                        else if (currentBlockType == UnitType.Else)
                        {
                            units[i].Type = UnitType.EndIf;
                        }
                        else if (currentBlockType == UnitType.Do)
                        {
                            units[i].Str = new string[] { "" };
                            i++;
                            if (i >= units.Length) { i--; throw new ScriptLoadingException("Invalid 'do' statement."); }
                            if (!lines[i].StartsWith("while(")) { i--; throw new ScriptLoadingException("'do' block must end with 'while' statement."); }
                            units[i].Type = UnitType.EndDo;

                            s = lines[i];
                            s = GetUnitStr(s);

                        }
                        else
                        {
                            units[i].Type = (UnitType)((int)currentBlockType * 10 + 1);
                        }

                        nestingDecrease = true;
                    }

                    else if (s == "break")
                    {
                        units[i].Type = UnitType.Break;

                    }
                    else if (s == "continue")
                    {
                        units[i].Type = UnitType.Continue;

                    }
                    else if (s == "default:")
                    {
                        units[i].Type = UnitType.DefaultCase;

                    }
                    else if (s == "do{")
                    {
                        units[i].Type = UnitType.Do;
                        types.Push(units[i].Type);
                    }
                    else if (s == "else{")
                    {
                        units[i].Type = UnitType.Else;
                        types.Push(units[i].Type);
                        units[i - 1].Type = UnitType.EndIfTrueBlock;

                    }
                    else if (s == "try{")
                    {
                        units[i].Type = UnitType.Try;
                        types.Push(units[i].Type);
                    }
                    else if (s == "finally{")
                    {
                        units[i].Type = UnitType.Finally;
                        types.Push(units[i].Type);
                    }
                    else if (s.StartsWith("goto "))
                    {
                        units[i].Type = UnitType.GoTo;
                        s = s.Substring(5);

                    }
                    else if (s.StartsWith("if(")) { units[i].Type = UnitType.If; types.Push(units[i].Type); s = GetUnitStr(s); }
                    else if (s.StartsWith("for(")) { units[i].Type = UnitType.For; types.Push(units[i].Type); s = GetUnitStr(s); }
                    else if (s.StartsWith("foreach(")) { units[i].Type = UnitType.ForEach; types.Push(units[i].Type); s = GetUnitStr(s); }
                    else if (s.StartsWith("while(")) { units[i].Type = UnitType.While; types.Push(units[i].Type); s = GetUnitStr(s); }
                    else if (s.StartsWith("elseif(")) { units[i].Type = UnitType.ElseIf; types.Push(units[i].Type); s = GetUnitStr(s); units[i - 1].Type = UnitType.EndIfTrueBlock; }
                    else if (s.StartsWith("switch(")) { units[i].Type = UnitType.Switch; types.Push(units[i].Type); s = GetUnitStr(s); }
                    else if (s.StartsWith("return(")) { units[i].Type = UnitType.Return; s = GetUnitStr(s); }
                    else if (s.StartsWith("return ")) { units[i].Type = UnitType.Return; s = s.Substring(7); }
                    else if (s.StartsWith("throw(")) { units[i].Type = UnitType.Throw; s = GetUnitStr(s); s = string.Join(ServiceChar, SmartSplit(s)); }
                    else if (s.StartsWith("throw ")) { units[i].Type = UnitType.Throw; s = s.Substring(6); }
                    else if (s.StartsWith("apply ")) { units[i].Type = UnitType.Apply; s = s.Substring(6); }
                    else if (s.StartsWith("apply$ ")) { units[i].Type = UnitType.Reapply; s = s.Substring(7); }
                    else if (s.StartsWith("apply(")) { units[i].Type = UnitType.Apply; s = GetUnitStr(s); }
                    else if (s.StartsWith("apply$(")) { units[i].Type = UnitType.Reapply; s = GetUnitStr(s); }

                    else if (s == "throw") { units[i].Type = UnitType.Throw; s = ""; }
                    else if (s == "return") { units[i].Type = UnitType.Return; s = ""; }
                    else if (s == "catch{") { units[i].Type = UnitType.Catch; types.Push(units[i].Type); s = ""; }
                    else if (s.StartsWith("catch(")) { units[i].Type = UnitType.Catch; types.Push(units[i].Type); s = GetUnitStr(s); }
                    else if (s.StartsWith("catch$(")) { units[i].Type = UnitType.CatchByName; types.Push(units[i].Type); s = GetUnitStr(s); }
                    else if (s.EndsWith(":"))
                    {
                        if (s.StartsWith("case "))
                        {
                            units[i].Type = UnitType.Case;
                            s = s.Substring(5, s.Length - 6);

                        }
                        else
                        {
                            units[i].Type = UnitType.Label;
                            s = s.Remove(s.Length - 1, 1);

                        }
                    }
                    else
                    {
                        units[i].Type = 0;

                    }

                    units[i].Code = ReplaceServiceChar(lines[i], units[i].Type);
                    if (units[i].Code.Length > 1) units[i].Code = units[i].Code.TrimEnd('{');
                    units[i].Str = s.Split(ServiceChar);
                    if (units[i].Type == UnitType.For && units[i].Str.Length != 3)
                        throw new ScriptLoadingException("Invalid 'for' statement.");
                    if (units[i].Type == UnitType.ForEach && units[i].Str.Length != 2)
                        throw new ScriptLoadingException("Invalid 'foreach' statement.");

                    units[i].Nesting = types.Count;
                    if (nestingDecrease) types.Pop();
#if EXON
                }
                catch (Exception ex)
                {
                    var lm = units[i].CodeLocation ?? units[0].CodeLocation;
                    throw new ScriptLoadingException(ErrMsgWithLoc("Code parsing error. " + ex.Message, lm));
                }
#endif
            }

            units = units.Where((x) => x.Type == UnitType.Return || x.Type == UnitType.Catch || x.Type == UnitType.Throw || (x.Str != null && (x.Str.Length > 1 || x.Str[0].Trim().Length > 0))).ToArray();

            return units;

        }
        static string ReplaceServiceChar(string str, UnitType utype) => str.Replace(ServiceChar, utype == UnitType.ForEach ? " in " : ";");
        public static string ErrMsgWithLoc(string msg, LocMark lm, string interpreterView = null, ScriptClass atClass = null, ScriptFunction atFunc = null)
        {
            string at = "";
            if (atClass != null && atFunc != null)
            {
                string fnPath = GetClassPath(atClass) + "." + FormatFuncSign(atFunc.Signature);
                at = $"at {fnPath} ";
            }
            if (interpreterView != null) interpreterView = $"{Environment.NewLine}\t\t\tInterpreter view: {interpreterView}";
            string locInfo = lm != null ? $"{at}in {lm.File}:line {lm.Line + 1}{Environment.NewLine}\t\t{lm.Code}{interpreterView}{Environment.NewLine}" : "unknown code location";
            return msg != null ? $"{msg}{Environment.NewLine}\t{locInfo}" : '\t' + locInfo;

        }


        const string ServiceChar = "\x3";

        private static void ProtectSpaces(ref string code)
        {

            char[] result = code.ToArray();
            int i = code.IndexOf(' ', 1);
            while (i >= 0)
            {
                int nextIndex = SkipLocationMarks(code, i + 1);
                if (nextIndex >= code.Length) break;
                char next = code[nextIndex];
                int prevIndex = RSkipLocationMarks(code, i - 1);
                if (prevIndex >= 0)
                {
                    char prev = code[prevIndex];




                    if ((prev == '_' || prev == ']' || prev == '$' || Char.IsLetterOrDigit(prev)) && (next == '_' || next == LiteralMark || next == BasicFunctionPrefix || next == FunctionLayerPrefix || next == RedefinePrefix || next == OrLocalFunctionPrefix[0] || next == OrBasicFunctionPrefix[0] || Char.IsLetterOrDigit(next) || next == TypeHintChar || next == AutoVarPrefix))
                        result[i] = ServiceChar[0];
                    else
                    {
                        if (next == '-' && prev == 'n')
                        {
                            int p = i - 6;
                            if (p >= 0 && code.Substring(p, 6) == "return")
                            {
                                char c = default(char);
                                if (p > 0) c = code[p - 1];
                                if (p == 0 || !Char.IsLetterOrDigit(c) && c != '_')
                                    result[i] = ServiceChar[0];

                            }
                        }
                    }
                }
                i = code.IndexOf(' ', i + 1);
            }
            code = new string(result);
        }

        private static void ExtractLiterals(ref string code, Script script)
        {

            ExtractStrLiterals(ref code, script);
            ExtractCharLiterals(ref code, script);
            ExtractNumLiterals(ref code, script);
        }

        const char AutoVarPrefix = '°';

        private static void ReplaceLock(ref string code, Script script)
        {
            int i2, i = code.IndexOf("lock(");
            char c;
            string s;

            while (i >= 0)
            {
                try
                {
                    if (i > 0) c = code[i - 1]; else c = default(char);


                    if (c != '_' && !Char.IsLetterOrDigit(c))
                    {
                        i2 = FindClosing(code, i, "(", ")");
                        if (i2 < 0) throw new ScriptLoadingException("Invalid lock statement.");
                        s = code.Substring(i + 5, i2 - (i + 5));
                        int i3 = code.IndexOf('{', i2);
                        if (i3 < 0) throw new ScriptLoadingException("Invalid lock statement.");
                        script.LockNum++;
                        string lockObj = $"{AutoVarPrefix}lock_obj_{script.LockNum}", lockWasTaken = $"{AutoVarPrefix}lock_WasTaken_{script.LockNum}";
                        string t = $"object {lockObj}{OperatorServiceStr[OperationKind.Assignment]}{s};bool {lockWasTaken};try{{Lock({lockObj},{lockWasTaken});";
                        code = code.Remove(i, i3 + 1 - i).Insert(i, t);
                        int i5 = i + t.Length;
                        int i4 = FindClosing(code, i5, "{", "}", i5);
                        if (i4 < 0) throw new ScriptLoadingException("Invalid lock statement.");
                        string finallyCode = $"finally{{if({lockWasTaken}){{Unlock({lockObj});}}}}";
                        code = code.Insert(i4 + 1, finallyCode);


                    }
                    i = code.IndexOf("lock(", i + 1);
                }
                catch (Exception ex)
                {
                    var lm = GetLocationMark(script, code.Substring(0, i)).Simplify();
                    throw new ScriptLoadingException(ErrMsgWithLoc(ex.Message, lm));
                }
            }

        }

        private static void ReplaceUsing(ref string code, Script script)
        {
            int i2, i = code.IndexOf("using(");
            char c;
            string s;

            while (i >= 0)
            {
                try
                {
                    if (i > 0) c = code[i - 1]; else c = default(char);


                    if (c != '_' && !Char.IsLetterOrDigit(c))
                    {
                        i2 = FindClosing(code, i, "(", ")");
                        if (i2 < 0) throw new ScriptLoadingException("Invalid using statement.");
                        s = code.Substring(i + 6, i2 - (i + 6));
                        int i3 = code.IndexOf('{', i2);
                        if (i3 < 0) throw new ScriptLoadingException("Invalid using statement.");


                        string t = $"{s};try{{";
                        code = code.Remove(i, i3 + 1 - i).Insert(i, t);
                        int i5 = i + t.Length;
                        int i4 = FindClosing(code, i5, "{", "}", i5);
                        if (i4 < 0) throw new ScriptLoadingException("Invalid using statement.");

                        int j = SmartCharPos(s, OperatorChar[0]);

                        if (j >= 0) s = s.Substring(0, j);
                        j = s.IndexOf(' ');

                        if (j >= 0) s = s.Substring(j + 1).TrimStart(RedefinePrefix);
                        if (!CheckCharsInVarName(s)) throw new ScriptLoadingException($"Invalid using statement. Variable '{s}' is invalid.");


                        string finallyCode = $"finally{{{s}?.Dispose();}}";
                        code = code.Insert(i4 + 1, finallyCode);


                    }
                    i = code.IndexOf("using(", i + 1);
                }
                catch (Exception ex)
                {
                    var lm = GetLocationMark(script, code.Substring(0, i)).Simplify();
                    throw new ScriptLoadingException(ErrMsgWithLoc(ex.Message, lm));
                }
            }

        }


        private static string ReplaceOptionalChaining(string s, Script script)
        {
            if (s.IndexOf('?') < 0) return s;


            char op = '?';
            int i = s.IndexOf(op);
            while (i >= 0)
            {
                int ii = i + 1;
                if (ii >= s.Length) break;
                char nextChar = s[ii];
                if (nextChar != '.' && nextChar != '[' && nextChar != ArrowOperator[0])
                {
                    i = s.IndexOf(op, ii); continue;
                }

                string left = s.Substring(0, i);
                int start = FindStartOfExpression(left);
                if (left[start + 1] == ' ') start++;
                start = SkipLocationMarks(left, start + 1);
                left = left.Substring(start);
                int opPos = SmartCharPosRev(left, OperatorChar[0]);
                if (opPos >= 0) { start = opPos; left = left.Substring(start + 1); }
                int sp = SmartCharPos(left, ' ');
                if (sp >= 0) { start = sp; left = left.Substring(start + 1); }

                while (left.StartsWith('('))
                {
                    int j = SmartCharPos(left, ')');
                    if (j < left.Length - 1) { start = j; left = left.Substring(start + 1); } else break;
                }



                string right = s.Substring(i + 1);

                int end = FindEndOfExpression(right);
                if (end < 0) end = right.Length;
                if (right[end - 1] == ' ') end--;
                end = RSkipLocationMarks(right, end - 1);
                right = right.Substring(0, end + 1);
                opPos = SmartCharPos(right, OperatorChar[0]);
                if (opPos >= 0) { end = opPos; right = right.Substring(0, end); }

                string val = left;
                ApplyTopSyntacticSugar(ref val, script);
                script.ChainItemNum++;
                string chainVar = $"{AutoVarPrefix}chainItem_{script.ChainItemNum}";


                string fn = $"{BasicFunctionPrefix}IfNotNull((var {chainVar}{OperatorServiceStr[OperationKind.Assignment]}{val}),_({ReplaceOptionalChaining(chainVar + right, script)},{chainVar}{OperatorServiceStr[OperationKind.Assignment]}null))";
                int i3 = i - left.Length;
                s = s.Substring(0, i3) + fn + s.Substring(i + 1 + right.Length);

                i = s.IndexOf(op, i3);
            }

            return s;
        }

        private static string ReplaceTernary(string s)
        {
            char op = '?';

            int i = s.LastIndexOf(op);
            while (i >= 0)
            {
                string left = s.Substring(0, i);
                int start = FindStartOfExpression(left);
                if (left[start + 1] == ' ') start++;
                start = SkipLocationMarks(left, start + 1);
                left = left.Substring(start);
                int assignPos = FindLastAssignOp(left);
                if (assignPos >= 0) left = left.Substring(assignPos + 1);
                int sp = SmartCharPos(left, ' ');
                if (sp >= 0) left = left.Substring(sp + 1);

                string right = s.Substring(i + 1);
                int end = FindEndOfExpression(right, 0, true);
                if (end < 0) end = right.Length;
                if (right[end - 1] == ' ') end--;
                end = RSkipLocationMarks(right, end - 1);
                right = right.Substring(0, end + 1);
                string trueVal = right, falseVal = "";
                int i2 = right.IndexOf(':');
                if (i2 >= 0)
                {
                    falseVal = right.Substring(i2 + 1);
                    trueVal = right.Substring(0, i2);
                }
                string fn = $"{BasicFunctionPrefix}If({left},{trueVal},{falseVal})";

                int i3 = i - left.Length;
                s = s.Substring(0, i3) + fn + s.Substring(i + 1 + right.Length);

                i = s.LastIndexOf(op, i3);
            }
            return s;
        }



        private Type ReplaceReflArrows(ref string s, bool top = false)
        {
            Type type = null;
            int arrowCount = 0;
            bool lastArrow = false;
            int arrowPos = s.LastIndexOf(ArrowOperator);
            while (arrowPos >= 0)
            {
                arrowCount++;
                int end = FindEndOfExpression(s, arrowPos);
                if (end < 0) end = s.Length;

                int start = FindStartOfExpression(s, arrowPos);
                string m = s.Substring(start + 1, end - (start + 1));
                arrowPos = arrowPos - (start + 1);
                int end2 = SmartCharPos(m, OperatorChar[0], arrowPos);
                int end22 = SmartCharPos(m, '.', arrowPos);
                if (end22 >= 0 && (end22 < end2 || end2 < 0))
                {
                    end2 = -1;
                    end = start + 1 + end22;
                    m = m.Substring(0, end22);
                }

                OperationKind op = OperationKind.None;
                bool isAssignment = false, isAdditionOrSubtractionAssignment = false;
                string setVal = null;

                if (end2 >= 0)
                {
                    op = (OperationKind)byte.Parse(m.Substring(end2 + 1, 2));
                    if (isAssignment = OpIsAnyAssign(op))
                    {
                        isAdditionOrSubtractionAssignment = op == OperationKind.AdditionAssignment || op == OperationKind.SubtractionAssignment;
                        if (op != OperationKind.Assignment && op != OperationKind.ForcedAssignment && !isAdditionOrSubtractionAssignment)
                            throw new ScriptLoadingException("Shorthand operators are not supported when using the arrow operator.");

                        setVal = m.Substring(end2 + 4);

                    }
                }
                else
                    end2 = m.Length;



                string member = m.Substring(arrowPos + 1, end2 - (arrowPos + 1));
                if (isAssignment) end2 = m.Length;

                int bp = member.IndexOfAny(MemberBrackets);
                if (bp >= 0)
                {
                    char bpc = member[bp];
                    char cb = bpc == '(' ? ')' : ']';
                    int p = SmartCharPos(member, cb);
                    if (p < 0) throw new ScriptLoadingException($"Invalid member '{member}'.");
                    p++;
                    int diff = member.Length - p;
                    if (diff != 0)
                    {
                        if (isAssignment) throw new ScriptLoadingException($"Invalid member '{member}'.");
                        else
                        {
                            end2 -= diff;
                            member = member.Substring(0, p);
                        }
                    }
                }

                int start2 = SmartCharPosRev(m, OperatorChar[0], arrowPos);
                while (m[start2 + 1] == '(')
                {
                    int j = SmartCharPos(m, ')', start2 + 1);
                    if (j + 1 < arrowPos) start2 = j; else break;
                }

                string obj = m.Substring(start2 + 1, arrowPos - (start2 + 1));



                Type leftType = ReplaceReflArrows(ref obj);
                bool typeUnknown = leftType == null || leftType == typeof(object);
                string inv = null;
                lastArrow = top || end < s.Length;
                bool isFunc = member.EndsWith(')');
                if (isFunc)
                {
                    int i = member.IndexOf('(');
                    string fn = member.Substring(0, i);
                    string args = member.Substring(i + 1, member.Length - (i + 2));
                    if (args.Length > 0) args = "," + args;
                    setInv:
                    if (typeUnknown)
                    {
                        int memberLitNum = SetLiteral<string>(fn);

                        inv = $"{BasicFunctionPrefix}InvokeMethod({obj},{LiteralMark + memberLitNum.ToString() + LiteralMark}{args})";
                    }
                    else
                    {
                        var argTypes = SmartSplit(args).Skip(1).Select(x => GetExpressionType(x, CurScript.CurrentBuildFn.VarTypes).Type);
                        var parameterTypes = argTypes.Select(x => x.TypeHint ?? (x.T != typeof(object) ? x.T : null)).ToArray();

                        if (parameterTypes.Contains(null)) { typeUnknown = true; goto setInv; }

                        Type atType;
                        var memberInfo = (System.Reflection.MethodInfo)BasicFunctions.GetMethod(fn, leftType, parameterTypes, out atType);
                        if (memberInfo == null) throw new ScriptLoadingException($"Could not find method '{fn.TrimStart(AtRuntimeTypeMethodPrefix)}({string.Join(',', parameterTypes.Select(x => x.FullName))})' at '{atType}'.");
                        int memberLitNum = SetLiteral<object>(memberInfo);
                        string left = obj;
                        if (memberInfo.IsStatic) left = "";

                        bool typed = false;
                        string typeArg = "";

                        if (lastArrow)
                        {
                            var rt = memberInfo.ReturnType;
                            string returnType = rt != typeof(void) && rt != typeof(object) ? GetReflType(rt) : "";
                            if (typed = returnType.Length > 0) typeArg = "," + returnType;
                        }
                        inv = $"{BasicFunctionPrefix}{(typed ? "T" : "")}Invoke({LiteralMark + memberLitNum.ToString() + LiteralMark}{typeArg}{(left.Length > 0 || args.Length > 0 ? "," + left : "")}{args})";
                        type = memberInfo.ReturnType;


                    }
                }
                else
                {
                    string left = obj;
                    string indexes = "";
                    if (member.EndsWith(']'))
                    {
                        int i = member.IndexOf('[');
                        indexes = member.Substring(i + 1, member.Length - (i + 2));
                        if (indexes.Length > 0) indexes = "," + indexes;
                        member = member.Substring(0, i);
                    }
                    if (typeUnknown)
                    {
                        int memberLitNum = SetLiteral<string>(member);
                        if (isAssignment)
                        {
                            if (!isAdditionOrSubtractionAssignment) inv = $"{BasicFunctionPrefix}SetMemberValue({obj},{LiteralMark + memberLitNum.ToString() + LiteralMark},{setVal}{indexes})";
                            else
                            {
                                string fn = op == OperationKind.AdditionAssignment ? "AddEventHandler" : "RemoveEventHandler";

                                inv = $"{BasicFunctionPrefix}{fn}({BasicFunctionPrefix}GetMember({obj},{LiteralMark + memberLitNum.ToString() + LiteralMark}),{obj},{setVal})";
                            }
                        }
                        else
                            inv = $"{BasicFunctionPrefix}GetMemberValue({obj},{LiteralMark + memberLitNum.ToString() + LiteralMark}{indexes})";
                    }
                    else
                    {

                        Type atType;
                        var memberInfo = BasicFunctions.GetPropertyOrField(member, leftType, out atType);
                        if (memberInfo == null) throw new ScriptLoadingException($"Member '{member.TrimStart(AtRuntimeTypeMethodPrefix)}' not found at '{atType}'.");

                        var field = memberInfo as System.Reflection.FieldInfo;
                        var prop = memberInfo as System.Reflection.PropertyInfo;
                        if (field != null && field.IsLiteral)
                        {
                            if (!isAssignment)
                            {
                                var val = field.GetValue(null);
                                var t = val.GetType();
                                int memberLitNum = SetLiteral(val);
                                inv = LiteralMark + memberLitNum.ToString() + LiteralMark;
                                type = field.FieldType;

                            }
                            else
                                throw new ScriptLoadingException($"Can't assign a value to a constant field '{field.Name}'.");
                        }
                        else
                        {
                            bool isProp = prop != null;
                            if (isProp)
                            {
                                bool isStatic;
                                if (isAssignment && !(indexes.Length > 0 && prop.PropertyType.IsArray))
                                {
                                    if (!prop.CanWrite) throw new ScriptLoadingException($"Property '{prop.Name}' is not writeable.");
                                    isStatic = prop.SetMethod.IsStatic;
                                }
                                else
                                {
                                    if (!prop.CanRead) throw new ScriptLoadingException($"Property '{prop.Name}' is not readable.");
                                    isStatic = prop.GetMethod.IsStatic;
                                }

                                if (isStatic) left = "";

                            }
                            else if (field != null && field.IsStatic) left = "";

                            var evt = memberInfo as System.Reflection.EventInfo;
                            bool isEvent = evt != null;
                            if (op != OperationKind.None)
                            {
                                if (isAdditionOrSubtractionAssignment)
                                {
                                    if (!isEvent) throw new ScriptLoadingException("Operators += and -= is supported only for events when using the arrow operator.");
                                }
                                else if (isEvent) throw new ScriptLoadingException("The assignment operator is not supported for events. Should be used += or -=.");
                            }

                            bool rowMember = false;
                            if (field == null && prop == null && (!isEvent || !isAssignment))
                            {
                                if (isAssignment) throw new ScriptLoadingException($"Member '{memberInfo.Name}' is not writeable.");
                                int memberLitNum;
                                object memberLit;
                                if (memberInfo is Type t) memberLit = type = t;
                                else
                                {
                                    memberLit = memberInfo;
                                    type = memberLit.GetType();
                                }
                                memberLitNum = SetLiteral<object>(memberLit);
                                inv = LiteralMark + memberLitNum.ToString() + LiteralMark;

                                rowMember = true;
                            }
                            else if (!isEvent) type = isProp ? prop.PropertyType : field.FieldType;
                            else type = typeof(System.Delegate);




                            if (!rowMember)
                            {

                                int memberLitNum = SetLiteral<object>(memberInfo);
                                bool arrayItem = ((field != null && field.FieldType.IsArray) || (prop != null && prop.PropertyType.IsArray)) && indexes.Length > 0;
                                if (arrayItem) type = type.GetElementType();
                                if (isAssignment)
                                {
                                    var vt = GetExpressionType(setVal, CurScript.CurrentBuildFn.VarTypes).Type;
                                    var valType = vt.GetAbsType();
                                    if (vt.SubTypeID != TypeID.None) valType = valType.GetType();
                                    if (valType != typeof(object) && !type.IsAssignableFrom(valType))
                                    {
                                        if (op == OperationKind.ForcedAssignment && CanCovert(valType, type))
                                        {
                                            int typeLitNum = SetLiteral<object>(type);
                                            string typeLit = LiteralMark + typeLitNum.ToString() + LiteralMark;
                                            setVal = $"{BasicFunctionPrefix}ChangeType({setVal},{typeLit})";
                                        }
                                        else throw new ScriptLoadingException($"Cannot assign value of type '{valType}'. Expected type: '{type}'.");

                                    }

                                    if (arrayItem)
                                        inv = $"{BasicFunctionPrefix}SetElement({LiteralMark + memberLitNum.ToString() + LiteralMark},{left},{setVal}{indexes})";
                                    else
                                    {
                                        if (!isEvent)
                                        {
                                            if (!isProp && field.IsInitOnly) throw new ScriptLoadingException($"Field '{field.Name}' is read only.");
                                            inv = $"{BasicFunctionPrefix}SetValue({LiteralMark + memberLitNum.ToString() + LiteralMark},{left},{setVal}{indexes})";
                                        }
                                        else
                                        {
                                            string fn = op == OperationKind.AdditionAssignment ? "AddEventHandler" : "RemoveEventHandler";

                                            inv = $"{BasicFunctionPrefix}{fn}({LiteralMark + memberLitNum.ToString() + LiteralMark},{left},{setVal})";

                                        }
                                    }
                                }
                                else
                                {

                                    bool typed = false;
                                    string typeArg = "";
                                    if (lastArrow && (!type.IsArray || !arrayItem))
                                    {

                                        string returnType = type != typeof(object) ? GetReflType(type) : "";
                                        if (typed = returnType.Length > 0)
                                        {
                                            typeArg = "," + returnType;

                                        }

                                    }


                                    if (arrayItem)
                                        inv = $"{BasicFunctionPrefix}{(typed ? "T" : "")}GetElement({LiteralMark + memberLitNum.ToString() + LiteralMark}{typeArg},{left}{indexes})";
                                    else
                                        inv = $"{BasicFunctionPrefix}{(typed ? "T" : "")}GetValue({LiteralMark + memberLitNum.ToString() + LiteralMark}{typeArg}{(left.Length > 0 || indexes.Length > 0 ? "," + left : "")}{indexes})";


                                }
                            }


                        }


                    }
                }


                int start3 = start + start2 + 2;
                int end3 = end - (m.Length - end2);
                lastArrow = end3 == s.Length;
                s = s.Substring(0, start3) + inv + s.Substring(end3);
                arrowPos = s.LastIndexOf(ArrowOperator);
            }

            if (arrowCount > 1) type = null;

            if (!top && (type == null || !lastArrow))
            {

                if (IsLiteral(s))
                {
                    object litObj = GetLitByStr(s);
                    if (litObj is Type t) type = t;
                    else type = litObj.GetType();
                }
                else
                {
                    var t = GetExpressionType(s, CurScript.CurrentBuildFn.VarTypes).Type;
                    type = t.GetAbsType();
                }
            }


            return type;
        }



        static char[] MemberBrackets = new char[] { '(', '[' };



        private void ExtractTypeLiterals(ref string code)
        {
            int i2, i = code.IndexOf("typeof(");
            char c;
            string s;
            object type;
            while (i >= 0)
            {
                try
                {
                    if (i > 0) c = code[i - 1]; else c = default(char);


                    if (c != '_' && !Char.IsLetterOrDigit(c))
                    {
                        i2 = code.IndexOf(')', i);
                        s = code.Substring(i + 7, i2 - (i + 7));
                        if (s.StartsWith(LiteralMark) || s.IndexOf(',')>=0)
                        {
                            string[] args = s.Split(',');
                            for (int n = 0; n < args.Length; n++)
                            {
                                if (!args[n].StartsWith(LiteralMark)) throw new ScriptLoadingException("Arguments of 'typeof' must be string literals.");
                                int litIndex = int.Parse(args[n].Trim(LiteralMark).Substring(1));
                                args[n] = GetLiteral<string>(litIndex);
                            }

                            if (args.Length > 1)
                            {
                                string path = ExpandVars(args[0], CurScript);
                                if (!System.IO.Path.IsPathFullyQualified(path)) path = System.IO.Path.GetFullPath(path, CurScript.ScriptDir);
                                var asm = System.Reflection.Assembly.LoadFrom(path);
                                type = asm.GetType(args[1], true);
                            }
                            else
                                type = Type.GetType(args[0], true);

                        }
                        else
                        {
                            if (!IsBasic(s))
                            {

                                bool isArr = false;
                                if (s.EndsWith(']'))
                                {
                                    s = s.Substring(0, s.Length - 2);
                                    isArr = true;
                                }
                                type = CustomType.Get(GetClassByNameOrException(s, this, true), isArr);
                            }
                            else
                                type = GetTypeByID(CurScript.AllTypes[s].ID);

                        }
                        int litNum = SetLiteral<object>(type);
                        code = code.Substring(0, i) + LiteralMark + litNum.ToString() + LiteralMark + code.Substring(i2 + 1);
                    }
                    i = code.IndexOf("typeof(", i + 1);
                }
                catch (Exception ex)
                {
                    var lm = GetLocationMark(CurScript, code.Substring(0, i)).Simplify();
                    throw new ScriptLoadingException(ErrMsgWithLoc(ex.Message, lm));
                }
            }
        }
        public static string ExpandVars(string path, Script script)
        {
            path = path.Replace("%APPDIR%", script.ScriptDir);
            path = path.Replace("%MODDIR%", Program.ModulesDir);
            path = path.Replace("%OVSDIR%", Program.OverScriptDir);
            path = path.Replace("%CURDIR%", Environment.CurrentDirectory);
            path = Environment.ExpandEnvironmentVariables(path);
            return path;
        }

        private static void ExtractCharLiterals(ref string code, Script script)
        {
            int litNum;
            int i2, i = code.IndexOf('\'');

            string str, orig;

            while (i >= 0)
            {
                try
                {
                    i2 = code.IndexOf('\'', i + 1);
                    if (i2 < 0) break;
                    if (i2 == i + 2 && code[i2 - 1] == '\'')
                    {
                        i2 = code.IndexOf('\'', i2 + 1);
                        if (i2 < 0) break;
                    }
                    orig = str = code.Substring(i + 1, i2 - (i + 1));
                    str = System.Text.RegularExpressions.Regex.Unescape(str);

                    if (str.Length > 1) throw new FormatException($"Char literal '{(orig.Length <= 10 ? orig : orig.Substring(0, 10) + "...")}' length is more than 1.");
                    else if (str.Length == 0) throw new FormatException("Char literal length is 0.");

                    litNum = SetLiteral<char>(str[0]);
                    code = code.Substring(0, i) + LiteralMark + litNum.ToString() + LiteralMark + code.Substring(i2 + 1);
                }
                catch (Exception ex)
                {
                    var lm = GetLocationMark(script, code.Substring(0, i)).Simplify();
                    throw new ScriptLoadingException(ErrMsgWithLoc(ex.Message, lm));
                }
                i = code.IndexOf('\'', i + 1);
            }
        }
        private static void ExtractStrLiterals(ref string code, Script script)
        {
            int litNum;
            int i2, i = code.IndexOf('\"');
            bool esc;
            string str;
            while (i >= 0)
            {
                try
                {
                    if (i < 1 || (code[i - 1] != '\'' && (i < 2 || code.Substring(i - 2, 2) != "'\\")))
                    {
                        esc = i == 0 || code[i - 1] != '@';
                        i2 = FindEndOfLiteral(code, i, esc);
                        if (i2 < 0)
                            throw new Exception($"Couldn't find end of string literal '{TrimToLineEnd(code.Substring(i + 1))}'.");

                        str = code.Substring(i + 1, i2 - (i + 1));
                        if (esc)
                        {
                            str = str.Replace("\n", "").Replace("\r", "");
                            str = System.Text.RegularExpressions.Regex.Unescape(str);
                        }
                        else
                            str = str.Replace("\"\"", "\"");
                        str = RemLocationMarks(str);
                        litNum = SetLiteral<string>(str);

                        if (!esc) i--;
                        code = code.Substring(0, i) + LiteralMark + litNum.ToString() + LiteralMark + code.Substring(i2 + 1);
                    }
                }
                catch (Exception ex)
                {
                    var lm = GetLocationMark(script, code.Substring(0, i)).Simplify();
                    throw new ScriptLoadingException(ErrMsgWithLoc(ex.Message, lm));
                }
                i = code.EIndexOf('\"', i + 1);
            }

            string TrimToLineEnd(string str)
            {
                int i = str.IndexOf('\n');
                if (i >= 0) str = str.Substring(0, i).TrimEnd('\r');
                int maxLen = 20;
                if (str.Length > maxLen) str = str.Substring(0, maxLen);
                return str + "...";
            }
        }

        private static bool IsEscaped(string str, int pos, bool esc = true)
        {
            char e = esc ? '\\' : '\"';
            int c = 0;
            while (pos > 0 && str[--pos] == e) c++;

            if (!esc && str[pos] == '@') c--;

            return c % 2 != 0;
        }

        public static int FindEndOfLiteral(string code, int pos, bool esc, char ec = '"')
        {

            do
            {
                pos = code.EIndexOf(ec, pos + 1);
                if (pos < 0) break;
            } while ((esc && IsEscaped(code, pos)) || (!esc && !((pos + 1 >= code.Length || code[pos + 1] != ec) && !IsEscaped(code, pos, false))));
            return pos;
        }
        private static (int i, int i2) FindNextBlock(string code, int i, int i2, bool esc)
        {
            if (i2 == code.Length)
                return (i2, i2);
            int i22 = FindEndOfLiteral(code, i2, esc);
            if (i22 < 0) i22 = code.Length;
            i = i22;

            i2 = FindStartOfLiteral(code, i22);
            if (i2 < 0)
                i2 = code.Length;
            return (i, i2);
        }
        private static int FindStartOfLiteral(string code, int i)
        {
            do
            {


                i = code.EIndexOf('"', i + 1);
            } while (!(i < 1 || (code[i - 1] != '\'' && (i < 2 || code.Substring(i - 2, 2) != "'\\"))));

            if (i < 0) i = code.Length;
            return i;
        }
        public static void RemoveComments(ref string code)
        {
            int i, i2, i3, i33, i4;

            string[] t = { "/*", "//" };
            string[] t2 = { "*/", "\n" };
            int n, l, ii;
            (int i, int i2) nb;
            bool esc = true;
            i = -1;



            ii = i + 1;

            i2 = FindStartOfLiteral(code, i);

            while (i2 >= 0)
            {
                esc = i2 == 0 || code[i2 - 1] != '@';

                do
                {
                    i3 = code.EIndexOf(t[0], ii);
                    i33 = code.EIndexOf(t[1], ii);
                    n = 0; l = 2;
                    if (i3 < 0 || (i33 < i3 && i33 >= 0)) { i3 = i33; n = 1; l = 0; }

                    if (i3 >= 0 && i3 < i2)
                    {
                        i4 = code.IndexOf(t2[n], i3 + 2, StringComparison.Ordinal);
                        bool end = false;
                        if (i4 < 0) { i4 = code.Length - l; end = true; }

                        code = code.Remove(i3, (i4 - i3) + l);

                        if (end) return;

                        i2 = FindStartOfLiteral(code, i);

                    }
                    else
                        break;
                } while (true);

                nb = FindNextBlock(code, i, i2, esc);

                i = nb.i;
                i2 = nb.i2;

                if (i == i2) break;
                ii = i + 1;
            }

        }

        private static bool IsNegativeNumber(string code, int pos, out int minusPos)
        {
            minusPos = -1;
            bool minus = false;
            do
            {
                if (!Char.IsWhiteSpace(code[pos]))
                {
                    if (!minus)
                    {
                        minus = code[pos] == '-';
                        if (!minus) return false;
                        minusPos = pos;
                    }
                    else
                    {
                        return !Char.IsLetterOrDigit(code[pos]) && code[pos] != '_' && code[pos] != ')' && code[pos] != ']' && code[pos] != LiteralMark;
                    }
                }
                pos--;
            } while (pos >= 0);
            return minus;
        }
        private static void ExtractNumLiterals(ref string code, Script script)
        {
            int litNum;
            int litStart, litEnd;
            string lit;

            char c, t;
            int sufLen = 0;

            litStart = litEnd = -1;
            bool isLit = false;
            for (int i = code.Length - 1; i >= 0; i--)
            {
                c = code[i];
                if (c == LiteralMark) isLit = !isLit;
                if (isLit) continue;

                if (char.IsNumber(c))
                {
                    if (litEnd < 0) litEnd = i;
                }
                else
                {

                    if (litEnd >= 0 && (c != '.' || code[i - 1] == '.'))
                    {

                        if (!char.IsLetter(c) && c != '_')
                        {
                            int minusPos;
                            if (IsNegativeNumber(code, i, out minusPos))
                                litStart = minusPos;
                            else
                            { litStart = i + 1; minusPos = -1; }

                            lit = code.Substring(litStart, litEnd + 1 - litStart);
                            if (minusPos >= 0 && minusPos < i) { lit = lit.Remove(1, i - minusPos); i = minusPos; }
                            string origLit = lit;

                            if (litEnd + 1 < code.Length) t = code.ToLower()[litEnd + 1]; else t = ' ';

                            litNum = 0;
                            try
                            {
                                if (char.IsLetter(t) && litEnd + 2 < code.Length)
                                {
                                    char nc = code[litEnd + 2];
                                    if (char.IsLetterOrDigit(nc) || nc == '_')
                                        throw new FormatException($"Unrecognized number format '{code.Substring(litStart, litEnd + 3 - litStart)}'.");

                                }

                                switch (t)
                                {
                                    case 'f':
                                        sufLen = 1;
                                        litNum = SetLiteral<float>(float.Parse(lit, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture));
                                        break;
                                    case 'd':
                                        sufLen = 1;
                                        litNum = SetLiteral<double>(double.Parse(lit, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture));
                                        break;
                                    case 'l':
                                        sufLen = 1;
                                        litNum = SetLiteral<long>(long.Parse(lit));
                                        break;
                                    case 'm':
                                        sufLen = 1;
                                        litNum = SetLiteral<decimal>(decimal.Parse(lit, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture));
                                        break;
                                    default:
                                        if (char.IsLetter(t))
                                            throw new FormatException($"Unrecognized data type suffix '{t}'.");

                                        sufLen = 0;
                                        litNum = lit.IndexOf('.') >= 0 ? SetLiteral<double>(double.Parse(lit, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture)) : SetLiteral<int>(int.Parse(lit));



                                        break;
                                }
                            }
                            catch (Exception ex)
                            {
                                var lm = GetLocationMark(script, code.Substring(0, litStart)).Simplify();
                                throw new ScriptLoadingException(ErrMsgWithLoc($"Invalid {GetSufType(t)} literal '{origLit}'. " + ex.Message, lm));
                            }
                            code = code.Substring(0, litStart) + LiteralMark + litNum.ToString() + LiteralMark + code.Substring(litEnd + 1 + sufLen);
                        }
                        litStart = litEnd = -1;
                    }
                }

            }

            string GetSufType(char c)
            {
                if (c == 'f') return "float";
                else if (c == 'd') return "double";
                else if (c == 'l') return "long";
                else if (c == 'm') return "decimal";
                else if (char.IsLetter(c)) return "numeric";
                else return "int";
            }
        }

        static char[] DeclStartChars = { CodeLocationChar, ';', '}', '{' };
        private static bool FindDeclStart(string code, int pos, out int start)
        {

            if (pos > 0 && code[pos] == ']' && code[pos - 1] == '[') pos -= 2;

            start = -1;
            for (int i = pos; i >= 0; i--)
            {
                char c = code[i];
                if (!char.IsLetterOrDigit(c) && c != ' ' && c != '.' && c != '_' && c != TypeHintChar)
                {
                    start = i;
                    return DeclStartChars.Contains(c) ? true : false;
                }
            }
            return false;
        }
        private static void ItemizeMultiDeclaring(ref string code)
        {

            string[] notType = { ClassSign, "new", "ref", "goto", "return", "throw", "apply", "apply$", "case" };
            string cs = " " + ClassSign;

            int i2, i3, i4;
            int i = code.LastIndexOf(' ');
            string varType, s;
            string[] parts;
            while (i >= 0)
            {
                if (!InsideBlock(code, i, "(", ")"))
                {
                    i3 = code.IndexOf(';', i);
                    i4 = -1;
                    if (i3 >= 0)
                        i4 = code.IndexOf("){", i);
                    if (i3 < 0 || (i4 >= 0 && i4 < i3))
                    {
                        i = code.ELastIndexOf(';', i - 1);
                        if (i < 0) break;
                        i = code.ELastIndexOf(' ', i - 1);
                        continue;
                    }




                    if (FindDeclStart(code, i - 1, out i2))
                    {
                        varType = code.Substring(i2 + 1, i - (i2 + 1));
                        if (!notType.Contains(varType) && !varType.EndsWith(cs))
                        {

                            s = code.Substring(i + 1, i3 - (i + 1));
                            if (s.IndexOf(',') >= 0)
                            {
                                parts = SmartSplit(s);
                                if (parts.Length > 1)
                                {
                                    for (int p = 1; p < parts.Length; p++) parts[p] = $"{varType} {parts[p]}";
                                    s = string.Join(';', parts);
                                    code = code.Substring(0, i + 1) + s + code.Substring(i3);
                                }
                            }
                        }
                    }
                    i = code.ELastIndexOf(' ', i2 - 1);
                }
                else { i = code.ELastIndexOf(' ', i - 1); }

            }

        }
        static char ProtectedOpenBrace = (char)5;
        static char ProtectedCloseBrace = (char)6;
        private static void ProtectInitializers(ref string code)
        {
            int i3, i2 = 0;
            int i = code.IndexOf('{');
            while (i >= 0)
            {
                i3 = RSkipLocationMarks(code, i - 1);
                bool sp = code[i3] == ' ' && (code[i3 - 1] == ']' || code[i3 - 1] == '=');
                if (code[i3] == ']' || code[i3] == '=' || sp)
                {
                    i2 = FindClosing(code, i + 1, "{", "}", i);
                    if (i2 < 0) break;

                    string s = code.Substring(i, i2 + 1 - i).Replace('{', ProtectedOpenBrace).Replace('}', ProtectedCloseBrace);
                    code = code.Substring(0, i) + s + code.Substring(i2 + 1);
                    if (sp) code = code.Remove(i3, 1);
                }
                else { i2 = i + 1; }
                i = code.EIndexOf('{', i2);
            }
        }

        private static void AddBraces(ref string code, Script script)
        {
            code = " " + code;

            int u, u3, u2 = 0;


            StatementKind st;
            u = FindLastStatementToAddBraces(code, code.Length - 1, out st);
            while (u >= 0)
            {
                try
                {
                    bool add = false;
                    if (st == StatementKind.Else)
                    {

                        u2 = u + 4;

                        code = code.Remove(u2, 1);

                        u3 = SkipLocationMarks(code, u2);
                        if (code[u3] != '{')
                        {
                            code = code.Insert(u2, "{");


                            if (CheckBlockStatementStarts(code, u3 + 1))
                                FindStatementEnd(code, u, ref u2, st);
                            else
                                u2 = code.EIndexOf(';', u3);



                            add = true;
                        }

                    }
                    else if (st == StatementKind.Do)
                    {
                        u2 = u;
                        u3 = SkipLocationMarks(code, u2 + 3);
                        if (code[u3] != '{')
                        {
                            code = code.Remove(u2 + 2, 1).Insert(u2 + 2, "{");

                            StatementKind bs;
                            if (CheckBlockStatementStarts(code, u3, out bs))
                            {

                                FindStatementEnd(code, u, ref u2, st);
                                if (bs == StatementKind.Do) u2 = code.EIndexOf(';', u2 + 1);
                            }
                            else
                                u2 = code.EIndexOf(';', u3);



                            add = true;
                        }
                    }
                    else
                    {
                        u2 = FindClosing(code, u + 1, "(", ")");

                        if (u2 < 0) break;

                        u3 = SkipLocationMarks(code, u2 + 1);

                        if (code[u3] != '{' && code[u3] != ';')
                        {
                            code = code.Insert(u2 + 1, "{");

                            if (CheckBlockStatementStarts(code, u3 + 1))
                                FindStatementEnd(code, u, ref u2, st);
                            else
                                u2 = code.EIndexOf(';', u3);

                            add = true;
                        }

                    }
                    if (add)
                    {
                        if (u2 < 0) throw new ScriptLoadingException("Could not find the ending.");

                        if (st != StatementKind.While || code[u3] != ';')
                            code = code.Insert(u2 + 1, "}");
                    }
                }
                catch (Exception ex)
                {
                    throw new ScriptLoadingException(ErrMsgWithLoc("Invalid statement. " + ex.Message, GetLocationMark(script, code.Substring(0, u)).Simplify()));
                }

                u = FindLastStatementToAddBraces(code, u - 1, out st);
            }



        }



        static int FindNextBreakBrace(string code, int u, StatementKind st)
        {

            int i = code.IndexOf('{', u);
            string s = code.Substring(i + 1);
            int p = SmartCharPos(s, '}');
            if (p < 0) return p;

            string s2;
            bool ok;
            do
            {
                ok = true;
                s2 = s.Substring(p + 1);
                if (s2.StartsWith("catch{") || s2.StartsWith("catch(") || s2.StartsWith("finally{")) ok = false;
                else if (st != StatementKind.Do && IsEndDo(s2)) ok = false;
                else if (s2.StartsWith("elseif(") || s2.StartsWith("else{"))
                {
                    int iec = IfElseComplete(s, 0, p);
                    ok = iec == 0;

                }
                if (!ok)
                {
                    p = SmartCharPos(s, '}', p + 1);
                    if (p < 0) return p;
                }
            } while (!ok);


            if (st == StatementKind.Do)
            {
                if (!s2.StartsWith("while("))
                    throw new ScriptLoadingException("The 'do' statement must end with 'while'.");

            }


            return p + i + 1;
        }
        static bool IsEndDo(string s)
        {
            if (!s.StartsWith("while(")) return false;
            int j = FindClosing(s, 5, "(", ")");
            j = SkipLocationMarks(s, j + 1);
            return s[j] == ';';

        }
        static int FindNextBreakSemicolon(string code, int u)
        {
            int i = code.IndexOf('{', u);
            string s = code.Substring(i + 1);
            int p = SmartCharPos(s, ';');
            if (p < 0) return p;
            return p + i + 1;
        }
        static void FindStatementEnd(string code, int u, ref int u2, StatementKind st)
        {

            int j = FindClosing(code, u2, "{", "}", u2);
            if (j < 0) j = code.Length;
            u2 = code.LastIndexOf('}', j - 1);
            int j2 = FindNextBreakSemicolon(code, u);
            if (j2 >= 0 && j2 < u2)
                u2 = j2;

            if (u2 >= 0)
            {
                j = FindNextBreakBrace(code, u, st);

                if (j >= 0 && (j <= u2))
                {
                    u2 = j;
                    return;
                }
                else if (u2 == j2)
                    return;

                string s = code.Substring(u2 + 1);
                if (IsEndDo(s))
                    u2 = code.IndexOf(';', u2);




            }

        }

        enum StatementKind : byte
        {
            None = 255, If = 0, ElseIf = 1, For = 2, While = 3, Else = 4, Do = 5, ForEach = 6, Using = 7, Lock = 8, Try = 9, Switch = 10
        }
        static string[] AddBracesStatements = { "if(", "elseif(", "for(", "while(", "else ", "do ", "foreach(", "using(", "lock(" };
        static string[] BlockStatements = { "if(", "elseif(", "for(", "while(", "else{", "do{", "foreach(", "using(", "lock(", "try{", "switch(" };
        static char[] StatementStartChars = { '(', ')', ';', ' ', '{', '}' };
        static int FindLastStatementToAddBraces(string code, int start, out StatementKind st)
        {
            int pos = -1;
            for (int n = 0; n < AddBracesStatements.Length; n++)
            {
                int i = code.LastIndexOf(AddBracesStatements[n], start);
                if (i >= 0 && (i > pos || pos < 0))
                {
                    char c = i == 0 ? default(char) : code[i - 1];
                    if (i == 0 || c == CodeLocationChar || StatementStartChars.Contains(c))
                        pos = i;

                }
            }
            st = pos >= 0 ? GetStatementKind(code, pos) : StatementKind.None;
            return pos;
        }
        static int IfElseComplete(string code, int start, int stop)
        {
            int ifCount = 0, elseCount = 0;
            bool ifStatement = true;
            string st = "if(";
            for (; ; )
            {
                int i = code.IndexOf(st, start);
                while (i >= 0 && i < stop)
                {
                    char c = i == 0 ? default(char) : code[i - 1];
                    if (i == 0 || c == CodeLocationChar || StatementStartChars.Contains(c))
                        if (ifStatement) ifCount++; else elseCount++;

                    i = code.IndexOf(st, i + 1);
                }

                if (!ifStatement) break;
                ifStatement = false;
                st = "else{";
            }
            return elseCount - ifCount;
        }
        static StatementKind GetStatementKind(string code, int start)
        {
            int cl = code.Length;
            for (int n = 0; n < AddBracesStatements.Length; n++)
            {
                string s = AddBracesStatements[n];
                if (code.IndexOf(s, start) == start) return (StatementKind)n;
            }

            throw new ScriptLoadingException("Invalid statement.");
        }
        static bool CheckBlockStatementStarts(string code, int start)
        {
            StatementKind bs;
            return CheckBlockStatementStarts(code, start, out bs);
        }
        static bool CheckBlockStatementStarts(string code, int start, out StatementKind bs)
        {

            for (int n = 0; n < BlockStatements.Length; n++)
            {
                string s = BlockStatements[n];
                if (code.IndexOf(s, start) == start) { bs = (StatementKind)n; return true; }
            }
            bs = StatementKind.None;
            return false;

        }

        static int SkipLocationMarks(string code, int pos)
        {
            while (pos < code.Length && code[pos] == CodeLocationChar)
            {
                pos = code.IndexOf(CodeLocationChar, pos + 1);
                pos = code.IndexOf(CodeLocationChar, pos + 1) + 1;

            }
            return pos;
        }
        static int RSkipLocationMarks(string code, int pos)
        {
            while (pos >= 0 && code[pos] == CodeLocationChar)
            {
                pos = code.LastIndexOf(CodeLocationChar, pos - 1);
                pos = code.LastIndexOf(CodeLocationChar, pos - 1) - 1;

            }
            return pos;
        }
        static void CheckBraces(string code, int step, Script script)
        {
            char open;
            char closing;
            if (step == 1) { open = '('; closing = ')'; }
            else { open = '{'; closing = '}'; }

            string openStr = open.ToString();
            string closingStr = closing.ToString();

            string between;
            int bi;
            int i2 = -1;
            int i = code.IndexOf(open);
            while (i >= 0)
            {
                between = code.Substring(i2 + 1, i - (i2 + 1));
                bi = between.IndexOf(closing);
                if (bi >= 0) BracesErrMsg($"Closing '{closingStr}' is excess.", code, (i2 + 1) + bi, script);
                i2 = FindClosing(code, i, openStr, closingStr);
                if (i2 < 0) BracesErrMsg($"Closing '{closingStr}' not found.", code, i, script);

                string block = code.Substring(i + 1, i2 - (i + 1));
                if (!block.TrimStart().StartsWith(CodeLocationChar))
                {
                    var lm = GetLocationMark(script, code.Substring(0, i));
                    if (lm != null)
                        block = ToLocationMarkStr(lm.CFile.Num, lm.Line2) + block;
                }

                if (block.Length > 0) CheckBraces(block, step, script);

                i = code.IndexOf(open, i2);
            }
            between = code.Substring(i2 + 1);
            bi = between.IndexOf(closing);
            if (bi >= 0) BracesErrMsg($"Closing '{closingStr}' is excess.", code, (i2 + 1) + bi, script);

        }

        static string BracesErrMsg(string msg, string code, int pos, Script script)
        {
            int start, stop, cur;

            int p = code.LastIndexOf(CodeLocationChar, pos);
            if (p >= 0) { p = RSkipLocationMarks(code, p); p++; cur = pos - p; start = p; } else { start = 0; cur = pos; }

            p = code.IndexOf(CodeLocationChar, pos);
            if (p >= 0) stop = p - 1; else stop = code.Length;

            string s = code.Substring(start, stop - start);
            s = s.Insert(cur + 1, "<<<\t");
            s = s.Insert(cur, "\t>>>");

            var lm = GetLocationMark(script, s.Substring(0, cur));
            s = RemLocationMarks(s);
            s = s.Trim();




            throw new ScriptLoadingException(ErrMsgWithLoc(msg, lm));

        }
        static Dictionary<OperationKind, string> OperatorStr = new Dictionary<OperationKind, string>() {

            { OperationKind.Equality,"=="},
            { OperationKind.Inequality,"!="},

            { OperationKind.SubtractionAssignment,"-="},
            { OperationKind.AdditionAssignment,"+="},
            { OperationKind.MultiplyAssignment,"*="},
            { OperationKind.DivisionAssignment,"/="},
            { OperationKind.ModulusAssignment,"%="},
            { OperationKind.BitwiseOrAssignment,"|="},
            { OperationKind.ExclusiveOrAssignment,"^="},
            { OperationKind.BitwiseAndAssignment,"&="},

            { OperationKind.ForcedAssignment , ":=" },
            { OperationKind.SubtractionForcedAssignment,"-:="},
            { OperationKind.AdditionForcedAssignment,"+:="},
            { OperationKind.MultiplyForcedAssignment,"*:="},
            { OperationKind.DivisionForcedAssignment,"/:="},
            { OperationKind.ModulusForcedAssignment,"%:="},
            { OperationKind.BitwiseOrForcedAssignment,"|:="},
            { OperationKind.ExclusiveOrForcedAssignment,"^:="},
            { OperationKind.BitwiseAndForcedAssignment,"&:="},

            { OperationKind.Decrement,"--"},
            { OperationKind.Increment,"++"},
            { OperationKind.LessThanOrEqual,"<="},
            { OperationKind.GreaterThanOrEqual,">="},
            { OperationKind.LogicalOr,"||"},
            { OperationKind.LogicalAnd,"&&"},
            { OperationKind.LeftShift,"<<"},
            { OperationKind.RightShift,">>"},
            { OperationKind.LessThan,"<"},
            { OperationKind.GreaterThan,">"},
            { OperationKind.BitwiseOr,"|"},
            { OperationKind.ExclusiveOr,"^"},
            { OperationKind.BitwiseAnd,"&"},
            { OperationKind.OnesComplement,"~"},
            { OperationKind.Modulus,"%"},
            { OperationKind.Subtraction,"-"},
            { OperationKind.Addition,"+"},
            { OperationKind.Division,"/"},
            { OperationKind.Multiply,"*"},
            { OperationKind.Assignment,"="},
            { OperationKind.LogicalNot,"!"},
            { OperationKind.Casting,"\\"},
            { OperationKind.Coalescing,"??"},

        };

        const string OperatorChar = "§";
        static Dictionary<OperationKind, string> OperatorServiceStr = OperatorStr.ToDictionary(x => x.Key, x => OperatorChar + ((byte)x.Key).ToString("D2") + OperatorChar);
        private static void ReplaceOperators(ref string code)
        {
            code = code.Replace(ArrowOperatorRaw, ArrowOperator, StringComparison.Ordinal);
            code = code.Replace(OperatorStr[OperationKind.Coalescing], OperatorServiceStr[OperationKind.Coalescing], StringComparison.Ordinal);

            code = code.Replace(OperatorStr[OperationKind.SubtractionForcedAssignment], OperatorServiceStr[OperationKind.SubtractionForcedAssignment], StringComparison.Ordinal);
            code = code.Replace(OperatorStr[OperationKind.AdditionForcedAssignment], OperatorServiceStr[OperationKind.AdditionForcedAssignment], StringComparison.Ordinal);
            code = code.Replace(OperatorStr[OperationKind.MultiplyForcedAssignment], OperatorServiceStr[OperationKind.MultiplyForcedAssignment], StringComparison.Ordinal);
            code = code.Replace(OperatorStr[OperationKind.DivisionForcedAssignment], OperatorServiceStr[OperationKind.DivisionForcedAssignment], StringComparison.Ordinal);
            code = code.Replace(OperatorStr[OperationKind.ModulusForcedAssignment], OperatorServiceStr[OperationKind.ModulusForcedAssignment], StringComparison.Ordinal);
            code = code.Replace(OperatorStr[OperationKind.ExclusiveOrForcedAssignment], OperatorServiceStr[OperationKind.ExclusiveOrForcedAssignment], StringComparison.Ordinal);
            code = code.Replace(OperatorStr[OperationKind.BitwiseOrForcedAssignment], OperatorServiceStr[OperationKind.BitwiseOrForcedAssignment], StringComparison.Ordinal);
            code = code.Replace(OperatorStr[OperationKind.BitwiseAndForcedAssignment], OperatorServiceStr[OperationKind.BitwiseAndForcedAssignment], StringComparison.Ordinal);

            code = code.Replace(OperatorStr[OperationKind.ForcedAssignment], OperatorServiceStr[OperationKind.ForcedAssignment], StringComparison.Ordinal);
            code = code.Replace(OperatorStr[OperationKind.Equality], OperatorServiceStr[OperationKind.Equality], StringComparison.Ordinal);
            code = code.Replace(OperatorStr[OperationKind.Inequality], OperatorServiceStr[OperationKind.Inequality], StringComparison.Ordinal);
            code = code.Replace(OperatorStr[OperationKind.SubtractionAssignment], OperatorServiceStr[OperationKind.SubtractionAssignment], StringComparison.Ordinal);
            code = code.Replace(OperatorStr[OperationKind.AdditionAssignment], OperatorServiceStr[OperationKind.AdditionAssignment], StringComparison.Ordinal);
            code = code.Replace(OperatorStr[OperationKind.MultiplyAssignment], OperatorServiceStr[OperationKind.MultiplyAssignment], StringComparison.Ordinal);
            code = code.Replace(OperatorStr[OperationKind.DivisionAssignment], OperatorServiceStr[OperationKind.DivisionAssignment], StringComparison.Ordinal);
            code = code.Replace(OperatorStr[OperationKind.ModulusAssignment], OperatorServiceStr[OperationKind.ModulusAssignment], StringComparison.Ordinal);

            code = code.Replace(OperatorStr[OperationKind.ExclusiveOrAssignment], OperatorServiceStr[OperationKind.ExclusiveOrAssignment], StringComparison.Ordinal);
            code = code.Replace(OperatorStr[OperationKind.BitwiseOrAssignment], OperatorServiceStr[OperationKind.BitwiseOrAssignment], StringComparison.Ordinal);
            code = code.Replace(OperatorStr[OperationKind.BitwiseAndAssignment], OperatorServiceStr[OperationKind.BitwiseAndAssignment], StringComparison.Ordinal);

            code = code.Replace(OperatorStr[OperationKind.Decrement], OperatorServiceStr[OperationKind.Decrement], StringComparison.Ordinal);
            code = code.Replace(OperatorStr[OperationKind.Increment], OperatorServiceStr[OperationKind.Increment], StringComparison.Ordinal);
            code = code.Replace(OperatorStr[OperationKind.LessThanOrEqual], OperatorServiceStr[OperationKind.LessThanOrEqual], StringComparison.Ordinal);
            code = code.Replace(OperatorStr[OperationKind.GreaterThanOrEqual], OperatorServiceStr[OperationKind.GreaterThanOrEqual], StringComparison.Ordinal);
            code = code.Replace(OperatorStr[OperationKind.LogicalOr], OperatorServiceStr[OperationKind.LogicalOr], StringComparison.Ordinal);
            code = code.Replace(OperatorStr[OperationKind.LogicalAnd], OperatorServiceStr[OperationKind.LogicalAnd], StringComparison.Ordinal);
            code = code.Replace(OperatorStr[OperationKind.LeftShift], OperatorServiceStr[OperationKind.LeftShift], StringComparison.Ordinal);
            code = code.Replace(OperatorStr[OperationKind.RightShift], OperatorServiceStr[OperationKind.RightShift], StringComparison.Ordinal);

            code = code.Replace(OperatorStr[OperationKind.Casting], OperatorServiceStr[OperationKind.Casting], StringComparison.Ordinal);

            code = code.Replace(OperatorStr[OperationKind.LessThan], OperatorServiceStr[OperationKind.LessThan], StringComparison.Ordinal);
            code = code.Replace(OperatorStr[OperationKind.GreaterThan], OperatorServiceStr[OperationKind.GreaterThan], StringComparison.Ordinal);

            code = code.Replace(OperatorStr[OperationKind.BitwiseOr], OperatorServiceStr[OperationKind.BitwiseOr], StringComparison.Ordinal);
            code = code.Replace(OperatorStr[OperationKind.ExclusiveOr], OperatorServiceStr[OperationKind.ExclusiveOr], StringComparison.Ordinal);
            code = code.Replace(OperatorStr[OperationKind.BitwiseAnd], OperatorServiceStr[OperationKind.BitwiseAnd], StringComparison.Ordinal);

            code = code.Replace(OperatorStr[OperationKind.OnesComplement], OperatorServiceStr[OperationKind.OnesComplement], StringComparison.Ordinal);

            code = code.Replace(OperatorStr[OperationKind.Modulus], OperatorServiceStr[OperationKind.Modulus], StringComparison.Ordinal);
            code = code.Replace(OperatorStr[OperationKind.Subtraction], OperatorServiceStr[OperationKind.Subtraction], StringComparison.Ordinal);
            code = code.Replace(OperatorStr[OperationKind.Addition], OperatorServiceStr[OperationKind.Addition], StringComparison.Ordinal);
            code = code.Replace(OperatorStr[OperationKind.Division], OperatorServiceStr[OperationKind.Division], StringComparison.Ordinal);
            code = code.Replace(OperatorStr[OperationKind.Multiply], OperatorServiceStr[OperationKind.Multiply], StringComparison.Ordinal);
            code = code.Replace(OperatorStr[OperationKind.Assignment], OperatorServiceStr[OperationKind.Assignment], StringComparison.Ordinal);
            code = code.Replace(OperatorStr[OperationKind.LogicalNot], OperatorServiceStr[OperationKind.LogicalNot], StringComparison.Ordinal);


        }

        private static string ReReplaceOperators(string code)
        {
            code = code.Replace(ArrowOperator, ArrowOperatorRaw, StringComparison.Ordinal);

            code = code.Replace(OperatorServiceStr[OperationKind.Coalescing], OperatorStr[OperationKind.Coalescing], StringComparison.Ordinal);

            code = code.Replace(OperatorServiceStr[OperationKind.Equality], OperatorStr[OperationKind.Equality], StringComparison.Ordinal);
            code = code.Replace(OperatorServiceStr[OperationKind.Inequality], OperatorStr[OperationKind.Inequality], StringComparison.Ordinal);
            code = code.Replace(OperatorServiceStr[OperationKind.GreaterThanOrEqual], OperatorStr[OperationKind.GreaterThanOrEqual], StringComparison.Ordinal);
            code = code.Replace(OperatorServiceStr[OperationKind.LessThanOrEqual], OperatorStr[OperationKind.LessThanOrEqual], StringComparison.Ordinal);
            code = code.Replace(OperatorServiceStr[OperationKind.LessThan], OperatorStr[OperationKind.LessThan], StringComparison.Ordinal);
            code = code.Replace(OperatorServiceStr[OperationKind.GreaterThan], OperatorStr[OperationKind.GreaterThan], StringComparison.Ordinal);
            code = code.Replace(OperatorServiceStr[OperationKind.LogicalAnd], OperatorStr[OperationKind.LogicalAnd], StringComparison.Ordinal);
            code = code.Replace(OperatorServiceStr[OperationKind.LogicalOr], OperatorStr[OperationKind.LogicalOr], StringComparison.Ordinal);
            code = code.Replace(OperatorServiceStr[OperationKind.LogicalNot], OperatorStr[OperationKind.LogicalNot], StringComparison.Ordinal);

            code = code.Replace(OperatorServiceStr[OperationKind.SubtractionAssignment], OperatorStr[OperationKind.SubtractionAssignment], StringComparison.Ordinal);
            code = code.Replace(OperatorServiceStr[OperationKind.AdditionAssignment], OperatorStr[OperationKind.AdditionAssignment], StringComparison.Ordinal);
            code = code.Replace(OperatorServiceStr[OperationKind.MultiplyAssignment], OperatorStr[OperationKind.MultiplyAssignment], StringComparison.Ordinal);
            code = code.Replace(OperatorServiceStr[OperationKind.DivisionAssignment], OperatorStr[OperationKind.DivisionAssignment], StringComparison.Ordinal);
            code = code.Replace(OperatorServiceStr[OperationKind.ModulusAssignment], OperatorStr[OperationKind.ModulusAssignment], StringComparison.Ordinal);
            code = code.Replace(OperatorServiceStr[OperationKind.BitwiseAndAssignment], OperatorStr[OperationKind.BitwiseAndAssignment], StringComparison.Ordinal);
            code = code.Replace(OperatorServiceStr[OperationKind.BitwiseOrAssignment], OperatorStr[OperationKind.BitwiseOrAssignment], StringComparison.Ordinal);
            code = code.Replace(OperatorServiceStr[OperationKind.ExclusiveOrAssignment], OperatorStr[OperationKind.ExclusiveOrAssignment], StringComparison.Ordinal);

            code = code.Replace(OperatorServiceStr[OperationKind.SubtractionForcedAssignment], OperatorStr[OperationKind.SubtractionForcedAssignment], StringComparison.Ordinal);
            code = code.Replace(OperatorServiceStr[OperationKind.AdditionForcedAssignment], OperatorStr[OperationKind.AdditionForcedAssignment], StringComparison.Ordinal);
            code = code.Replace(OperatorServiceStr[OperationKind.MultiplyForcedAssignment], OperatorStr[OperationKind.MultiplyForcedAssignment], StringComparison.Ordinal);
            code = code.Replace(OperatorServiceStr[OperationKind.DivisionForcedAssignment], OperatorStr[OperationKind.DivisionForcedAssignment], StringComparison.Ordinal);
            code = code.Replace(OperatorServiceStr[OperationKind.ModulusForcedAssignment], OperatorStr[OperationKind.ModulusForcedAssignment], StringComparison.Ordinal);
            code = code.Replace(OperatorServiceStr[OperationKind.BitwiseAndForcedAssignment], OperatorStr[OperationKind.BitwiseAndForcedAssignment], StringComparison.Ordinal);
            code = code.Replace(OperatorServiceStr[OperationKind.BitwiseOrForcedAssignment], OperatorStr[OperationKind.BitwiseOrForcedAssignment], StringComparison.Ordinal);
            code = code.Replace(OperatorServiceStr[OperationKind.ExclusiveOrForcedAssignment], OperatorStr[OperationKind.ExclusiveOrForcedAssignment], StringComparison.Ordinal);
            code = code.Replace(OperatorServiceStr[OperationKind.Assignment], OperatorStr[OperationKind.Assignment], StringComparison.Ordinal);
            code = code.Replace(OperatorServiceStr[OperationKind.Decrement], OperatorStr[OperationKind.Decrement], StringComparison.Ordinal);
            code = code.Replace(OperatorServiceStr[OperationKind.Increment], OperatorStr[OperationKind.Increment], StringComparison.Ordinal);
            code = code.Replace(OperatorServiceStr[OperationKind.Modulus], OperatorStr[OperationKind.Modulus], StringComparison.Ordinal);
            code = code.Replace(OperatorServiceStr[OperationKind.Subtraction], OperatorStr[OperationKind.Subtraction], StringComparison.Ordinal);
            code = code.Replace(OperatorServiceStr[OperationKind.Addition], OperatorStr[OperationKind.Addition], StringComparison.Ordinal);
            code = code.Replace(OperatorServiceStr[OperationKind.Division], OperatorStr[OperationKind.Division], StringComparison.Ordinal);
            code = code.Replace(OperatorServiceStr[OperationKind.Multiply], OperatorStr[OperationKind.Multiply], StringComparison.Ordinal);
            code = code.Replace(OperatorServiceStr[OperationKind.Casting], OperatorStr[OperationKind.Casting], StringComparison.Ordinal);

            code = code.Replace(OperatorServiceStr[OperationKind.LeftShift], OperatorStr[OperationKind.LeftShift], StringComparison.Ordinal);
            code = code.Replace(OperatorServiceStr[OperationKind.RightShift], OperatorStr[OperationKind.RightShift], StringComparison.Ordinal);
            code = code.Replace(OperatorServiceStr[OperationKind.BitwiseOr], OperatorStr[OperationKind.BitwiseOr], StringComparison.Ordinal);
            code = code.Replace(OperatorServiceStr[OperationKind.ExclusiveOr], OperatorStr[OperationKind.ExclusiveOr], StringComparison.Ordinal);
            code = code.Replace(OperatorServiceStr[OperationKind.BitwiseAnd], OperatorStr[OperationKind.BitwiseAnd], StringComparison.Ordinal);
            code = code.Replace(OperatorServiceStr[OperationKind.OnesComplement], OperatorStr[OperationKind.OnesComplement], StringComparison.Ordinal);
            code = code.Replace(OperatorServiceStr[OperationKind.ForcedAssignment], OperatorStr[OperationKind.ForcedAssignment], StringComparison.Ordinal);
            return code;
        }

        private static bool InsideBlock(string text, int pos, string char1, string char2, int startPos = 0)
        {

            int h, k1 = 0, k2 = 0;

            h = text.IndexOf(char1, startPos);
            while (h >= 0 && h < pos) { k1++; h = text.EIndexOf(char1, h + 1); }

            h = text.IndexOf(char2, startPos);
            while (h >= 0 && h <= pos) { k2++; h = text.EIndexOf(char2, h + 1); }

            return k1 > k2;
        }



        private string GetUnitStr(string str)
        {
            str = str.TrimEnd('{');
            int i = str.IndexOf('(');
            if (i < 0) throw new FormatException("Invalid statement.");
            int i2 = SmartCharPos(str, ')', i);
            if (i2 != str.Length - 1) return str.Substring(i);
            return str.Substring(i + 1, i2 - (i + 1));
        }
        private static bool InBrackets(string str, int pos)
        {
            return InsideBlock(str, pos, "(", ")") || InsideBlock(str, pos, "[", "]") || InsideBlock(str, pos, "{", "}");
        }

        private static string[] SmartSplit(string argStr, char comma = ',')
        {
            char argDelimiter = (char)7;
            int commaPos = argStr.IndexOf(comma);
            char[] chars = argStr.ToCharArray();
            while (commaPos >= 0)
            {
                if (!InBrackets(argStr, commaPos))
                {
                    chars[commaPos] = argDelimiter;
                }
                commaPos = argStr.IndexOf(comma, commaPos + 1);
            }

            return new string(chars).Split(argDelimiter);
        }
        public static string ReplaceCharInString(string source, int index, Char newSymb)
        {
            char[] chars = source.ToCharArray();
            chars[index] = newSymb;
            return new string(chars);
        }

        private ArgBlocks[] GetArgEvalUnits(string strToEval, Dictionary<string, VarType> varTypes)
        {

            List<ArgBlocks> ab = new List<ArgBlocks>();

            char openBracket, closingBracket;

            int firstBracketPos = strToEval.IndexOfAny(Brackets);
            if (firstBracketPos < 0) return null;
            while (firstBracketPos >= 0)
            {

                openBracket = strToEval[firstBracketPos];
                if (openBracket == '(') { closingBracket = ')'; }
                else if (openBracket == '[') { closingBracket = ']'; }
                else { closingBracket = '}'; }

                EvalUnit[] result = null;

                int i2 = FindClosing(strToEval, firstBracketPos, openBracket.ToString(), closingBracket.ToString());
                string argStr = strToEval.Substring(firstBracketPos + 1, i2 - firstBracketPos - 1);
                result = GetArgEU(argStr, varTypes);

                ab.Add(new ArgBlocks() { Args = result });
                firstBracketPos = strToEval.IndexOfAny(Brackets, i2);

            }

            return ab.ToArray();
        }
        EvalUnit[] GetArgEU(string argStr, Dictionary<string, VarType> varTypes)
        {
            string[] args = SmartSplit(argStr);
            EvalUnit[] result = new EvalUnit[args.Length];
            for (int j = 0; j < args.Length; j++) result[j] = GetEvalUnit(args[j], varTypes);
            if (result.Length == 1 && result[0].Kind == EvalUnitKind.Empty) result = null;
            return result;
        }
        public static ScriptClass GetClassByNameOrException(string name, ScriptClass c = null, bool last = false)
        {
            var tc = GetClassByName(name, c, last);
            if (tc == null)
                throw new ScriptLoadingException($"Type '{name}' not found at '{c.ClassFullName}'{(GetClassByName(name, c, false, true) != null ? " (the class exists but is not public)" : "")}.");
            return tc;
        }
        public static ScriptClass GetClassByName(string name, ScriptClass currentClass, bool last = false, bool ignoreModifiers = false)
        {
            if (name == currentClass.ClassName || name == currentClass.ClassFullName) return currentClass;
            string[] c = name.Split('.');
            ScriptClass result = null;
            IEnumerable<ScriptClass> sc;
            for (int i = 0; i < c.Length; i++)
            {
                sc = currentClass.SubClasses.Where(x => x.ClassName == c[i] && (i == 0 || x.IsPublic || ignoreModifiers));
                if (!last) result = sc.FirstOrDefault(); else result = sc.LastOrDefault();

                if (result == null && currentClass.OuterClass != null && i == 0) result = GetClassByName(c[i], currentClass.OuterClass);
                if (result == null) break;
                currentClass = result;
            }

            return result;
        }
        private ScriptClass GetClassByVarPath(string path, Dictionary<string, VarType> varTypes, out bool isStatic)
        {
            bool lastIsCustom;
            return GetClassByVarPath(path, varTypes, out isStatic, out lastIsCustom);
        }






        private ScriptClass GetClassByVarPath(string path, Dictionary<string, VarType> varTypes, out bool isStatic, out bool lastIsCustom)
        {
            string[] c = SmartSplit(path, '.');
            if (c.Contains("")) throw new ScriptLoadingException("Empty member.");

            string p = string.Join('.', c.Take(c.Length - 1));

            var et = GetExpressionType(p, varTypes);
            var t = et.Type;
            lastIsCustom = t.CType != null && t.ID != TypeID.CustomArray && !t.CType.IsArray;

            isStatic = lastIsCustom && t.ID == TypeID.Object;

            return lastIsCustom ? t.CType.Class : CurScript.FThis;
        }

        struct ExpressionType
        {
            public VarType Type;
            public VarType InnerType;

            public ExpressionType(VarType t, VarType it)
            {
                Type = t.ID == TypeID.None ? it : t;
                InnerType = it.ID == TypeID.None ? t : it;

            }
            public ExpressionType(VarType t)
            {
                Type = t;
                InnerType = t;

            }
        }
        struct CacheItemKey
        {
            public string StrToEval;
            public Dictionary<string, VarType> VarTypes;

            public CacheItemKey(string str, Dictionary<string, VarType> vtypes)
            {
                StrToEval = str;
                VarTypes = vtypes;

            }
        }

        private static string GetClassPath(ScriptClass c, ScriptClass current = null)
        {
            if (c != current)
            {
                if (c.OuterClass == null) return current != null ? "" : c.ClassName;
                string path = GetClassPath(c.OuterClass, current);
                return path.Length > 0 ? path + "." + c.ClassName : c.ClassName;
            }
            else return "";
        }

        static void GetFuncPrefix(ref string fnName, out bool orBasic, out bool orLocal, out bool basic)
        {
            orBasic = orLocal = basic = false;
            if (orBasic = fnName.StartsWith(OrBasicFunctionPrefix)) fnName = fnName.Remove(0, OrBasicFunctionPrefix.Length);
            else if (orLocal = fnName.StartsWith(OrLocalFunctionPrefix)) fnName = fnName.Remove(0, OrLocalFunctionPrefix.Length);
            else if (basic = fnName.StartsWith(BasicFunctionPrefix)) fnName = fnName.Remove(0, 1);
            else basic = fnName.IndexOf(BasicFunctionPrefix) >= 0;
        }

        static string FindCastingType(ref string str)
        {
            if (str.StartsWith('('))
            {
                int i = FindClosing(str, 1, "(", ")");
                int ii = i + 1;
                if (i > 0 && i < str.Length - 1 && str[ii] != OperatorChar[0] && str[ii] != '.' && str[ii] != '[')
                {
                    string type = str.Substring(1, i - 1);
                    if (IsCastingBlock(type))
                    {
                        str = str.Substring(ii);
                        return type;
                    }
                }
            }
            return null;
        }





        static bool IsCastingBlock(string str)
        {
            int start = 0, stop = str.Length;
            if (str.StartsWith('`')) start = 1;
            if (str.EndsWith("[]")) stop = str.Length - 2;

            for (int i = start; i < stop; i++)
            {
                char c = str[i];
                if (!char.IsLetterOrDigit(c) && c != '_' && c != '.') return false;
            }

            return true;
        }
        static bool HasCasting(string str)
        {
            string castingType = FindCastingType(ref str);
            return castingType != null;
        }
        Dictionary<CacheItemKey, ExpressionType> ExprTypeCache = new Dictionary<CacheItemKey, ExpressionType>();
        private ExpressionType GetExpressionType(string strToEval, Dictionary<string, VarType> varTypes, EvalUnit parent = null, OperandPosition operandPos = default, string otherPart = null)
        {
            var key = new CacheItemKey(strToEval, varTypes);
            ExpressionType result;
            if (ExprTypeCache.TryGetValue(key, out result)) return result;
            bool noCache = false;
            result = DirectGetExpressionType(strToEval, varTypes, ref noCache, parent, operandPos, otherPart);
            if (!noCache) ExprTypeCache.Add(key, result);
            return result;
        }
        private ExpressionType DirectGetExpressionType(string strToEval, Dictionary<string, VarType> varTypes, ref bool noCache, EvalUnit parent = null, OperandPosition operandPos = default, string otherPart = null)
        {
            OperationKind opKind = 0;

            RemoveExcessBrackets(ref strToEval);



            int opPos = FindOperator(strToEval, ref opKind);
            if (opPos < 0 && strToEval.StartsWith("(") && strToEval.EndsWith(")"))
            {
                int bp = strToEval.IndexOf(")(");
                if (bp >= 0)
                {
                    bp = FindClosing(strToEval, 0, "(", ")");
                    if (strToEval[bp + 1] == '(')
                    {
                        string newStrToEval = strToEval.Substring(1, bp - 1);
                        RemoveExcessBrackets(ref newStrToEval);
                        opPos = FindOperator(newStrToEval, ref opKind);


                        if (opPos >= 0 || newStrToEval.EndsWith(')') || HasCasting(newStrToEval)) strToEval = newStrToEval;
                        else if (!IsCastingBlock(newStrToEval))
                            throw new ScriptLoadingException($"Incorrect use of block '{ strToEval.Substring(bp + 1)}'. The left expression is not a function call.");

                    }
                }
            }

            VarType generalType = new VarType(TypeID.None);

            string part1 = "", part2 = "";
            VarType type1 = default(VarType), type2 = default(VarType);
            bool fnIsOp = false;

            if (opPos >= 0)
            {
                part1 = strToEval.Substring(0, opPos);
                part2 = strToEval.Substring(opPos + 4);
                bool isCasting = opKind == OperationKind.Casting;



                if (isCasting)
                {
                    bool byRef = part1.StartsWith("ref ");
                    if (byRef) part1 = part1.Remove(0, 4);
                    type1 = GetTypeFromDict(part1);
                    type1.IsByRef = byRef;
                }
                else
                    type1 = GetExpressionType(part1, varTypes, null, OperandPosition.Left, part2).Type;




                if (opKind > OperationKind.BitwiseOrForcedAssignment)
                {
                    type2 = GetExpressionType(part2, varTypes, parent, OperandPosition.Right).Type;

                    if (isCasting)
                        generalType = type1;
                    else
                    {
                        if (type1.ID == TypeID.Custom || TypeIsArray(type1.ID))
                            generalType = type1;
                        else if (type2.ID == TypeID.Custom || TypeIsArray(type2.ID))
                            generalType = type2;
                        else
                            generalType = GetMaxType(type1.ID, type2.ID) == type1.ID ? type1 : type2;

                    }

                    string type = isCasting ? generalType.ID.ToString() : "";
                    string fnPath = "";
                    string fnArgs = "";
                    if (isCasting)
                    {
                        if (part2.Length == 0) return new ExpressionType(type1);

                        fnArgs = part2;
                        if (generalType.TypeHint != null)
                        {
                            fnArgs += ',' + part1;
                            type = "ByHint";
                            if (type1.IsHintArray) type += "Array";
                        }
                    }
                    else
                        fnArgs = OpIsUnary(opKind) ? (part2.Length > 0 ? part2 : part1) : $"{part1},{part2}";

                    if ((generalType.ID == TypeID.Custom || generalType.ID == TypeID.CustomArray) || (isCasting && (type2.ID == TypeID.Custom || type2.ID == TypeID.CustomArray)))

                    {
                        fnPath = GetClassPath((type1.CType != null ? type1 : type2).CType.Class, CurScript.FThis);

                        if (isCasting && type1.CType != null)
                        {
                            if (fnPath.Length > 0) fnPath += "." + OrBasicFunctionPrefix;
                            type = type1.CType.IsArray ? "Array" : "";

                        }
                        else
                        {
                            if (fnPath.Length > 0) fnPath += "." + OrLocalFunctionPrefix;
                        }
                    }
                    strToEval = fnPath + OperatorFunctionPrefix + type + opKind.ToString() + $"({fnArgs})";

                    fnIsOp = true;

                }
                else
                    return new ExpressionType(type1);
            }

            string castingType = FindCastingType(ref strToEval);
            if (castingType != null)
            {


                strToEval = castingType + OperatorServiceStr[OperationKind.Casting] + strToEval;
                type1 = GetTypeFromDict(castingType);
                return new ExpressionType(type1);
            }

            if (strToEval.Length == 0)
            {

                var vt = CurScript.AllTypes[TypeStr.Object].MakeEmpty();


                return new ExpressionType(vt);
            }
            else if (strToEval.StartsWith(LiteralMark) && strToEval.EndsWith(LiteralMark))
            {
                int literalID = int.Parse(strToEval.Trim(LiteralMark));
                return new ExpressionType(GetVarTypeByID(LiteralTypes[literalID]));
            }
            else if (strToEval == TrueStr || strToEval == FalseStr)
            {
                var vt = CurScript.AllTypes[TypeStr.Bool];
                return new ExpressionType(vt);
            }
            else if (strToEval == NullStr)
            {
                var vt = CurScript.AllTypes[TypeStr.Object].MakeNull();
                return new ExpressionType(vt);
            }






            else if (strToEval.StartsWith("{"))
            {
                if (parent == null) throw new ScriptLoadingException($"Incorrect use of the initializer because it is impossible to determine the type of the array.");
                if (!TypeIsArray(parent.Type.ID)) throw new ScriptLoadingException($"Initializers not supported for type '{parent.Type.Name}'.");
                noCache = true;
                return new ExpressionType(parent.Type);
            }
            else if (strToEval.StartsWith("new ") && SmartCharPos(strToEval, '.') <= strToEval.IndexOfAny(Brackets))
            {
                var nt = GetTypeOfNew(strToEval);
                return new ExpressionType(nt);
            }
            else if (IsBasic(strToEval))
            {
                var vt = CurScript.AllTypes[TypeStr.Object];

                vt.SubTypeID = (TypeID)Enum.Parse(typeof(TypeID), CurScript.AllTypes[strToEval].ID.ToString() + "Type");
                return new ExpressionType(vt);

            }
            else if (strToEval.EndsWith(']') && !strToEval.StartsWith(TypeHintChar))
            {

                int i = strToEval.LastIndexOf('[');
                while (i > 0 && InsideBlock(strToEval, i, "[", "]")) i = strToEval.LastIndexOf('[', i - 1);
                if (i == 0) throw new ScriptLoadingException($"Invalid array '{strToEval}'.");

                string arrName = strToEval.Substring(0, i);
                var et = GetExpressionType(arrName, varTypes).Type;
                if (et.SubTypeID == TypeID.CustomType)
                {
                    et = new VarType(et.ID, CustomType.Get(et.CType.Class, true));
                    et.SubTypeID = TypeID.CustomType;
                    return new ExpressionType(et);
                }
                else
                {
                    string indexArg = strToEval.Substring(i + 1, strToEval.Length - (i + 2));
                    var indexes = SmartSplit(indexArg);
                    if (TypeIsArray(et.ID))
                    {
                        var et2 = et;

                        if (indexes.Length == 1)
                        {

                            et = GetElementVarType(et);

                        }
                        return new ExpressionType(et, et2);
                    }
                    else if (et.ID == TypeID.String)
                        return new ExpressionType(indexes.Length == 1 ? CurScript.AllTypes[TypeStr.Char] : CurScript.AllTypes[TypeStr.String], CurScript.AllTypes[TypeStr.String]);
                    else if (et.ID == TypeID.Custom)
                    {

                        string fnName, valuePart;
                        if (operandPos == OperandPosition.Left) { fnName = "SetItem"; valuePart = "," + otherPart; }
                        else { fnName = "GetItem"; valuePart = ""; }
                        noCache = true;

                        strToEval = $"{arrName}.{fnName}({strToEval.Substring(i + 1, strToEval.Length - (i + 2))}{valuePart})";
                        var vt = GetExpressionType(strToEval, varTypes);
                        return new ExpressionType(vt.Type, et);
                    }
                    else
                        throw new ScriptLoadingException($"Cannot apply indexing with [] to an expression of type '{et.Name}'.");
                }

            }
            else if (strToEval.EndsWith(')'))
            {

                string fnName = strToEval;

                EvalUnit[] fnArgs;
                fnName = GetFuncNameAndArgEU(fnName, out fnArgs, varTypes);
                ScriptFunction f = null;
                ScriptClass c = this;
                bool isStatic = false, lastIsCustom = false;
                string path = "";
                bool local = true;
                bool isThis = false;
                int i = fnName.LastIndexOf('.');
                if (i > 0)
                {
                    path = fnName.Substring(0, i);
                    isThis = path == ThisStr;
                    local = false;

                    c = GetClassByVarPath(fnName, varTypes, out isStatic, out lastIsCustom);
                    fnName = fnName.Substring(i + 1);

                }



                bool orBasic, orLocal, basic;
                GetFuncPrefix(ref fnName, out orBasic, out orLocal, out basic);
                bool orLocalorBasic = orLocal || orBasic;










                bool refindLocal = false;

            find:
                if (refindLocal || (!local && !lastIsCustom && !isStatic && !isThis))
                {
                    bool isCast = IsCustomCastFunc(fnName);
                    bool argLack = isCast && fnArgs.Length < 2;

                    if (!orLocalorBasic || argLack)
                    {

                        var fnArgs0 = fnArgs;
                        fnArgs = new EvalUnit[fnArgs0 != null ? fnArgs0.Length + 1 : 1];
                        string p = argLack && fnName == "op_ArrayCast" ? path + "[]" : path;
                        fnArgs[0] = GetEvalUnit(p, varTypes);
                        if (fnArgs0 != null) Array.Copy(fnArgs0, 0, fnArgs, 1, fnArgs0.Length);
                    }
                    local = true;

                    if (orBasic)
                        basic = true;
                    else
                        c = this;
                }

                if (!basic)
                {

                    if (!local)
                    {
                        bool? onlyPublic = isThis ? null : true;
                        f = c.GetFunc(fnName, fnArgs, true, onlyPublic, isStatic);


                        if (f == null && (!isStatic || orLocalorBasic))
                        {

                            refindLocal = true;
                            isStatic = CurScript.CurrentBuildFn.IsStatic;
                            goto find;
                        }
                    }
                    else
                        f = FindFuncInThisOrOuters(fnName, fnArgs);

                }



                if (f == null && (orLocalorBasic || basic || (!isThis && !isStatic)) && !fnName.StartsWith(FunctionLayerPrefix))
                {
                    OverloadVariant ov;
                    if (CurScript.BFuncs.TryGetFuncType(fnName, fnArgs, out ov, CurScript.FThis))
                    {
                        if (opKind == OperationKind.Casting) return new ExpressionType(generalType, type2);

                        if (ov.ReturnTypeByArg >= 0)
                        {
                            if (ov.ReturnTypeByHint)
                            {
                                var t = fnArgs[ov.ReturnTypeByArg].Type;
                                if (ov.ReturnTypeByHintArray) t.SetType(TypeID.ObjectArray);
                                if (ov.ReturnTypeArgIsTypeObj) t.SetHintBySubType();

                                if (ov.ReturnTypeArgElemArrConv) t.TypeHint = t.TypeHint.IsArray ? t.TypeHint.GetElementType() : t.TypeHint.MakeArrayType();

                                return new ExpressionType(t, generalType);
                            }
                            else
                            {
                                CustomType ct;
                                string typeName;
                                if (ov.ReturnTypeArgIsTypeObj)
                                {
                                    ct = fnArgs[ov.ReturnTypeByArg].Type.CType;
                                    typeName = ct.FullName;

                                    if (ov.ReturnTypeArgElemArrConv)
                                        typeName = ct.IsArray ? typeName.Substring(0, typeName.Length - 2) : typeName + "[]";
                                    return new ExpressionType(CurScript.AllTypes[typeName], generalType);
                                }
                                else
                                {
                                    var t = fnArgs[ov.ReturnTypeByArg].Type;
                                    if (ov.ReturnTypeByArg2 >= 0)
                                        t = GetCommonType(t, fnArgs[ov.ReturnTypeByArg2].Type);

                                    if (ov.ReturnTypeArgElemArrConv)
                                    {
                                        ct = t.CType;
                                        typeName = ct.FullName;
                                        typeName = ct.IsArray ? typeName.Substring(0, typeName.Length - 2) : typeName + "[]";
                                        t = CurScript.AllTypes[typeName];
                                    }

                                    return new ExpressionType(t, generalType);
                                }
                            }
                        }

                        return new ExpressionType(GetVarTypeByID(ov.ReturnType), generalType);
                    }
                }

                if (f == null)
                {

                    string at;
                    if (!basic) at = $" at '{c.ClassFullName}'{ (c.FuncNames.Contains(fnName) ? " (the function exists but has inappropriate modifiers/parameters)" : "")}";
                    else at = $" among basic functions{ (CurScript.BFuncs.BasicFuncs.Keys.Contains(fnName) ? " (the function exists but has inappropriate parameters)" : "")}";

                    throw new ScriptLoadingException($"{(isStatic || CurScript.CurrentBuildFn.IsStatic ? "Static f" : "F")}unction '{FormatFuncSign(GetFuncSign(fnName, fnArgs))}'{(fnIsOp ? " for operator '" + OperatorStr[opKind] + "'" : "")} not found{at}.");
                }
                if (opKind == OperationKind.Casting && (generalType.ID != f.ReturnType.ID || (generalType.CType != null && generalType.CType != f.ReturnType.CType)))
                    throw new ScriptLoadingException($"Cast to '{generalType.Name}' function must not return value of type '{f.ReturnType.Name}'.");

                return new ExpressionType(f.ReturnType, opKind == OperationKind.Casting ? type2 : generalType);
            }
            else
            {
                VarType t;
                bool byRef = strToEval.StartsWith("ref ");
                if (byRef) strToEval = strToEval.Remove(0, 4);
                strToEval = RemModifs(strToEval);
                int spacePos = strToEval.IndexOf(' ');

                if (spacePos >= 0 && !strToEval.StartsWith("new ") && !strToEval.StartsWith('(') && !InBrackets(strToEval, spacePos))
                {
                    string typeName = strToEval.Substring(0, spacePos);
                    if (typeName != "var") t = GetTypeFromDict(typeName);
                    else t = GetExpressionType(strToEval.Substring(4).TrimStart(RedefinePrefix), varTypes).Type;

                }
                else
                {

                    if (strToEval.StartsWith(TypeHintChar))
                    {
                        strToEval = strToEval.Remove(0, 1);
                        try { t = GetTypeByHint(strToEval, CurScript.CurrentBuildFn, false); }
                        catch (Exception ex) { throw new ScriptLoadingException(ex.Message); }
                        t.IsHintType = true;
                        t.IsByRef = byRef;



                        return new ExpressionType(t);
                    }

                    int i = strToEval.LastIndexOf('.');

                    ScriptClass tc = null;
                    if (i > 0)
                    {
                        bool isStatic;
                        ScriptClass c = GetClassByVarPath(strToEval, varTypes, out isStatic);
                        string path = strToEval.Substring(0, i);
                        bool isThis = path == ThisStr;
                        string varName = strToEval.Substring(i + 1);
                        bool pub = !isThis ? c.PublicVars.Contains(varName) : true;

                        if (pub && !isStatic && c.VarTypes.ContainsKey(varName)) t = c.VarTypes[varName];
                        else if (pub && isStatic && c.StaticVarTypes.ContainsKey(varName)) t = c.StaticVarTypes[varName];

                        else if ((tc = GetClassByName(strToEval, CurScript.FThis)) != null) t = GetVarTypeOfCustomType(tc);

                        else
                        {
                            string why = WhyVarNotFound(varName, c, isStatic);
                            throw new ScriptLoadingException($"{(isStatic ? "Static v" : "V")}ariable '{varName}' not found at '{c.ClassFullName}'{(why.Length > 0 ? $" (the variable exists but {why})" : "")}.");
                        }

                    }
                    else
                    {
                        if (varTypes.ContainsKey(strToEval)) t = varTypes[strToEval];
                        else if (!CurScript.CurrentBuildFn.IsStatic && VarTypes.ContainsKey(strToEval)) t = VarTypes[strToEval];
                        else if ((t = GetStaticVarType(CurScript.FThis, strToEval)).ID != TypeID.None) { }

                        else if ((tc = GetClassByName(strToEval, CurScript.FThis)) != null) t = GetVarTypeOfCustomType(tc);

                        else throw new ScriptLoadingException($"{(CurScript.CurrentBuildFn.IsStatic ? "Static v" : "V")}ariable '{strToEval}' not found at '{this.ClassFullName}'{(CurScript.CurrentBuildFn.IsStatic && VarTypes.ContainsKey(strToEval) ? " (the variable exists but not static)" : "")}.");
                    }



                }
                t.IsByRef = byRef;
                return new ExpressionType(t);

            }
        }

        public VarType GetCommonType(VarType t, VarType t2)
        {
            var ct = t.CType;
            var ct2 = t2.CType;
            if (ct2 == null) return t;
            else if (ct == null) return t2;
            else if (ct == ct2) return t;

            if (ct.IsArray == ct2.IsArray)
            {
                if (IsLvl(ct.Class, ct2.Class) > 0) return t2;
                else if (IsLvl(ct2.Class, ct.Class) > 0) return t;
            }

            throw new ScriptLoadingException("Argument types are incompatible.");
        }





        public static VarType GetVarTypeOfCustomType(ScriptClass tc, bool isArray = false)
        {
            var t = CustomType.Get(tc, isArray);

            var vt = new VarType(TypeID.Object, t);
            vt.SubTypeID = TypeID.CustomType;

            return vt;
        }

        string GetFuncNameAndArgEU(string fnName, out EvalUnit[] args, Dictionary<string, VarType> varTypes)
        {


            var f = GetFuncNameAndArgs(fnName);

            args = GetArgEU(f.args, varTypes);
            return f.name;
        }
        static (string name, string args, int bracketPos) GetFuncNameAndArgs(string str)
        {
            int i2 = str.Length;
            bool notFirst = false;
            bool inBlock = false;
            int i = str.LastIndexOf('(');
            while (i > 0 && ((notFirst = (str[i - 1] == ')')) | (inBlock = InsideBlock(str, i, "(", ")"))))
            {
                if (notFirst && !inBlock) i2 = i;
                i = str.LastIndexOf('(', i - 1);
                inBlock = false;
            }
            if (i < 0)
                throw new ScriptLoadingException($"Invalid function '{str}'.");


            string args = str.Substring(i + 1, i2 - 2 - i);
            string name = str.Substring(0, i);
            return (name, args, i);
        }

        static string WhyVarNotFound(string varName, ScriptClass c, bool isStatic)
        {
            bool inVarTypes = c.VarTypes.ContainsKey(varName);
            bool inStaticVarTypes = c.StaticVarTypes.ContainsKey(varName);
            bool fnExists = inVarTypes || inStaticVarTypes;
            string why = "";
            if (fnExists)
                why = isStatic ? (inVarTypes ? "not static" : "not public") : (inStaticVarTypes ? "is static" : "not public");

            return why;
        }
        public VarType GetTypeOfNew(string strToEval)
        {
            string newType = strToEval.Substring(4);
            int i = newType.IndexOfAny(Brackets);

            if (i >= 0)
            {
                char c = newType[i];
                newType = newType.Substring(0, i);
                if (c == '[') newType += "[]";
            }
            return GetTypeFromDict(newType);

        }

        private static OperationKind[] ScriptOperators = {
            OperationKind.Casting,
            OperationKind.LogicalNot,
            OperationKind.Increment,
            OperationKind.Decrement,


            OperationKind.Multiply,
            OperationKind.Division,
            OperationKind.Modulus,
            OperationKind.Addition,
            OperationKind.Subtraction,

            OperationKind.OnesComplement,
            OperationKind.BitwiseOr,
            OperationKind.ExclusiveOr,
            OperationKind.BitwiseAnd,
            OperationKind.RightShift,
            OperationKind.LeftShift,

            OperationKind.LessThan,
            OperationKind.GreaterThan,
            OperationKind.LessThanOrEqual,
            OperationKind.GreaterThanOrEqual,
            OperationKind.Equality,
            OperationKind.Inequality,
            OperationKind.LogicalAnd,
            OperationKind.LogicalOr,
            OperationKind.Coalescing,

            OperationKind.AdditionAssignment,
            OperationKind.SubtractionAssignment,
            OperationKind.MultiplyAssignment,
            OperationKind.DivisionAssignment,
            OperationKind.ModulusAssignment,
            OperationKind.BitwiseOrAssignment,
            OperationKind.ExclusiveOrAssignment,
            OperationKind.BitwiseAndAssignment,

            OperationKind.AdditionForcedAssignment,
            OperationKind.SubtractionForcedAssignment,
            OperationKind.MultiplyForcedAssignment,
            OperationKind.DivisionForcedAssignment,
            OperationKind.ModulusForcedAssignment,
            OperationKind.BitwiseOrForcedAssignment,
            OperationKind.ExclusiveOrForcedAssignment,
            OperationKind.BitwiseAndForcedAssignment,

            OperationKind.Assignment,
            OperationKind.ForcedAssignment
        };

        private static string[] Operators = ScriptOperators.Select((x) => OperatorServiceStr[x]).ToArray();
        static string[] NoRevOps = {
            OperatorServiceStr[OperationKind.Assignment],
            OperatorServiceStr[OperationKind.ForcedAssignment],
             OperatorServiceStr[OperationKind.Casting],
             OperatorServiceStr[OperationKind.LogicalNot],
              OperatorServiceStr[OperationKind.OnesComplement]
        };

        static string FAssignOpServiceStr = OperatorServiceStr[OperationKind.ForcedAssignment];
   

        private static int FindOperator(string str, ref OperationKind foundOperator)
        {

            if (str.IndexOf(OperatorChar) < 0) return -1;
            string op;
            int pos;
            int c = Operators.Length - 2;
            for (int i = c; i >= 0; i--)
            {
                op = Operators[i];
                bool noRev = NoRevOps.Contains(op);

                pos = noRev ? -1 : str.Length;
                do
                {
                    if (noRev)
                        pos = str.EIndexOf(op, ++pos, StringComparison.Ordinal);
                    else
                        pos = str.ELastIndexOf(op, --pos, StringComparison.Ordinal);

                } while (pos >= 0 && InBrackets(str, pos));

                if (i == c)
                {
                    int pos2 = -1;
                    do { pos2 = str.IndexOf(FAssignOpServiceStr, ++pos2); } while (pos2 >= 0 && InBrackets(str, pos2));

                    if (pos < 0 || (pos2 >= 0 && pos2 < pos)) { pos = pos2; op = FAssignOpServiceStr; }
                }
                if (pos >= 0)
                {
                    foundOperator = (OperationKind)(byte.Parse(op.Substring(1, 2)));
                    return pos;
                }

            }

            foundOperator = 0;
            return -1;
        }

        private static int FindClosing(string code, int i, string char1, string char2, int startPos = 0)
        {

            do
            {
                i = code.EIndexOf(char2, i);
                if (i < 0 || InsideBlock(code, i, char1, char2, startPos) == false) return i; else i++;
            } while (true);

        }

        static void SetCurrentBuild(Script script, ScriptFunction fn, CodeUnit cu, ScriptClass c, bool constEval = false)
        {
            script.CurrentBuildFn = fn;
            script.FThis = fn.Class;
            script.CurrentBuildCU = cu;
            script.FnLayer = constEval ? 0 : c.FnLayers[fn];
        }
        bool ApplySyntacticSugar(ref string str)
        {
            string orig = str;

            ReplaceReflArrows(ref str, true);
            return orig != str;

        }
        private void BuildEvalUnits(ScriptFunction fn)
        {

            SetCurrentBuild(CurScript, fn, null, this);

            CodeUnit u = new CodeUnit();
            u.Code = "";
            string s;
#if EXON
            try
            {
#endif
                for (int i = 0; i < fn.Units.Length; i++)
                {
                    u = CurScript.CurrentBuildCU = fn.Units[i];
                    fn.Units[i].EU = new EvalUnit[u.Str.Length];
                    bool updCode = false;
                    for (int j = 0; j < u.Str.Length; j++)
                    {
                        if (ApplySyntacticSugar(ref u.Str[j])) updCode = true;
                        s = u.Str[j];
                        if ((int)u.Type <= 6 || u.Type == UnitType.EndDo || u.Type == UnitType.ForEach || ((u.Type == UnitType.Return || u.Type == UnitType.Throw || u.Type == UnitType.CatchByName || u.Type == UnitType.Catch || u.Type == UnitType.Apply || u.Type == UnitType.Reapply) && s.Length > 0))
                            fn.Units[i].EU[j] = GetEvalUnit(s, fn.VarTypes);

                    }
                    if (updCode)
                        u.Code = ReplaceServiceChar(string.Join(ServiceChar, u.Str), u.Type);

                    if (u.Type == UnitType.For && u.EU[1].Type.ID != TypeID.Bool) throw new ScriptLoadingException($"Invalid statement. Condition must be boolean.");
                    if ((u.Type == UnitType.If || u.Type == UnitType.While || u.Type == UnitType.EndDo) && u.EU[0].Type.ID != TypeID.Bool) throw new ScriptLoadingException($"Invalid statement. Condition must be boolean.");


                    else if (u.Type == UnitType.ForEach)
                    {
                        var type2 = u.EU[1].Type;
                        if (type2.IsNull || u.EU[1].Kind == EvalUnitKind.Empty)
                            throw new ScriptLoadingException($"Invalid foreach statement.");

                        if (!TypeIsArray(type2.ID) && type2.ID != TypeID.String && type2.ID != TypeID.Object && type2.ID != TypeID.Custom)
                            throw new ScriptLoadingException($"foreach statement cannot operate on objects of type '{type2.Name}'.");

                        var type = u.EU[0].Type;
                        bool hint = type.TypeHint != null;
                        if (type.ID != TypeID.Object || hint)
                        {
                            if (type2.ID != TypeID.Object && type2.ID != TypeID.ObjectArray)
                            {


                                if (TypeIsArray(type2.ID))
                                {

                                    var et = GetElementType(type2.ID);
                                    if (type.ID != et)
                                        throw new ScriptLoadingException($"Type mismatch in foreach statement. Var type: '{type.Name}'. Item type: '{et}'.");
                                }
                                else if (type2.ID == TypeID.String && type.ID != TypeID.Char)
                                    throw new ScriptLoadingException($"Left variable in foreach statement must be of type 'char'.");
                            }
                            else if (type2.TypeHint != null)
                            {
                                if (type2.ID == TypeID.ObjectArray)
                                {


                                    var et = type2.TypeHint.GetElementType();
                                    if (!type.T.IsAssignableFrom(et))
                                        throw new ScriptLoadingException($"Type mismatch in foreach statement. Var type: '{type.Name}'. Item type: '{et}'.");
                                }
                                else
                                {
                                    var en = GetObjEnumeratorItemType(type2.TypeHint);



                                    if (!en.isIEnumerator)
                                        throw new ScriptLoadingException($"foreach statement cannot operate on object of type '{type2.TypeHint}'. Missing 'GetEnumerator' method.");


                                    var t = hint ? type.TypeHint : type.T;
                                    if (en.itemType != null && !t.IsAssignableFrom(en.itemType))
                                        throw new ScriptLoadingException($"Type mismatch in foreach statement. Var type: '{type.Name}'. Item type: '{en.itemType}'.");
                                }
                            }
                        }

                    }
                    else if (u.Type == UnitType.Return && u.EU.Length > 0 && u.EU[0] != null && !ArgIsValid(fn.ReturnType, u.EU[0].Type))
                        throw new ScriptLoadingException($"Type of return value must be '{fn.ReturnType.Name}'.");
                    else if (u.Type == UnitType.Throw && u.EU.Length > 0 && u.EU[0] != null && u.EU[0].Type.CType != null)
                    {
                        var c = u.EU[0].Type.CType.Class;
                        VarType vt;

                        bool invalidMsgVar = false, invalidNameVar = !c.VarTypes.TryGetValue(ExceptionVarName.NameVarInCustomExClass, out vt) || vt.ID != TypeID.String;
                        if (!invalidNameVar) invalidMsgVar = !c.VarTypes.TryGetValue(ExceptionVarName.MessageVarInCustomExClass, out vt) || vt.ID != TypeID.String;
                        if (invalidNameVar || invalidMsgVar) throw new ScriptLoadingException($"For use as exception type, the class '{c.ClassFullName}' must have string variables '{ExceptionVarName.NameVarInCustomExClass}' and '{ExceptionVarName.MessageVarInCustomExClass}'.");

                    }


                }
                CurScript.CurrentBuildFn = null;
                CurScript.FnLayer = 0;

#if EXON
            }
            catch (Exception ex)
            {

                throw new ScriptLoadingException(ErrMsgWithLoc(ex.Message, u.CodeLocation ?? fn.LocationMark, RestoreCode(u.Code), fn.Class, fn));

            }
#endif

        }

        (bool isIEnumerator, Type itemType) GetObjEnumeratorItemType(Type type)
        {

            var mi = type.GetMethod("GetEnumerator");
            if (mi == null) return (false, null);
            var gt = mi.ReturnType.GetGenericArguments().FirstOrDefault();
            return (true, gt);
        }



        private static int FindLastAssignOp(string s)
        {

            int i = s.LastIndexOf(OperatorChar);
            while (i >= 0)
            {
                string opStr = s.Substring(i - 2, 2);
                var op = (OperationKind)int.Parse(opStr);
                if (OpIsAssignment(op) && !InBrackets(s, i)) return i;
                i = s.ELastIndexOf(OperatorChar, i - 4);
            }
            return -1;
        }


        static string GetReflType(Type type)
        {
            TypeID tid;
            if (TypeIds.TryGetValue(type, out tid)) return GetTypeName(tid);
            return "";
        }

        private static string InterpolateStrings(string s)
        {
            string lit, str;
            int i2,  id, i = s.IndexOf('$');
            while (i >= 0)
            {
                i++;
                if (i >= s.Length) break;

                if (s[i] == LiteralMark)
                {

                    i2 = s.IndexOf(LiteralMark, i + 1);
                    lit = s.Substring(i + 1, i2 - (i + 1));
          
                    id = Int32.Parse(lit.Substring(1));

                    str = GetLiteral<string>(id);
                    List<string> args;
                    Interpolate(ref str, out args);
                    int memberLitNum = SetLiteral<string>(str);
                    string argStr = String.Join(',', args);
                    str = $"{BasicFunctionPrefix}Format({LiteralMark + memberLitNum.ToString() + LiteralMark},{argStr})";

                    s = s.Remove(i - 1, i2 - (i - 2)).Insert(i - 1, str);

                }

                i = s.IndexOf('$', i);
            }

            return s;
        }

        static char[] InterpolateEndChars = { ',', ':', '}' };
        private static void Interpolate(ref string s, out List<string> args)
        {
            args = new List<string>();

            int i2, i = s.IndexOf('{');
            int c = 0;

            while (i >= 0)
            {
                i++;
                c++;
                if (s[i] != '{')
                {
                    if (c % 2 != 0)
                    {

                        i2 = s.IndexOfAny(InterpolateEndChars, i);
                        string v = s.Substring(i, i2 - i).Trim();
                        if (v.Length > 0)
                        {
                            args.Add(v);
                            s = s.Remove(i, i2 - i).Insert(i, (args.Count - 1).ToString());
                        }
                    }
                    c = 0;
                    i = s.IndexOf('{', i);

                }
            }
        }

        static TypeID[] SupportedLitTypes = { TypeID.Int, TypeID.Long, TypeID.Float, TypeID.Double, TypeID.Decimal, TypeID.Char, TypeID.String, TypeID.Object };

        private void GetConsts(ScriptFunction fn, Dictionary<string, VarType> varTypes, Dictionary<string, int> constants, List<string> pubVars = null, bool onlyTypes = false)
        {
            char[] lineStart = { UnitSeparator[0], CodeLocationChar };
            string code = fn.Code;
            var defChar = default(char);
            string assignOp = OperatorServiceStr[OperationKind.Assignment];
            string forcedAssignOp = OperatorServiceStr[OperationKind.ForcedAssignment];
            string cs = "const ";
            int i2, i3, i = code.IndexOf(cs);
            bool isPublic;
            Func<LocMark> getLM = () => GetLocationMark(CurScript, code.Substring(0, i)).Simplify();
            while (i >= 0)
            {
                char c = i > 0 ? code[i - 1] : defChar;
                var withModif = c == ' ';
                if (c == defChar || withModif || c == UnitSeparator[0] || c == CodeLocationChar)
                {
                    i2 = code.IndexOf(';', i + 1);
                    if (i2 < 0) break;
                    int opPos = code.IndexOf(assignOp, i);
                    if (opPos < 0 || opPos > i2)
                    {
                        opPos = code.IndexOf(forcedAssignOp, i);
                        if (opPos < 0 || opPos > i2)
                            throw new ScriptLoadingException(ErrMsgWithLoc("Constant must be initialized.", getLM()));
                    }
                    i3 = i + cs.Length;
                    string typeAndName = code.Substring(i3, opPos - i3);
                    i3 = typeAndName.IndexOf(' ');
                    if (i3 < 0)
                        throw new ScriptLoadingException(ErrMsgWithLoc("Invalid constant.", getLM()));

                    isPublic = false;
                    if (withModif)
                    {
                        int i0 = i;
                        i = code.LastIndexOfAny(lineStart, i);
                        if (i < 0) break;
                        i++;
                        isPublic = code.Substring(i, i0 - i) == "public ";
                        if (!isPublic)
                            throw new ScriptLoadingException(ErrMsgWithLoc("Invalid constant modifier.", getLM()));

                    }

                    string typeName = typeAndName.Substring(0, i3);
                    string varName = typeAndName.Substring(i3 + 1);
                    bool redefine = varName.StartsWith(RedefinePrefix);
                    if (redefine)
                        varName = varName.TrimStart(RedefinePrefix);

                    if (!CheckCharsInVarName(varName))
                        throw new ScriptLoadingException(ErrMsgWithLoc("Invalid constant name.", getLM()));

                    if (typeName.StartsWith(TypeHintChar))
                        throw new ScriptLoadingException(ErrMsgWithLoc($"Invalid constant type. Type hinting is not supported for constants.", getLM()));

                    VarType t, t2;
                    try { t2 = GetTypeFromDict(typeName, fn.Class); }
                    catch (Exception ex) { throw new ScriptLoadingException(ErrMsgWithLoc("Invalid constant type. " + ex.Message, getLM())); }



                    if (t2.ID == TypeID.Custom || t2.ID == TypeID.CustomArray)
                        throw new ScriptLoadingException(ErrMsgWithLoc($"Invalid constant type. Only built-in types allowed.", getLM()));

                    if (onlyTypes || !fn.IsStaticFunc)
                    {
                        if (varTypes.TryGetValue(varName, out t) && (!redefine || t != t2))
                        {
                            string msg;
                            if (redefine) msg = $"The constant '{varName}' is declared with different types.";
                            else msg = $"The constant '{varName}' has already been declared.";
                            throw new ScriptLoadingException(ErrMsgWithLoc(msg, getLM()));
                        }
                    }
                    varTypes[varName] = t2;

                    if (isPublic) pubVars.Add(varName);
                    if (!onlyTypes)
                    {
                        opPos += assignOp.Length;
                        string literal = code.Substring(opPos, i2 - opPos).TrimEnd(UnitSeparator[0]);


                        bool notLitType = false;
                        if (!IsLiteral(literal))
                        {
                            ApplySyntacticSugar(ref literal);

                            try { literal = EvalConstLit(t2.ID, literal, fn, varTypes, ref notLitType); }
                            catch (Exception ex) { throw new ScriptLoadingException(ErrMsgWithLoc($"Invalid constant '{varName}'. " + ex.Message, getLM())); }
                        }
                        if (!literal.StartsWith(LiteralMark)) throw new ScriptLoadingException(ErrMsgWithLoc($"Invalid constant '{varName}'.", getLM()));
                        getLit:
                        string lit = literal.Trim(LiteralMark);
                        int literalID = int.Parse(lit.Substring(1));
                        int litTypeNum = int.Parse(lit.Substring(0, 1));
                        Type litType = LitTypeID.FirstOrDefault(x => x.Value == litTypeNum).Key;
                        if (litType != t2.T && !notLitType)
                        {


                            try { literal = EvalConstLit(t2.ID, literal, fn, varTypes, ref notLitType); }
                            catch (Exception ex) { throw new ScriptLoadingException(ErrMsgWithLoc($"Invalid constant '{varName}'. " + ex.Message, getLM())); }

                            goto getLit;
                        }



                        constants[varName] = literalID;
                        if (litType == typeof(object) && !notLitType)
                        {
                            object litObj = GetLitByStr(literal);
                            if (litObj is Type lt)
                                varTypes[varName] = varTypes[varName].AddSubType(lt);

                            varTypes[varName] = varTypes[varName].AddHint(litObj.GetType());
                        }

                    }
                    if (i2 + 1 < code.Length && code[i2 + 1] == UnitSeparator[0]) i2++;
                    code = code.Remove(i, i2 + 1 - i);
                    i--;


                }
                i = code.IndexOf(cs, i + 1);
            }

            if (!onlyTypes) fn.Code = code;
        }

        static bool IsDigitsOnly(string str)
        {
            foreach (char c in str)
            {
                if (c < '0' || c > '9')
                    return false;
            }

            return true;
        }
        static bool IsLiteral(string str)
        {
            if (str.StartsWith(LiteralMark) && str.EndsWith(LiteralMark))
            {
                return IsDigitsOnly(str.Substring(1, str.Length - 2));
            }
            return false;
        }


        string EvalConstLit(TypeID typeId, string str, ScriptFunction fn, Dictionary<string, VarType> varTypes, ref bool notLitType)
        {
            notLitType = false;
            switch (typeId)
            {
                case TypeID.Int: return EvalConstLit<int>(str, fn, varTypes);
                case TypeID.Double: return EvalConstLit<double>(str, fn, varTypes);
                case TypeID.String: return EvalConstLit<string>(str, fn, varTypes);
                case TypeID.Float: return EvalConstLit<float>(str, fn, varTypes);
                case TypeID.Decimal: return EvalConstLit<decimal>(str, fn, varTypes);
                case TypeID.Long: return EvalConstLit<long>(str, fn, varTypes);
                case TypeID.Char: return EvalConstLit<char>(str, fn, varTypes);
                case TypeID.Object: return EvalConstLit<object>(str, fn, varTypes);

                default:
                    notLitType = true;

                    return EvalConstLit<object>(str, fn, varTypes, GetTypeByID(typeId));
            }
        }
        static ClassInstance ConstInst = ClassInstance.GetConstInst();
        string EvalConstLit<T>(string str, ScriptFunction fn, Dictionary<string, VarType> varTypes, Type convType = null)
        {
            try
            {

                SetCurrentBuild(CurScript, fn, null, this, true);
                ApplyTopSyntacticSugar(ref str, CurScript);
                var eu = GetEvalUnit(str, varTypes);
                eu.CU = new CodeUnit() { Fn = fn };
           
                var val = eu.Ev<T>(0, ConstInst, null);
                int litNum = convType == null ? SetLiteral<T>(val) : SetLiteral<object>(Convert.ChangeType(val, convType));
                return LiteralMark + litNum.ToString() + LiteralMark;
            }
            catch (Exception ex)
            {
                string msg = $"Could not calculate the value of the constant due to an error.";
                if (ex is ScriptLoadingException || ex is ScriptExecutionException) msg += " " + ex.Message;

                throw new ScriptLoadingException(msg);
            }
        }

        const char TypeHintChar = '`';
        private void AddVarTypes(ScriptFunction fn, Dictionary<string, VarType> varTypes, Dictionary<string, int> constants, List<string> pubVars = null)
        {

            if (!fn.IsInstanceFunc && !fn.IsStaticFunc)
            {
                if (!CurScript.AvtProcessed.Contains(varTypes)) CurScript.AvtProcessed.Add(varTypes);
                else return;
            }

            var newVarTypes = new Dictionary<string, VarType>();

            string typeName, varName;
            int spacePos;
            string unitPart;
            char[] c = { ',', '?', ':', '(', '{', '[', OperatorChar[0] };



            CodeUnit u;
            for (int i = 0; i < fn.Units.Length; i++)
            {
                u = fn.Units[i];
                if (u.Type > UnitType.Switch && u.Type != UnitType.EndDo && u.Type != UnitType.Return && u.Type != UnitType.Throw && u.Type != UnitType.ForEach) continue;

                bool isForEach = u.Type == UnitType.ForEach;
                int j;
                for (int n = 0; n < u.Str.Length; n++)
                {
                    j = n;
                    if (isForEach) j = n == 0 ? 1 : 0;
                    unitPart = u.Str[j];

                    int p, p2, p3;
                    spacePos = unitPart.IndexOf(' ');
                    while (spacePos > 0)
                    {
                        p = spacePos;
                        do
                        {
                            p = unitPart.LastIndexOfAny(c, --p);
                        } while (p >= 0 && unitPart[p] == '[' && unitPart[p + 1] == ']');



                        p3 = FindEndOfExpression(unitPart, spacePos);
                        if (p3 < 0) p3 = unitPart.Length;
                        p2 = unitPart.IndexOf(OperatorChar[0], spacePos);
                        if (p2 < 0 || p2 > p3) p2 = p3;

                        typeName = unitPart.Substring(p + 1, spacePos - (p + 1));


                        bool isPublic = typeName == "public";

                        if (isPublic)
                        {
                            if (!fn.IsInstanceFunc && !fn.IsStaticFunc) throw new ScriptLoadingException(ErrMsgWithLoc($"The public modifier cannot be used for variables inside functions.", u.CodeLocation ?? fn.LocationMark));
                            spacePos = unitPart.IndexOf(' ', spacePos + 1);
                            if (spacePos > p2) spacePos = -1;

                            if (spacePos < 0)
                                throw new ScriptLoadingException(ErrMsgWithLoc($"Type of variable '{unitPart.Substring(p + 8, p2 - (p + 8))}' in {fn.Class.ClassFullName}.{FormatFuncSign(fn.Signature)} not set.", u.CodeLocation ?? fn.LocationMark));
                            p += 8;
                            typeName = unitPart.Substring(p, spacePos - p);
                        }



                        bool isHint = typeName[0] == TypeHintChar;
                        if (typeName != "new" && typeName != "ref" && (isHint || Char.IsLetter(typeName[0])))
                        {
                            varName = unitPart.Substring(spacePos + 1, p2 - (spacePos + 1));

                            bool redefine = varName.StartsWith(RedefinePrefix);
                            if (redefine)
                                varName = varName.TrimStart(RedefinePrefix);

                            if (!CheckCharsInVarName(varName))
                            {
                                string msg = $"Invalid variable name '{varName}'. A valid variable name starts with a letter or underscore, followed by any number of letters, numbers, or underscores.";

                                throw new ScriptLoadingException(ErrMsgWithLoc(msg, u.CodeLocation ?? fn.LocationMark));
                            }

                            if (constants.ContainsKey(varName))
                                throw new ScriptLoadingException(ErrMsgWithLoc($"The name '{varName}' is already in use by a constant.", u.CodeLocation ?? fn.LocationMark));

                            if (ExceptionVarNames.Contains(varName))
                                throw new ScriptLoadingException(ErrMsgWithLoc($"The name '{varName}' is reserved for exceptions.", u.CodeLocation ?? fn.LocationMark));

                            VarType t, t2;
                            bool inEx = false;
                            try
                            {
                                if (isHint)
                                {
                                    string hint = typeName.Remove(0, 1);
                                    t2 = GetTypeByHint(hint, fn);
                                }
                                else if (typeName == "var")
                                {
                                    string valPart;
                                    if (!isForEach || j == 1)
                                    {
                                        int assignOpPos = unitPart.IndexOf(OperatorServiceStr[OperationKind.Assignment], p2);
                                        if (assignOpPos != p2) throw new ScriptLoadingException($"Incorrect use of the 'var' keyword.");
                                        valPart = unitPart.Substring(assignOpPos + 4, p3 - (assignOpPos + 4));


                                    }
                                    else
                                        valPart = u.Str[1];


                                    SetCurrentBuild(CurScript, fn, u, this);
                                    ApplySyntacticSugar(ref valPart);
                                    try { t2 = GetExpressionType(valPart, varTypes).Type; }
                                    catch (Exception ex) { inEx = true; throw new ScriptLoadingException(ErrMsgWithLoc($"Could not determine type of variable '{varName}'. {(ex is ScriptLoadingException ? ex.Message : "")}", u.CodeLocation ?? fn.LocationMark)); }
                                    if (isForEach && j == 0)
                                    {
                                        if (TypeIsArray(t2.ID))
                                            t2 = GetElementVarType(t2);
                                        else if (t2.ID == TypeID.String)
                                            t2 = CurScript.AllTypes[TypeStr.Char];
                                        else if (t2.ID == TypeID.Object)
                                        {
                                            if (t2.TypeHint == null) t2 = CurScript.AllTypes[TypeStr.Object];
                                            else
                                            {
                                                var en = GetObjEnumeratorItemType(t2.TypeHint);
                                                if (!en.isIEnumerator) throw new ScriptLoadingException($"foreach statement cannot operate on objects of type '{t2.TypeHint}'. Missing 'GetEnumerator' method.");
                                                var tid = GetTypeID(en.itemType, true);
                                                if (tid != TypeID.None) t2 = GetVarTypeByID(tid);
                                                else
                                                {
                                                    t2 = CurScript.AllTypes[TypeStr.Object];
                                                    t2.AddHint(en.itemType);
                                                }

                                            }

                                        }
                                        else
                                            throw new ScriptLoadingException($"foreach statement cannot operate on objects of type '{t2.Name}'.");
                                    }

                                }
                                else
                                    t2 = GetTypeFromDict(typeName, fn.Class);

                            }
                            catch (Exception ex)
                            {
                                if (inEx) throw;
                                throw new ScriptLoadingException(ErrMsgWithLoc(ex.Message, u.CodeLocation ?? fn.LocationMark));
                            }


                            if (newVarTypes.TryGetValue(varName, out t) && (!redefine || t != t2))
                            {
                                string msg;
                                if (redefine) msg = $"The variable '{varName}' is declared with different types.";
                                else msg = $"The variable '{varName}' has already been declared.";

                                throw new ScriptLoadingException(ErrMsgWithLoc(msg, u.CodeLocation ?? fn.LocationMark));

                            }


                            varTypes[varName] = t2;
                            newVarTypes[varName] = t2;

                            if (isPublic) pubVars.Add(varName);



                        }
                        spacePos = unitPart.IndexOf(' ', spacePos + 1);
                    }

                }
            }
        }
        static int FindEndOfExpression(string s, int pos = 0, bool ignoreFirstColon = false)
        {
            int open = 0, close = 0;
            for (int i = pos; i < s.Length; i++)
            {
                char c = s[i];
                int ii = i + 1;


                if (c == '(' || c == '[' || c == '{' || c == ProtectedOpenBrace) open++;
                else if (c == ')' || c == ']' || c == '}' || c == ProtectedCloseBrace)
                {
                    close++;
                    if (close > open) return i;

                }
                else if (c == ',' || c == ':' || (c == '?' && (ii >= s.Length || (s[ii] != '.' && s[ii] != '[' && s[ii] != ArrowOperator[0]))))
                {

                    if (close == open)
                    {
                        if (ignoreFirstColon && c == ':')
                        {
                            ignoreFirstColon = false;
                            continue;
                        }
                        return i;
                    }
                }
                else if (c == ';' || c == ServiceChar[0])
                    return i;
            }
            return -1;
        }
        static int FindStartOfExpression(string s, int pos = -1)
        {
            if (pos < 0) pos = s.Length - 1;
            int open = 0, close = 0;
            for (int i = pos; i >= 0; i--)
            {
                char c = s[i];
                int ii = i + 1;


                if (c == ')' || c == ']' || c == '}' || c == ProtectedCloseBrace) close++;
                else if (c == '(' || c == '[' || c == '{' || c == ProtectedOpenBrace)
                {
                    open++;
                    if (open > close) return i;

                }
                else if (c == ',' || c == ':' || (c == '?' && (ii >= s.Length || (s[ii] != '.' && s[ii] != '[' && s[ii] != ArrowOperator[0]))))
                {
                    if (close == open) return i;
                }
                else if (c == ';' || c == ServiceChar[0])
                    return i;
            }
            return -1;
        }
        VarType GetTypeByHint(string hint, ScriptFunction fn, bool setHint = true)
        {
            string orig = hint;
            ScriptClass sc = this;
            int i = hint.LastIndexOf('.');
            if (i > 0)
            {
                string path = hint.Substring(0, i);
                sc = GetClassByName(path, this);
                hint = hint.Substring(i + 1);



            }
            bool isArray = hint.EndsWith("[]");
            if (isArray) hint = hint.Remove(hint.Length - 2);
            VarType t;
            if (fn == null || !fn.VarTypes.TryGetValue(hint, out t))
                t = GetStaticVarType(sc, hint);

            if (t.SubType == null)
                throw new ScriptLoadingException($"Failed to get type of hint '{orig}'. {(t.ID == TypeID.None ? "Type constant not found" : "Value of the constant is not a type")}.");

            if (setHint)
            {
                t.SetHintBySubType();
                if (isArray)
                {
                    t.SetType(TypeID.ObjectArray);

                }

            }
            if (isArray) t.MakeArrayTypeHint(setHint);

            return t;
        }

        public static bool CheckCharsInVarName(string name)
        {
            if (name.Length < 1) return false;
            if (char.IsDigit(name[0])) return false;

            foreach (char c in name)
                if (!char.IsLetterOrDigit(c) && c != '_' && c != AutoVarPrefix) return false;

            return true;
        }
        private VarsToClear GetForCleaning(Dictionary<string, VarType> varTypes)
        {
            var types = varTypes.Values.Distinct();
            VarsToClear vtc = new VarsToClear();
            vtc.UsedTypeCount = types.Count();

            foreach (var item in types)
            {
                var varsOfType = varTypes.Where(x => x.Value.ID == item.ID);

                var typeId = item.ID;
                if (typeId == TypeID.CustomArray) typeId = TypeID.Custom;
                int typeNum = (int)typeId;

                switch (typeId)
                {
                    case TypeID.String: vtc.UsedTypes |= TypeFlag.String; vtc.VarIDs[typeNum] = varsOfType.Select(x => CurScript.GetVarID<string>(x.Key)).ToArray(); break;

                    case TypeID.Object: vtc.UsedTypes |= TypeFlag.Object; vtc.VarIDs[typeNum] = varsOfType.Select(x => CurScript.GetVarID<object>(x.Key)).ToArray(); break;
                    case TypeID.IntArray: vtc.UsedTypes |= TypeFlag.IntArray; vtc.VarIDs[typeNum] = varsOfType.Select(x => CurScript.GetVarID<int[]>(x.Key)).ToArray(); break;
                    case TypeID.LongArray: vtc.UsedTypes |= TypeFlag.LongArray; vtc.VarIDs[typeNum] = varsOfType.Select(x => CurScript.GetVarID<long[]>(x.Key)).ToArray(); break;
                    case TypeID.DoubleArray: vtc.UsedTypes |= TypeFlag.DoubleArray; vtc.VarIDs[typeNum] = varsOfType.Select(x => CurScript.GetVarID<double[]>(x.Key)).ToArray(); break;
                    case TypeID.FloatArray: vtc.UsedTypes |= TypeFlag.FloatArray; vtc.VarIDs[typeNum] = varsOfType.Select(x => CurScript.GetVarID<float[]>(x.Key)).ToArray(); break;
                    case TypeID.BoolArray: vtc.UsedTypes |= TypeFlag.BoolArray; vtc.VarIDs[typeNum] = varsOfType.Select(x => CurScript.GetVarID<bool[]>(x.Key)).ToArray(); break;
                    case TypeID.DecimalArray: vtc.UsedTypes |= TypeFlag.DecimalArray; vtc.VarIDs[typeNum] = varsOfType.Select(x => CurScript.GetVarID<decimal[]>(x.Key)).ToArray(); break;
                    case TypeID.StringArray: vtc.UsedTypes |= TypeFlag.StringArray; vtc.VarIDs[typeNum] = varsOfType.Select(x => CurScript.GetVarID<string[]>(x.Key)).ToArray(); break;
                    case TypeID.CharArray: vtc.UsedTypes |= TypeFlag.CharArray; vtc.VarIDs[typeNum] = varsOfType.Select(x => CurScript.GetVarID<char[]>(x.Key)).ToArray(); break;
                    case TypeID.ObjectArray: vtc.UsedTypes |= TypeFlag.ObjectArray; vtc.VarIDs[typeNum] = varsOfType.Select(x => CurScript.GetVarID<object[]>(x.Key)).ToArray(); break;
                    case TypeID.ShortArray: vtc.UsedTypes |= TypeFlag.ShortArray; vtc.VarIDs[typeNum] = varsOfType.Select(x => CurScript.GetVarID<short[]>(x.Key)).ToArray(); break;
                    case TypeID.ByteArray: vtc.UsedTypes |= TypeFlag.ByteArray; vtc.VarIDs[typeNum] = varsOfType.Select(x => CurScript.GetVarID<byte[]>(x.Key)).ToArray(); break;
                    case TypeID.DateArray: vtc.UsedTypes |= TypeFlag.DateArray; vtc.VarIDs[typeNum] = varsOfType.Select(x => CurScript.GetVarID<DateTime[]>(x.Key)).ToArray(); break;

                    case TypeID.Custom: vtc.UsedTypes |= TypeFlag.Custom; vtc.VarIDs[typeNum] = varsOfType.Select(x => CurScript.GetVarID<CustomObject>(x.Key)).ToArray(); break;
                }

            }

            vtc.SetUsedTypes();
            return vtc.UsedTypeCount > 0 ? vtc : null;

        }
        private VarType GetTypeFromDict(string typeName, ScriptClass context = null)
        {
            VarType vt;
            if (typeName.StartsWith(TypeHintChar))
            {

                typeName = typeName.Remove(0, 1);



                vt = GetTypeByHint(typeName, CurScript.CurrentBuildFn);

                return vt;
            }

            if (IsBasic(typeName)) return CurScript.AllTypes[typeName];

            bool isArray = typeName.EndsWith("[]");
            if (isArray) typeName = typeName.Remove(typeName.Length - 2, 2);
            var c = GetClassByNameOrException(typeName, context ?? CurScript.FThis);

            typeName = c.ClassFullName;
            if (isArray) typeName += "[]";

            if (CurScript.AllTypes.TryGetValue(typeName, out vt))
                return vt;
            else
                throw new ScriptLoadingException($"Type '{typeName}' not found'.");

        }

        private void RemoveExcessBrackets(ref string str)
        {
            while (str.StartsWith('(') && str.EndsWith(')'))
            {
                int i2 = FindClosing(str, 0, "(", ")");
                if (i2 == str.Length - 1)
                {
                    str = str.Remove(0, 1);
                    str = str.Remove(str.Length - 1, 1);
                }
                else break;
            }
        }
        static string RemModifs(string s)
        {
            if (s.StartsWith("public ")) s = s.Remove(0, 7);


            return s;
        }


        bool ConvNotAvailable(VarType generalType, VarType convType)
        {
            return generalType.ID != convType.ID && generalType.ID != TypeID.Object && (!TypeConverter.ConvertAbility(convType.ID, generalType.ID));
        }
        private EvalUnit GetEvalUnit<T1>(string strToEval, Dictionary<string, VarType> varTypes, ref bool noCache, EvalUnit parent = null, OperandPosition operandPos = default, string otherPart = null)
        {
            EvalUnit result = new EvalUnit();
            string Operand1 = "";
            string Operand2 = "";
            result.Code = strToEval;
            result.Parent = parent;

            ExpressionType eType;

            RemoveExcessBrackets(ref strToEval);



            strToEval = RemModifs(strToEval);
            int s = strToEval.IndexOf(' ');

            if (InBrackets(strToEval, s)) s = -1;

            int s2 = strToEval.IndexOf(OperatorChar);
            if (s2 >= 0 && s2 < s) s = -1;


            bool startsWith_new = false;
            bool startsWith_ref = false;
            if (s >= 0)
            {
                startsWith_new = strToEval.StartsWith("new ");
                startsWith_ref = strToEval.StartsWith("ref ");

                if (!startsWith_new && !startsWith_ref)
                {

                    strToEval = strToEval.Substring(s + 1);
                    strToEval = strToEval.TrimStart(RedefinePrefix);
                    result.Define = true;
                }


            }

            eType = GetExpressionType(strToEval, varTypes, parent, operandPos, otherPart);
            if (startsWith_ref) strToEval = strToEval.Remove(0, 4);
            ref VarType InnerType = ref eType.InnerType;

            result.OpKind = OperationKind.None;


            OperationKind opKind = 0;

            string exprOnExBlock = null;
            int opPos = FindOperator(strToEval, ref opKind);
            if (opPos < 0 && strToEval.StartsWith("(") && strToEval.EndsWith(")"))
            {
                RemoveExcessBrackets(ref strToEval);

                opPos = FindOperator(strToEval, ref opKind);

                if (opPos < 0 && strToEval.StartsWith("(") && strToEval.EndsWith(")"))
                {
                    int bp = strToEval.IndexOf(")(");
                    if (bp >= 0)
                    {
                        bp = FindClosing(strToEval, 0, "(", ")");
                        if (strToEval[bp + 1] == '(')
                        {
                            string newStrToEval = strToEval.Substring(1, bp - 1);
                            RemoveExcessBrackets(ref newStrToEval);
                            opPos = FindOperator(newStrToEval, ref opKind);
                            if (opPos >= 0 || newStrToEval.EndsWith(')') || HasCasting(newStrToEval))
                            {
                                exprOnExBlock = strToEval.Substring(bp + 1);
                                strToEval = newStrToEval;
                            }
                        }

                    }
                }
            }

            Operand1 = strToEval;
            result.OpKind = opKind;

            result.Type = eType.Type;

            bool fnIsOp = false;

            if (opPos >= 0)
            {
                result.Kind = EvalUnitKind.Expression;

                Operand1 = strToEval.Substring(0, opPos);
                Operand2 = strToEval.Substring(opPos + 4);

                bool uncheck = false;
                if (opKind == OperationKind.ForcedAssignment || (opKind >= OperationKind.AdditionForcedAssignment && opKind <= OperationKind.BitwiseOrForcedAssignment))
                {
                    uncheck = true;

                    result.OpKind = (OperationKind)Enum.Parse(typeof(OperationKind), opKind.ToString().Replace("Forced", ""));
                }
                string type = InnerType.ID.ToString();
                string fnPath = "";

                if (result.OpKind == OperationKind.Casting && (result.Type.ID == TypeID.Custom || result.Type.ID == TypeID.CustomArray || InnerType.ID == TypeID.Custom || InnerType.ID == TypeID.CustomArray))
                {
                    var castClass = (result.Type.CType != null ? result.Type.CType : InnerType.CType).Class;
                    fnPath = GetClassPath(castClass, CurScript.FThis);
                    if (fnPath.Length > 0) fnPath += "." + (result.Type.ID == TypeID.Custom || result.Type.ID == TypeID.CustomArray ? OrBasicFunctionPrefix : OrLocalFunctionPrefix);
                }
                else if (result.OpKind != OperationKind.Assignment && (InnerType.ID == TypeID.Custom || InnerType.ID == TypeID.CustomArray))
                {
                    fnPath = GetClassPath(InnerType.CType.Class, CurScript.FThis);
                    if (fnPath.Length > 0) fnPath += "." + OrLocalFunctionPrefix;
                    type = type.Replace("Custom", "");
                }

                if (result.OpKind == OperationKind.Casting)
                {
                    var maxType = result.Type.ID;
                    type = maxType.ToString().Replace("Custom", "");
                    if (Operand2.Length == 0)
                        return EvalUnit.GetEUWithSpecificValue(result.Type.TypeHint == null || !result.Type.TypeHint.IsValueType ? default(T1) : Activator.CreateInstance(result.Type.TypeHint), result.Type);

                    var castArgs = Operand2;
                    if (Operand1.StartsWith(TypeHintChar)) { castArgs += ',' + Operand1; type = "ByHint" + (result.Type.IsHintArray ? "Array" : ""); }
                    strToEval = Operand1 = fnPath + OperatorFunctionPrefix + type + result.OpKind.ToString() + $"({castArgs})";
                    Operand2 = "";
                    result.Kind = EvalUnitKind.Function;
                    result.OpKind = OperationKind.None;
                    InnerType = result.Type;
                    fnIsOp = true;
                }
                else if (result.OpKind >= OperationKind.AdditionAssignment && result.OpKind <= OperationKind.BitwiseOrAssignment)
                {
                    var op2Type = GetExpressionType(Operand2, varTypes);
                    var maxType = GetMaxType(result.Type.ID, op2Type.Type.ID);
                    type = maxType.ToString().Replace("Custom", "");

                    Operand2 = fnPath + OperatorFunctionPrefix + result.OpKind.ToString().Replace("Assignment", "") + $"({Operand1},{Operand2})";
                    result.OpKind = OperationKind.Assignment;
                    InnerType = result.Type;
                    fnIsOp = true;
                }
                else if (result.OpKind == OperationKind.Increment || result.OpKind == OperationKind.Decrement)
                {
                    result.Postfix = Operand1.Length > 0;
                    string opr = result.Postfix ? Operand1 : Operand2;
                    Operand2 = fnPath + OperatorFunctionPrefix + result.OpKind.ToString() + $"({opr})";
                    Operand1 = opr;
                    result.OpKind = OperationKind.Assignment;
                    fnIsOp = true;

                }
                else if (result.OpKind != OperationKind.Assignment)
                {
                    bool unary = OpIsUnary(result.OpKind);

                    strToEval = Operand1 = fnPath + OperatorFunctionPrefix + result.OpKind.ToString() + (unary ? $"({(Operand2.Length > 0 ? Operand2 : Operand1)})" : $"({Operand1},{Operand2})");

                    fnIsOp = true;
                    result.Kind = EvalUnitKind.Function;
                    result.OpKind = OperationKind.None;

                    Operand2 = "";
                    InnerType = result.Type;
                }
                if (result.OpKind == OperationKind.Assignment)
                {
                    result.Op1_Unit = GetEvalUnit(Operand1, varTypes, result, OperandPosition.Left, Operand2);
                    if (result.OpKind != OperationKind.Assignment)
                        return result.Op1_Unit;

                    if (result.Define)
                    {
                        result.VarID = result.Op1_Unit.VarID;
                        result.ScopeKind = result.Op1_Unit.ScopeKind;
                    }

                    try
                    {
                        result.Op2_Unit = GetEvalUnit(Operand2, varTypes, result, OperandPosition.Right, Operand1);
                    }
                    catch (ScriptLoadingException ex)
                    {
                        if (fnIsOp) throw new ScriptLoadingException($"Operator '{OperatorStr[opKind]}' use error. {ex.Message}");
                        else throw;
                    }

                    if (!uncheck)
                    {
                        if (!ArgIsValid(result.Op1_Unit.Type, result.Op2_Unit.Type, true))
                            throw new ScriptLoadingException($"Can't put value of type '{result.Op2_Unit.Type.Name}' into variable of type '{result.Op1_Unit.Type.Name}'.");
                    }
                    else
                    {

                        VarType generalType = InnerType;
                        VarType convType = result.Op1_Unit.Type.ID == generalType.ID ? result.Op2_Unit.Type : result.Op1_Unit.Type;

                        if (ConvNotAvailable(generalType, convType))
                            throw new ScriptLoadingException($"Operation '{OperatorStr[opKind]}' can't be performed due to type mismatch. Can't convert type '{convType.Name}' to type '{generalType.Name}'.");
                    }

                }
                if (fnIsOp && exprOnExBlock != null)
                    Operand1 = strToEval += exprOnExBlock;

            }
            else
            {
                string castingType = FindCastingType(ref strToEval);
                if (castingType != null)
                {
                    strToEval = castingType + OperatorServiceStr[OperationKind.Casting] + strToEval;
                    if (exprOnExBlock != null)
                        strToEval = $"({strToEval}){exprOnExBlock}";
                    var eu = GetEvalUnit(strToEval, varTypes);
                    return eu;
                }

                if (strToEval.Length == 0)
                {
                    result.Kind = EvalUnitKind.Empty;
                }
                else if (strToEval == TrueStr || strToEval == FalseStr)
                {

                    result.SpecificValue = strToEval == TrueStr ? true : false;
                    result.Kind = EvalUnitKind.SpecificValue;

                }
                else if (strToEval == NullStr)
                {

                    result.SpecificValue = null;
                    result.Kind = EvalUnitKind.SpecificValue;
                }
                else if (strToEval == ThisStr)
                {
                    result.Kind = EvalUnitKind.This;
                }
                else if (strToEval.EndsWith(LiteralMark))
                {
                    result.VarID = int.Parse(strToEval.Trim(LiteralMark).Substring(1));
                    result.Kind = EvalUnitKind.Literal;

                }
                else if (strToEval.StartsWith("{"))
                {
                    result.Kind = EvalUnitKind.New;
                    noCache = true;
                }
                else if (startsWith_new && SmartCharPos(strToEval, '.') <= strToEval.IndexOfAny(Brackets))
                {
                    result.Kind = EvalUnitKind.New;

                }
                else if (IsBasic(strToEval))
                {
                    result.SpecificValue = GetTypeByID(CurScript.AllTypes[strToEval].ID);
                    result.Kind = EvalUnitKind.BasicType;
                }
                else if (strToEval.EndsWith("]"))
                {
                    int i = strToEval.LastIndexOf('[');
                    while (i > 0 && InsideBlock(strToEval, i, "[", "]"))
                        i = strToEval.LastIndexOf('[', i - 1);

                    string objArg = strToEval.Substring(0, i);
                    string indexArg = strToEval.Substring(i + 1, strToEval.Length - (i + 2));
                    var indexes = SmartSplit(indexArg);
                    bool onCustomObj = InnerType.ID == TypeID.Custom;
                    bool onStringObj = result.Type.ID == TypeID.Char && InnerType.ID == TypeID.String;
                    if (onCustomObj || (onStringObj && indexes.Length == 1))
                    {


                        if (onCustomObj)
                        {
                            string fnName, valuePart;
                            if (operandPos == OperandPosition.Left) { fnName = "SetItem"; valuePart = "," + otherPart; parent.OpKind = OperationKind.None; }
                            else { fnName = "GetItem"; valuePart = ""; }
                            noCache = true;
                            strToEval = $"{objArg}.{fnName}({strToEval.Substring(i + 1, strToEval.Length - (i + 2))}{valuePart})";
                        }
                        else
                            strToEval = $"@CharAt({objArg},{indexArg})";


                        return GetEvalUnit(strToEval, varTypes);
                    }
                    else if (indexes.Length == 2)
                    {
                        strToEval = $"@Slice({objArg},{indexArg})";
                        return GetEvalUnit(strToEval, varTypes);
                    }
                    else if (indexes.Length == 1)
                        result.Kind = EvalUnitKind.ArrayItem;
                    else
                        throw new ScriptLoadingException("Invalid indexer.");
                }
                else if (strToEval.EndsWith(")"))
                {
                    result.Kind = EvalUnitKind.Function;
                }
                else
                {
                    result.Kind = EvalUnitKind.Variable;
                }

            }
            if (result.Kind == EvalUnitKind.Variable || result.Kind == EvalUnitKind.Function || result.Kind == EvalUnitKind.New || result.Kind == EvalUnitKind.ArrayItem)
            {

                string varName = Operand1.Length > 0 ? Operand1 : Operand2;
                string varPath = "";
                int i = -1, i2 = -1;
                if (result.Kind == EvalUnitKind.Function)
                {
                    var f = GetFuncNameAndArgs(varName);
                    varName = f.name;
                    i = f.bracketPos;

                    i2 = varName.LastIndexOf('.');


                    string fnName = varName;
                    if (i2 >= 0)
                    {
                        varPath = varName.Substring(0, i2);
                        fnName = varName.Substring(i2 + 1);

                        if (varPath != ThisStr)
                        {

                            bool orBasic, orLocal, basic;
                            GetFuncPrefix(ref fnName, out orBasic, out orLocal, out basic);
                            bool orLocalorBasic = orLocal || orBasic;

                            bool isStatic, lastIsCustom;
                            var tc = GetClassByVarPath(strToEval, varTypes, out isStatic, out lastIsCustom);
                            bool funcExists = false;
                            string fnNameWithoutPrefix = fnName.TrimStart(FunctionLayerPrefix);
                            if (lastIsCustom) funcExists = (isStatic ? tc.PublicStaticFuncNames : tc.PublicNonStaticFuncNames).Contains(fnNameWithoutPrefix);
                            if (!funcExists && (!isStatic || orLocalorBasic))
                            {
                                bool cast = IsCustomCastFunc(fnNameWithoutPrefix);
                                if (!orLocalorBasic || cast)
                                {
                                    if (Operand1[i + 1] != ')')
                                    {
                                        Operand1 = Operand1.Insert(i + 1, ",");

                                    }


                                    if (fnNameWithoutPrefix == "op_ArrayCasting") varPath += "[]";
                                    Operand1 = Operand1.Insert(i + 1, varPath);
                                }
                                varName = Operand1.Substring(i2 + 1);
                                GetFuncPrefix(ref varName, out orBasic, out orLocal, out basic);
                                if (orBasic) varName = BasicFunctionPrefix + varName;
                                Operand1 = varName;

                                i = varName.IndexOf('(');

                                varPath = "";
                                i2 = -1;

                            }
                        }
                    }

                }
                else if (result.Kind != EvalUnitKind.New)
                {
                    if (result.Kind == EvalUnitKind.ArrayItem)
                    {
                        i = varName.LastIndexOf('[');
                        while (i > 0 && InsideBlock(varName, i, "[", "]"))
                            i = varName.LastIndexOf('[', i - 1);
                    }

                    i2 = varName.LastIndexOf('.');
                    while (i2 > 0 && InsideBlock(varName, i2, "[", "]"))
                        i2 = varName.LastIndexOf('.', i2 - 1);

                }

                if (i >= 0 && i > i2) varName = varName.Substring(0, i);
                if (result.Kind == EvalUnitKind.Function || result.Kind == EvalUnitKind.ArrayItem || result.Kind == EvalUnitKind.New)
                {
                    string opr = Operand1;

                    if (result.Kind == EvalUnitKind.ArrayItem && (varName.EndsWith(')') || varName.EndsWith(']')))
                        opr = opr.Substring(i);

                    else
                    {
                        if (i2 >= 0)
                            opr = opr.Substring(i2 + 1);
                    }

                    if (result.Kind == EvalUnitKind.New && opr.StartsWith("{")) opr = "[]" + opr;
                    result.Nested = GetArgEvalUnits(opr, varTypes);

                    if (result.Kind == EvalUnitKind.New)
                    {

                        if (result.Nested == null) throw new ScriptLoadingException("A new expression requires (), [], or {} after type.");

                        if (TypeIsArray(result.Type.ID))
                        {
                            if (result.Nested.Length >= 2)
                            {

                                var initArgs = result.Nested[1].Args;

                                if (initArgs != null)
                                {
                                    if (result.Type.ID != TypeID.ObjectArray || result.Type.IsHintArray)
                                    {
                                        var et = GetElementVarType(result.Type);
                                        for (int q = 0; q < initArgs.Length; q++)
                                        {
                                            var argType = initArgs[q].Type;
                                            if (argType != et && !ArgIsValid(et, argType))
                                                throw new ScriptLoadingException($"Array of type '{result.Type.Name}' cannot be initialized with value of type '{argType.Name}' (at index {q}).");
                                        }
                                    }
                                }
                                else
                                    throw new ScriptLoadingException("Array initializer is empty.");


                            }
                            else if (result.Nested.Length < 1 || result.Nested[0].Args == null)
                                throw new ScriptLoadingException("Array creation must have array size or array initializer.");
                            else if (!TypeIsNumeric(result.Nested[0].Args[0].Type.ID))
                                throw new ScriptLoadingException("Array size must be a number.");

                        }
                    }

                }

                if (result.Kind == EvalUnitKind.Variable || (result.Kind == EvalUnitKind.Function && i2 >= 0) || (result.Kind == EvalUnitKind.ArrayItem && !varName.EndsWith(')') && !varName.EndsWith(']')))
                {
                    bool hint = false;
                    if (hint = varName.StartsWith(TypeHintChar))
                        varName = varName.Remove(0, 1);

                    if (i2 >= 0)
                    {
                        bool isStatic;
                        if ((result.ClassLink = GetClassByVarPath(varName, varTypes, out isStatic)) != null && isStatic)
                        {
                            string vn = varName.Substring(i2 + 1);
                            if (result.ClassLink.StatVars.Where(x => x.Name == vn).FirstOrDefault() == null && IsCustom(strToEval, out result.SpecificValue))
                            {
                                result.ClassLink = null;
                                result.Kind = EvalUnitKind.CustomType;
                            }
                            else
                                result.ScopeKind = VarScopeKind.Static;

                        }
                        else
                        {
                            result.ClassLink = null;
                            varPath = varName.Substring(0, i2);
                            if (varPath != ThisStr)
                            {
                                result.Path_Unit = GetEvalUnit(varPath, varTypes);
                                result.ScopeKind = VarScopeKind.Inst;
                            }
                            else
                                result.ScopeKind = VarScopeKind.This;

                        }
                        varName = varName.Substring(i2 + 1);
                        if (Operand1.Length > 0) Operand1 = varName; else Operand2 = varName;
                    }
                    else if (varTypes.ContainsKey(varName)) result.ScopeKind = VarScopeKind.Local;
                    else if (VarTypes.ContainsKey(varName)) result.ScopeKind = VarScopeKind.This;
                    else if (GetStaticVarType(this, varName, ref result.ClassLink).ID != TypeID.None) result.ScopeKind = VarScopeKind.Static;
                    else if (IsCustom(strToEval, out result.SpecificValue)) result.Kind = EvalUnitKind.CustomType;
                    else
                        throw new ScriptLoadingException($"Can't find member '{varName}'.");

                    if (result.ScopeKind == VarScopeKind.Local || result.ScopeKind == VarScopeKind.Static)
                    {
                        var consts = result.ScopeKind == VarScopeKind.Local ? (CurScript.CurrentBuildFn.IsStatic ? CurScript.CurrentBuildFn.Class.Constants : CurScript.CurrentBuildFn.Constants) : result.ClassLink.Constants;
                        if (consts == null) { result.ClassLink.GetConstants(true); consts = result.ClassLink.Constants; }

                        int litNum;
                        if (consts.TryGetValue(varName, out litNum))
                        {
                            if (hint && result.Kind == EvalUnitKind.ArrayItem)
                            {
                                result.Kind = EvalUnitKind.SpecificValue;
                                Type t = (Type)GetLiteral<object>(litNum);
                                result.SpecificValue = t.MakeArrayType();
                            }
                            else
                            {
                                if (result.Kind != EvalUnitKind.ArrayItem)
                                {
                                    if (SupportedLitTypes.Contains(result.Type.ID))
                                    {
                                        result.VarID = litNum;
                                        result.Kind = EvalUnitKind.Literal;
                                    }
                                    else
                                    {

                                        result.SpecificValue = GetLiteral<object>(litNum);
                                        result.Kind = EvalUnitKind.SpecificValue;
                                    }
                                }
                                else
                                    result.Op1_Unit = EvalUnit.GetEUWithSpecificValue(GetLiteral<object>(litNum), GetVarTypeByID(GetArrayTypeId(result.Type.ID)));


                            }
                            result.ClassLink = null;
                            result.ScopeKind = VarScopeKind.None;
                        }
                    }
                }
                else
                    result.ScopeKind = VarScopeKind.This;

                if (result.Kind == EvalUnitKind.Function)
                {
                    string fnName = varName;

                    bool orBasic, orLocal, basic;
                    GetFuncPrefix(ref fnName, out orBasic, out orLocal, out basic);
                    bool orLocalorBasic = orLocal || orBasic;
                    bool local = varPath.Length == 0;
                    ScriptClass sc = CurScript.FThis;
                    if (fnName.Length == 0 && !basic) throw new ScriptLoadingException("Function name is empty.");

                    bfind:
                    if (basic)
                    {
                        result.Func = CurScript.BFuncs.GetFunc<T1>(fnName, ref result.Nested[0].Args, sc);
                    }
                    else
                    {
                        string fnNameWithoutPrefix = fnName.TrimStart(FunctionLayerPrefix);
                        bool isStatic = CurScript.CurrentBuildFn.IsStatic;
                        ref var args = ref result.Nested[0].Args;

                    find:
                        if (local)
                        {
                            var fn = FindFuncInThisOrOuters(fnName, args, isStatic);
                            if (fn != null)
                                result.Func = fn.IsVirtual || fn.IsOverride ? GetVirtFuncToCall<T1>(fnName, args) : GetFuncToCall<T1>(fn);
                            else
                                result.Func = CurScript.BFuncs.GetFunc<T1>(fnName, ref args, sc);

                        }
                        else
                        {
                            bool isThis = varPath == ThisStr;
                            sc = isThis ? this : GetClassByVarPath(strToEval, varTypes, out isStatic);
                            var ff = sc.GetFunc<T1>(fnName, args, true, isThis ? null : true, !isStatic ? null : isStatic);

                            if (ff == null && (!isStatic || orLocalorBasic))
                            {
                                result.ScopeKind = VarScopeKind.This;
                                if (orBasic)
                                {
                                    basic = true;
                                    goto bfind;
                                }
                                else
                                {
                                    local = true;

                                    isStatic = CurScript.CurrentBuildFn.IsStatic;
                                    goto find;
                                }

                            }

                            if (ff != null)
                                result.Func = ff;
                            else
                                throw new ScriptLoadingException($"Function '{FormatFuncSign(GetFuncSign(fnName, args))}' not found at '{sc.ClassFullName}' (the function exists but has inappropriate modifiers/parameters).");

                        }
                    }

                }
                else if (result.Kind == EvalUnitKind.New && (InnerType.ID == TypeID.Custom || InnerType.ID == TypeID.CustomArray))
                {

                    if (result.Type.CType.Class.IsStatic) throw new ScriptLoadingException($"Can't create an {(InnerType.ID == TypeID.Custom ? "instance" : "array of instances")} of the static class '{Operand1}'.");

                    if (InnerType.ID == TypeID.Custom)
                    {

                        var ctorArgs = result.Nested[0].Args;
                        bool defaultCtor = ctorArgs == null && !result.Type.CType.Class.HasCtors;
                        bool? pub = result.Type.CType.Class.Is(this) ? null : true;
                        result.Func = result.Type.CType.Class.GetFunc<object>(CtorFuncName, ctorArgs, defaultCtor, pub);

                    }
                }

                if (result.Kind == EvalUnitKind.Variable || result.Kind == EvalUnitKind.ArrayItem)
                {
                    if (result.Kind == EvalUnitKind.ArrayItem)
                    {

                        if (varName.EndsWith(')') || varName.EndsWith(']'))
                            result.Op1_Unit = GetEvalUnit(varName, varTypes);
                        else if (result.Type.ID == TypeID.Custom)
                            result.VarID = CurScript.GetVarID<T1>(varName);
                        else
                            result.VarID = CurScript.GetVarID<T1[]>(varName);
                    }
                    else
                        result.VarID = CurScript.GetVarID<T1>(varName);

                    byte pn = 0;
                    for (int n = 0; n < CurScript.CurrentBuildFn.Params.Length; n++)
                    {
                        var p = CurScript.CurrentBuildFn.Params[n];
                        if (p.ByRef)
                        {

                            if (p.ParamName == varName)
                            {
                                result.ScopeKind = VarScopeKind.Ref;
                                result.RefParamNum = pn;
                                break;
                            }
                            pn++;
                        }
                    }

                }

            }

            result.IsArrayItem = result.Kind == EvalUnitKind.ArrayItem;



            if (result.Define)
                result.ScopeKindIsThisOrStatic = result.ScopeKind == VarScopeKind.This || result.ScopeKind == VarScopeKind.Static;



            result.CU = CurScript.CurrentBuildCU;

            SetProcessUnit<T1>(result);
            if (result.Kind == EvalUnitKind.Function && result.Nested.Length > 1)
            {
                result.FuncOnEx = new List<EvalUnit.FuncOnExData>();
                for (int i = 1; i < result.Nested.Length; i++)
                {
                    var n = result.Nested[i];

                    EvalUnit conditionEU = null;
                    EvalUnit triesEU = null;
                    EvalUnit retryWhileEU = null;
                    EvalUnit[] retryEU = null;
                    EvalUnit valueEU = null;
                    bool retry = false;

                    if (n.Args != null)
                    {
                        valueEU = n.Args[0];


                        if (n.Args.Length > 1)
                        {
                            var eu = n.Args[1];
                            if (eu.Kind != EvalUnitKind.Empty)
                            {
                                if (eu.Type.ID == TypeID.Bool) conditionEU = eu;
                                else throw new ScriptLoadingException("Exception condition must be boolean.");
                            }
                            if (n.Args.Length > 2)
                            {
                                retry = true;
                                eu = n.Args[2];
                                if (eu.Kind != EvalUnitKind.Empty)
                                {
                                    if (TypeIsNumeric(eu.Type.ID)) triesEU = eu;
                                    else throw new ScriptLoadingException("Number of tries must be numeric.");
                                }
                                if (n.Args.Length > 3)
                                {
                                    eu = n.Args[3];
                                    if (eu.Kind != EvalUnitKind.Empty)
                                    {
                                        if (eu.Type.ID == TypeID.Bool) retryWhileEU = eu;
                                        else throw new ScriptLoadingException("Retry cancel value must be boolean.");
                                    }


                                    if (n.Args.Length > 4)
                                        retryEU = n.Args.Skip(4).ToArray();


                                }

                            }
                        }

                        if (result.Type.ID != TypeID.Void && valueEU.Kind != EvalUnitKind.Empty && !ArgIsValid(result.Type, valueEU.Type))
                            throw new ScriptLoadingException($"Exception case value must be of type '{result.Type.Name}'.");
                    }


                    result.FuncOnEx.Add(new EvalUnit.FuncOnExData(valueEU, conditionEU, retry, triesEU, retryWhileEU, retryEU));

                }
                Array.Resize(ref result.Nested, 1);
            }

            result.IsAssignment = result.Kind == EvalUnitKind.Expression;
            if (result.IsAssignment) result.Kind = EvalUnitKind.Assignment;

            if (CurScript.CurrentBuildCU == null)
                result.ScopeKind = VarScopeKind.Local;

            return result;
        }

        static bool IsCustomCastFunc(string fnName) => fnName == "op_Casting" || fnName == "op_ArrayCasting";

        static bool OpIsUnary(OperationKind OpKind) => OpKind == OperationKind.Casting || OpKind == OperationKind.Increment || OpKind == OperationKind.Decrement;
        private ScriptFunction FindFuncInThisOrOuters(string name, EvalUnit[] args, bool onlyStatic = false)
        {
            if (!onlyStatic) onlyStatic = CurScript.CurrentBuildFn.IsStatic;
            bool? isStatic = onlyStatic ? true : null;

            ScriptClass c = this;
            ScriptFunction fn;
            List<ScriptFunction> funcs;
            do
            {
                funcs = c.Functions;

                fn = c.GetFunc(name, args, true, null, isStatic);

                if (fn != null) return fn;

                c = c.OuterClass;
                isStatic = true;

            } while (fn == null && c != null);

            return null;
        }


        private VarType GetStaticVarType(ScriptClass c, string name, ref Dictionary<string, VarType> vt, ref ScriptClass cc)
        {
            VarType type;
            vt = c.StaticVarTypes;
            cc = c;
            while (!vt.TryGetValue(name, out type))
            {

                c = c.OuterClass;

                if (c == null) break;
                cc = c;
                vt = c.StaticVarTypes;
            }

            return type;
        }
        private VarType GetStaticVarType(ScriptClass c, string name)
        {
            Dictionary<string, VarType> vt = null;
            ScriptClass cc = null;
            return GetStaticVarType(c, name, ref vt, ref cc);
        }
        private VarType GetStaticVarType(ScriptClass c, string name, ref ScriptClass cc)
        {
            Dictionary<string, VarType> vt = null;

            return GetStaticVarType(c, name, ref vt, ref cc);
        }

        Dictionary<CacheItemKey, EvalUnit> EUCache = new Dictionary<CacheItemKey, EvalUnit>();
        private EvalUnit GetEvalUnit(string s, Dictionary<string, VarType> varTypes, EvalUnit parent = null, OperandPosition operandPos = default, string otherPart = null)
        {
            EvalUnit result;
            var key = new CacheItemKey(s, varTypes);
            if (EUCache.TryGetValue(key, out result)) return result;
            bool noCache = false;
            TypeID eType = GetExpressionType(s, varTypes, parent, operandPos, otherPart).Type.ID;

            switch (eType)
            {
                case TypeID.Int: result = GetEvalUnit<int>(s, varTypes, ref noCache, parent, operandPos, otherPart); break;
                case TypeID.String: result = GetEvalUnit<string>(s, varTypes, ref noCache, parent, operandPos, otherPart); break;
                case TypeID.Char: result = GetEvalUnit<char>(s, varTypes, ref noCache, parent, operandPos, otherPart); break;
                case TypeID.Long: result = GetEvalUnit<long>(s, varTypes, ref noCache, parent, operandPos, otherPart); break;
                case TypeID.Double: result = GetEvalUnit<double>(s, varTypes, ref noCache, parent, operandPos, otherPart); break;
                case TypeID.Float: result = GetEvalUnit<float>(s, varTypes, ref noCache, parent, operandPos, otherPart); break;
                case TypeID.Decimal: result = GetEvalUnit<decimal>(s, varTypes, ref noCache, parent, operandPos, otherPart); break;
                case TypeID.Bool: result = GetEvalUnit<bool>(s, varTypes, ref noCache, parent, operandPos, otherPart); break;
                case TypeID.Object: result = GetEvalUnit<object>(s, varTypes, ref noCache, parent, operandPos, otherPart); break;
                case TypeID.Short: result = GetEvalUnit<short>(s, varTypes, ref noCache, parent, operandPos, otherPart); break;
                case TypeID.Byte: result = GetEvalUnit<byte>(s, varTypes, ref noCache, parent, operandPos, otherPart); break;
                case TypeID.Date: result = GetEvalUnit<DateTime>(s, varTypes, ref noCache, parent, operandPos, otherPart); break;

                case TypeID.IntArray: result = GetEvalUnit<int[]>(s, varTypes, ref noCache, parent, operandPos, otherPart); break;
                case TypeID.StringArray: result = GetEvalUnit<string[]>(s, varTypes, ref noCache, parent, operandPos, otherPart); break;
                case TypeID.CharArray: result = GetEvalUnit<char[]>(s, varTypes, ref noCache, parent, operandPos, otherPart); break;
                case TypeID.LongArray: result = GetEvalUnit<long[]>(s, varTypes, ref noCache, parent, operandPos, otherPart); break;
                case TypeID.DoubleArray: result = GetEvalUnit<double[]>(s, varTypes, ref noCache, parent, operandPos, otherPart); break;
                case TypeID.FloatArray: result = GetEvalUnit<float[]>(s, varTypes, ref noCache, parent, operandPos, otherPart); break;
                case TypeID.DecimalArray: result = GetEvalUnit<decimal[]>(s, varTypes, ref noCache, parent, operandPos, otherPart); break;
                case TypeID.BoolArray: result = GetEvalUnit<bool[]>(s, varTypes, ref noCache, parent, operandPos, otherPart); break;
                case TypeID.ObjectArray: result = GetEvalUnit<object[]>(s, varTypes, ref noCache, parent, operandPos, otherPart); break;
                case TypeID.ShortArray: result = GetEvalUnit<short[]>(s, varTypes, ref noCache, parent, operandPos, otherPart); break;
                case TypeID.ByteArray: result = GetEvalUnit<byte[]>(s, varTypes, ref noCache, parent, operandPos, otherPart); break;
                case TypeID.DateArray: result = GetEvalUnit<DateTime[]>(s, varTypes, ref noCache, parent, operandPos, otherPart); break;

                case TypeID.Void: result = GetEvalUnit<object>(s, varTypes, ref noCache, parent, operandPos, otherPart); break;

                case TypeID.CustomArray:
                case TypeID.Custom: result = GetEvalUnit<CustomObject>(s, varTypes, ref noCache, parent, operandPos, otherPart); break;

                default: throw new ScriptLoadingException($"Unknown type '{eType}'.");
            }
            if (!noCache) EUCache.Add(key, result);
            return result;
        }

        bool GetConstVal(EvalUnit eu, out object v)
        {
            v = null;
            if (eu.Kind == EvalUnitKind.Literal)
                v = GetLiteral(eu.VarID, eu.Type.ID);
            else if (eu.Kind == EvalUnitKind.SpecificValue)
                v = eu.SpecificValue;
            else if (eu.Kind == EvalUnitKind.BasicType || eu.Kind == EvalUnitKind.CustomType)
                v = eu.SpecificValue;
            else
                return false;

            return true;

        }
        void CalcJumps(ScriptFunction fn)
        {
            CodeUnit[] units = fn.Units;

            for (int c = 0; c < units.Length; c++)
            {
                units[c].EU0 = units[c].EU.Length > 0 ? units[c].EU[0] : null;
                units[c].EU1 = units[c].EU.Length > 1 ? units[c].EU[1] : null;
                units[c].EU2 = units[c].EU.Length > 2 ? units[c].EU[2] : null;
            }

            CodeUnit u;
            Stack<CodeUnit> tryes = new Stack<CodeUnit>();
            Stack<bool> inCatchPrev = new Stack<bool>();
            Stack<bool> inFinallyPrev = new Stack<bool>();

            int forEachNum = 0, reapplyNum = 0;

            int i, lim;
            int currentUnit = 0, totalUnits = units.Length;

            bool inCatch = false, inFinally = false;
            inCatchPrev.Push(false);
            inFinallyPrev.Push(false);

            for (int n = 0; n < totalUnits; n++) units[n].Index = n;

            try
            {
                while (currentUnit < totalUnits)
                {
                    u = units[currentUnit];

                    u.Next = u.TrueNext = u.FalseNext = null;
                    int next = -1, trueNext = -1, falseNext = -1;

                    switch (u.Type)
                    {
                        case UnitType.Expression:

                            trueNext = currentUnit + 1;
                            break;
                        case UnitType.Apply:
                            trueNext = currentUnit + 1;
                            break;
                        case UnitType.Reapply:
                            trueNext = currentUnit + 1;

                            u.Order = reapplyNum;
                            reapplyNum++;
                            break;
                        case UnitType.Break:
                            next = FindUnit(units, UnitTypes.EndLoopOrEndSwitch, currentUnit, false, true) + 1;

                            break;
                        case UnitType.Continue:
                            next = FindUnit(units, UnitTypes.EndLoop, currentUnit, false, true);

                            break;

                        case UnitType.ForEach:
                            trueNext = currentUnit + 1;
                            falseNext = FindUnit(units, UnitTypes.EndForEach, currentUnit) + 1;

                            u.Order = forEachNum;
                            forEachNum++;
                            break;
                        case UnitType.For:

                            trueNext = currentUnit + 1;
                            falseNext = FindUnit(units, UnitTypes.EndFor, currentUnit) + 1;
                            break;

                        case UnitType.EndForEach:
                            trueNext = FindUnit(units, UnitTypes.ForEach, currentUnit, true);
                            falseNext = currentUnit + 1;
                            break;
                        case UnitType.EndFor:

                            trueNext = FindUnit(units, UnitTypes.For, currentUnit, true);
                            falseNext = currentUnit + 1;
                            break;
                        case UnitType.If:
                        case UnitType.ElseIf:

                            falseNext = FindUnit(units, UnitTypes.ElseOrEndif, currentUnit);
                            trueNext = currentUnit + 1;

                            break;

                        case UnitType.EndIfTrueBlock:
                            next = FindUnit(units, UnitTypes.Endif, currentUnit) + 1;
                            break;

                        case UnitType.While:
                            falseNext = FindUnit(units, UnitTypes.EndWhile, currentUnit) + 1;
                            trueNext = currentUnit + 1;
                            break;

                        case UnitType.EndWhile:
                            next = FindUnit(units, UnitTypes.While, currentUnit, true);

                            break;

                        case UnitType.EndDo:
                            trueNext = FindUnit(units, UnitTypes.Do, currentUnit, true) + 1;
                            falseNext = currentUnit + 1;
                            break;

                        case UnitType.Switch:
                            var switchDict = u.SwitchDict = new Dictionary<NullableObject<object>, CodeUnit>();
                            Type switchType = u.EU0.Type.T;
                            lim = FindUnit(units, UnitTypes.EndSwitch, currentUnit);
                            falseNext = lim + 1;
                            i = FindUnit(units, UnitTypes.Case, currentUnit, false, false, lim);
                            while (i >= 0)
                            {
                                u = units[i];

                                NullableObject<object> val;
                                object euVal;
                                if (!GetConstVal(u.EU0, out euVal))
                                {
                                    currentUnit = i;
                                    throw new ScriptLoadingException("Invalid case: '" + u.Code + "'. A constant value is expected.");
                                }
                                else
                                    val = euVal;

                                try { val = val.IsNull || switchType.IsAssignableFrom(val.Item.GetType()) ? val.Item : Convert.ChangeType(val.Item, switchType); }
                                catch (Exception ex) { currentUnit = i; throw new ScriptLoadingException("Invalid case value. " + ex.Message); }

                                switchDict.Add(val, units[i + 1]);
                                i = FindUnit(units, UnitTypes.Case, i, false, false, lim);
                            }
                            i = FindUnit(units, UnitTypes.DefaultCase, currentUnit, false, false, lim);

                            if (i >= 0) falseNext = i + 1;
                            break;
                        case UnitType.Try:
                            tryes.Push(u);
                            inCatchPrev.Push(inCatch);
                            inFinallyPrev.Push(inFinally);
                            inCatch = inFinally = false;
                            next = currentUnit + 1;

                            var caseDict = u.CaseDict = new Dictionary<string, CodeUnit>();
                            var caseList = u.CaseList = new List<KeyValuePair<object, CodeUnit>>();
                            int tlim = FindUnit(units, UnitTypes.Try, currentUnit);
                            lim = currentUnit;
                            i = currentUnit;
                            while (i >= 0) { lim = i; i = FindUnit(units, UnitTypes.EndTryEndCatchEndFinally, i, false, false, tlim); }
                            falseNext = lim + 1;
                            i = FindUnit(units, UnitTypes.CatchByName, currentUnit, false, true, lim);
                            while (i >= 0)
                            {
                                u = units[i];

                                if (u.EU0 != null)
                                {
                                    if (u.EU0.Kind != EvalUnitKind.Literal || u.EU0.Type.ID != TypeID.String) { currentUnit = i; throw new ScriptLoadingException("Catch (by name) block must contain constant string name of exception."); }
                                    string litVal = GetLiteral<string>(u.EU0.VarID);
                                    caseDict.Add(litVal, units[i + 1]);
                                }
                                else { trueNext = i + 1; }
                                i = FindUnit(units, UnitTypes.CatchByName, i, false, false, lim);
                            }
                            i = FindUnit(units, UnitTypes.Finally, currentUnit, false, false, lim);

                            if (i >= 0) { falseNext = i + 1; units[currentUnit].Finally = units[i + 1]; }

                            i = FindUnit(units, UnitTypes.Catch, currentUnit, false, true, lim);
                            while (i >= 0)
                            {
                                u = units[i];

                                if (u.EU0 != null)
                                {

                                    object euVal;
                                    if (!GetConstVal(u.EU0, out euVal))
                                    {
                                        currentUnit = i;
                                        throw new ScriptLoadingException("Invalid catch value '" + u.EU0.Code + "'. A constant value is expected.");
                                    }

                                    if (euVal is string str)
                                    {
                                        if (str.IndexOf('.') < 0) str = "System." + str;
                                        try { euVal = Type.GetType(str, true); }
                                        catch { currentUnit = i; throw; }
                                    }
                                    else if (!(euVal is Type || euVal is CustomType))
                                    {
                                        currentUnit = i;
                                        throw new ScriptLoadingException("Invalid catch value '" + u.EU0.Code + "'. A type value is expected.");
                                    }



                                    caseList.Add(new KeyValuePair<object, CodeUnit>(euVal, units[i + 1]));

                                }
                                else { trueNext = i + 1; }
                                i = FindUnit(units, UnitTypes.Catch, i, false, false, lim);
                            }
                            break;
                        case UnitType.GoTo:

                            string lbl = u.Str[0];
                            if (lbl.StartsWith("case "))
                            {
                                lbl = lbl.Substring(5);
                                i = FindUnit(units, UnitTypes.Switch, currentUnit, true);

                                lim = FindUnit(units, UnitTypes.EndSwitch, i);

                                do
                                {
                                    i = FindUnit(units, UnitTypes.Case, i, false, false, lim);
                                    if (i < 0) break;
                                    u = units[i];
                                } while (u.Str[0] != lbl);
                                if (i >= 0) next = i + 1; else throw new ScriptLoadingException($"Case '{lbl}' not found.");
                            }
                            else if (lbl == "default")
                            {
                                i = FindUnit(units, UnitTypes.Switch, currentUnit, true);
                                lim = FindUnit(units, UnitTypes.EndSwitch, i);
                                i = FindUnit(units, UnitTypes.DefaultCase, i, false, false, lim);
                                if (i >= 0) next = i + 1; else throw new ScriptLoadingException($"Default case not found.");

                            }
                            else
                            {
                                i = -1;
                                do
                                {

                                    i = FindUnit(units, UnitTypes.Label, i, false, true, -1, true);
                                    if (i < 0) break;
                                    u = units[i];
                                } while (u.Str[0] != lbl);
                                if (i >= 0) next = i + 1; else throw new ScriptLoadingException($"Label '{lbl}' not found.");
                            }
                            break;

                        case UnitType.Return:

                            next = -1;
                            break;
                        case UnitType.Finally:
                            inFinally = true;
                            next = currentUnit + 1;
                            break;
                        case UnitType.Catch:
                        case UnitType.CatchByName:
                            inCatch = true;
                            next = currentUnit + 1;

                            if (u.Type == UnitType.CatchByName) u.Type = UnitType.Catch;
                            break;
                        case UnitType.EndTry:
                        case UnitType.EndCatch:
                        case UnitType.EndCatchByName:
                            int n = currentUnit + 1;
                            CodeUnit nx = n < units.Length ? units[n] : null;
                            if (nx == null || (nx.Type != UnitType.Catch && nx.Type != UnitType.CatchByName))
                            {
                                tryes.Pop();

                                inCatch = inCatchPrev.Pop();

                                if (nx != null && nx.Type == UnitType.Finally) inFinally = true; else inFinally = inFinallyPrev.Pop();
                            }

                            lim = FindUnit(units, UnitTypes.Try, currentUnit);

                            i = FindUnit(units, UnitTypes.Finally, currentUnit, false, false, lim);
                            if (i >= 0) { trueNext = i + 1; }
                            else
                            {
                                int i2;
                                i = currentUnit;
                                do
                                {
                                    i2 = i;
                                    i = FindUnit(units, UnitTypes.EndCatch, i, false, false, lim);

                                } while (i >= 0);
                                trueNext = i2 + 1;

                            }

                            if (u.Type == UnitType.EndCatchByName) u.Type = UnitType.EndCatch;
                            break;
                        case UnitType.EndFinally:
                            inFinally = inFinallyPrev.Pop();
                            trueNext = currentUnit + 1;
                            break;
                        case UnitType.Throw:
                            break;
                        default:
                            next = currentUnit + 1;
                            break;

                    }
                    u = units[currentUnit];
                    if (u.Type == UnitType.Break || u.Type == UnitType.Continue || u.Type == UnitType.GoTo || u.Type == UnitType.Return)
                    {
                        CodeUnit currentTry;
                        tryes.TryPeek(out currentTry);
                        if (currentTry != null && currentTry.FalseNext != null && (next < currentTry.Index || next >= currentTry.FalseNext.Index) && units[currentTry.FalseNext.Index - 1].Type == UnitType.Finally)
                        {
                            falseNext = next;
                            trueNext = currentTry.FalseNext.Index;
                            next = -1;
                        }
                    }

                    if (next >= 0 && next < totalUnits) { u.Next = units[next]; u.Forward = u.Type == UnitType.Try ? false : true; }
                    if (trueNext >= 0 && trueNext < totalUnits) u.TrueNext = units[trueNext];
                    if (falseNext >= 0 && falseNext < totalUnits) u.FalseNext = units[falseNext];
                    u.InCatch = inCatch;
                    u.InFinally = inFinally;



                    tryes.TryPeek(out u.Try);
                    currentUnit++;

                }
                fn.ForEachCount = forEachNum;
                fn.ReapplyCount = reapplyNum;
            }
            catch (Exception ex)
            {
                throw new ScriptLoadingException(ErrMsgWithLoc(ex.Message, units[currentUnit].CodeLocation ?? fn.LocationMark));
            }
        }

        private static int FindUnit(CodeUnit[] units, UnitType[] uType, int pos, bool reverse = false, bool ignoreNesting = false, int limit = -1, bool totalIgnoreNesting = false)
        {
            if (limit < 0) limit = units.Length;
            int cur = pos;
            int nesting = pos >= 0 ? units[pos].Nesting : 0;
            UnitType t;
            do
            {
                if (reverse) cur--; else cur++;
                if (cur < 0 || cur >= limit || (!totalIgnoreNesting && !ignoreNesting && units[cur].Nesting < nesting)) return -1;
                t = units[cur].Type;
                if (uType.Any((x) => x == t) && (units[cur].Nesting == nesting || totalIgnoreNesting || (ignoreNesting && units[cur].Nesting <= nesting))) return cur;

            } while (true);

        }

        public static string GetFuncSign(string fnName, EvalUnit[] args)
        {
            if (!(args != null && (args[0].Kind != EvalUnitKind.Empty || args.Length > 1))) return fnName;

            string fnSign = fnName;
            if (args != null && (args[0].Kind != EvalUnitKind.Empty || args.Length > 1))
            {
                for (int i = 0; i < args.Length; i++)
                {

                    fnSign += " " + SimplifyTypeName(args[i].Type.Name);
                }
            }

            return fnSign;
        }

        static string SimplifyTypeName(string typeName)
        {
            if (typeName.IndexOf('/') >= 0) return typeName;

            int i = typeName.IndexOf('.');
            if (i >= 0)
                typeName = typeName.Substring(i + 1);

            return typeName;
        }

        public FuncToCall<T>[] GetServiceFuncs<T>(string fnName, EvalUnit[] args)
        {
            List<ScriptFunction> funcs = new List<ScriptFunction>();

            if (fnName == InstanceFuncName || fnName == StaticFuncName)
            {

                ScriptFunction[] f = FindSuitableFunc(fnName, args);
                for (int i = f.Length - 1; i >= 0; i--)
                {

                    var func = f[i];
                    funcs.Add(func);

                }
                return GetFuncsToCall<T>(funcs.ToArray());
            }
            else
            {
                if (fnName == FinalizeFuncName)
                {

                    ScriptFunction[] f = FindSuitableFunc(fnName, args);

                    for (int i = 0; i < f.Length; i++)
                    {
                        var func = f[i];
                        funcs.Add(func);

                    }
                    return GetFuncsToCall<T>(funcs.ToArray());
                }
            }

            throw new ScriptLoadingException($"Service function '{fnName}' not found.");
        }
        public void CheckOverrides()
        {
            int lastIndex = Functions.Count - 1;
            for (int i = 0; i < lastIndex; i++)
            {
                var f = Functions[i];
                if (f.IsOverride)
                {


                    ScriptFunction prev = null;
                    for (int j = i + 1; j <= lastIndex; j++)
                    {
                        if (Functions[j].Signature == f.Signature)
                        {
                            prev = Functions[j];
                            break;
                        }
                    }

                    if (prev == null) continue;
                    if (prev.IsSealed || (!prev.IsVirtual && !prev.IsOverride))
                        throw new ScriptLoadingException(ErrMsgWithLoc($"No suitable method '{f.Name}' found to override.", f.LocationMark));

                }
            }

            foreach (var c in SubClasses)
                c.CheckOverrides();
        }

        static ScriptFunction[] SelectVirt(ScriptFunction[] fn, ScriptClass context)
        {

            int skip = 0;
            int lastIndex = fn.Length - 1;
            ScriptFunction overrided = null;
            bool c;
            for (int i = 0; i <= lastIndex; i++)
            {

                var f = fn[i];
                if (overrided == null || overrided.IsOverride)
                {

                    if (f.IsOverride && i < lastIndex)
                    {
                        int p = i + 1;
                        var next = fn[p];

                        fn[p] = f;
                        overrided = next;
                    }
                    else if (overrided != null)
                    {
                        f = overrided;
                        overrided = null;

                    }

                }
                else
                {

                    f = overrided;
                    overrided = null;
                }
                c = context.Is(f.Class);

                if (c)
                {
                    skip = i;
                    break;
                }

            }


            if (skip == 0) return fn;

            var newFn = new ScriptFunction[fn.Length - skip];
            Array.Copy(fn, skip, newFn, 0, newFn.Length);

            return newFn;

        }

        public ScriptFunction GetFunc(string fnName, EvalUnit[] args, bool noException = false, bool? isPublic = null, bool? isStatic = null, TypeID returnType = TypeID.None, int exactParamCount = -1, ScriptClass virtClass = null)
        {
            if (fnName[0] == FunctionLayerPrefix) return DirectGetFunc(fnName, args, noException, isPublic, isStatic, returnType, exactParamCount, virtClass);
            var key = new GetFuncCacheKey(fnName, args, isPublic, isStatic, returnType, exactParamCount, virtClass);
            ScriptFunction f;
            if (!GetFuncCache.TryGetValue(key, out f))
            {
                f = DirectGetFunc(fnName, args, noException, isPublic, isStatic, returnType, exactParamCount, virtClass);

                if (f != null) GetFuncCache.Add(key, f);
            }
            return f;
        }

        public ScriptFunction DirectGetFunc(string fnName, EvalUnit[] args, bool noException = false, bool? isPublic = null, bool? isStatic = null, TypeID returnType = TypeID.None, int exactParamCount = -1, ScriptClass virtClass = null)
        {

            int layer = CurScript?.CurrentBuildFn == null ? 0 : CurScript.FnLayer;
            int fnIndexOrig;
            int fnIndex = fnIndexOrig = fnName.LastIndexOf(FunctionLayerPrefix) + 1;
            if (fnIndex > 0) fnName = fnName.Substring(fnIndex);

            int lim = fnIndex > 0 || virtClass != null ? Functions.Count : 1;

            ScriptFunction[] fn = FindSuitableFunc(fnName, args, isPublic, isStatic, returnType, exactParamCount).Take(lim).ToArray();

            if (virtClass != null)
                fn = SelectVirt(fn, virtClass);


            if (fnIndex > 0) fnIndex += layer;
            if (fn.Length == 0 || fnIndex >= fn.Length)
                if (noException) return null; else FuncNotFound();

            var f = fn[fnIndex];

            return f;



            void FuncNotFound()
            {
                string fnSign = new String(FunctionLayerPrefix, fnIndexOrig) + FormatFuncSign(GetFuncSign(fnName, args));
                throw new ScriptLoadingException($"Function '{fnSign}' not found at '{ClassFullName}'.");
            }
        }
        public FuncToCall<T> GetFunc<T>(string fnName, EvalUnit[] args, bool noException = false, bool? isPublic = null, bool? isStatic = null, TypeID returnType = TypeID.None, int exactParamCount = -1)
        {
            var fn = GetFunc(fnName, args, noException, isPublic, isStatic, returnType, exactParamCount);
            if (fn == null) return null;
            return fn.IsVirtual || fn.IsOverride ? GetVirtFuncToCall<T>(fnName, args, isPublic, isStatic, returnType, exactParamCount) : GetFuncToCall<T>(fn);

        }
        public static FuncToCall<T>[] GetFuncsToCall<T>(ScriptFunction[] funcs)
        {
            if (funcs == null || funcs.Length == 0) return null;
            List<FuncToCall<T>> ftc = new List<FuncToCall<T>>();
            foreach (var f in funcs)
                ftc.Add(GetFuncToCall<T>(f));


            return ftc.ToArray();
        }
        public static FuncToCall<T> GetFuncToCall<T>(ScriptFunction f) => f == null ? null : (EvalUnit[] fnArgs, int baseScope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ExecuteFunction<T>(f, fnArgs, baseScope, srcInst, inst, cstack, csrc);

        struct GetFuncCacheKey
        {
            string FnName;
            Arg[] Args;

            bool? IsPublic;
            bool? IsStatic;
            TypeID ReturnType;
            int ExactParamCount;
            ScriptClass VirtClass;
            int? HashCode;

            struct Arg
            {
                public string TypeName;
                public EvalUnitKind Kind;
                public Arg(string typeName, EvalUnitKind kind)
                {
                    TypeName = typeName;
                    Kind = kind;
                }
            }
            void SetArgs(EvalUnit[] args)
            {
                Args = new Arg[args.Length];
                for (int i = 0; i < args.Length; i++)
                {
                    Args[i].TypeName = args[i].Type.Name;
                    Args[i].Kind = args[i].Kind;
                }
            }
            public GetFuncCacheKey(string fnName, EvalUnit[] args, bool? isPublic = null, bool? isStatic = null, TypeID returnType = TypeID.None, int exactParamCount = -1, ScriptClass virtClass = null)
            {
                HashCode = null;
                FnName = fnName;

                IsPublic = isPublic;
                IsStatic = isStatic;
                ReturnType = returnType;
                ExactParamCount = exactParamCount;
                VirtClass = virtClass;
                Args = null;
                if (args != null) SetArgs(args);

            }
            public override int GetHashCode()
            {
                if (HashCode != null) return HashCode.Value;
                int hash = 17;
                unchecked
                {

                    hash = hash * 23 + FnName.GetHashCode();

                    hash = hash * 23 + IsPublic.GetHashCode();
                    hash = hash * 23 + IsStatic.GetHashCode();
                    hash = hash * 23 + ReturnType.GetHashCode();
                    hash = hash * 23 + ExactParamCount.GetHashCode();
                    if (VirtClass != null) hash = hash * 23 + VirtClass.GetHashCode();
                    if (Args != null)
                    {
                        for (int i = 0; i < Args.Length; i++)
                        {
                            hash = hash * 23 + Args[i].TypeName.GetHashCode();
                            hash = hash * 23 + Args[i].Kind.GetHashCode();
                        }
                    }

                }
                HashCode = hash;
                return hash;
            }
            public override bool Equals(Object obj)
            {
                if ((obj == null) || !this.GetType().Equals(obj.GetType()))
                {
                    return false;
                }
                else
                {
                    GetFuncCacheKey key = (GetFuncCacheKey)obj;
                    if (Args != null && key.Args != null)
                    {
                        if (Args.Length != key.Args.Length) return false;
                        for (int i = 0; i < Args.Length; i++)
                        {
                            if (Args[i].TypeName != key.Args[i].TypeName) return false;
                            if (Args[i].Kind != key.Args[i].Kind) return false;
                        }
                    }
                    else if (Args != key.Args) return false;

                    return (FnName == key.FnName) && (IsPublic == key.IsPublic) && (IsStatic == key.IsStatic) && (ReturnType == key.ReturnType) && (ExactParamCount == key.ExactParamCount) && (VirtClass == key.VirtClass);
                }
            }
        }
        Dictionary<GetFuncCacheKey, ScriptFunction> GetFuncCache = new Dictionary<GetFuncCacheKey, ScriptFunction>();
        public FuncToCall<T> GetVirtFuncToCall<T>(string fnName, EvalUnit[] args, bool? isPublic = null, bool? isStatic = null, TypeID returnType = TypeID.None, int exactParamCount = -1)
        {
            if (fnName.StartsWith(FunctionLayerPrefix)) throw new NotSupportedException($"Prefix '{FunctionLayerPrefix}' not supported for virtual functions.");
            return (EvalUnit[] fnArgs, int baseScope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) =>
            {



                var f = inst.Class.GetFunc(fnName, args, true, isPublic, isStatic, returnType, exactParamCount, this);
                if (f == null) throw new ArgumentException($"Failed to call virtual function '{FormatFuncSign(GetFuncSign(fnName, args))}' because suitable function not found at '{inst.Class.ClassFullName}'.");



                T result = ExecuteFunction<T>(f, fnArgs, baseScope, srcInst, inst, cstack, csrc);
                return result;
            };
        }
        class FuncMatching
        {
            public ScriptFunction Fn;
            public int Matching;
            public bool NotMatch;
            public FuncMatching(ScriptFunction fn)
            {
                Fn = fn;
                Matching = 0;
                NotMatch = false;
            }
        }

        ScriptFunction[] FindSuitableFunc(string fnName, EvalUnit[] args, bool? isPublic = null, bool? isStatic = null, TypeID returnType = TypeID.None, int exactParamCount = -1)
        {
            int argCount = args == null ? 0 : args.Length;
            var funcs = Functions.Where(x => x.Name == fnName && (returnType == TypeID.None || x.ReturnType.ID == returnType) && (isPublic == null || x.IsPublic == isPublic) && (isStatic == null || x.IsStatic == isStatic) && (exactParamCount < 0 ? x.HasParams || (argCount <= x.Params.Length && argCount >= x.MinArgNum) : x.Params.Length == exactParamCount)).Select(x => new FuncMatching(x));
            var groups = funcs.GroupBy(x => x.Fn.Params.Count()).OrderBy(x => x.Key).ToArray();
            foreach (var g in groups)
            {
                var fm = g.ToArray();
                foreach (var f in fm)
                {
                    if (args != null)
                    {
                        var prm = f.Fn.Params;

                        for (int n = 0; n < args.Length; n++)
                        {
                            var a = args[n];
                            var p = n < prm.Length ? prm[n] : prm.Last();
                            var pt = f.Fn.HasParams && n >= f.Fn.ParamsIndex ? f.Fn.ParamsType : p.ParamType;


                            int lvl;
                            if (f.Fn.HasParams && n == f.Fn.ParamsIndex && n == args.Length - 1 && TypeIsArray(a.Type.ID) && ArgIsValid(p.ParamType, a.Type, out lvl)) { f.Matching += lvl; continue; }
                            if (a.Type.IsByRef && !p.ByRef) { f.NotMatch = true; break; }
                            if (p.ByRef && ((a.Kind != EvalUnitKind.Variable && !a.Type.IsByRef) || a.Type.ID != pt.ID || a.Type.CType != pt.CType)) { f.NotMatch = true; break; }
                            bool argIsEmpty = a.Kind == EvalUnitKind.Empty;
                            if (argIsEmpty && !p.Optional) { f.NotMatch = true; break; }
                            if (argIsEmpty || (a.Type.ID == pt.ID && pt.TypeHint == null && a.Type.ID != TypeID.Custom && a.Type.ID != TypeID.CustomArray)) continue;


                            if (ArgIsValid(pt, a.Type, out lvl))
                                f.Matching += lvl;
                            else
                            { f.NotMatch = true; break; }

                        }

                    }
                    else if (f.Fn.MinArgNum > 0)
                        f.NotMatch = true;

                }

                var mostMatchingFunc = fm.Where(x => !x.NotMatch).OrderBy(x => x.Matching).FirstOrDefault();
                if (mostMatchingFunc != null)
                {
                    var identicalFuncs = fm.Where(x => x.Fn.Signature == mostMatchingFunc.Fn.Signature).Select(x => x.Fn).ToArray();
                    return identicalFuncs;
                }
            }

            return new ScriptFunction[0];
        }

        public static bool ArgIsValid(VarType p, VarType a, bool strict = false)
        {
            int lvl;
            return ArgIsValid(p, a, out lvl, strict);
        }

        public static bool ArgIsValid(VarType p, VarType a, out int lvl, bool strict = false)
        {
            const int objAssignLvl = 1000;
            lvl = -1;
            var prmType = p.ID;
            var argType = a.ID;
            bool argIsNull = a.IsNull;
            if (prmType == TypeID.Empty)
                throw new ScriptLoadingException("Validation for an empty parameter is not supported.");
            else if (prmType == TypeID.OfRefType && !a.T.IsValueType)
                lvl = 0;
            else if (prmType == TypeID.HintType && a.IsHintType)
                lvl = 0;
            else if (prmType == TypeID.HintArrayType && a.IsHintType && a.IsHintArray)
                lvl = 0;
            else if (prmType == TypeID.Array && (argIsNull || TypeIsArray(argType)))
                lvl = 0;
            else if (prmType == TypeID.Type && argType == TypeID.Object && a.SubTypeID != TypeID.None)
                lvl = 0;
            else if (argType == TypeID.Object && ((prmType == TypeID.CustomType && a.SubTypeID == TypeID.CustomType) || (prmType == TypeID.BasicType && a.SubTypeID != TypeID.None && a.SubTypeID != TypeID.CustomType)))
                lvl = 0;
            else if (TypeIdIsType(prmType) && argType == TypeID.Object && a.SubTypeID == prmType)
                lvl = 0;

            else if (prmType == TypeID.Object)
            {
                if (p.TypeHint == null)
                    lvl = argType == prmType ? 0 : objAssignLvl;
                else if (argIsNull)
                    lvl = 0;
                else if (a.TypeHint != null && argType == TypeID.Object)
                {


                    if (p.TypeHint == a.TypeHint) lvl = 0;
                    else if (p.TypeHint.IsAssignableFrom(a.TypeHint)) lvl = 0;
                }
                else if (argType != TypeID.Object)
                {
                    if (p.TypeHint.IsAssignableFrom(a.T)) lvl = objAssignLvl;
                }

            }
            else if (prmType == TypeID.ObjectArray && p.TypeHint != null)
            {
                if (argType == TypeID.ObjectArray && a.TypeHint != null)
                {
                    if (p.TypeHint == a.TypeHint) lvl = 0;
                    else if (p.TypeHint.IsAssignableFrom(a.TypeHint)) lvl = 0;
                }
            }
            else
            {

                if (prmType == TypeID.Custom || prmType == TypeID.CustomArray)
                {

                    if (argType != p.ID && !argIsNull) return false;

                    lvl = a.CType == null || p.CType == null ? 0 : IsLvl(a.CType.Class, p.CType.Class);
                }
                else
                {

                    bool prmTypeIsArray = TypeIsArray(prmType);
                    if (TypeIsNumeric(prmType) && (TypeIsNumeric(argType) || (argType == TypeID.Char && (prmType == TypeID.Int || prmType == TypeID.Long))))
                    {

                        lvl = TypeOrder[prmType] - TypeOrder[argType];
                        if (lvl < 0 && !strict) lvl = 100 - lvl;
                    }
                    else
                        lvl = prmType == argType || (argIsNull && (prmTypeIsArray || prmType == TypeID.String)) ? 0 : -1;
                }

            }
            return lvl >= 0;
        }
        private static int IsLvl(ScriptClass c, ScriptClass f)
        {
            int lvl = 0;
            if (c == f) return lvl;
            lvl++;
            if (c.InheritedClasses.Contains(f)) return lvl;

            for (int i = 0; i < c.InheritedClasses.Count; i++)
            {
                int l = IsLvl(c.InheritedClasses[i], f);
                if (l >= 0) return lvl + l;

            }
            return -1;
        }


        private static int SmartCharPos(string str, char c = '.', int start = 0)
        {
            int i = str.IndexOf(c, start);
            while (i >= 0 && InBrackets(str, i)) i = str.EIndexOf(c, i + 1);
            return i;
        }
        private static int SmartCharPosRev(string str, char c = '.', int start = -1)
        {
            if (start < 0) start = str.Length - 1;
            int i = str.LastIndexOf(c, start);
            while (i >= 0 && InBrackets(str, i)) i = str.ELastIndexOf(c, i - 1);
            return i;
        }
        public static string FormatFuncSign(string fnSign)
        {
            int i = fnSign.IndexOf(' ');
            if (i >= 0)
            {
                fnSign = fnSign.Insert(i, "(");
                fnSign = fnSign.Remove(i + 1, 1);
                fnSign = fnSign.TrimEnd();
                fnSign = fnSign.Replace(" ", ", ");
                fnSign = fnSign.Replace('*', ' ');
                fnSign = fnSign + ")";
            }
            else fnSign = fnSign + "()";
            return fnSign;
        }
        public static string RestoreCode(string s)
        {

            s = ReReplaceOperators(s);
            int i = s.IndexOf(LiteralMark);
            int i2, n, id;
            string t = "";
            string lit;
            while (i >= 0)
            {
                i2 = s.IndexOf(LiteralMark, i + 1);
                lit = s.Substring(i + 1, i2 - (i + 1));
                n = Int32.Parse(lit);
                id = Int32.Parse(lit.Substring(1));
                switch (LiteralTypes[n])
                {

                    case TypeID.String: t = "\"" + System.Web.HttpUtility.JavaScriptStringEncode(GetLiteral<string>(id)) + "\""; break;
                    case TypeID.Char: t = "'" + System.Web.HttpUtility.JavaScriptStringEncode((GetLiteral<char>(id)).ToString()) + "'"; break;
                    case TypeID.Float: t = GetLiteral<float>(id).ToString(System.Globalization.CultureInfo.InvariantCulture); break;
                    case TypeID.Double: t = GetLiteral<double>(id).ToString(System.Globalization.CultureInfo.InvariantCulture); break;
                    case TypeID.Long: t = GetLiteral<long>(id).ToString(); break;
                    case TypeID.Decimal: t = GetLiteral<decimal>(id).ToString(System.Globalization.CultureInfo.InvariantCulture); break;
                    case TypeID.Int: t = GetLiteral<int>(id).ToString(); break;
                    case TypeID.Object: t = $"#{GetLiteral<object>(id)}#"; break;
                    default: throw new ScriptExecutionException("Literal not found.");
                }
                s = s.Substring(0, i) + t + s.Substring(i2 + 1);
                i = s.IndexOf(LiteralMark, i);
            }
            return s;
        }

    }
}
