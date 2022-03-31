using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using static OverScript.Executor;
using static OverScript.ScriptClass;

namespace OverScript
{


    public partial class BasicFunctions
    {
        static BasicFunctions()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        }
        static readonly Random RandGen = new Random();
        static readonly object RandGenLock = new object();





        static FuncToCall<T1> ForceCast<T2, T1>(FuncToCall<T2> f)
        {
            try
            {
                return (FuncToCall<T1>)(object)f;
            }
            catch
            {
                throw new ScriptLoadingException($"Could't convert function type '{GetTypeName(TypeIds[typeof(T2)])}' to type '{GetTypeName(TypeIds[typeof(T1)])}'.");
            }
        }

        class BFuncMatching
        {
            public OverloadVariant Fn;
            public int Matching;
            public bool NotMatch;
            public BFuncMatching(OverloadVariant fn)
            {
                Fn = fn;
                Matching = 0;
                NotMatch = false;
            }
        }

        public static FuncToCall<T2> GetFTC<T, T2>(object method)
        {
            var m = (FuncToCall<T>)method;
            return ForceCast<T, T2>(m);
        }
        public static FuncToCall<T> GetFTC<T>(OverloadVariant ov)
        {
            switch (ov.ReturnType)
            {

                case TypeID.Int: return GetFTC<int, T>(ov.Method);
                case TypeID.Bool: return GetFTC<bool, T>(ov.Method);
                case TypeID.String: return GetFTC<string, T>(ov.Method);
                case TypeID.Char: return GetFTC<char, T>(ov.Method);
                case TypeID.Decimal: return GetFTC<decimal, T>(ov.Method);
                case TypeID.Long: return GetFTC<long, T>(ov.Method);
                case TypeID.Void:
                case TypeID.Object: return GetFTC<object, T>(ov.Method);
                case TypeID.Double: return GetFTC<double, T>(ov.Method);
                case TypeID.Float: return GetFTC<float, T>(ov.Method);
                case TypeID.Byte: return GetFTC<byte, T>(ov.Method);
                case TypeID.Short: return GetFTC<short, T>(ov.Method);
                case TypeID.Date: return GetFTC<DateTime, T>(ov.Method);

                case TypeID.IntArray: return GetFTC<int[], T>(ov.Method);
                case TypeID.BoolArray: return GetFTC<bool[], T>(ov.Method);
                case TypeID.StringArray: return GetFTC<string[], T>(ov.Method);
                case TypeID.CharArray: return GetFTC<char[], T>(ov.Method);
                case TypeID.DecimalArray: return GetFTC<decimal[], T>(ov.Method);
                case TypeID.LongArray: return GetFTC<long[], T>(ov.Method);
                case TypeID.ObjectArray: return GetFTC<object[], T>(ov.Method);
                case TypeID.DoubleArray: return GetFTC<double[], T>(ov.Method);
                case TypeID.FloatArray: return GetFTC<float[], T>(ov.Method);
                case TypeID.ByteArray: return GetFTC<byte[], T>(ov.Method);
                case TypeID.ShortArray: return GetFTC<short[], T>(ov.Method);
                case TypeID.DateArray: return GetFTC<DateTime[], T>(ov.Method);
                case TypeID.CustomArray:
                case TypeID.Custom: return GetFTC<CustomObject, T>(ov.Method);

                default:
                    throw new ScriptExecutionException($"Wrong type {ov.ReturnType}.");
            }
        }
        public bool TryGetFuncType(string name, EvalUnit[] args, out OverloadVariant ov, ScriptClass cc)
        {
            ov = null;
            var f = GetFunc(name, ref args, true, cc);
            if (f == null) return false;

            ov = f;
            return true;
        }

        public FuncToCall<T> GetFunc<T>(string name, ref EvalUnit[] args, ScriptClass cc)
        {
            var f = GetFunc(name, ref args, false, cc);
            return GetFTC<T>(f);
        }

        public OverloadVariant GetFunc(string name, ref EvalUnit[] args, bool noException, ScriptClass cc)
        {

            BF bf;
            if (!BasicFuncs.TryGetValue(name, out bf))
                if (noException) return null;
                else throw new ScriptLoadingException($"Basic function '{name}' not found.");

            if (name.StartsWith("op_") && name.EndsWith("Casting"))
            {
                bool isCustomArrayCast = name == "op_ArrayCasting";
                bool customCast = name == "op_Casting" || isCustomArrayCast;
                if (customCast && args.Length < 2)
                {
                    var vt = GetVarTypeOfCustomType(cc, (isCustomArrayCast ? true : false));
                    EvalUnit typeUnit = new EvalUnit();
                    typeUnit.SpecificValue = vt.CType;
                    typeUnit.Kind = EvalUnitKind.CustomType;
                    typeUnit.Type = vt;
                    args = new EvalUnit[] { typeUnit, args[0] };
                }
                bool isByHintArrayCast = name == "op_ByHintArrayCasting";
                bool hintCast = name == "op_ByHintCasting" || isByHintArrayCast;

                if (args.Length != (customCast || hintCast ? 2 : 1)) return null;
                int lvl;

                VarType castType, valType;
                if (customCast)
                {
                    castType = args[0].Type;
                    castType.ID = castType.CType.IsArray ? TypeID.CustomArray : TypeID.Custom;
                    valType = args[1].Type;
                }
                else if (hintCast)
                {
                    castType = args[1].Type;

                    castType.SetHintBySubType();
                    if (isByHintArrayCast) castType.SetType(TypeID.ObjectArray);
                    valType = args[0].Type;
                }
                else
                {
                    var tid = (TypeID)Enum.Parse(typeof(TypeID), name.Substring(3, name.Length - 10));
                    castType = GetVarTypeByID(tid);
                    valType = args[0].Type;
                }

                if ((castType.CType != null && valType.CType != null && castType.CType == valType.CType) || (!customCast && castType.ID == valType.ID && (castType.TypeHint == valType.TypeHint || (castType.TypeHint != null && valType.TypeHint != null && castType.TypeHint.IsAssignableFrom(valType.TypeHint)))))
                    throw new ScriptLoadingException($"Excess casting of type '{valType.Name}' to type '{castType.Name}'.");

                if ((valType.ID != TypeID.Object || valType.TypeHint != null) && !ArgIsValid(castType, valType, out lvl))
                {
                    bool err = true;
                    if (valType.TypeHint != null)
                    {
                        if (hintCast)
                        {
                            if (isByHintArrayCast)
                                err = valType.TypeHint != castType.TypeHint;
                            else
                                err = valType.TypeHint != castType.TypeHint && !CanCovert(valType.TypeHint, castType.TypeHint);
                        }
                        else
                            err = valType.TypeHint != castType.T && !CanCovert(valType.TypeHint, castType.T);

                    }
                    else if (isByHintArrayCast && valType.ID == TypeID.ObjectArray)
                        err = false;
                    else if (hintCast)
                        err = valType.T != castType.TypeHint && !CanCovert(valType.T, castType.TypeHint);

                    if (err)
                        throw new ScriptLoadingException($"Can't cast object of type '{valType.Name}' to type '{castType.Name}'.");

                }
                if (bf.OV.Count == 1) return bf.OV[0];

            }

            bool argsIsNull = args == null;
            int argCount = argsIsNull ? 0 : args.Length;

            var funcs = bf.OV.Where(x => !x.FewerArgsAllowed ? argCount == x.ParamTypes.Length || (argCount >= x.ParamTypes.Length && x.HasParams) : (argCount <= x.ParamTypes.Length || x.HasParams) && argCount >= x.MinArgsRequired).Select(x => new BFuncMatching(x));

            var groups = funcs.GroupBy(x => x.Fn.ParamTypes.Count() + (x.Fn.HasParams ? 1 : 0)).OrderBy(x => x.Key).ToArray();
            foreach (var g in groups)
            {
                var fm = g.ToArray();
                foreach (var f in fm)
                {
                    if (args != null)
                    {
                        var prm = f.Fn.ParamTypes;
                        for (int n = 0; n < args.Length; n++)
                        {

                            var a = args[n];
                            var pt = n < prm.Length ? prm[n] : f.Fn.TypeOfParams;
                            if (pt.Kind != null && a.Kind != pt.Kind)
                            {
                                f.NotMatch = true; break;
                            }

                            var t = pt.Type;

                            if (f.Fn.HasParams && n == f.Fn.ParamTypes.Length && n == args.Length - 1 && TypeIsArray(a.Type.ID) && f.Fn.ParamsArrType == a.Type.ID) continue;
                            bool prmIsNotCustom = pt.Type.ID != TypeID.Custom && pt.Type.ID != TypeID.CustomArray;
                            bool sameTypeId = a.Type.ID == t.ID;

                            int lvl;
                            if (a.Kind == EvalUnitKind.Empty || (sameTypeId && prmIsNotCustom)) continue;
                            if (t.ID == TypeID.Empty) { f.NotMatch = true; break; }

                            if (pt.IsStrict && prmIsNotCustom)
                            {
                                if (!sameTypeId) { f.NotMatch = true; break; }
                                continue;
                            }


                            if (ArgIsValid(t, a.Type, out lvl))
                                f.Matching += lvl;
                            else
                            {
                                f.NotMatch = true;
                                break;
                            }

                        }
                    }
                }

                var mostMatchingFunc = fm.Where(x => !x.NotMatch).OrderBy(x => x.Matching).FirstOrDefault();
                if (mostMatchingFunc != null) return mostMatchingFunc.Fn;

            }
            if (noException) return null; else throw new ScriptLoadingException($"Basic function '{FormatFuncSign(GetFuncSign(name, args))}' not found (the function exists but has inappropriate parameters).");
        }



        private static int ToInt_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalInt(scope, inst, cstack);
        private static bool ToBool_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalBool(scope, inst, cstack);
        private static byte ToByte_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalByte(scope, inst, cstack);
        private static short ToShort_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalShort(scope, inst, cstack);
        private static long ToLong_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalLong(scope, inst, cstack);
        private static float ToFloat_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalFloat(scope, inst, cstack);
        private static double ToDouble_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalDouble(scope, inst, cstack);
        private static decimal ToDecimal_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalDecimal(scope, inst, cstack);
        private static DateTime ToDate_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalDate(scope, inst, cstack);

        private static byte ToByte_string_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Convert.ToByte(fnArgs[0].EvalString(scope, inst, cstack), fnArgs[1].EvalInt(scope, inst, cstack));
        private static short ToShort_string_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Convert.ToInt16(fnArgs[0].EvalString(scope, inst, cstack), fnArgs[1].EvalInt(scope, inst, cstack));
        private static int ToInt_string_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Convert.ToInt32(fnArgs[0].EvalString(scope, inst, cstack), fnArgs[1].EvalInt(scope, inst, cstack));
        private static long ToLong_string_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Convert.ToInt64(fnArgs[0].EvalString(scope, inst, cstack), fnArgs[1].EvalInt(scope, inst, cstack));

        private static int TickCount(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Environment.TickCount;
        private static bool ArgExists(EvalUnit[] fnArgs, int n)
        {
            return fnArgs != null && fnArgs.Length > n && fnArgs[n].Kind != EvalUnitKind.Empty;
        }

        private static DateTime Now(bool utc, bool dateOnly)
        {
            DateTime d = utc ? DateTime.UtcNow : DateTime.Now;
            return dateOnly ? d.Date : d;
        }
        private static DateTime Now_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Now(fnArgs[0].EvalBool(scope, inst, cstack), fnArgs[1].EvalBool(scope, inst, cstack));
        private static DateTime Now_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalBool(scope, inst, cstack) ? DateTime.UtcNow : DateTime.Now;
        private static DateTime Now(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => DateTime.Now;
        private static DateTime ToLocalTime_date(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalDate(scope, inst, cstack).ToLocalTime();

        private static DateTime ToUniversalTime_date(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalDate(scope, inst, cstack).ToUniversalTime();
        private static string CurrentDirectory(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Environment.CurrentDirectory;
        private static object SetCurrentDirectory_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { Environment.CurrentDirectory = GetFullPath(fnArgs[0].EvalString(scope, inst, cstack), cstack, csrc); return null; }

        private static object SetBasePath_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { cstack.SetBasePath(GetFullPath(fnArgs[0].EvalString(scope, inst, cstack), cstack, csrc)); return null; }
        private static string BasePath(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetBasePath(cstack, csrc);
        private static string CodeFile(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => csrc.CU.CodeLocation.File;


        private static string Substring_string_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).Substring(fnArgs[1].EvalInt(scope, inst, cstack));
        private static string Substring_string_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).Substring(fnArgs[1].EvalInt(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack));

        private static string Left_string_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            string s = fnArgs[0].EvalString(scope, inst, cstack);
            int len = fnArgs[1].EvalInt(scope, inst, cstack);
            if (len > s.Length) len = s.Length;
            return s.Substring(0, len);
        }
        private static string Right_string_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            string s = fnArgs[0].EvalString(scope, inst, cstack);
            int len = fnArgs[1].EvalInt(scope, inst, cstack);
            int start = s.Length - len;
            if (start < 0) start = 0;

            return s.Substring(start);
        }
        private static int StrLen_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            string s = fnArgs[0].EvalString(scope, inst, cstack);
            return s.Length;
        }
        private static int StrPos_string_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).IndexOf(fnArgs[1].EvalChar(scope, inst, cstack));
        private static int StrPos_string_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).IndexOf(fnArgs[1].EvalString(scope, inst, cstack));
        private static int StrPos_string_char_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).IndexOf(fnArgs[1].EvalChar(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack));
        private static int StrPos_string_string_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).IndexOf(fnArgs[1].EvalString(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack));
        private static int StrPos_string_char_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).IndexOf(fnArgs[1].EvalChar(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack), fnArgs[3].EvalInt(scope, inst, cstack));
        private static int StrPos_string_string_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).IndexOf(fnArgs[1].EvalString(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack), fnArgs[3].EvalInt(scope, inst, cstack));

        private static int StrPos_string_string_int_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).IndexOf(fnArgs[1].EvalString(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack), fnArgs[3].EvalInt(scope, inst, cstack), (StringComparison)fnArgs[4].EvalInt(scope, inst, cstack));
        private static int StrPos_string_string_int_int_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).IndexOf(fnArgs[1].EvalString(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack), fnArgs[3].EvalInt(scope, inst, cstack), (StringComparison)fnArgs[4].EvalObject(scope, inst, cstack));

        private static int StrPos_string_string_int_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).IndexOf(fnArgs[1].EvalString(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack), (StringComparison)fnArgs[3].EvalObject(scope, inst, cstack));
        private static int StrPos_string_string_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).IndexOf(fnArgs[1].EvalString(scope, inst, cstack), (StringComparison)fnArgs[2].EvalObject(scope, inst, cstack));
        private static int StrPos_string_string_int_int_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).IndexOf(fnArgs[1].EvalString(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack), fnArgs[3].EvalInt(scope, inst, cstack), fnArgs[4].EvalBool(scope, inst, cstack) ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture);
        private static int StrPos_string_string_int_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).IndexOf(fnArgs[1].EvalString(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack), fnArgs[3].EvalBool(scope, inst, cstack) ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture);
        private static int StrPos_string_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).IndexOf(fnArgs[1].EvalString(scope, inst, cstack), fnArgs[2].EvalBool(scope, inst, cstack) ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture);

        private static int StrRPos_string_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).LastIndexOf(fnArgs[1].EvalChar(scope, inst, cstack));
        private static int StrRPos_string_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).LastIndexOf(fnArgs[1].EvalString(scope, inst, cstack));
        private static int StrRPos_string_char_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).LastIndexOf(fnArgs[1].EvalChar(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack));
        private static int StrRPos_string_string_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).LastIndexOf(fnArgs[1].EvalString(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack));
        private static int StrRPos_string_char_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).LastIndexOf(fnArgs[1].EvalChar(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack), fnArgs[3].EvalInt(scope, inst, cstack));
        private static int StrRPos_string_string_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).LastIndexOf(fnArgs[1].EvalString(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack), fnArgs[3].EvalInt(scope, inst, cstack));

        private static int StrRPos_string_string_int_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).LastIndexOf(fnArgs[1].EvalString(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack), fnArgs[3].EvalInt(scope, inst, cstack), (StringComparison)fnArgs[4].EvalInt(scope, inst, cstack));
        private static int StrRPos_string_string_int_int_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).LastIndexOf(fnArgs[1].EvalString(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack), fnArgs[3].EvalInt(scope, inst, cstack), (StringComparison)fnArgs[4].EvalObject(scope, inst, cstack));

        private static int StrRPos_string_string_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).LastIndexOf(fnArgs[1].EvalString(scope, inst, cstack), (StringComparison)fnArgs[2].EvalObject(scope, inst, cstack));
        private static int StrRPos_string_string_int_int_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).LastIndexOf(fnArgs[1].EvalString(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack), fnArgs[3].EvalInt(scope, inst, cstack), fnArgs[4].EvalBool(scope, inst, cstack) ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture);
        private static int StrRPos_string_string_int_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).LastIndexOf(fnArgs[1].EvalString(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack), fnArgs[3].EvalBool(scope, inst, cstack) ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture);
        private static int StrRPos_string_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).LastIndexOf(fnArgs[1].EvalString(scope, inst, cstack), fnArgs[2].EvalBool(scope, inst, cstack) ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture);
        private static int IndexOfAny_string_charArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).IndexOfAny(fnArgs[1].EvalCharArray(scope, inst, cstack));
        private static int IndexOfAny_string_charArray_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).IndexOfAny(fnArgs[1].EvalCharArray(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack));
        private static int IndexOfAny_string_charArray_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).IndexOfAny(fnArgs[1].EvalCharArray(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack), fnArgs[3].EvalInt(scope, inst, cstack));
        private static int LastIndexOfAny_string_charArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).LastIndexOfAny(fnArgs[1].EvalCharArray(scope, inst, cstack));
        private static int LastIndexOfAny_string_charArray_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).LastIndexOfAny(fnArgs[1].EvalCharArray(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack));
        private static int LastIndexOfAny_string_charArray_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).LastIndexOfAny(fnArgs[1].EvalCharArray(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack), fnArgs[3].EvalInt(scope, inst, cstack));
        private static string Trim_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).Trim();
        private static string Trim_string_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).Trim(fnArgs[1].EvalChar(scope, inst, cstack));
        private static string TrimStart_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).TrimStart();
        private static string TrimStart_string_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).TrimStart(fnArgs[1].EvalChar(scope, inst, cstack));

        private static string TrimEnd_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).TrimEnd();
        private static string TrimEnd_string_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).TrimEnd(fnArgs[1].EvalChar(scope, inst, cstack));
        private static string ToUpper_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).ToUpper();
        private static string ToLower_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).ToLower();
        private static string ToUpperFirst_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            string s;
            s = fnArgs[0].EvalString(scope, inst, cstack);
            if (s.Length == 0) return "";
            return s.First().ToString().ToUpper() + s.Substring(1);
        }
        private static string ToLowerFirst_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            string s;
            s = fnArgs[0].EvalString(scope, inst, cstack);
            if (s.Length == 0) return "";
            return s.First().ToString().ToLower() + s.Substring(1);
        }
        private static string Replace_string_char_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).Replace(fnArgs[1].EvalChar(scope, inst, cstack), fnArgs[2].EvalChar(scope, inst, cstack));
        private static string Replace_string_string_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).Replace(fnArgs[1].EvalString(scope, inst, cstack), fnArgs[2].EvalString(scope, inst, cstack));

        private static string Replace_string_string_string_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).Replace(fnArgs[1].EvalString(scope, inst, cstack), fnArgs[2].EvalString(scope, inst, cstack), (StringComparison)fnArgs[3].EvalInt(scope, inst, cstack));
        private static string Replace_string_string_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).Replace(fnArgs[1].EvalString(scope, inst, cstack), fnArgs[2].EvalString(scope, inst, cstack), fnArgs[3].EvalBool(scope, inst, cstack) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        private static string Replace_string_string_string_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).Replace(fnArgs[1].EvalString(scope, inst, cstack), fnArgs[2].EvalString(scope, inst, cstack), (StringComparison)fnArgs[3].EvalObject(scope, inst, cstack));
        private static string RemoveStr_string_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).Remove(fnArgs[1].EvalInt(scope, inst, cstack));
        private static string RemoveStr_string_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).Remove(fnArgs[1].EvalInt(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack));

        private static string InsertStr_string_int_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).Insert(fnArgs[1].EvalInt(scope, inst, cstack), fnArgs[2].EvalString(scope, inst, cstack));
        private static string ToString_byte_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Convert.ToString(fnArgs[0].EvalByte(scope, inst, cstack), fnArgs[1].EvalInt(scope, inst, cstack));
        private static string ToString_short_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Convert.ToString(fnArgs[0].EvalShort(scope, inst, cstack), fnArgs[1].EvalInt(scope, inst, cstack));
        private static string ToString_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Convert.ToString(fnArgs[0].EvalInt(scope, inst, cstack), fnArgs[1].EvalInt(scope, inst, cstack));
        private static string ToString_long_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Convert.ToString(fnArgs[0].EvalLong(scope, inst, cstack), fnArgs[1].EvalInt(scope, inst, cstack));

        private static string ToString(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack);

        private static int Rand_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {

            int min = fnArgs[0].EvalInt(scope, inst, cstack);
            int max = fnArgs[1].EvalInt(scope, inst, cstack) + 1;
            lock (RandGenLock)
            {
                return RandGen.Next(min, max);
            }
        }

        private static bool StartsWith_string_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).StartsWith(fnArgs[1].EvalChar(scope, inst, cstack));
        private static bool StartsWith_string_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).StartsWith(fnArgs[1].EvalString(scope, inst, cstack));
        private static bool StartsWith_string_string_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).StartsWith(fnArgs[1].EvalString(scope, inst, cstack), (StringComparison)fnArgs[2].EvalInt(scope, inst, cstack));
        private static bool StartsWith_string_string_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).StartsWith(fnArgs[1].EvalString(scope, inst, cstack), (StringComparison)fnArgs[2].EvalObject(scope, inst, cstack));
        private static bool StartsWith_string_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).StartsWith(fnArgs[1].EvalString(scope, inst, cstack), fnArgs[2].EvalBool(scope, inst, cstack) ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture);
        private static bool StartsWith_string_string_bool_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).StartsWith(fnArgs[1].EvalString(scope, inst, cstack), fnArgs[2].EvalBool(scope, inst, cstack), (System.Globalization.CultureInfo)fnArgs[3].EvalObject(scope, inst, cstack));

        private static bool EndsWith_string_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).EndsWith(fnArgs[1].EvalChar(scope, inst, cstack));
        private static bool EndsWith_string_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).EndsWith(fnArgs[1].EvalString(scope, inst, cstack));
        private static bool EndsWith_string_string_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).EndsWith(fnArgs[1].EvalString(scope, inst, cstack), (StringComparison)fnArgs[2].EvalInt(scope, inst, cstack));
        private static bool EndsWith_string_string_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).EndsWith(fnArgs[1].EvalString(scope, inst, cstack), (StringComparison)fnArgs[2].EvalObject(scope, inst, cstack));
        private static bool EndsWith_string_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).EndsWith(fnArgs[1].EvalString(scope, inst, cstack), fnArgs[2].EvalBool(scope, inst, cstack) ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture);
        private static bool EndsWith_string_string_bool_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).EndsWith(fnArgs[1].EvalString(scope, inst, cstack), fnArgs[2].EvalBool(scope, inst, cstack), (System.Globalization.CultureInfo)fnArgs[3].EvalObject(scope, inst, cstack));
        private static string Format_string_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            string s = fnArgs[0].EvalString(scope, inst, cstack);
            object[] prms = EvalArgs(1, fnArgs, scope, inst, cstack);
            return String.Format(s, prms);
        }
        private static string Format_string_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            string s = fnArgs[0].EvalString(scope, inst, cstack);
            object obj = fnArgs[1].EvalObject(scope, inst, cstack);
            return String.Format(s, obj);
        }

        private static int Ord_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalChar(scope, inst, cstack);
        private static int Ord_string_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack)[fnArgs[1].EvalInt(scope, inst, cstack)];

        private static int Ord_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack)[0];

        private static char CharAt_string_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack)[fnArgs[1].EvalInt(scope, inst, cstack)];

        private static char ToChar_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalChar(scope, inst, cstack);

        private static int Asc_string_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Asc(fnArgs[0].EvalString(scope, inst, cstack)[0], Encoding.GetEncoding(fnArgs[1].EvalString(scope, inst, cstack)));
        private static int Asc_string_int_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Asc(fnArgs[0].EvalString(scope, inst, cstack)[fnArgs[1].EvalInt(scope, inst, cstack)], Encoding.GetEncoding(fnArgs[2].EvalString(scope, inst, cstack)));
        private static int Asc_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Asc(fnArgs[0].EvalString(scope, inst, cstack)[0], null);
        private static int Asc_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Asc(fnArgs[0].EvalChar(scope, inst, cstack), null);
        private static int Asc_string_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Asc(fnArgs[0].EvalString(scope, inst, cstack)[0], (Encoding)fnArgs[1].EvalObject(scope, inst, cstack));
        private static int Asc_string_int_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Asc(fnArgs[0].EvalString(scope, inst, cstack)[fnArgs[1].EvalInt(scope, inst, cstack)], (Encoding)fnArgs[2].EvalObject(scope, inst, cstack));
        private static int Asc_char_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Asc(fnArgs[0].EvalChar(scope, inst, cstack), (Encoding)fnArgs[1].EvalObject(scope, inst, cstack));
        private static int Asc_char_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Asc(fnArgs[0].EvalChar(scope, inst, cstack), Encoding.GetEncoding(fnArgs[1].EvalString(scope, inst, cstack)));
        private static int Asc(char c, Encoding enc)
        {
            if (enc == null) enc = Encoding.Latin1;

            byte[] bytes = Encoding.Default.GetBytes(new char[] { c });
            byte[] asciiBytes = Encoding.Convert(Encoding.Default, enc, bytes);
            return asciiBytes[0];
        }
        private static char Chr_int_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Chr(fnArgs[0].EvalByte(scope, inst, cstack), Encoding.GetEncoding(fnArgs[1].EvalString(scope, inst, cstack)));
        private static char Chr_int_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Chr(fnArgs[0].EvalByte(scope, inst, cstack), (Encoding)fnArgs[1].EvalObject(scope, inst, cstack));
        private static char Chr_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Chr(fnArgs[0].EvalByte(scope, inst, cstack), null);

        private static char Chr(byte v, Encoding enc)
        {
            if (enc == null) enc = Encoding.Latin1;
            return enc.GetChars(new byte[] { v })[0];
        }

        private static bool CharIsControl_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => char.IsControl(fnArgs[0].EvalChar(scope, inst, cstack));

        private static bool CharIsDigit_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => char.IsDigit(fnArgs[0].EvalChar(scope, inst, cstack));

        private static bool CharIsHighSurrogate_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => char.IsHighSurrogate(fnArgs[0].EvalChar(scope, inst, cstack));

        private static bool CharIsLetter_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => char.IsLetter(fnArgs[0].EvalChar(scope, inst, cstack));

        private static bool CharIsLetterOrDigit_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => char.IsLetterOrDigit(fnArgs[0].EvalChar(scope, inst, cstack));

        private static bool CharIsLower_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => char.IsLower(fnArgs[0].EvalChar(scope, inst, cstack));

        private static bool CharIsLowSurrogate_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => char.IsLowSurrogate(fnArgs[0].EvalChar(scope, inst, cstack));

        private static bool CharIsNumber_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => char.IsNumber(fnArgs[0].EvalChar(scope, inst, cstack));

        private static bool CharIsPunctuation_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => char.IsPunctuation(fnArgs[0].EvalChar(scope, inst, cstack));

        private static bool CharIsSeparator_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => char.IsSeparator(fnArgs[0].EvalChar(scope, inst, cstack));

        private static bool CharIsSurrogate_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => char.IsSurrogate(fnArgs[0].EvalChar(scope, inst, cstack));

        private static bool CharIsSurrogatePair_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => char.IsSurrogatePair(fnArgs[0].EvalChar(scope, inst, cstack), fnArgs[1].EvalChar(scope, inst, cstack));

        private static bool CharIsSymbol_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => char.IsSymbol(fnArgs[0].EvalChar(scope, inst, cstack));

        private static bool CharIsUpper_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => char.IsUpper(fnArgs[0].EvalChar(scope, inst, cstack));

        private static bool CharIsWhiteSpace_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => char.IsWhiteSpace(fnArgs[0].EvalChar(scope, inst, cstack));
        private static decimal Floor_decimal(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Math.Floor(fnArgs[0].EvalDecimal(scope, inst, cstack));
        private static float Floor_float(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (float)Math.Floor(fnArgs[0].EvalFloat(scope, inst, cstack));
        private static double Floor_double(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Math.Floor(fnArgs[0].EvalDouble(scope, inst, cstack));

        private static decimal Ceiling_decimal(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Math.Ceiling(fnArgs[0].EvalDecimal(scope, inst, cstack));
        private static float Ceiling_float(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (float)Math.Ceiling(fnArgs[0].EvalFloat(scope, inst, cstack));
        private static double Ceiling_double(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Math.Ceiling(fnArgs[0].EvalDouble(scope, inst, cstack));

        private static double Truncate_double(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Math.Truncate(fnArgs[0].EvalDouble(scope, inst, cstack));
        private static decimal Truncate_decimal(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Math.Truncate(fnArgs[0].EvalDecimal(scope, inst, cstack));


        private static decimal Round_decimal_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Math.Round(fnArgs[0].EvalDecimal(scope, inst, cstack), fnArgs[1].EvalInt(scope, inst, cstack), (MidpointRounding)fnArgs[2].EvalInt(scope, inst, cstack));
        private static float Round_float_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (float)Math.Round(fnArgs[0].EvalFloat(scope, inst, cstack), fnArgs[1].EvalInt(scope, inst, cstack), (MidpointRounding)fnArgs[2].EvalInt(scope, inst, cstack));
        private static double Round_double_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Math.Round(fnArgs[0].EvalDouble(scope, inst, cstack), fnArgs[1].EvalInt(scope, inst, cstack), (MidpointRounding)fnArgs[2].EvalInt(scope, inst, cstack));
        private static decimal Round_decimal_int_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Math.Round(fnArgs[0].EvalDecimal(scope, inst, cstack), fnArgs[1].EvalInt(scope, inst, cstack), (MidpointRounding)fnArgs[2].EvalObject(scope, inst, cstack));
        private static float Round_float_int_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (float)Math.Round(fnArgs[0].EvalFloat(scope, inst, cstack), fnArgs[1].EvalInt(scope, inst, cstack), (MidpointRounding)fnArgs[2].EvalObject(scope, inst, cstack));
        private static double Round_double_int_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Math.Round(fnArgs[0].EvalDouble(scope, inst, cstack), fnArgs[1].EvalInt(scope, inst, cstack), (MidpointRounding)fnArgs[2].EvalObject(scope, inst, cstack));
        private static decimal Round_decimal_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Math.Round(fnArgs[0].EvalDecimal(scope, inst, cstack), fnArgs[1].EvalInt(scope, inst, cstack));
        private static float Round_float_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (float)Math.Round(fnArgs[0].EvalFloat(scope, inst, cstack), fnArgs[1].EvalInt(scope, inst, cstack));
        private static double Round_double_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Math.Round(fnArgs[0].EvalDouble(scope, inst, cstack), fnArgs[1].EvalInt(scope, inst, cstack));
        private static decimal Round_decimal(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Math.Round(fnArgs[0].EvalDecimal(scope, inst, cstack));
        private static float Round_float(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (float)Math.Round(fnArgs[0].EvalFloat(scope, inst, cstack));
        private static double Round_double(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Math.Round(fnArgs[0].EvalDouble(scope, inst, cstack));
        private static decimal Abs_decimal(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Math.Abs(fnArgs[0].EvalDecimal(scope, inst, cstack));
        private static float Abs_float(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Math.Abs(fnArgs[0].EvalFloat(scope, inst, cstack));
        private static double Abs_double(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Math.Abs(fnArgs[0].EvalDouble(scope, inst, cstack));

        private static short Abs_short(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Math.Abs(fnArgs[0].EvalShort(scope, inst, cstack));
        private static int Abs_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Math.Abs(fnArgs[0].EvalInt(scope, inst, cstack));
        private static long Abs_long(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Math.Abs(fnArgs[0].EvalLong(scope, inst, cstack));
        private static double Sin_double(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Math.Sin(fnArgs[0].EvalDouble(scope, inst, cstack));
        private static double Cos_double(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Math.Cos(fnArgs[0].EvalDouble(scope, inst, cstack));
        private static double Tan_double(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Math.Tan(fnArgs[0].EvalDouble(scope, inst, cstack));
        private static double Exp_double(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Math.Exp(fnArgs[0].EvalDouble(scope, inst, cstack));
        private static double Log_double(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Math.Log(fnArgs[0].EvalDouble(scope, inst, cstack));
        private static double Log_double_double(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Math.Log(fnArgs[0].EvalDouble(scope, inst, cstack), fnArgs[1].EvalDouble(scope, inst, cstack));
        private static double Log10_double(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Math.Log10(fnArgs[0].EvalDouble(scope, inst, cstack));
        private static double Log2_double(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Math.Log2(fnArgs[0].EvalDouble(scope, inst, cstack));

        private static double Acos_double(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Math.Acos(fnArgs[0].EvalDouble(scope, inst, cstack));
        private static double Asin_double(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Math.Asin(fnArgs[0].EvalDouble(scope, inst, cstack));
        private static double Atan_double(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Math.Atan(fnArgs[0].EvalDouble(scope, inst, cstack));
        private static double Atan2_double_double(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Math.Atan2(fnArgs[0].EvalDouble(scope, inst, cstack), fnArgs[1].EvalDouble(scope, inst, cstack));

        private static double Pow_double_double(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Math.Pow(fnArgs[0].EvalDouble(scope, inst, cstack), fnArgs[1].EvalDouble(scope, inst, cstack));

        private static object Resize_intArray_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { Resize<int>(fnArgs, scope, inst, cstack); return null; }
        private static object Resize_longArray_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { Resize<long>(fnArgs, scope, inst, cstack); return null; }
        private static object Resize_floatArray_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { Resize<float>(fnArgs, scope, inst, cstack); return null; }
        private static object Resize_doubleArray_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { Resize<double>(fnArgs, scope, inst, cstack); return null; }
        private static object Resize_decimalArray_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { Resize<decimal>(fnArgs, scope, inst, cstack); return null; }
        private static object Resize_boolArray_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { Resize<bool>(fnArgs, scope, inst, cstack); return null; }
        private static object Resize_stringArray_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { Resize<string>(fnArgs, scope, inst, cstack); return null; }
        private static object Resize_charArray_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { Resize<char>(fnArgs, scope, inst, cstack); return null; }
        private static object Resize_objectArray_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { Resize<object>(fnArgs, scope, inst, cstack); return null; }
        private static object Resize_byteArray_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { Resize<byte>(fnArgs, scope, inst, cstack); return null; }
        private static object Resize_shortArray_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { Resize<short>(fnArgs, scope, inst, cstack); return null; }
        private static object Resize_dateArray_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { Resize<DateTime>(fnArgs, scope, inst, cstack); return null; }
        private static object Resize_customArray_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { Resize<CustomObject>(fnArgs, scope, inst, cstack, true); return null; }

        private static object Copy_object_object_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            Array.Copy(fnArgs[0].EvalArray(scope, inst, cstack), fnArgs[1].EvalArray(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack));
            return null;
        }
        private static object Copy_object_int_object_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            Array.Copy(fnArgs[0].EvalArray(scope, inst, cstack), fnArgs[1].EvalInt(scope, inst, cstack), fnArgs[2].EvalArray(scope, inst, cstack), fnArgs[3].EvalInt(scope, inst, cstack), fnArgs[4].EvalInt(scope, inst, cstack));
            return null;
        }
        private static void Resize<T>(EvalUnit[] fnArgs, int scope, ClassInstance inst, CallStack cstack, bool custom = false)
        {
            VarInfo varInfo = fnArgs[0].GetVarInfo(scope, inst, cstack);

            Executor exec = inst.Exec;
            CustomObject customArr = null;
            T[] arr = custom ? (T[])(customArr = ScriptVars<CustomObject>.Get(exec, varInfo.ID, varInfo.Scope)).Object : ScriptVars<T[]>.Get(exec, varInfo.ID, varInfo.Scope);

            int size = fnArgs[1].EvalInt(scope, inst, cstack);
            Array.Resize(ref arr, size);

            if (!custom) ScriptVars<T[]>.Set(exec, varInfo.ID, varInfo.Scope, ref arr);
            else customArr.Object = arr;



        }

        private static object Resize_object_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {

            VarInfo varInfo = fnArgs[0].GetVarInfo(scope, inst, cstack);

            Executor exec = inst.Exec;
            object obj = ScriptVars<object>.Get(exec, varInfo.ID, varInfo.Scope);

            Array arr = obj is CustomObject co ? (Array)co : (Array)obj;



            var et = arr.GetType().GetElementType();
            int size = fnArgs[1].EvalInt(scope, inst, cstack);
            var arr2 = Array.CreateInstance(et, size);
            arr.CopyTo(arr2, 0);
            object arr3 = arr2;
            ScriptVars<object>.Set(exec, varInfo.ID, varInfo.Scope, ref arr3);

            return null;
        }
        private static int Count_array(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalArray(scope, inst, cstack).Length;

        private static int Count_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            var arr = fnArgs[0].EvalObject(scope, inst, cstack);
            return GetCountOfUnknown(arr);
        }

        private static string Join_string_intArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Join<int>(fnArgs, scope, inst, cstack);
        private static string Join_string_longArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Join<long>(fnArgs, scope, inst, cstack);
        private static string Join_string_floatArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Join<float>(fnArgs, scope, inst, cstack);
        private static string Join_string_doubleArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Join<double>(fnArgs, scope, inst, cstack);
        private static string Join_string_decimalArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Join<decimal>(fnArgs, scope, inst, cstack);
        private static string Join_string_boolArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Join<bool>(fnArgs, scope, inst, cstack);
        private static string Join_string_stringArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Join<string>(fnArgs, scope, inst, cstack);
        private static string Join_string_charArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Join<char>(fnArgs, scope, inst, cstack);
        private static string Join_string_objectArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Join<object>(fnArgs, scope, inst, cstack);
        private static string Join_string_shortArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Join<short>(fnArgs, scope, inst, cstack);
        private static string Join_string_byteArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Join<byte>(fnArgs, scope, inst, cstack);
        private static string Join_string_dateArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Join<DateTime>(fnArgs, scope, inst, cstack);
        private static string Join_string_customArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => JoinCustom(fnArgs, scope, inst, cstack);
        private static string Join_string_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => String.Join(fnArgs[0].EvalString(scope, inst, cstack), Enumerable.Cast<object>((System.Collections.IEnumerable)fnArgs[1].EvalObject(scope, inst, cstack)));

        private static string Join_char_intArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Join2<int>(fnArgs, scope, inst, cstack);
        private static string Join_char_longArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Join2<long>(fnArgs, scope, inst, cstack);
        private static string Join_char_floatArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Join2<float>(fnArgs, scope, inst, cstack);
        private static string Join_char_doubleArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Join2<double>(fnArgs, scope, inst, cstack);
        private static string Join_char_decimalArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Join2<decimal>(fnArgs, scope, inst, cstack);
        private static string Join_char_boolArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Join2<bool>(fnArgs, scope, inst, cstack);
        private static string Join_char_stringArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Join2<string>(fnArgs, scope, inst, cstack);
        private static string Join_char_charArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Join2<char>(fnArgs, scope, inst, cstack);
        private static string Join_char_objectArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Join2<object>(fnArgs, scope, inst, cstack);
        private static string Join_char_shortArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Join2<short>(fnArgs, scope, inst, cstack);
        private static string Join_char_byteArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Join2<byte>(fnArgs, scope, inst, cstack);
        private static string Join_char_dateArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Join2<DateTime>(fnArgs, scope, inst, cstack);
        private static string Join_char_customArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => JoinCustom2(fnArgs, scope, inst, cstack);
        private static string Join_char_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => String.Join(fnArgs[0].EvalChar(scope, inst, cstack), Enumerable.Cast<object>((System.Collections.IEnumerable)fnArgs[1].EvalObject(scope, inst, cstack)));

        private static string Join<T>(EvalUnit[] fnArgs, int scope, ClassInstance inst, CallStack cstack)
        {
            T[] arr = fnArgs[1].Eval<T[]>(scope, inst, cstack);
            string separator = fnArgs[0].EvalString(scope, inst, cstack);
            return string.Join(separator, arr);
        }
        private static string Join2<T>(EvalUnit[] fnArgs, int scope, ClassInstance inst, CallStack cstack)
        {
            T[] arr = fnArgs[1].Eval<T[]>(scope, inst, cstack);
            char separator = fnArgs[0].EvalChar(scope, inst, cstack);
            return string.Join(separator, arr);
        }
        private static string JoinCustom(EvalUnit[] fnArgs, int scope, ClassInstance inst, CallStack cstack)
        {
            var arr = (object[])(CustomObject[])fnArgs[1].EvalCustomArray(scope, inst, cstack);
            string separator = fnArgs[0].EvalString(scope, inst, cstack);
            return string.Join(separator, arr);
        }
        private static string JoinCustom2(EvalUnit[] fnArgs, int scope, ClassInstance inst, CallStack cstack)
        {
            var arr = (object[])(CustomObject[])fnArgs[1].EvalCustomArray(scope, inst, cstack);

            char separator = fnArgs[0].EvalChar(scope, inst, cstack);
            return string.Join(separator, arr);
        }

        private static string[] Split_string_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).Split(fnArgs[1].EvalString(scope, inst, cstack));
        private static string[] Split_string_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).Split(fnArgs[1].EvalChar(scope, inst, cstack));
        private static string[] Split_string_string_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).Split(fnArgs[1].EvalString(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack));
        private static string[] Split_string_char_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).Split(fnArgs[1].EvalChar(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack));
        private static string[] Split_string_string_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).Split(fnArgs[1].EvalString(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack), (StringSplitOptions)fnArgs[3].EvalInt(scope, inst, cstack));
        private static string[] Split_string_char_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).Split(fnArgs[1].EvalChar(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack), (StringSplitOptions)fnArgs[3].EvalInt(scope, inst, cstack));
        private static string[] Split_string_string_int_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).Split(fnArgs[1].EvalString(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack), (StringSplitOptions)fnArgs[3].EvalObject(scope, inst, cstack));
        private static string[] Split_string_char_int_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).Split(fnArgs[1].EvalChar(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack), (StringSplitOptions)fnArgs[3].EvalObject(scope, inst, cstack));
        private static string[] Split_string_string_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).Split(fnArgs[1].EvalString(scope, inst, cstack), (StringSplitOptions)fnArgs[2].EvalInt(scope, inst, cstack));
        private static string[] Split_string_char_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalString(scope, inst, cstack).Split(fnArgs[1].EvalChar(scope, inst, cstack), (StringSplitOptions)fnArgs[2].EvalInt(scope, inst, cstack));


        private static int IndexOf_intArray_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.IndexOf(fnArgs[0].EvalIntArray(scope, inst, cstack), fnArgs[1].EvalInt(scope, inst, cstack));
        private static int IndexOf_intArray_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.IndexOf(fnArgs[0].EvalIntArray(scope, inst, cstack), fnArgs[1].EvalInt(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack));
        private static int IndexOf_intArray_int_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.IndexOf(fnArgs[0].EvalIntArray(scope, inst, cstack), fnArgs[1].EvalInt(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack), fnArgs[3].EvalInt(scope, inst, cstack));

        private static int IndexOf_longArray_long(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.IndexOf(fnArgs[0].EvalLongArray(scope, inst, cstack), fnArgs[1].EvalLong(scope, inst, cstack));
        private static int IndexOf_longArray_long_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.IndexOf(fnArgs[0].EvalLongArray(scope, inst, cstack), fnArgs[1].EvalLong(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack));
        private static int IndexOf_longArray_long_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.IndexOf(fnArgs[0].EvalLongArray(scope, inst, cstack), fnArgs[1].EvalLong(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack), fnArgs[3].EvalInt(scope, inst, cstack));

        private static int IndexOf_floatArray_float(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.IndexOf(fnArgs[0].EvalFloatArray(scope, inst, cstack), fnArgs[1].EvalFloat(scope, inst, cstack));
        private static int IndexOf_floatArray_float_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.IndexOf(fnArgs[0].EvalFloatArray(scope, inst, cstack), fnArgs[1].EvalFloat(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack));
        private static int IndexOf_floatArray_float_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.IndexOf(fnArgs[0].EvalFloatArray(scope, inst, cstack), fnArgs[1].EvalFloat(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack), fnArgs[3].EvalInt(scope, inst, cstack));

        private static int IndexOf_doubleArray_double(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.IndexOf(fnArgs[0].EvalDoubleArray(scope, inst, cstack), fnArgs[1].EvalDouble(scope, inst, cstack));
        private static int IndexOf_doubleArray_double_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.IndexOf(fnArgs[0].EvalDoubleArray(scope, inst, cstack), fnArgs[1].EvalDouble(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack));
        private static int IndexOf_doubleArray_double_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.IndexOf(fnArgs[0].EvalDoubleArray(scope, inst, cstack), fnArgs[1].EvalDouble(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack), fnArgs[3].EvalInt(scope, inst, cstack));

        private static int IndexOf_decimalArray_decimal(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.IndexOf(fnArgs[0].EvalDecimalArray(scope, inst, cstack), fnArgs[1].EvalDecimal(scope, inst, cstack));
        private static int IndexOf_decimalArray_decimal_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.IndexOf(fnArgs[0].EvalDecimalArray(scope, inst, cstack), fnArgs[1].EvalDecimal(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack));
        private static int IndexOf_decimalArray_decimal_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.IndexOf(fnArgs[0].EvalDecimalArray(scope, inst, cstack), fnArgs[1].EvalDecimal(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack), fnArgs[3].EvalInt(scope, inst, cstack));

        private static int IndexOf_boolArray_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.IndexOf(fnArgs[0].EvalBoolArray(scope, inst, cstack), fnArgs[1].EvalBool(scope, inst, cstack));
        private static int IndexOf_boolArray_bool_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.IndexOf(fnArgs[0].EvalBoolArray(scope, inst, cstack), fnArgs[1].EvalBool(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack));
        private static int IndexOf_boolArray_bool_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.IndexOf(fnArgs[0].EvalBoolArray(scope, inst, cstack), fnArgs[1].EvalBool(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack), fnArgs[3].EvalInt(scope, inst, cstack));

        private static int IndexOf_stringArray_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.IndexOf(fnArgs[0].EvalStringArray(scope, inst, cstack), fnArgs[1].EvalString(scope, inst, cstack));
        private static int IndexOf_stringArray_string_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.IndexOf(fnArgs[0].EvalStringArray(scope, inst, cstack), fnArgs[1].EvalString(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack));
        private static int IndexOf_stringArray_string_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.IndexOf(fnArgs[0].EvalStringArray(scope, inst, cstack), fnArgs[1].EvalString(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack), fnArgs[3].EvalInt(scope, inst, cstack));

        private static int IndexOf_charArray_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.IndexOf(fnArgs[0].EvalCharArray(scope, inst, cstack), fnArgs[1].EvalChar(scope, inst, cstack));
        private static int IndexOf_charArray_char_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.IndexOf(fnArgs[0].EvalCharArray(scope, inst, cstack), fnArgs[1].EvalChar(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack));
        private static int IndexOf_charArray_char_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.IndexOf(fnArgs[0].EvalCharArray(scope, inst, cstack), fnArgs[1].EvalChar(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack), fnArgs[3].EvalInt(scope, inst, cstack));

        private static int IndexOf_objectArray_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.IndexOf(fnArgs[0].EvalObjectArray(scope, inst, cstack), fnArgs[1].EvalObject(scope, inst, cstack));
        private static int IndexOf_objectArray_object_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.IndexOf(fnArgs[0].EvalObjectArray(scope, inst, cstack), fnArgs[1].EvalObject(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack));
        private static int IndexOf_objectArray_object_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.IndexOf(fnArgs[0].EvalObjectArray(scope, inst, cstack), fnArgs[1].EvalObject(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack), fnArgs[3].EvalInt(scope, inst, cstack));

        private static int IndexOf_shortArray_short(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.IndexOf(fnArgs[0].EvalShortArray(scope, inst, cstack), fnArgs[1].EvalShort(scope, inst, cstack));
        private static int IndexOf_shortArray_short_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.IndexOf(fnArgs[0].EvalShortArray(scope, inst, cstack), fnArgs[1].EvalShort(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack));
        private static int IndexOf_shortArray_short_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.IndexOf(fnArgs[0].EvalShortArray(scope, inst, cstack), fnArgs[1].EvalShort(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack), fnArgs[3].EvalInt(scope, inst, cstack));

        private static int IndexOf_byteArray_byte(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.IndexOf(fnArgs[0].EvalByteArray(scope, inst, cstack), fnArgs[1].EvalByte(scope, inst, cstack));
        private static int IndexOf_byteArray_byte_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.IndexOf(fnArgs[0].EvalByteArray(scope, inst, cstack), fnArgs[1].EvalByte(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack));
        private static int IndexOf_byteArray_byte_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.IndexOf(fnArgs[0].EvalByteArray(scope, inst, cstack), fnArgs[1].EvalByte(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack), fnArgs[3].EvalInt(scope, inst, cstack));

        private static int IndexOf_dateArray_date(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.IndexOf(fnArgs[0].EvalDateArray(scope, inst, cstack), fnArgs[1].EvalDate(scope, inst, cstack));
        private static int IndexOf_dateArray_date_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.IndexOf(fnArgs[0].EvalDateArray(scope, inst, cstack), fnArgs[1].EvalDate(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack));
        private static int IndexOf_dateArray_date_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.IndexOf(fnArgs[0].EvalDateArray(scope, inst, cstack), fnArgs[1].EvalDate(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack), fnArgs[3].EvalInt(scope, inst, cstack));

        private static int IndexOf_customArray_custom(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.IndexOf((Array)fnArgs[0].EvalCustomArray(scope, inst, cstack), fnArgs[1].EvalCustom(scope, inst, cstack));
        private static int IndexOf_customArray_custom_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.IndexOf((Array)fnArgs[0].EvalCustomArray(scope, inst, cstack), fnArgs[1].EvalCustom(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack));
        private static int IndexOf_customArray_custom_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.IndexOf((Array)fnArgs[0].EvalCustomArray(scope, inst, cstack), fnArgs[1].EvalCustom(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack), fnArgs[3].EvalInt(scope, inst, cstack));

        private static int IndexOf_object_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ((System.Collections.IList)fnArgs[0].EvalObject(scope, inst, cstack)).IndexOf(fnArgs[1].EvalObject(scope, inst, cstack));


        private static int LastIndexOf_intArray_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.LastIndexOf(fnArgs[0].EvalIntArray(scope, inst, cstack), fnArgs[1].EvalInt(scope, inst, cstack));
        private static int LastIndexOf_intArray_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.LastIndexOf(fnArgs[0].EvalIntArray(scope, inst, cstack), fnArgs[1].EvalInt(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack));
        private static int LastIndexOf_intArray_int_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.LastIndexOf(fnArgs[0].EvalIntArray(scope, inst, cstack), fnArgs[1].EvalInt(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack), fnArgs[3].EvalInt(scope, inst, cstack));

        private static int LastIndexOf_longArray_long(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.LastIndexOf(fnArgs[0].EvalLongArray(scope, inst, cstack), fnArgs[1].EvalLong(scope, inst, cstack));
        private static int LastIndexOf_longArray_long_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.LastIndexOf(fnArgs[0].EvalLongArray(scope, inst, cstack), fnArgs[1].EvalLong(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack));
        private static int LastIndexOf_longArray_long_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.LastIndexOf(fnArgs[0].EvalLongArray(scope, inst, cstack), fnArgs[1].EvalLong(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack), fnArgs[3].EvalInt(scope, inst, cstack));

        private static int LastIndexOf_floatArray_float(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.LastIndexOf(fnArgs[0].EvalFloatArray(scope, inst, cstack), fnArgs[1].EvalFloat(scope, inst, cstack));
        private static int LastIndexOf_floatArray_float_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.LastIndexOf(fnArgs[0].EvalFloatArray(scope, inst, cstack), fnArgs[1].EvalFloat(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack));
        private static int LastIndexOf_floatArray_float_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.LastIndexOf(fnArgs[0].EvalFloatArray(scope, inst, cstack), fnArgs[1].EvalFloat(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack), fnArgs[3].EvalInt(scope, inst, cstack));

        private static int LastIndexOf_doubleArray_double(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.LastIndexOf(fnArgs[0].EvalDoubleArray(scope, inst, cstack), fnArgs[1].EvalDouble(scope, inst, cstack));
        private static int LastIndexOf_doubleArray_double_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.LastIndexOf(fnArgs[0].EvalDoubleArray(scope, inst, cstack), fnArgs[1].EvalDouble(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack));
        private static int LastIndexOf_doubleArray_double_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.LastIndexOf(fnArgs[0].EvalDoubleArray(scope, inst, cstack), fnArgs[1].EvalDouble(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack), fnArgs[3].EvalInt(scope, inst, cstack));

        private static int LastIndexOf_decimalArray_decimal(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.LastIndexOf(fnArgs[0].EvalDecimalArray(scope, inst, cstack), fnArgs[1].EvalDecimal(scope, inst, cstack));
        private static int LastIndexOf_decimalArray_decimal_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.LastIndexOf(fnArgs[0].EvalDecimalArray(scope, inst, cstack), fnArgs[1].EvalDecimal(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack));
        private static int LastIndexOf_decimalArray_decimal_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.LastIndexOf(fnArgs[0].EvalDecimalArray(scope, inst, cstack), fnArgs[1].EvalDecimal(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack), fnArgs[3].EvalInt(scope, inst, cstack));

        private static int LastIndexOf_boolArray_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.LastIndexOf(fnArgs[0].EvalBoolArray(scope, inst, cstack), fnArgs[1].EvalBool(scope, inst, cstack));
        private static int LastIndexOf_boolArray_bool_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.LastIndexOf(fnArgs[0].EvalBoolArray(scope, inst, cstack), fnArgs[1].EvalBool(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack));
        private static int LastIndexOf_boolArray_bool_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.LastIndexOf(fnArgs[0].EvalBoolArray(scope, inst, cstack), fnArgs[1].EvalBool(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack), fnArgs[3].EvalInt(scope, inst, cstack));

        private static int LastIndexOf_stringArray_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.LastIndexOf(fnArgs[0].EvalStringArray(scope, inst, cstack), fnArgs[1].EvalString(scope, inst, cstack));
        private static int LastIndexOf_stringArray_string_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.LastIndexOf(fnArgs[0].EvalStringArray(scope, inst, cstack), fnArgs[1].EvalString(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack));
        private static int LastIndexOf_stringArray_string_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.LastIndexOf(fnArgs[0].EvalStringArray(scope, inst, cstack), fnArgs[1].EvalString(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack), fnArgs[3].EvalInt(scope, inst, cstack));

        private static int LastIndexOf_charArray_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.LastIndexOf(fnArgs[0].EvalCharArray(scope, inst, cstack), fnArgs[1].EvalChar(scope, inst, cstack));
        private static int LastIndexOf_charArray_char_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.LastIndexOf(fnArgs[0].EvalCharArray(scope, inst, cstack), fnArgs[1].EvalChar(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack));
        private static int LastIndexOf_charArray_char_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.LastIndexOf(fnArgs[0].EvalCharArray(scope, inst, cstack), fnArgs[1].EvalChar(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack), fnArgs[3].EvalInt(scope, inst, cstack));

        private static int LastIndexOf_objectArray_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.LastIndexOf(fnArgs[0].EvalObjectArray(scope, inst, cstack), fnArgs[1].EvalObject(scope, inst, cstack));
        private static int LastIndexOf_objectArray_object_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.LastIndexOf(fnArgs[0].EvalObjectArray(scope, inst, cstack), fnArgs[1].EvalObject(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack));
        private static int LastIndexOf_objectArray_object_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.LastIndexOf(fnArgs[0].EvalObjectArray(scope, inst, cstack), fnArgs[1].EvalObject(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack), fnArgs[3].EvalInt(scope, inst, cstack));

        private static int LastIndexOf_shortArray_short(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.LastIndexOf(fnArgs[0].EvalShortArray(scope, inst, cstack), fnArgs[1].EvalShort(scope, inst, cstack));
        private static int LastIndexOf_shortArray_short_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.LastIndexOf(fnArgs[0].EvalShortArray(scope, inst, cstack), fnArgs[1].EvalShort(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack));
        private static int LastIndexOf_shortArray_short_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.LastIndexOf(fnArgs[0].EvalShortArray(scope, inst, cstack), fnArgs[1].EvalShort(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack), fnArgs[3].EvalInt(scope, inst, cstack));

        private static int LastIndexOf_byteArray_byte(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.LastIndexOf(fnArgs[0].EvalByteArray(scope, inst, cstack), fnArgs[1].EvalByte(scope, inst, cstack));
        private static int LastIndexOf_byteArray_byte_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.LastIndexOf(fnArgs[0].EvalByteArray(scope, inst, cstack), fnArgs[1].EvalByte(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack));
        private static int LastIndexOf_byteArray_byte_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.LastIndexOf(fnArgs[0].EvalByteArray(scope, inst, cstack), fnArgs[1].EvalByte(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack), fnArgs[3].EvalInt(scope, inst, cstack));

        private static int LastIndexOf_dateArray_date(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.LastIndexOf(fnArgs[0].EvalDateArray(scope, inst, cstack), fnArgs[1].EvalDate(scope, inst, cstack));
        private static int LastIndexOf_dateArray_date_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.LastIndexOf(fnArgs[0].EvalDateArray(scope, inst, cstack), fnArgs[1].EvalDate(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack));
        private static int LastIndexOf_dateArray_date_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.LastIndexOf(fnArgs[0].EvalDateArray(scope, inst, cstack), fnArgs[1].EvalDate(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack), fnArgs[3].EvalInt(scope, inst, cstack));

        private static int LastIndexOf_customArray_custom(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.LastIndexOf((Array)fnArgs[0].EvalCustomArray(scope, inst, cstack), fnArgs[1].EvalCustom(scope, inst, cstack));
        private static int LastIndexOf_customArray_custom_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.LastIndexOf((Array)fnArgs[0].EvalCustomArray(scope, inst, cstack), fnArgs[1].EvalCustom(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack));
        private static int LastIndexOf_customArray_custom_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Array.LastIndexOf((Array)fnArgs[0].EvalCustomArray(scope, inst, cstack), fnArgs[1].EvalCustom(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack), fnArgs[3].EvalInt(scope, inst, cstack));




        private static object Reverse_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { Array.Reverse(fnArgs[0].EvalArray(scope, inst, cstack)); return null; }

        private static object Reverse_object_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { Array.Reverse(fnArgs[0].EvalArray(scope, inst, cstack), fnArgs[1].EvalInt(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack)); return null; }
        private static object Sort_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { Array.Sort(fnArgs[0].EvalArray(scope, inst, cstack)); return null; }
        private static object Sort_object_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { Array.Sort(fnArgs[0].EvalArray(scope, inst, cstack), (Array)fnArgs[1].EvalArray(scope, inst, cstack)); return null; }
        private static object Sort_object_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { Array.Sort(fnArgs[0].EvalArray(scope, inst, cstack), fnArgs[1].EvalInt(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack)); return null; }

        private static bool IsNumeric_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            string s = fnArgs[0].EvalString(scope, inst, cstack);
            decimal d;
            return decimal.TryParse(s, out d);
        }

        private static object WriteLine(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { Console.WriteLine(); return null; }
        private static object WriteLine_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { Console.WriteLine(fnArgs[0].EvalString(scope, inst, cstack)); return null; }

        private static object WriteLine_string_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { Console.WriteLine(Format_string_objectParams(fnArgs, scope, srcInst, inst, cstack, csrc)); return null; }
        private static object Write_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { Console.Write(fnArgs[0].EvalString(scope, inst, cstack)); return null; }

        private static object Write_string_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { Console.Write(Format_string_objectParams(fnArgs, scope, srcInst, inst, cstack, csrc)); return null; }

        private static string PadLeft_string_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            string s = fnArgs[0].EvalString(scope, inst, cstack);
            int w = fnArgs[1].EvalInt(scope, inst, cstack);

            return s.PadLeft(w, ' ');
        }
        private static string PadLeft_string_int_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            string s = fnArgs[0].EvalString(scope, inst, cstack);
            int w = fnArgs[1].EvalInt(scope, inst, cstack);
            char c = fnArgs[2].EvalChar(scope, inst, cstack);
            return s.PadLeft(w, c);
        }
        private static string PadRight_string_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            string s = fnArgs[0].EvalString(scope, inst, cstack);
            int w = fnArgs[1].EvalInt(scope, inst, cstack);

            return s.PadRight(w, ' ');
        }
        private static string PadRight_string_int_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            string s = fnArgs[0].EvalString(scope, inst, cstack);
            int w = fnArgs[1].EvalInt(scope, inst, cstack);
            char c = fnArgs[2].EvalChar(scope, inst, cstack);
            return s.PadRight(w, c);
        }

        private static int CursorTop(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Console.CursorTop;
        private static object SetCursorTop_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { Console.CursorTop = fnArgs[0].EvalInt(scope, inst, cstack); return null; }

        private static int CursorLeft(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Console.CursorLeft;
        private static object SetCursorLeft_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { Console.CursorLeft = fnArgs[0].EvalInt(scope, inst, cstack); return null; }
        private static bool CursorVisible(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Console.CursorVisible;
        private static object SetCursorVisible_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { Console.CursorVisible = fnArgs[0].EvalBool(scope, inst, cstack); return null; }
        private static object GetTypeByName_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Type.GetType(fnArgs[0].EvalString(scope, inst, cstack), true);
        private static object GetTypeByName_string_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            var asm = Assembly.LoadFrom(GetFullPath(fnArgs[0].EvalString(scope, inst, cstack), cstack, csrc));
            return asm.GetType(fnArgs[1].EvalString(scope, inst, cstack), true);
        }
        private static object GetTypeByName_object_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            var asm = (Assembly)fnArgs[0].EvalObject(scope, inst, cstack);
            return asm.GetType(fnArgs[1].EvalString(scope, inst, cstack), true);

        }
        private static object LoadAssembly_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Assembly.Load(fnArgs[0].EvalString(scope, inst, cstack));
        private static object LoadAssemblyFrom_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Assembly.LoadFrom(GetFullPath(fnArgs[0].EvalString(scope, inst, cstack), cstack, csrc));


        private static object Create_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Activator.CreateInstance((Type)fnArgs[0].EvalObject(scope, inst, cstack));
        private static object Create_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Activator.CreateInstance((Type)fnArgs[0].EvalObject(scope, inst, cstack), EvalArgs(1, fnArgs, scope, inst, cstack));
        private static object Create_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Activator.CreateInstance(Type.GetType(fnArgs[0].EvalString(scope, inst, cstack), true));
        private static object Create_string_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Activator.CreateInstance(Type.GetType(fnArgs[0].EvalString(scope, inst, cstack), true), EvalArgs(1, fnArgs, scope, inst, cstack));

        private static object CreateFrom_object_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => CreateFrom((Assembly)fnArgs[0].EvalObject(scope, inst, cstack), fnArgs[1].EvalString(scope, inst, cstack), null);
        private static object CreateFrom_string_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => CreateFrom(Assembly.LoadFrom(GetFullPath(fnArgs[0].EvalString(scope, inst, cstack), cstack, csrc)), fnArgs[1].EvalString(scope, inst, cstack), null);
        private static object CreateFrom_object_string_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => CreateFrom((Assembly)fnArgs[0].EvalObject(scope, inst, cstack), fnArgs[1].EvalString(scope, inst, cstack), EvalArgs(2, fnArgs, scope, inst, cstack));
        private static object CreateFrom_string_string_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => CreateFrom(Assembly.LoadFrom(GetFullPath(fnArgs[0].EvalString(scope, inst, cstack), cstack, csrc)), fnArgs[1].EvalString(scope, inst, cstack), EvalArgs(2, fnArgs, scope, inst, cstack));

        private static object CreateFrom(Assembly asm, string typeName, object[] args)
        {
            var type = asm.GetType(typeName, true);
            return args != null ? Activator.CreateInstance(type, args) : Activator.CreateInstance(type);
        }

        public static object[] EvalArgs(int start, EvalUnit[] fnArgs, int scope, ClassInstance inst, CallStack cstack) => EvalArgs<object>(start, fnArgs, scope, inst, cstack, true);



        public static T[] EvalArgs<T>(int start, EvalUnit[] fnArgs, int scope, ClassInstance inst, CallStack cstack, bool nonGen = false)
        {
            T[] args = null;
            if (fnArgs.Length > start)
            {
                int n = fnArgs.Length - start;
                if (n == 1)
                {
                    var first = fnArgs[start];

                    if (nonGen)
                    {
                        if (first.Type.ID == TypeID.ObjectArray) return first.Eval<T[]>(scope, inst, cstack);
                    }
                    else if (first.Type.T == typeof(T[])) return first.Eval<T[]>(scope, inst, cstack);


                }
                args = new T[n];
                for (int i = 0; i < args.Length; i++)
                    args[i] = fnArgs[i + start].Ev<T>(scope, inst, cstack);
            }

            return args;
        }


        private static object InvokeMethod_object_string_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            object obj = fnArgs[0].EvalObject(scope, inst, cstack);
            string member = fnArgs[1].EvalString(scope, inst, cstack);

            object[] args = EvalArgs(2, fnArgs, scope, inst, cstack);
            Type[] parameterTypes = args != null ? Array.ConvertAll(args, prm => prm.GetType()) : new Type[0];

            Type atType;
            var memberInfo = GetMethod(member, obj, parameterTypes, out atType);

            if (memberInfo == null) throw new ScriptExecutionException($"Method '{member.TrimStart(AtRuntimeTypeMethodPrefix)}({string.Join<Type>(", ", parameterTypes)})' not found at '{atType}'.");

            return ((MethodInfo)memberInfo).Invoke(obj, args);
        }
        public static MemberInfo GetMethod(string member, object obj, Type[] parameterTypes, out Type atType)
        {

            bool gt = !String.IsNullOrEmpty(member) && member[0] == AtRuntimeTypeMethodPrefix;
            if (gt) member = member.Remove(0, 1);

            atType = !gt && obj is Type type ? type : obj.GetType();



            MemberInfo memberInfo = atType.GetMethod(member, parameterTypes);
            return memberInfo;
        }
        public static Type GetTargetType(ref string member, object obj)
        {
            bool gt = !String.IsNullOrEmpty(member) && member[0] == AtRuntimeTypeMethodPrefix;
            if (gt) member = member.Remove(0, 1);

            return !gt && obj is Type type ? type : obj.GetType();
        }
        public static MemberInfo GetPropertyOrField(string member, object obj, out Type atType)
        {
            atType = GetTargetType(ref member, obj);


            MemberInfo memberInfo = atType.GetProperty(member);
            if (memberInfo == null)
            {


                memberInfo = atType.GetField(member);

                if (memberInfo == null) memberInfo = atType.GetMember(member).FirstOrDefault();

            }



            return memberInfo;
        }

        static object ConvertValue(object v, Type type)
        {
            Type valType;
            if (v != null && !type.IsAssignableFrom(valType = v.GetType()))
            {
                try { v = Convert.ChangeType(v, type); }
                catch { throw new InvalidCastException($"Failed to convert value of type '{valType}' to type '{type}'."); }
            }
            return v;
        }
        private static object Array_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => CreateArray(Type.GetType(fnArgs[0].EvalString(scope, inst, cstack), true));
        private static object Array_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => CreateArray((Type)fnArgs[0].EvalObject(scope, inst, cstack));
        private static object Array_string_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => CreateArray(Type.GetType(fnArgs[0].EvalString(scope, inst, cstack), true), EvalArgs(1, fnArgs, scope, inst, cstack));
        private static object Array_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => CreateArray((Type)fnArgs[0].EvalObject(scope, inst, cstack), EvalArgs(1, fnArgs, scope, inst, cstack));
        private static object CreateArray(Type arrType, object[] args = null)
        {
            if (args == null)
                return Array.CreateInstance(arrType, 0);

            Array arr = Array.CreateInstance(arrType, args.Length);

            for (int i = 0; i < args.Length; i++)
                arr.SetValue(ConvertValue(args[i], arrType), i);

            return arr;
        }
        private static object ConvertArray_object_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ConvertArray((Array)fnArgs[0].EvalObject(scope, inst, cstack), (Type)fnArgs[1].EvalObject(scope, inst, cstack));
        private static object ConvertArray_object_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ConvertArray((Array)fnArgs[0].EvalObject(scope, inst, cstack), Type.GetType(fnArgs[1].EvalString(scope, inst, cstack), true));


        private static Array ConvertArray(Array arr, Type type)
        {


            Array result;
            try
            {
                result = Array.CreateInstance(type, arr.Length);
                if (type.IsAssignableFrom(arr.GetType().GetElementType()))
                    arr.CopyTo(result, 0);
                else
                {
                    for (int i = 0; i < arr.Length; i++)
                        result.SetValue(Convert.ChangeType(arr.GetValue(i), type), i);
                }

            }
            catch (Exception ex)
            {
                throw new ScriptExecutionException($"Failed to cast array. " + ex.Message);
            }
            return result;
        }

        private static object ToArray_object_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ToArray((System.Collections.IEnumerable)fnArgs[0].EvalObject(scope, inst, cstack), Type.GetType(fnArgs[1].EvalString(scope, inst, cstack), true));

        private static object ToArray_object_string_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ToArray((System.Collections.IEnumerable)fnArgs[0].EvalObject(scope, inst, cstack), Type.GetType(fnArgs[1].EvalString(scope, inst, cstack), true), fnArgs[2].EvalInt(scope, inst, cstack));
        private static object ToArray_object_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ToArray((System.Collections.IEnumerable)fnArgs[0].EvalObject(scope, inst, cstack), (Type)fnArgs[1].EvalObject(scope, inst, cstack));
        private static object ToArray_object_object_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ToArray((System.Collections.IEnumerable)fnArgs[0].EvalObject(scope, inst, cstack), (Type)fnArgs[1].EvalObject(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack));

        private static Array ToArray(System.Collections.IEnumerable en, Type type, int size = -1)
        {
            bool withSize = size >= 0;
            int capacity = size >= 0 ? size : byte.MaxValue;
            int index = 0;
            Array result;
            try
            {
                result = Array.CreateInstance(type, capacity);
                foreach (var item in en)
                {
                    if (index >= capacity)
                    {
                        capacity *= 2;
                        var result2 = Array.CreateInstance(type, capacity);
                        result.CopyTo(result2, 0);
                        result = result2;
                    }
                    if (type.IsAssignableFrom(item.GetType()))
                        result.SetValue(item, index);
                    else
                        result.SetValue(Convert.ChangeType(item, type), index);

                    index++;
                    if (withSize && index == size) break;
                }

            }
            catch (Exception ex)
            {
                throw new ScriptExecutionException($"Failed to create array. " + ex.Message);
            }
            if (index != capacity)
            {
                var result2 = Array.CreateInstance(type, index);
                Array.Copy(result, result2, index);

                result = result2;
            }
            return result;
        }


        private static object[] ToObjectArray_object_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ToObjectArray((Array)fnArgs[0].EvalObject(scope, inst, cstack), Type.GetType(fnArgs[1].EvalString(scope, inst, cstack), true));
        private static object[] ToObjectArray_object_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ToObjectArray((Array)fnArgs[0].EvalObject(scope, inst, cstack), (Type)fnArgs[1].EvalObject(scope, inst, cstack));
        private static object[] ToObjectArray_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ToObjectArray((Array)fnArgs[0].EvalObject(scope, inst, cstack));

        private static object[] ToObjectArray(Array arr)
        {
            object[] result = new object[arr.Length];
            arr.CopyTo(result, 0);
            return result;
        }
        private static object[] ToObjectArray(Array arr, Type type)
        {
            object[] result = new object[arr.Length];
            try
            {
                for (int i = 0; i < arr.Length; i++)
                    result.SetValue(Convert.ChangeType(arr.GetValue(i), type), i);

            }
            catch (Exception ex)
            {
                throw new ScriptExecutionException($"Failed to cast array. " + ex.Message);
            }
            return result;
        }

        private static object GetValue_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack));
        private static object GetValue_object_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            var memberInfo = (MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack);
            object o = GetObjArgOrNull(fnArgs[1], scope, inst, cstack);
            return GetValue(memberInfo, o);
        }

        private static object GetValue_object_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            var memberInfo = (MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack);
            object o = GetObjArgOrNull(fnArgs[1], scope, inst, cstack);
            object[] args = EvalArgs(2, fnArgs, scope, inst, cstack);

            return GetValue(memberInfo, o, args);

        }
        private static object GetElement_object_object_intParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            var memberInfo = (MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack);
            object o = GetObjArgOrNull(fnArgs[1], scope, inst, cstack);
            if (fnArgs.Length == 3)
            {
                int index = fnArgs[2].EvalInt(scope, inst, cstack);
                return GetElement(memberInfo, o, index);
            }
            else
            {
                int[] indexes = EvalArgs<int>(2, fnArgs, scope, inst, cstack);
                return GetElement(memberInfo, o, indexes);
            }
        }
        private static object SetElement_object_object_object_intParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            var memberInfo = (MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack);
            object o = GetObjArgOrNull(fnArgs[1], scope, inst, cstack);
            object val = fnArgs[2].Ev<object>(scope, inst, cstack);
            if (fnArgs.Length == 4)
            {
                int index = fnArgs[3].EvalInt(scope, inst, cstack);
                SetElement(memberInfo, o, val, index);
            }
            else
            {
                int[] indexes = EvalArgs<int>(3, fnArgs, scope, inst, cstack);
                SetElement(memberInfo, o, val, indexes);

            }
            return val;
        }

        private static int TGetElement_object_intType_object_intParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetArrayElement<int>(fnArgs, scope, inst, cstack, csrc);
        private static long TGetElement_object_longType_object_intParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetArrayElement<long>(fnArgs, scope, inst, cstack, csrc);
        private static float TGetElement_object_floatType_object_intParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetArrayElement<float>(fnArgs, scope, inst, cstack, csrc);
        private static double TGetElement_object_doubleType_object_intParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetArrayElement<double>(fnArgs, scope, inst, cstack, csrc);
        private static decimal TGetElement_object_decimalType_object_intParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetArrayElement<decimal>(fnArgs, scope, inst, cstack, csrc);
        private static bool TGetElement_object_boolType_object_intParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetArrayElement<bool>(fnArgs, scope, inst, cstack, csrc);
        private static char TGetElement_object_charType_object_intParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetArrayElement<char>(fnArgs, scope, inst, cstack, csrc);
        private static string TGetElement_object_stringType_object_intParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetArrayElement<string>(fnArgs, scope, inst, cstack, csrc);
        private static short TGetElement_object_shortType_object_intParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetArrayElement<short>(fnArgs, scope, inst, cstack, csrc);
        private static byte TGetElement_object_byteType_object_intParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetArrayElement<byte>(fnArgs, scope, inst, cstack, csrc);
        private static DateTime TGetElement_object_dateType_object_intParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetArrayElement<DateTime>(fnArgs, scope, inst, cstack, csrc);

        private static T GetArrayElement<T>(EvalUnit[] fnArgs, int scope, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            var memberInfo = (MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack);
            object o = GetObjArgOrNull(fnArgs[2], scope, inst, cstack);
            if (fnArgs.Length == 4)
            {
                int index = fnArgs[3].EvalInt(scope, inst, cstack);
                return (T)GetElement(memberInfo, o, index);
            }
            else
            {
                int[] indexes = EvalArgs<int>(3, fnArgs, scope, inst, cstack);
                return (T)GetElement(memberInfo, o, indexes);
            }
        }


        private static object GetElement(MemberInfo memberInfo, object o, int[] indexes)
        {
            Array arr = (Array)GetFieldOrPropValue(memberInfo, o);
            return arr.GetValue(indexes);
        }
        private static object GetElement(MemberInfo memberInfo, object o, int index)
        {
            Array arr = (Array)GetFieldOrPropValue(memberInfo, o);
            return arr.GetValue(index);
        }


        private static void SetElement(MemberInfo memberInfo, object o, object val, int[] indexes)
        {
            Array arr = (Array)GetFieldOrPropValue(memberInfo, o);
            arr.SetValue(val, indexes);
        }
        private static void SetElement(MemberInfo memberInfo, object o, object val, int index)
        {
            Array arr = (Array)GetFieldOrPropValue(memberInfo, o);
            arr.SetValue(val, index);
        }
        private static object GetFieldOrPropValue(MemberInfo memberInfo, object o) => memberInfo is FieldInfo fi ? fi.GetValue(o) : ((PropertyInfo)memberInfo).GetValue(o);


        private static object Invoke_object_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            var memberInfo = (MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack);
            object o = GetObjOrNullIfEmpty(fnArgs[1], scope, inst, cstack);
            object[] args = EvalArgs(2, fnArgs, scope, inst, cstack);
            return memberInfo.Invoke(o, args);

        }
        private static object Invoke_object_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            var memberInfo = (MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack);
            object o = GetObjOrNullIfEmpty(fnArgs[1], scope, inst, cstack);
            return memberInfo.Invoke(o, null);

        }
        static object GetObjOrNullIfEmpty(EvalUnit eu, int scope, ClassInstance inst, CallStack cstack) => eu.Kind != EvalUnitKind.Empty ? eu.EvalObject(scope, inst, cstack) : null;
        private static object Invoke_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            var memberInfo = (MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack);
            return memberInfo.Invoke(null, null);
        }
        private static T TInvoke_object<T>(MethodInfo memberInfo) => (T)memberInfo.Invoke(null, null);
        private static T TInvoke_object_object<T>(MethodInfo memberInfo, object o) => (T)memberInfo.Invoke(o, null);
        private static T TInvoke_object_object_object<T>(MethodInfo memberInfo, object o, object[] args) => (T)memberInfo.Invoke(o, args);

        private static int TInvoke_object_intType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object<int>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack));
        private static long TInvoke_object_longType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object<long>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack));
        private static double TInvoke_object_doubleType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object<double>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack));
        private static float TInvoke_object_floatType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object<float>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack));
        private static decimal TInvoke_object_decimalType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object<decimal>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack));
        private static bool TInvoke_object_boolType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object<bool>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack));
        private static char TInvoke_object_charType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object<char>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack));
        private static string TInvoke_object_stringType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object<string>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack));
        private static short TInvoke_object_shortType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object<short>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack));
        private static byte TInvoke_object_byteType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object<byte>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack));
        private static DateTime TInvoke_object_dateType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object<DateTime>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack));

        private static int TInvoke_object_intType_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object_object<int>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack));
        private static long TInvoke_object_longType_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object_object<long>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack));
        private static double TInvoke_object_doubleType_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object_object<double>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack));
        private static float TInvoke_object_floatType_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object_object<float>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack));
        private static decimal TInvoke_object_decimalType_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object_object<decimal>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack));
        private static bool TInvoke_object_boolType_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object_object<bool>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack));
        private static char TInvoke_object_charType_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object_object<char>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack));
        private static string TInvoke_object_stringType_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object_object<string>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack));
        private static short TInvoke_object_shortType_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object_object<short>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack));
        private static byte TInvoke_object_byteType_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object_object<byte>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack));
        private static DateTime TInvoke_object_dateType_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object_object<DateTime>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack));

        private static int TInvoke_object_intType_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object_object_object<int>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack), EvalArgs(3, fnArgs, scope, inst, cstack));
        private static long TInvoke_object_longType_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object_object_object<long>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack), EvalArgs(3, fnArgs, scope, inst, cstack));
        private static double TInvoke_object_doubleType_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object_object_object<double>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack), EvalArgs(3, fnArgs, scope, inst, cstack));
        private static float TInvoke_object_floatType_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object_object_object<float>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack), EvalArgs(3, fnArgs, scope, inst, cstack));
        private static decimal TInvoke_object_decimalType_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object_object_object<decimal>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack), EvalArgs(3, fnArgs, scope, inst, cstack));
        private static bool TInvoke_object_boolType_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object_object_object<bool>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack), EvalArgs(3, fnArgs, scope, inst, cstack));
        private static char TInvoke_object_charType_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object_object_object<char>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack), EvalArgs(3, fnArgs, scope, inst, cstack));
        private static string TInvoke_object_stringType_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object_object_object<string>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack), EvalArgs(3, fnArgs, scope, inst, cstack));
        private static short TInvoke_object_shortType_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object_object_object<short>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack), EvalArgs(3, fnArgs, scope, inst, cstack));
        private static byte TInvoke_object_byteType_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object_object_object<byte>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack), EvalArgs(3, fnArgs, scope, inst, cstack));
        private static DateTime TInvoke_object_dateType_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object_object_object<DateTime>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack), EvalArgs(3, fnArgs, scope, inst, cstack));

        private static object[] TInvoke_object_objectArrayType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object<object[]>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack));
        private static int[] TInvoke_object_intArrayType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object<int[]>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack));
        private static long[] TInvoke_object_longArrayType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object<long[]>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack));
        private static double[] TInvoke_object_doubleArrayType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object<double[]>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack));
        private static float[] TInvoke_object_floatArrayType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object<float[]>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack));
        private static decimal[] TInvoke_object_decimalArrayType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object<decimal[]>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack));
        private static bool[] TInvoke_object_boolArrayType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object<bool[]>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack));
        private static char[] TInvoke_object_charArrayType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object<char[]>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack));
        private static string[] TInvoke_object_stringArrayType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object<string[]>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack));
        private static short[] TInvoke_object_shortArrayType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object<short[]>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack));
        private static byte[] TInvoke_object_byteArrayType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object<byte[]>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack));
        private static DateTime[] TInvoke_object_dateArrayType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object<DateTime[]>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack));

        private static object[] TInvoke_object_objectArrayType_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object_object<object[]>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack));
        private static int[] TInvoke_object_intArrayType_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object_object<int[]>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack));
        private static long[] TInvoke_object_longArrayType_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object_object<long[]>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack));
        private static double[] TInvoke_object_doubleArrayType_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object_object<double[]>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack));
        private static float[] TInvoke_object_floatArrayType_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object_object<float[]>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack));
        private static decimal[] TInvoke_object_decimalArrayType_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object_object<decimal[]>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack));
        private static bool[] TInvoke_object_boolArrayType_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object_object<bool[]>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack));
        private static char[] TInvoke_object_charArrayType_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object_object<char[]>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack));
        private static string[] TInvoke_object_stringArrayType_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object_object<string[]>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack));
        private static short[] TInvoke_object_shortArrayType_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object_object<short[]>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack));
        private static byte[] TInvoke_object_byteArrayType_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object_object<byte[]>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack));
        private static DateTime[] TInvoke_object_dateArrayType_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object_object<DateTime[]>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack));

        private static object[] TInvoke_object_objectArrayType_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object_object_object<object[]>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack), EvalArgs(3, fnArgs, scope, inst, cstack));
        private static int[] TInvoke_object_intArrayType_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object_object_object<int[]>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack), EvalArgs(3, fnArgs, scope, inst, cstack));
        private static long[] TInvoke_object_longArrayType_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object_object_object<long[]>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack), EvalArgs(3, fnArgs, scope, inst, cstack));
        private static double[] TInvoke_object_doubleArrayType_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object_object_object<double[]>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack), EvalArgs(3, fnArgs, scope, inst, cstack));
        private static float[] TInvoke_object_floatArrayType_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object_object_object<float[]>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack), EvalArgs(3, fnArgs, scope, inst, cstack));
        private static decimal[] TInvoke_object_decimalArrayType_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object_object_object<decimal[]>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack), EvalArgs(3, fnArgs, scope, inst, cstack));
        private static bool[] TInvoke_object_boolArrayType_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object_object_object<bool[]>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack), EvalArgs(3, fnArgs, scope, inst, cstack));
        private static char[] TInvoke_object_charArrayType_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object_object_object<char[]>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack), EvalArgs(3, fnArgs, scope, inst, cstack));
        private static string[] TInvoke_object_stringArrayType_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object_object_object<string[]>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack), EvalArgs(3, fnArgs, scope, inst, cstack));
        private static short[] TInvoke_object_shortArrayType_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object_object_object<short[]>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack), EvalArgs(3, fnArgs, scope, inst, cstack));
        private static byte[] TInvoke_object_byteArrayType_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object_object_object<byte[]>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack), EvalArgs(3, fnArgs, scope, inst, cstack));
        private static DateTime[] TInvoke_object_dateArrayType_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => TInvoke_object_object_object<DateTime[]>((MethodInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack), EvalArgs(3, fnArgs, scope, inst, cstack));


        private static int TGetValue_object_intType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<int>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack));
        private static long TGetValue_object_longType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<long>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack));
        private static double TGetValue_object_doubleType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<double>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack));
        private static float TGetValue_object_floatType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<float>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack));
        private static decimal TGetValue_object_decimalType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<decimal>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack));
        private static bool TGetValue_object_boolType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<bool>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack));
        private static char TGetValue_object_charType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<char>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack));
        private static string TGetValue_object_stringType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<string>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack));
        private static short TGetValue_object_shortType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<short>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack));
        private static byte TGetValue_object_byteType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<byte>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack));
        private static DateTime TGetValue_object_dateType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<DateTime>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack));

        private static int TGetValue_object_intType_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<int>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack));
        private static long TGetValue_object_longType_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<long>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack));
        private static double TGetValue_object_doubleType_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<double>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack));
        private static float TGetValue_object_floatType_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<float>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack));
        private static decimal TGetValue_object_decimalType_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<decimal>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack));
        private static bool TGetValue_object_boolType_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<bool>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack));
        private static char TGetValue_object_charType_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<char>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack));
        private static string TGetValue_object_stringType_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<string>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack));
        private static short TGetValue_object_shortType_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<short>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack));
        private static byte TGetValue_object_byteType_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<byte>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack));
        private static DateTime TGetValue_object_dateType_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<DateTime>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack));

        private static int TGetValue_object_intType_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<int>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack), EvalArgs(3, fnArgs, scope, inst, cstack));
        private static long TGetValue_object_longType_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<long>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack), EvalArgs(3, fnArgs, scope, inst, cstack));
        private static double TGetValue_object_doubleType_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<double>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack), EvalArgs(3, fnArgs, scope, inst, cstack));
        private static float TGetValue_object_floatType_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<float>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack), EvalArgs(3, fnArgs, scope, inst, cstack));
        private static decimal TGetValue_object_decimalType_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<decimal>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack), EvalArgs(3, fnArgs, scope, inst, cstack));
        private static bool TGetValue_object_boolType_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<bool>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack), EvalArgs(3, fnArgs, scope, inst, cstack));
        private static char TGetValue_object_charType_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<char>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack), EvalArgs(3, fnArgs, scope, inst, cstack));
        private static string TGetValue_object_stringType_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<string>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack), EvalArgs(3, fnArgs, scope, inst, cstack));
        private static short TGetValue_object_shortType_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<short>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack), EvalArgs(3, fnArgs, scope, inst, cstack));
        private static byte TGetValue_object_byteType_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<byte>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack), EvalArgs(3, fnArgs, scope, inst, cstack));
        private static DateTime TGetValue_object_dateType_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<DateTime>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack), EvalArgs(3, fnArgs, scope, inst, cstack));

        private static object[] TGetValue_object_objectArrayType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<object[]>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack));
        private static int[] TGetValue_object_intArrayType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<int[]>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack));
        private static long[] TGetValue_object_longArrayType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<long[]>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack));
        private static double[] TGetValue_object_doubleArrayType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<double[]>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack));
        private static float[] TGetValue_object_floatArrayType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<float[]>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack));
        private static decimal[] TGetValue_object_decimalArrayType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<decimal[]>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack));
        private static bool[] TGetValue_object_boolArrayType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<bool[]>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack));
        private static char[] TGetValue_object_charArrayType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<char[]>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack));
        private static string[] TGetValue_object_stringArrayType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<string[]>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack));
        private static short[] TGetValue_object_shortArrayType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<short[]>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack));
        private static byte[] TGetValue_object_byteArrayType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<byte[]>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack));
        private static DateTime[] TGetValue_object_dateArrayType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<DateTime[]>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack));

        private static object[] TGetValue_object_objectArrayType_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<object[]>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack));
        private static int[] TGetValue_object_intArrayType_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<int[]>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack));
        private static long[] TGetValue_object_longArrayType_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<long[]>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack));
        private static double[] TGetValue_object_doubleArrayType_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<double[]>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack));
        private static float[] TGetValue_object_floatArrayType_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<float[]>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack));
        private static decimal[] TGetValue_object_decimalArrayType_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<decimal[]>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack));
        private static bool[] TGetValue_object_boolArrayType_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<bool[]>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack));
        private static char[] TGetValue_object_charArrayType_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<char[]>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack));
        private static string[] TGetValue_object_stringArrayType_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<string[]>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack));
        private static short[] TGetValue_object_shortArrayType_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<short[]>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack));
        private static byte[] TGetValue_object_byteArrayType_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<byte[]>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack));
        private static DateTime[] TGetValue_object_dateArrayType_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<DateTime[]>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack));

        private static object[] TGetValue_object_objectArrayType_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<object[]>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack), EvalArgs(3, fnArgs, scope, inst, cstack));
        private static int[] TGetValue_object_intArrayType_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<int[]>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack), EvalArgs(3, fnArgs, scope, inst, cstack));
        private static long[] TGetValue_object_longArrayType_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<long[]>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack), EvalArgs(3, fnArgs, scope, inst, cstack));
        private static double[] TGetValue_object_doubleArrayType_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<double[]>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack), EvalArgs(3, fnArgs, scope, inst, cstack));
        private static float[] TGetValue_object_floatArrayType_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<float[]>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack), EvalArgs(3, fnArgs, scope, inst, cstack));
        private static decimal[] TGetValue_object_decimalArrayType_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<decimal[]>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack), EvalArgs(3, fnArgs, scope, inst, cstack));
        private static bool[] TGetValue_object_boolArrayType_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<bool[]>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack), EvalArgs(3, fnArgs, scope, inst, cstack));
        private static char[] TGetValue_object_charArrayType_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<char[]>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack), EvalArgs(3, fnArgs, scope, inst, cstack));
        private static string[] TGetValue_object_stringArrayType_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<string[]>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack), EvalArgs(3, fnArgs, scope, inst, cstack));
        private static short[] TGetValue_object_shortArrayType_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<short[]>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack), EvalArgs(3, fnArgs, scope, inst, cstack));
        private static byte[] TGetValue_object_byteArrayType_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<byte[]>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack), EvalArgs(3, fnArgs, scope, inst, cstack));
        private static DateTime[] TGetValue_object_dateArrayType_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetValue<DateTime[]>((MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack), GetObjOrNullIfEmpty(fnArgs[2], scope, inst, cstack), EvalArgs(3, fnArgs, scope, inst, cstack));


        private static object SetValue_object_object_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            var memberInfo = (MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack);

            object o = fnArgs[1].Kind != EvalUnitKind.Empty ? fnArgs[1].EvalObject(scope, inst, cstack) : null;
            object value = fnArgs[2].EvalObject(scope, inst, cstack);
            object[] args = EvalArgs(3, fnArgs, scope, inst, cstack);

            SetValue(o, memberInfo, value, args);

            return value;
        }
        private static object SetValue_object_object_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            var memberInfo = (MemberInfo)fnArgs[0].EvalObject(scope, inst, cstack);

            object o = fnArgs[1].Kind != EvalUnitKind.Empty ? fnArgs[1].EvalObject(scope, inst, cstack) : null;
            object value = fnArgs[2].EvalObject(scope, inst, cstack);

            SetValue(o, memberInfo, value, null);

            return value;
        }
        private static object AddEventHandler_object_object_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            var eventInfo = (EventInfo)fnArgs[0].EvalObject(scope, inst, cstack);
            object o = fnArgs[1].Kind != EvalUnitKind.Empty ? fnArgs[1].EvalObject(scope, inst, cstack) : null;
            var value = (Delegate)fnArgs[2].EvalObject(scope, inst, cstack);
            eventInfo.AddEventHandler(o, value);

            return null;
        }
        private static object RemoveEventHandler_object_object_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            var eventInfo = (EventInfo)fnArgs[0].EvalObject(scope, inst, cstack);
            object o = fnArgs[1].Kind != EvalUnitKind.Empty ? fnArgs[1].EvalObject(scope, inst, cstack) : null;
            var value = (Delegate)fnArgs[2].EvalObject(scope, inst, cstack);
            eventInfo.RemoveEventHandler(o, value);

            return null;
        }

        private static object GetMethod_object_string_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            object obj = fnArgs[0].EvalObject(scope, inst, cstack);
            string member = fnArgs[1].EvalString(scope, inst, cstack);
            object[] args = EvalArgs(2, fnArgs, scope, inst, cstack);
            Type[] parameterTypes = args != null ? Array.ConvertAll(args, prm => (Type)prm) : new Type[0];

            Type atType;
            var memberInfo = GetMethod(member, obj, parameterTypes, out atType);
            return memberInfo;
        }

        private static object GetMember_object_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetMembers_object_string(fnArgs, scope, srcInst, inst, cstack, csrc).FirstOrDefault();
        private static object[] GetMembers_object_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            object obj = fnArgs[0].EvalObject(scope, inst, cstack);
            string member = fnArgs[1].EvalString(scope, inst, cstack);

            Type atType = GetTargetType(ref member, obj);
            var memberInfo = atType.GetMember(member);

            return memberInfo;
        }

        private static object GetMemberValue_object_string_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            object obj = fnArgs[0].EvalObject(scope, inst, cstack);
            string member = fnArgs[1].EvalString(scope, inst, cstack);
            object[] args = EvalArgs(2, fnArgs, scope, inst, cstack);


            Type atType;
            var memberInfo = GetPropertyOrField(member, obj, out atType);
            if (memberInfo == null) throw new ScriptExecutionException($"Member '{member}' not found at '{atType}'.");

            return GetValue(memberInfo, obj, args);

        }
        private static object GetValue(MemberInfo memberInfo, object o = null, object[] args = null)
        {
            if (memberInfo.MemberType == MemberTypes.Property)
                return ((PropertyInfo)memberInfo).GetValue(o, args);
            else if (memberInfo.MemberType == MemberTypes.Field)
            {
                var v = ((FieldInfo)memberInfo).GetValue(o);
                if (args == null)
                    return v;
                else
                {
                    return ((Array)v).GetValue(GetIndexes(args));
                }
            }
            else
                return memberInfo;




        }
        static int[] GetIndexes(object[] args)
        {
            int[] indexes = new int[args.Length];
            try { args.CopyTo(indexes, 0); }
            catch (InvalidCastException)
            {
                throw new ArgumentException("Indexes must be of type int.");
            }
            return indexes;
        }

        private static T GetValue<T>(MemberInfo memberInfo, object o = null, object[] args = null) => (T)GetValue(memberInfo, o, args);


        private static object SetMemberValue_object_string_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            object obj = fnArgs[0].EvalObject(scope, inst, cstack);
            string member = fnArgs[1].EvalString(scope, inst, cstack);
            object value = fnArgs[2].EvalObject(scope, inst, cstack);
            object[] args = EvalArgs(3, fnArgs, scope, inst, cstack);

            MemberInfo memberInfo = null;

            Type atType;
            memberInfo = GetPropertyOrField(member, obj, out atType);
            if (memberInfo == null) throw new ScriptExecutionException($"Member '{member}' not found at '{atType}'.");
            SetValue(obj, memberInfo, value, args);
            return value;
        }
        private static void SetValue(object o, MemberInfo memberInfo, object value, object[] args)
        {
            if (memberInfo.MemberType == MemberTypes.Property)
                ((PropertyInfo)memberInfo).SetValue(o, value, args);
            else if (memberInfo.MemberType == MemberTypes.Field)
            {

                var field = (FieldInfo)memberInfo;
                if (field.IsInitOnly) throw new ArgumentException($"Field '{field.Name}' is read only.");
                if (args == null) field.SetValue(o, value);
                else
                {
                    var arr = (Array)field.GetValue(o);
                    arr.SetValue(value, GetIndexes(args));

                }
            }
            else if (memberInfo.MemberType == MemberTypes.Event)
                throw new ScriptExecutionException($"Can't set value to event '{memberInfo.Name}'. Should be used AddEventHandler.");
            else
                throw new ScriptExecutionException($"{memberInfo.MemberType} '{memberInfo.Name}' value setting not supported.");


        }

        private static object GetObjArgOrNull(EvalUnit fnArg, int scope, ClassInstance inst, CallStack cstack)
        {
            return fnArg.Kind != EvalUnitKind.Empty ? fnArg.EvalObject(scope, inst, cstack) : null;
        }

        static BindingFlags StrToBindingFlag(string str)
        {
            try
            {
                return (BindingFlags)Enum.Parse(typeof(BindingFlags), str);
            }
            catch
            {
                throw new ScriptExecutionException($"Can't convert string '{str}' to binding flag.");
            }
        }

        private static object InvokeMember_object_string_string_object_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => InvokeMember((Type)GetObjArgOrNull(fnArgs[0], scope, inst, cstack), fnArgs[1].EvalString(scope, inst, cstack), StrToBindingFlag(fnArgs[2].EvalString(scope, inst, cstack)), (Binder)GetObjArgOrNull(fnArgs[3], scope, inst, cstack), GetObjArgOrNull(fnArgs[4], scope, inst, cstack), null);
        private static object InvokeMember_object_string_string_object_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => InvokeMember((Type)GetObjArgOrNull(fnArgs[0], scope, inst, cstack), fnArgs[1].EvalString(scope, inst, cstack), StrToBindingFlag(fnArgs[2].EvalString(scope, inst, cstack)), (Binder)GetObjArgOrNull(fnArgs[3], scope, inst, cstack), GetObjArgOrNull(fnArgs[4], scope, inst, cstack), EvalArgs(5, fnArgs, scope, inst, cstack));
        private static object InvokeMember_object_string_object_object_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => InvokeMember((Type)GetObjArgOrNull(fnArgs[0], scope, inst, cstack), fnArgs[1].EvalString(scope, inst, cstack), (BindingFlags)fnArgs[2].EvalObject(scope, inst, cstack), (Binder)GetObjArgOrNull(fnArgs[3], scope, inst, cstack), GetObjArgOrNull(fnArgs[4], scope, inst, cstack), null);
        private static object InvokeMember_object_string_object_object_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => InvokeMember((Type)GetObjArgOrNull(fnArgs[0], scope, inst, cstack), fnArgs[1].EvalString(scope, inst, cstack), (BindingFlags)fnArgs[2].EvalObject(scope, inst, cstack), (Binder)GetObjArgOrNull(fnArgs[3], scope, inst, cstack), GetObjArgOrNull(fnArgs[4], scope, inst, cstack), EvalArgs(5, fnArgs, scope, inst, cstack));
        private static object InvokeMember_object_string_int_object_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => InvokeMember((Type)GetObjArgOrNull(fnArgs[0], scope, inst, cstack), fnArgs[1].EvalString(scope, inst, cstack), (BindingFlags)fnArgs[2].EvalInt(scope, inst, cstack), (Binder)GetObjArgOrNull(fnArgs[3], scope, inst, cstack), GetObjArgOrNull(fnArgs[4], scope, inst, cstack), null);
        private static object InvokeMember_object_string_int_object_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => InvokeMember((Type)GetObjArgOrNull(fnArgs[0], scope, inst, cstack), fnArgs[1].EvalString(scope, inst, cstack), (BindingFlags)fnArgs[2].EvalInt(scope, inst, cstack), (Binder)GetObjArgOrNull(fnArgs[3], scope, inst, cstack), GetObjArgOrNull(fnArgs[4], scope, inst, cstack), EvalArgs(5, fnArgs, scope, inst, cstack));

        private static object InvokeMember(Type type, string member, BindingFlags flags, Binder binder, object obj, object[] args)
        {
            if (type == null)
                type = obj.GetType();

            return type.InvokeMember(member, flags, binder, obj, args);
        }

        private static string ReadAllText_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => File.ReadAllText(GetFullPath(fnArgs[0].EvalString(scope, inst, cstack), cstack, csrc));
        private static string ReadAllText_string_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => File.ReadAllText(GetFullPath(fnArgs[0].EvalString(scope, inst, cstack), cstack, csrc), Encoding.GetEncoding(fnArgs[1].EvalString(scope, inst, cstack)));
        private static string ReadAllText_string_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => File.ReadAllText(GetFullPath(fnArgs[0].EvalString(scope, inst, cstack), cstack, csrc), Encoding.GetEncoding(fnArgs[1].EvalInt(scope, inst, cstack)));
        private static string ReadAllText_string_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => File.ReadAllText(GetFullPath(fnArgs[0].EvalString(scope, inst, cstack), cstack, csrc), (Encoding)fnArgs[1].EvalObject(scope, inst, cstack));


        static string GetFullPath(string path, CallStack cstack, EvalUnit csrc) => Path.IsPathFullyQualified(path) ? path : Path.GetFullPath(path, GetBasePath(cstack, csrc));
        static string GetBasePath(CallStack cstack, EvalUnit csrc) => cstack != null ? cstack.GetBasePath() : csrc.CU.Fn.BasePath;
        private static object WriteAllText_string_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { File.WriteAllText(GetFullPath(fnArgs[0].EvalString(scope, inst, cstack), cstack, csrc), fnArgs[1].EvalString(scope, inst, cstack)); return null; }
        private static object WriteAllText_string_string_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { File.WriteAllText(GetFullPath(fnArgs[0].EvalString(scope, inst, cstack), cstack, csrc), fnArgs[1].EvalString(scope, inst, cstack), Encoding.GetEncoding(fnArgs[2].EvalString(scope, inst, cstack))); return null; }
        private static object WriteAllText_string_string_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { File.WriteAllText(GetFullPath(fnArgs[0].EvalString(scope, inst, cstack), cstack, csrc), fnArgs[1].EvalString(scope, inst, cstack), Encoding.GetEncoding(fnArgs[2].EvalInt(scope, inst, cstack))); return null; }
        private static object WriteAllText_string_string_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { File.WriteAllText(GetFullPath(fnArgs[0].EvalString(scope, inst, cstack), cstack, csrc), fnArgs[1].EvalString(scope, inst, cstack), (Encoding)fnArgs[2].EvalObject(scope, inst, cstack)); return null; }

        private static object AppendAllText_string_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { File.AppendAllText(GetFullPath(fnArgs[0].EvalString(scope, inst, cstack), cstack, csrc), fnArgs[1].EvalString(scope, inst, cstack)); return null; }
        private static object AppendAllText_string_string_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { File.AppendAllText(GetFullPath(fnArgs[0].EvalString(scope, inst, cstack), cstack, csrc), fnArgs[1].EvalString(scope, inst, cstack), Encoding.GetEncoding(fnArgs[2].EvalString(scope, inst, cstack))); return null; }
        private static object AppendAllText_string_string_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { File.AppendAllText(GetFullPath(fnArgs[0].EvalString(scope, inst, cstack), cstack, csrc), fnArgs[1].EvalString(scope, inst, cstack), Encoding.GetEncoding(fnArgs[2].EvalInt(scope, inst, cstack))); return null; }
        private static object AppendAllText_string_string_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { File.AppendAllText(GetFullPath(fnArgs[0].EvalString(scope, inst, cstack), cstack, csrc), fnArgs[1].EvalString(scope, inst, cstack), (Encoding)fnArgs[2].EvalObject(scope, inst, cstack)); return null; }
        private static object GetEncoding_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Encoding.GetEncoding(fnArgs[0].EvalInt(scope, inst, cstack));
        private static object GetEncoding_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Encoding.GetEncoding(fnArgs[0].EvalString(scope, inst, cstack));

        private static object CopyFile_string_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            File.Copy(GetFullPath(fnArgs[0].EvalString(scope, inst, cstack), cstack, csrc), GetFullPath(fnArgs[1].EvalString(scope, inst, cstack), cstack, csrc));
            return null;
        }
        private static object CopyFile_string_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            File.Copy(GetFullPath(fnArgs[0].EvalString(scope, inst, cstack), cstack, csrc), GetFullPath(fnArgs[1].EvalString(scope, inst, cstack), cstack, csrc), fnArgs[2].EvalBool(scope, inst, cstack));
            return null;
        }

        private static object MoveFile_string_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            File.Move(GetFullPath(fnArgs[0].EvalString(scope, inst, cstack), cstack, csrc), GetFullPath(fnArgs[1].EvalString(scope, inst, cstack), cstack, csrc));
            return null;
        }
        private static object MoveFile_string_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            File.Move(GetFullPath(fnArgs[0].EvalString(scope, inst, cstack), cstack, csrc), GetFullPath(fnArgs[1].EvalString(scope, inst, cstack), cstack, csrc), fnArgs[2].EvalBool(scope, inst, cstack));
            return null;
        }
        private static object DeleteFile_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { File.Delete(GetFullPath(fnArgs[0].EvalString(scope, inst, cstack), cstack, csrc)); return null; }

        private static object CreateDir_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            Directory.CreateDirectory(GetFullPath(fnArgs[0].EvalString(scope, inst, cstack), cstack, csrc));
            return null;
        }

        private static object DeleteDir_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            Directory.Delete(GetFullPath(fnArgs[0].EvalString(scope, inst, cstack), cstack, csrc));
            return null;
        }
        private static object DeleteDir_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            Directory.Delete(GetFullPath(fnArgs[0].EvalString(scope, inst, cstack), cstack, csrc), fnArgs[1].EvalBool(scope, inst, cstack));
            return null;
        }

        private static object MoveDir_string_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            Directory.Move(GetFullPath(fnArgs[0].EvalString(scope, inst, cstack), cstack, csrc), GetFullPath(fnArgs[1].EvalString(scope, inst, cstack), cstack, csrc));
            return null;
        }
        private static string[] GetFiles_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Directory.GetFiles(GetFullPath(fnArgs[0].EvalString(scope, inst, cstack), cstack, csrc));
        private static string[] GetFiles_string_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Directory.GetFiles(GetFullPath(fnArgs[0].EvalString(scope, inst, cstack), cstack, csrc), fnArgs[1].EvalString(scope, inst, cstack));
        private static string[] GetFiles_string_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Directory.GetFiles(GetFullPath(fnArgs[0].EvalString(scope, inst, cstack), cstack, csrc), fnArgs[1].EvalString(scope, inst, cstack), fnArgs[2].EvalBool(scope, inst, cstack) ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
        private static string GetFileName_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Path.GetFileName(fnArgs[0].EvalString(scope, inst, cstack));
        private static string GetFileNameWithoutExtension_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Path.GetFileNameWithoutExtension(fnArgs[0].EvalString(scope, inst, cstack));

        private static string GetExtension_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Path.GetExtension(fnArgs[0].EvalString(scope, inst, cstack));

        private static string[] GetDirs_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Directory.GetDirectories(GetFullPath(fnArgs[0].EvalString(scope, inst, cstack), cstack, csrc));
        private static string[] GetDirs_string_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Directory.GetDirectories(GetFullPath(fnArgs[0].EvalString(scope, inst, cstack), cstack, csrc), fnArgs[1].EvalString(scope, inst, cstack));
        private static string[] GetDirs_string_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Directory.GetDirectories(GetFullPath(fnArgs[0].EvalString(scope, inst, cstack), cstack, csrc), fnArgs[1].EvalString(scope, inst, cstack), fnArgs[2].EvalBool(scope, inst, cstack) ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
        private static long FileSize_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            string path = fnArgs[0].EvalString(scope, inst, cstack);
            path = GetFullPath(path, cstack, csrc);
            FileInfo fi = new FileInfo(path);
            return fi.Length;
        }
        private static bool FileExists_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => File.Exists(GetFullPath(fnArgs[0].EvalString(scope, inst, cstack), cstack, csrc));

        private static bool DirExists_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Directory.Exists(GetFullPath(fnArgs[0].EvalString(scope, inst, cstack), cstack, csrc));
        private static string ToJson_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => SerializeJson(fnArgs[0].EvalObject(scope, inst, cstack));
        private static string ToJson_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => SerializeJson(fnArgs[0].EvalObject(scope, inst, cstack), fnArgs[1].EvalBool(scope, inst, cstack));
        private static string CallChain_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => CallChain(fnArgs[0].EvalString(scope, inst, cstack), cstack);
        private static string CallChain(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => CallChain("->", cstack);

        private static string CallChain(string separator, CallStack cstack)
        {

            var items = cstack.Items.ToArray();
            string s = "";

            for (int i = 0; i < items.Length; i++)
            {
                var csitem = items[i];
                var fn = csitem.Fn;



                int layer = fn.OrigFn.Class.FnLayers[fn.OrigFn];
                bool orig = csitem.FnClass == fn.Class;
                s += $"{(orig ? "" : csitem.FnClass.ClassFullName + ":")}{ fn.Class.ClassFullName}.{new string(FunctionLayerPrefix, layer)}{ FormatFuncSign(fn.Signature)}{separator}";

            }
            s = s.Substring(0, s.Length - separator.Length);
            return s;
        }

        private static string SerializeJson(object o, bool withPrivate = false)
        {
            string s = "";
            CustomObject custom = o as CustomObject;
            bool isCustom = custom != null;

            if (isCustom && !custom.Type.IsArray)
            {
                var ci = (ClassInstance)(CustomObject)o;

                s = "{";
                List<Var> vars = GetVars(ci, !withPrivate);
                for (int n = 0; n < vars.Count; n++)
                {
                    s += $"\"{vars[n].Name}\":" + SerializeJson(vars[n].Value, !withPrivate) + ",";
                }
                if (vars.Count > 0) s = s.Remove(s.Length - 1, 1);
                s += "}";
            }
            else
            {
                if (isCustom || o.GetType() == TypeOfObjectArray)
                {

                    Array arr;
                    if (isCustom) arr = (Array)(CustomObject)o; else arr = (Array)o;
                    s = "[";
                    foreach (object item in arr) s += SerializeJson(item, !withPrivate) + ",";

                    if (arr.Length > 0) s = s.Remove(s.Length - 1, 1);
                    s += "]";
                }
                else
                {
                    s += System.Text.Json.JsonSerializer.Serialize(o);
                }
            }

            return s;
        }
        private static List<Var> GetVars(ClassInstance ci, bool pub = true)
        {
            var c = ci.Class;
            int scope = ci.Scope;
            List<Var> vars = (pub ? c.InstVars.Where(x => x.IsPublic) : c.InstVars).Select(x => new Var(x.Name, GetVarValue(ci.Exec, x.Type.ID, x.Id, scope))).ToList();

            return vars;
        }
        public static List<VarName> GetVarNames(ScriptClass c, bool pub = false)
        {

            List<VarName> vars = (pub ? c.InstVars.Where(x => x.IsPublic) : c.InstVars).Select(x => new VarName(c.CurScript, x.Name, x.Type)).ToList();
            return vars;
        }

        private static TypeArg GetTypeArgFromStr(string typeName)
        {
            TypeID tId;
            CustomType ct = null;
            Type t = null;

            t = Type.GetType(typeName, true);

            if (!TypeIds.TryGetValue(t, out tId)) tId = TypeID.Object;

            return new TypeArg(t, ct, tId);
        }
        private static TypeArg GetTypeArgFromObj(object type, bool allTypes = true)
        {

            if (type is Type t)
                return GetTypeArgFromType(t, allTypes);
            else
                return GetTypeArgFromCustomType((CustomType)type);

        }
        private static TypeArg GetTypeArgFromType(Type type, bool allTypes = true)
        {
            TypeID tId;
            CustomType ct = null;
            Type t = null;

            tId = GetTypeID(type, allTypes);
            t = type;
            return new TypeArg(t, ct, tId);
        }
        private static TypeArg GetTypeArgFromCustomType(CustomType ct)
        {
            TypeID tId = ct.IsArray ? TypeID.CustomArray : TypeID.Custom;
            return new TypeArg(typeof(CustomObject), ct, tId);
        }

        private static object FromJson_string_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FromJson(fnArgs[0].EvalString(scope, inst, cstack), GetTypeArgFromStr(fnArgs[1].EvalString(scope, inst, cstack)), false, scope, inst, cstack);

        private static object FromJson_string_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FromJson(fnArgs[0].EvalString(scope, inst, cstack), GetTypeArgFromStr(fnArgs[1].EvalString(scope, inst, cstack)), fnArgs[2].EvalBool(scope, inst, cstack), scope, inst, cstack);

        private static object FromJson_string_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FromJson(fnArgs[0].EvalString(scope, inst, cstack), GetTypeArgFromObj(fnArgs[1].EvalObject(scope, inst, cstack), true), false, scope, inst, cstack);
        private static object FromJson_string_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FromJson(fnArgs[0].EvalString(scope, inst, cstack), GetTypeArgFromObj(fnArgs[1].EvalObject(scope, inst, cstack)), fnArgs[2].EvalBool(scope, inst, cstack), scope, inst, cstack);

        private static CustomObject FromJson_string_customType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (CustomObject)FromJson(fnArgs[0].EvalString(scope, inst, cstack), GetTypeArgFromCustomType((CustomType)fnArgs[1].EvalObject(scope, inst, cstack)), false, scope, inst, cstack);
        private static CustomObject FromJson_string_customType_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (CustomObject)FromJson(fnArgs[0].EvalString(scope, inst, cstack), GetTypeArgFromCustomType((CustomType)fnArgs[1].EvalObject(scope, inst, cstack)), fnArgs[2].EvalBool(scope, inst, cstack), scope, inst, cstack);
        private static object FromJson(string json, TypeArg typeArg, bool withPrivate, int scope, ClassInstance inst, CallStack cstack)
        {
            if (typeArg.Id == TypeID.None)
                return System.Text.Json.JsonSerializer.Deserialize(json, typeArg.Type);

            var jsDoc = System.Text.Json.JsonDocument.Parse(json);
            var jsElement = jsDoc.RootElement;

            return DeserializeJson(in jsElement, typeArg.Id, typeArg.CType, withPrivate, scope, inst, cstack);
        }
        private static object DeserializeJson(in System.Text.Json.JsonElement je, TypeID type, CustomType ctype, bool withPrivate, int scope, ClassInstance inst, CallStack cstack)
        {
            bool isCustomArray = type == TypeID.CustomArray;
            if (isCustomArray || type == TypeID.Custom)
            {

                if (isCustomArray)
                {

                    int size = je.GetArrayLength();
                    TypeID itemType;
                    CustomType ct = null;
                    Array arr;
                    if (isCustomArray)
                    {
                        arr = new CustomObject[size];
                        itemType = TypeID.Custom;
                        ct = CustomType.Get(ctype.Class);
                    }
                    else
                    {
                        arr = new object[size];
                        itemType = TypeID.Object;
                    }
                    int i = 0;
                    foreach (var item in je.EnumerateArray())
                    {
                        var j = item;

                        arr.SetValue(DeserializeJson(in j, itemType, ct, withPrivate, scope, inst, cstack), i++);
                    }
                    return isCustomArray ? new CustomObject(inst.Exec, arr, ctype.Class, true) : arr;

                }

                ArgBlocks[] nest = new ArgBlocks[] { new ArgBlocks() };
                ClassInstance o = (ClassInstance)NewClassInstance(ctype.Class, nest, scope, inst, true);
                List<VarName> vars = GetVarNames(o.Class, !withPrivate);
                int newScope = o.Scope;
                Executor exec = inst.Exec;
                for (int i = 0; i < vars.Count; i++)
                {

                    System.Text.Json.JsonElement el;

                    var v = vars[i];
                    if (je.TryGetProperty(v.Name, out el))
                    {
                        int varId = v.Id;

                        if (v.Type.ID == TypeID.Custom || v.Type.ID == TypeID.CustomArray)
                        {

                            var ct = v.Type.CType;
                            CustomObject val = (CustomObject)DeserializeJson(in el, v.Type.ID, ct, withPrivate, newScope, o, cstack);
                            ScriptVars<CustomObject>.Set(exec, varId, newScope, ref val);
                        }
                        else
                        {

                            if (!TypeIsArray(v.Type.ID))
                            {
                                switch (v.Type.ID)
                                {
                                    case TypeID.Int: { int val = el.GetInt32(); ScriptVars<int>.Set(exec, varId, newScope, ref val); break; }
                                    case TypeID.Long: { long val = el.GetInt64(); ScriptVars<long>.Set(exec, varId, newScope, ref val); break; }
                                    case TypeID.Byte: { byte val = el.GetByte(); ScriptVars<byte>.Set(exec, varId, newScope, ref val); break; }
                                    case TypeID.Short: { short val = el.GetInt16(); ScriptVars<short>.Set(exec, varId, newScope, ref val); break; }
                                    case TypeID.Float: { float val = el.GetSingle(); ScriptVars<float>.Set(exec, varId, newScope, ref val); break; }
                                    case TypeID.Double: { double val = el.GetDouble(); ScriptVars<double>.Set(exec, varId, newScope, ref val); break; }
                                    case TypeID.Decimal: { decimal val = el.GetDecimal(); ScriptVars<decimal>.Set(exec, varId, newScope, ref val); break; }
                                    case TypeID.Bool: { bool val = el.GetBoolean(); ScriptVars<bool>.Set(exec, varId, newScope, ref val); break; }
                                    case TypeID.Date: { DateTime val = el.GetDateTime(); ScriptVars<DateTime>.Set(exec, varId, newScope, ref val); break; }
                                    case TypeID.String: { string val = el.GetString(); ScriptVars<string>.Set(exec, varId, newScope, ref val); break; }
                                    case TypeID.Char: { char val = el.GetString()[0]; ScriptVars<char>.Set(exec, varId, newScope, ref val); break; }
                                    case TypeID.Object: { object val = el.GetString(); ScriptVars<object>.Set(exec, varId, newScope, ref val); break; }

                                    default: throw new ScriptExecutionException($"Wrong type '{v.Type.ID}'.");
                                }
                            }
                            else
                            {

                                switch (v.Type.ID)
                                {
                                    case TypeID.IntArray: { int[] val = el.EnumerateArray().Select(x => x.GetInt32()).ToArray(); ScriptVars<int[]>.Set(exec, varId, newScope, ref val); break; }
                                    case TypeID.LongArray: { long[] val = el.EnumerateArray().Select(x => x.GetInt64()).ToArray(); ScriptVars<long[]>.Set(exec, varId, newScope, ref val); break; }
                                    case TypeID.ByteArray: { byte[] val = el.ValueKind == System.Text.Json.JsonValueKind.Array ? el.EnumerateArray().Select(x => x.GetByte()).ToArray() : el.GetBytesFromBase64(); ScriptVars<byte[]>.Set(exec, varId, newScope, ref val); break; }
                                    case TypeID.ShortArray: { short[] val = el.EnumerateArray().Select(x => x.GetInt16()).ToArray(); ScriptVars<short[]>.Set(exec, varId, newScope, ref val); break; }
                                    case TypeID.FloatArray: { float[] val = el.EnumerateArray().Select(x => x.GetSingle()).ToArray(); ScriptVars<float[]>.Set(exec, varId, newScope, ref val); break; }
                                    case TypeID.DoubleArray: { double[] val = el.EnumerateArray().Select(x => x.GetDouble()).ToArray(); ScriptVars<double[]>.Set(exec, varId, newScope, ref val); break; }
                                    case TypeID.DecimalArray: { decimal[] val = el.EnumerateArray().Select(x => x.GetDecimal()).ToArray(); ScriptVars<decimal[]>.Set(exec, varId, newScope, ref val); break; }
                                    case TypeID.BoolArray: { bool[] val = el.EnumerateArray().Select(x => x.GetBoolean()).ToArray(); ScriptVars<bool[]>.Set(exec, varId, newScope, ref val); break; }
                                    case TypeID.DateArray: { DateTime[] val = el.EnumerateArray().Select(x => x.GetDateTime()).ToArray(); ScriptVars<DateTime[]>.Set(exec, varId, newScope, ref val); break; }
                                    case TypeID.StringArray: { string[] val = el.EnumerateArray().Select(x => x.GetString()).ToArray(); ScriptVars<string[]>.Set(exec, varId, newScope, ref val); break; }
                                    case TypeID.CharArray: { char[] val = el.EnumerateArray().Select(x => x.GetString()[0]).ToArray(); ScriptVars<char[]>.Set(exec, varId, newScope, ref val); break; }
                                    case TypeID.ObjectArray: { object[] val = el.EnumerateArray().Select(x => x.GetString()).ToArray(); ScriptVars<object[]>.Set(exec, varId, newScope, ref val); break; }
                                    default: throw new ScriptExecutionException($"Wrong type '{v.Type.ID}'.");
                                }

                            }

                        }
                    }
                }
                return new CustomObject(inst.Exec, o, o.Class);
            }

            switch (type)
            {
                case TypeID.Int: return je.GetUInt32();
                case TypeID.Long: return je.GetUInt64();
                case TypeID.Byte: return je.GetByte();
                case TypeID.Short: return je.GetUInt16();
                case TypeID.Float: return je.GetSingle();
                case TypeID.Double: return je.GetDouble();
                case TypeID.Decimal: return je.GetDecimal();
                case TypeID.Date: return je.GetDateTime();
                case TypeID.Bool: return je.GetBoolean();
                case TypeID.String: return je.GetString();
                case TypeID.Char: return je.GetString()[0];
                case TypeID.Object: return je.GetString();

                case TypeID.IntArray: return je.EnumerateArray().Select(x => x.GetInt32()).ToArray<int>();
                case TypeID.LongArray: return je.EnumerateArray().Select(x => x.GetInt64()).ToArray<long>();
                case TypeID.ByteArray: return je.GetBytesFromBase64();
                case TypeID.ShortArray: return je.EnumerateArray().Select(x => x.GetInt16()).ToArray<short>();
                case TypeID.FloatArray: return je.EnumerateArray().Select(x => x.GetSingle()).ToArray<float>();
                case TypeID.DoubleArray: return je.EnumerateArray().Select(x => x.GetDouble()).ToArray<double>();
                case TypeID.DecimalArray: return je.EnumerateArray().Select(x => x.GetDecimal()).ToArray<decimal>();
                case TypeID.DateArray: return je.EnumerateArray().Select(x => x.GetDateTime()).ToArray<DateTime>();
                case TypeID.BoolArray: return je.EnumerateArray().Select(x => x.GetBoolean()).ToArray<bool>();
                case TypeID.StringArray: return je.EnumerateArray().Select(x => x.GetString()).ToArray<string>();
                case TypeID.CharArray: return je.EnumerateArray().Select(x => x.GetString()[0]).ToArray<char>();
                case TypeID.ObjectArray: return je.EnumerateArray().Select(x => (object)x).ToArray<object>();

                default:
                    throw new ScriptExecutionException($"Type '{type}' deserialization not supported.");
            }
        }

        private static object InterruptThread_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            Thread t = (Thread)fnArgs[0].EvalObject(scope, inst, cstack);
            t.Interrupt();
            return null;
        }
        private static object Sleep_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            int ms = fnArgs[0].EvalInt(scope, inst, cstack);
            Thread.Sleep(ms);
            return null;
        }

        private static bool ThreadIsAlive_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            Thread t = (Thread)fnArgs[0].EvalObject(scope, inst, cstack);
            return t.IsAlive;
        }
        private static object ThreadState_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            Thread t = (Thread)fnArgs[0].EvalObject(scope, inst, cstack);
            return t.ThreadState;

        }
        private static string ThreadName_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            Thread t = (Thread)fnArgs[0].EvalObject(scope, inst, cstack);
            return t.Name;

        }
        private static string ThreadName(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Thread.CurrentThread.Name;
        private static object CurrentThread(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Thread.CurrentThread;



        private static object Task_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => CreateTask(fnArgs, scope, inst, cstack, csrc, false);

        private static object TaskByFuncRef_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => CreateTask(fnArgs, scope, inst, cstack, csrc, true);
        private static object CreateTask(EvalUnit[] fnArgs, int scope, ClassInstance inst, CallStack cstack, EvalUnit csrc, bool byFnRef)
        {

            var fnRef = GetFuncRefArg(fnArgs[0], byFnRef, scope, inst, cstack, csrc);
            var args = GetSVArgs(1, fnArgs, scope, inst, cstack);
            var ts = new FuncStarter(args, csrc);
            return new Task<object>((object obj) => ((FuncStarter)obj).Start(fnRef), ts);

        }
        private static object StartTask_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            var t = (Task<object>)fnArgs[0].EvalObject(scope, inst, cstack);
            t.Start();

            return null;
        }
        private static object CancellationTokenSource(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => new CancellationTokenSource();
        private static object CancellationTokenSource_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => new CancellationTokenSource(fnArgs[0].EvalInt(scope, inst, cstack));
        private static object CancellationToken_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ((CancellationTokenSource)fnArgs[0].EvalObject(scope, inst, cstack)).Token;
        private static bool IsCancellationRequested_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ((CancellationToken)fnArgs[0].EvalObject(scope, inst, cstack)).IsCancellationRequested;
        private static object Cancel_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { ((CancellationTokenSource)fnArgs[0].EvalObject(scope, inst, cstack)).Cancel(); return null; }
        private static object Cancel_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { ((CancellationTokenSource)fnArgs[0].EvalObject(scope, inst, cstack)).Cancel(fnArgs[1].EvalBool(scope, inst, cstack)); return null; }

        private static object ThrowIfCancellationRequested_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { ((CancellationToken)fnArgs[0].EvalObject(scope, inst, cstack)).ThrowIfCancellationRequested(); return null; }



        class FuncStarter
        {



            public EvalUnit[] Args;
            public EvalUnit FnEU;
            public FuncStarter(EvalUnit[] args, EvalUnit csrc)
            {


                FnEU = csrc;

                Args = args;

            }
            public object Start(FuncReference fnRef)
            {
                try
                {
                    return CallFunc(fnRef.Func, fnRef.Inst, Args, -1, null, null, FnEU);
                }
                catch (ThreadInterruptedException)
                {
                    return null;
                }

            }
        }

        private static object Thread_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => CreateThread(GetFuncRef(fnArgs[0], scope, inst, cstack, csrc));

        private static object Thread_object_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => CreateThread(GetFuncRef(fnArgs[0], scope, inst, cstack, csrc), fnArgs[1].EvalString(scope, inst, cstack));

        private static object Thread_object_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => CreateThread(GetFuncRef(fnArgs[0], scope, inst, cstack, csrc), fnArgs[1].EvalString(scope, inst, cstack), fnArgs[2].EvalBool(scope, inst, cstack));
        private static object Thread_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => CreateThread(GetFuncRef(fnArgs[0], scope, inst, cstack, csrc), null, fnArgs[1].EvalBool(scope, inst, cstack));

        private static object ThreadByFuncRef_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => CreateThread((FuncReference)fnArgs[0].EvalObject(scope, inst, cstack));

        private static object ThreadByFuncRef_object_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => CreateThread((FuncReference)fnArgs[0].EvalObject(scope, inst, cstack), fnArgs[1].EvalString(scope, inst, cstack));

        private static object ThreadByFuncRef_object_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => CreateThread((FuncReference)fnArgs[0].EvalObject(scope, inst, cstack), fnArgs[1].EvalString(scope, inst, cstack), fnArgs[2].EvalBool(scope, inst, cstack));
        private static object ThreadByFuncRef_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => CreateThread((FuncReference)fnArgs[0].EvalObject(scope, inst, cstack), null, fnArgs[1].EvalBool(scope, inst, cstack));


        private static Thread CreateThread(FuncReference fnRef, string name = null, bool isBackground = false)
        {
            var t = new Thread((object obj) => { if (obj is FuncStarter ts) ts.Start(fnRef); else throw new InvalidOperationException($"Incorrect thread start ({fnRef})."); });
            t.Name = name;
            t.IsBackground = isBackground;
            return t;
        }

        private static object LocalThread_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => CreateLocalThread(fnArgs[0], scope, inst);

        private static object LocalThread_object_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => CreateLocalThread(fnArgs[0], scope, inst, fnArgs[1].EvalString(scope, inst, cstack));

        private static object LocalThread_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => CreateLocalThread(fnArgs[0], scope, inst, null, fnArgs[1].EvalBool(scope, inst, cstack));

        private static object LocalThread_object_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => CreateLocalThread(fnArgs[0], scope, inst, fnArgs[1].EvalString(scope, inst, cstack), fnArgs[2].EvalBool(scope, inst, cstack));


        private static object LocalThreadByExpr_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => CreateLocalThread(((Expr)fnArgs[0].EvalObject(scope, inst, cstack)).EU, scope, inst);

        private static object LocalThreadByExpr_object_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => CreateLocalThread(((Expr)fnArgs[0].EvalObject(scope, inst, cstack)).EU, scope, inst, fnArgs[1].EvalString(scope, inst, cstack));

        private static object LocalThreadByExpr_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => CreateLocalThread(((Expr)fnArgs[0].EvalObject(scope, inst, cstack)).EU, scope, inst, null, fnArgs[1].EvalBool(scope, inst, cstack));

        private static object LocalThreadByExpr_object_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => CreateLocalThread(((Expr)fnArgs[0].EvalObject(scope, inst, cstack)).EU, scope, inst, fnArgs[1].EvalString(scope, inst, cstack), fnArgs[2].EvalBool(scope, inst, cstack));



        private static Thread CreateLocalThread(EvalUnit eu, int scope, ClassInstance inst, string name = null, bool isBackground = false, bool run = false)
        {
            Thread t = new Thread((object obj) => Run(eu, scope, inst));
            t.Name = name;
            t.IsBackground = isBackground;
            if (run) t.Start();
            return t;
        }

        class RunThreadData
        {
            public int Scope;
            public ClassInstance Inst;
            public EvalUnit EU;
            public RunThreadData(EvalUnit eu, int scope, ClassInstance inst)
            {
                EU = eu;
                Scope = scope;
                Inst = inst;
            }
        }
        private static void Run(EvalUnit eu, int scope, ClassInstance inst)
        {


            try
            {
                eu.EvalObject(scope, inst, null);
            }
            catch (ThreadInterruptedException)
            {

            }
        }
        private static object RunAndReturn(EvalUnit eu, int scope, ClassInstance inst)
        {


            try
            {
                return eu.EvalObject(scope, inst, null);
            }
            catch (ThreadInterruptedException)
            {
                return null;
            }
        }

        private static object LocalTask_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            var t = new Task<object>(() => RunAndReturn(fnArgs[0], scope, inst));
            return t;
        }
        private static object LocalTaskByExpr_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            EvalUnit eu = ((Expr)fnArgs[0].EvalObject(scope, inst, cstack)).EU;
            var t = new Task<object>(() => RunAndReturn(eu, scope, inst));
            return t;
        }
        static EvalUnit CopyEUAndBoxLocal(EvalUnit eu, int scope, ClassInstance inst, CallStack cstack)
        {
            eu = CopyEU(eu);
            BoxAll(ref eu, scope, inst, cstack, true);
            return eu;
        }
        private static object RunTask_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {


            var eu = CopyEUAndBoxLocal(fnArgs[0], scope, inst, cstack);
            var t = new Task<object>(() => RunAndReturn(eu, scope, inst));
            t.Start();
            return t;
        }
        private static object RunLocalTask_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            var t = new Task<object>(() => RunAndReturn(fnArgs[0], scope, inst));
            t.Start();
            return t;
        }
        private static object RunLocalThread_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => CreateLocalThread(fnArgs[0], scope, inst, null, false, true);
        private static object RunLocalThread_object_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => CreateLocalThread(fnArgs[0], scope, inst, fnArgs[1].EvalString(scope, inst, cstack), false, true);
        private static object RunLocalThread_object_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => CreateLocalThread(fnArgs[0], scope, inst, fnArgs[1].EvalString(scope, inst, cstack), fnArgs[2].EvalBool(scope, inst, cstack), true);
        private static object RunLocalThread_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => CreateLocalThread(fnArgs[0], scope, inst, null, fnArgs[1].EvalBool(scope, inst, cstack), true);

        private static object RunThread_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => CreateLocalThread(CopyEUAndBoxLocal(fnArgs[0], scope, inst, cstack), scope, inst, null, false, true);
        private static object RunThread_object_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => CreateLocalThread(CopyEUAndBoxLocal(fnArgs[0], scope, inst, cstack), scope, inst, fnArgs[1].EvalString(scope, inst, cstack), false, true);
        private static object RunThread_object_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => CreateLocalThread(CopyEUAndBoxLocal(fnArgs[0], scope, inst, cstack), scope, inst, fnArgs[1].EvalString(scope, inst, cstack), fnArgs[2].EvalBool(scope, inst, cstack), true);
        private static object RunThread_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => CreateLocalThread(CopyEUAndBoxLocal(fnArgs[0], scope, inst, cstack), scope, inst, null, fnArgs[1].EvalBool(scope, inst, cstack), true);


        private static bool Wait_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            return Wait(fnArgs[0].EvalObject(scope, inst, cstack));
        }
        private static bool Wait_object_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            return Wait(fnArgs[0].EvalObject(scope, inst, cstack), fnArgs[1].EvalInt(scope, inst, cstack));
        }
        private static bool Wait(object obj, int ms = -1)
        {
            switch (obj)
            {
                case WaitHandle waitHandle:
                    return ms >= 0 ? waitHandle.WaitOne(ms) : waitHandle.WaitOne();

                case Thread thread:
                    if (ms >= 0)
                        return thread.Join(ms);
                    else
                    {
                        thread.Join();
                        return true;
                    }
                case Task task:
                    if (ms >= 0)
                        return task.Wait(ms);
                    else
                    {
                        task.Wait();
                        return true;
                    }

                default:
                    throw new ScriptExecutionException($"Invalid argument type '{obj.GetType().Name}'.");
            }
        }

        private static object TaskResult_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ((Task<object>)fnArgs[0].EvalObject(scope, inst, cstack)).Result;


        private static object TaskException_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ((Task<object>)fnArgs[0].EvalObject(scope, inst, cstack)).Exception;
        private static object TaskInnerException_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            var ex = ((Task<object>)fnArgs[0].EvalObject(scope, inst, cstack)).Exception;
            if (ex == null) return null;
            return ex.InnerException;
        }
        private static int TaskId_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ((Task<object>)fnArgs[0].EvalObject(scope, inst, cstack)).Id;
        private static int ManagedThreadId_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ((Thread)fnArgs[0].EvalObject(scope, inst, cstack)).ManagedThreadId;
        private static int ManagedThreadId(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Thread.CurrentThread.ManagedThreadId;

        private static object TaskStatus_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ((Task<object>)fnArgs[0].EvalObject(scope, inst, cstack)).Status;
        private static bool IsCanceled_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ((Task<object>)fnArgs[0].EvalObject(scope, inst, cstack)).IsCanceled;
        private static bool IsCompleted_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ((Task<object>)fnArgs[0].EvalObject(scope, inst, cstack)).IsCompleted;
        private static bool IsCompletedSuccessfully_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ((Task<object>)fnArgs[0].EvalObject(scope, inst, cstack)).IsCompletedSuccessfully;
        private static bool IsFaulted_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ((Task<object>)fnArgs[0].EvalObject(scope, inst, cstack)).IsFaulted;


        private static object LocalTimer_object_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => LocalTimer_object_int(fnArgs, scope, inst, cstack, false);
        private static object LocalTimer_object_int_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => LocalTimer_object_int_bool(fnArgs, scope, inst, cstack, false);
        private static object LocalTimer_object_int_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => LocalTimer_object_int_bool_bool(fnArgs, scope, inst, cstack, false);
        private static object LocalTimer_object_int_bool_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => LocalTimer_object_int_bool_bool_bool(fnArgs, scope, inst, cstack, false);
        private static object LocalTimerByExpr_object_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => LocalTimer_object_int(fnArgs, scope, inst, cstack, true);
        private static object LocalTimerByExpr_object_int_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => LocalTimer_object_int_bool(fnArgs, scope, inst, cstack, true);
        private static object LocalTimerByExpr_object_int_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => LocalTimer_object_int_bool_bool(fnArgs, scope, inst, cstack, true);
        private static object LocalTimerByExpr_object_int_bool_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => LocalTimer_object_int_bool_bool_bool(fnArgs, scope, inst, cstack, true);

        private static object RunTimer_object_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => CreateLocalTimer(fnArgs, scope, inst, cstack, false, true, true, false, true);
        private static object RunTimer_object_int_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => CreateLocalTimer(fnArgs, scope, inst, cstack, false, true, fnArgs[2].EvalBool(scope, inst, cstack), false, true);
        private static object RunTimer_object_int_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => CreateLocalTimer(fnArgs, scope, inst, cstack, false, true, fnArgs[2].EvalBool(scope, inst, cstack), fnArgs[3].EvalBool(scope, inst, cstack), true);



        private static System.Timers.Timer LocalTimer_object_int(EvalUnit[] fnArgs, int scope, ClassInstance inst, CallStack cstack, bool byExpr) => CreateLocalTimer(fnArgs, scope, inst, cstack, byExpr);



        private static System.Timers.Timer LocalTimer_object_int_bool(EvalUnit[] fnArgs, int scope, ClassInstance inst, CallStack cstack, bool byExpr) => CreateLocalTimer(fnArgs, scope, inst, cstack, byExpr, fnArgs[2].EvalBool(scope, inst, cstack));


        private static System.Timers.Timer LocalTimer_object_int_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance inst, CallStack cstack, bool byExpr) => CreateLocalTimer(fnArgs, scope, inst, cstack, byExpr, fnArgs[2].EvalBool(scope, inst, cstack), fnArgs[3].EvalBool(scope, inst, cstack));
        private static System.Timers.Timer LocalTimer_object_int_bool_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance inst, CallStack cstack, bool byExpr) => CreateLocalTimer(fnArgs, scope, inst, cstack, byExpr, fnArgs[2].EvalBool(scope, inst, cstack), fnArgs[3].EvalBool(scope, inst, cstack), fnArgs[4].EvalBool(scope, inst, cstack));

        private static System.Timers.Timer CreateLocalTimer(EvalUnit[] fnArgs, int scope, ClassInstance inst, CallStack cstack, bool byExpr, bool enabled = false, bool autoReset = true, bool flat = false, bool boxLocal = false)
        {
            int interval = fnArgs[1].EvalInt(scope, inst, cstack);
            System.Timers.Timer timer = new System.Timers.Timer(interval);

            EvalUnit eu = byExpr ? ((Expr)fnArgs[0].EvalObject(scope, inst, cstack)).EU : fnArgs[0];
            if (boxLocal) eu = CopyEUAndBoxLocal(eu, scope, inst, cstack);
            if (flat)
            {
                bool complete = true;
                timer.Elapsed += (sender, e) => { if (complete) { complete = false; eu.EvalObject(scope, inst, null); complete = true; } };
            }
            else
                timer.Elapsed += (sender, e) => eu.EvalObject(scope, inst, null);

            timer.AutoReset = autoReset;
            timer.Enabled = enabled;

            return timer;
        }
        private static System.Timers.Timer CreateTimer(EvalUnit[] fnArgs, int scope, ClassInstance inst, CallStack cstack, EvalUnit csrc, bool byFnRef, bool flat = false, EvalUnit[] extraArgs = null)
        {
            int interval = fnArgs[1].EvalInt(scope, inst, cstack);
            System.Timers.Timer timer = new System.Timers.Timer(interval);

            var fnRef = GetFuncRefArg(fnArgs[0], byFnRef, scope, inst, cstack, csrc);

            if (flat)
            {

                bool withArgs = extraArgs != null;
                var timerArgs = new EvalUnit[withArgs ? extraArgs.Length + 2 : 2];
                timerArgs[0] = EvalUnit.GetEUWithSpecificValue(null);
                timerArgs[1] = EvalUnit.GetEUWithSpecificValue(null);
                if (withArgs) extraArgs.CopyTo(timerArgs, 2);

                bool complete = true;
                timer.Elapsed += (sender, e) => { if (complete) { complete = false; CallFunc(fnRef.Func, fnRef.Inst, UpdateArgsForTimer(sender, e, timerArgs), -1, null, null, csrc); complete = true; } };

            }
            else
                timer.Elapsed += (sender, e) => CallFunc(fnRef.Func, fnRef.Inst, GetArgsForTimer(sender, e, extraArgs), -1, null, null, csrc);

            return timer;
        }
        private static EvalUnit[] UpdateArgsForTimer(object sender, object e, EvalUnit[] args)
        {
            args[0].SpecificValue = sender;
            args[1].SpecificValue = e;

            return args;

        }
        private static EvalUnit[] GetArgsForTimer(object sender, object e, EvalUnit[] args)
        {
            int c = args == null ? 0 : args.Length;

            var result = new EvalUnit[c + 2];
            result[0] = EvalUnit.GetEUWithSpecificValue(sender);
            result[1] = EvalUnit.GetEUWithSpecificValue(e);
            if (c > 0)
            {
                for (int i = 0; i < c; i++)
                    result[i + 2] = args[i];
            }
            return result;


        }
        private static object Timer_object_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Timer_object_int(fnArgs, scope, inst, cstack, csrc, false);


        private static object Timer_object_int_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Timer_object_int_bool(fnArgs, scope, inst, cstack, csrc, false);

        private static object Timer_object_int_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Timer_object_int_bool_bool(fnArgs, scope, inst, cstack, csrc, false);
        private static object Timer_object_int_bool_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Timer_object_int_bool_bool_bool(fnArgs, scope, inst, cstack, csrc, false);

        private static object TimerByFuncRef_object_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Timer_object_int(fnArgs, scope, inst, cstack, csrc, true);
        private static object TimerByFuncRef_object_int_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Timer_object_int_bool(fnArgs, scope, inst, cstack, csrc, true);
        private static object TimerByFuncRef_object_int_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Timer_object_int_bool_bool(fnArgs, scope, inst, cstack, csrc, true);
        private static object TimerByFuncRef_object_int_bool_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Timer_object_int_bool_bool_bool(fnArgs, scope, inst, cstack, csrc, true);

        private static System.Timers.Timer Timer_object_int(EvalUnit[] fnArgs, int scope, ClassInstance inst, CallStack cstack, EvalUnit csrc, bool byFnRef)
        {
            return CreateTimer(fnArgs, scope, inst, cstack, csrc, byFnRef);
        }
        private static System.Timers.Timer Timer_object_int_bool(EvalUnit[] fnArgs, int scope, ClassInstance inst, CallStack cstack, EvalUnit csrc, bool byFnRef)
        {
            System.Timers.Timer timer = CreateTimer(fnArgs, scope, inst, cstack, csrc, byFnRef);

            timer.Enabled = fnArgs[2].EvalBool(scope, inst, cstack);
            return timer;
        }
        private static System.Timers.Timer Timer_object_int_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance inst, CallStack cstack, EvalUnit csrc, bool byFnRef)
        {
            System.Timers.Timer timer = CreateTimer(fnArgs, scope, inst, cstack, csrc, byFnRef);
            timer.AutoReset = fnArgs[3].EvalBool(scope, inst, cstack);
            timer.Enabled = fnArgs[2].EvalBool(scope, inst, cstack);

            return timer;
        }
        private static System.Timers.Timer Timer_object_int_bool_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance inst, CallStack cstack, EvalUnit csrc, bool byFnRef)
        {
            bool flat = fnArgs[4].EvalBool(scope, inst, cstack);
            System.Timers.Timer timer = CreateTimer(fnArgs, scope, inst, cstack, csrc, byFnRef, flat);
            timer.AutoReset = fnArgs[3].EvalBool(scope, inst, cstack);
            timer.Enabled = fnArgs[2].EvalBool(scope, inst, cstack);

            return timer;
        }


        private static object Timer_object_int_bool_bool_bool_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Timer_object_int_bool_bool_bool_objectParams(fnArgs, scope, inst, cstack, csrc, false);
        private static object TimerByFuncRef_object_int_bool_bool_bool_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Timer_object_int_bool_bool_bool_objectParams(fnArgs, scope, inst, cstack, csrc, true);
        private static object Timer_object_int_bool_bool_bool_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance inst, CallStack cstack, EvalUnit csrc, bool byFnRef)
        {
            bool flat = fnArgs[4].EvalBool(scope, inst, cstack);
            var svArgs = GetSVArgs(5, fnArgs, scope, inst, cstack);
            System.Timers.Timer timer = CreateTimer(fnArgs, scope, inst, cstack, csrc, byFnRef, flat, svArgs);
            timer.AutoReset = fnArgs[3].EvalBool(scope, inst, cstack);
            timer.Enabled = fnArgs[2].EvalBool(scope, inst, cstack);

            return timer;
        }


        private static object StartTimer_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            System.Timers.Timer timer = (System.Timers.Timer)fnArgs[0].EvalObject(scope, inst, cstack);
            timer.Start();
            return null;
        }
        private static object StopTimer_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            System.Timers.Timer timer = (System.Timers.Timer)fnArgs[0].EvalObject(scope, inst, cstack);
            timer.Stop();
            return null;
        }
        private static object DisposeObject_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            object o = fnArgs[0].EvalObject(scope, inst, cstack);
            ((IDisposable)o).Dispose();
            return null;
        }

        private static object GetObjectType_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            object o = fnArgs[0].EvalObject(scope, inst, cstack);
            if (o is CustomObject) return ((CustomObject)o).Type;

            return o.GetType();
        }
        private static string GetObjectTypeName_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetObjectTypeName(fnArgs[0].EvalObject(scope, inst, cstack), false);
        private static string GetObjectTypeName_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetObjectTypeName(fnArgs[0].EvalObject(scope, inst, cstack), fnArgs[1].EvalBool(scope, inst, cstack));
        private static string GetObjectTypeName(object o, bool fullName)
        {
            if (o is CustomObject) return fullName ? ((CustomObject)o).Type.FullName : ((CustomObject)o).Type.Name;
            else return fullName ? o.GetType().ToString() : o.GetType().Name;

        }
        private static int GetObjectHashCode_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            object o = fnArgs[0].EvalObject(scope, inst, cstack);
            return o.GetHashCode();
        }
        private static bool Equals_object_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            object o = fnArgs[0].EvalObject(scope, inst, cstack);
            object o2 = fnArgs[1].EvalObject(scope, inst, cstack);
            return o.Equals(o2);
        }
        private static object StartThread_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            Thread t = (Thread)fnArgs[0].EvalObject(scope, inst, cstack);
            t.Start(new FuncStarter(null, csrc));

            return null;
        }

        private static EvalUnit[] GetSVArgs(int start, EvalUnit[] fnArgs, int scope, ClassInstance inst, CallStack cstack)
        {
            object[] vals = EvalArgs(start, fnArgs, scope, inst, cstack);

            if (vals == null) return null;
            EvalUnit[] args = new EvalUnit[vals.Length];
            for (int i = 0; i < args.Length; i++)
                args[i] = EvalUnit.GetEUWithSpecificValue(vals[i], fnArgs[i + start].Type);

            return args;
        }
        private static object StartThread_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            Thread t = (Thread)fnArgs[0].EvalObject(scope, inst, cstack);


            var args = GetSVArgs(1, fnArgs, scope, inst, cstack);


            t.Start(new FuncStarter(args, csrc));

            return null;
        }


        private static bool SetEventWaitHandle_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            EventWaitHandle e = (EventWaitHandle)fnArgs[0].EvalObject(scope, inst, cstack);

            return e.Set();

        }
        private static bool ResetEventWaitHandle_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            EventWaitHandle e = (EventWaitHandle)fnArgs[0].EvalObject(scope, inst, cstack);

            return e.Reset();

        }

        private static bool WaitAll_objectArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => WaitAll(fnArgs[0].EvalObjectArray(scope, inst, cstack));

        private static bool WaitAll_objectArray_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => WaitAll(fnArgs[0].EvalObjectArray(scope, inst, cstack), fnArgs[1].EvalInt(scope, inst, cstack));

        private static bool WaitAll(object[] o, int timeout = -1)
        {
            if (o[0] is WaitHandle)
            {
                WaitHandle[] e = o.Select(x => (WaitHandle)x).ToArray();
                return WaitHandle.WaitAll(e, timeout);
            }
            else
            {
                var tasks = o.Select(x => (Task<object>)x).ToArray();
                return Task.WaitAll(tasks, timeout);
            }
        }

        private static int WaitAny_objectArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => WaitAny(fnArgs[0].EvalObjectArray(scope, inst, cstack));
        private static int WaitAny_objectArray_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => WaitAny(fnArgs[0].EvalObjectArray(scope, inst, cstack), fnArgs[1].EvalInt(scope, inst, cstack));
        private static int WaitAny(object[] o, int timeout = -1)
        {
            if (o[0] is WaitHandle)
            {
                WaitHandle[] e = o.Select(x => (WaitHandle)x).ToArray();
                return WaitHandle.WaitAny(e, timeout);
            }
            else
            {
                var tasks = o.Select(x => (Task<object>)x).ToArray();
                return Task.WaitAny(tasks, timeout);
            }
        }

        private static object ChangeType_object_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            object obj = fnArgs[0].EvalObject(scope, inst, cstack);
            string typeName = fnArgs[1].EvalString(scope, inst, cstack);
            Type t = Type.GetType(typeName, true);
            return Convert.ChangeType(obj, t);
        }

        private static object ChangeType_object_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            object obj = fnArgs[0].EvalObject(scope, inst, cstack);
            Type t = (Type)fnArgs[1].EvalObject(scope, inst, cstack);
            return Convert.ChangeType(obj, t);
        }

        private static object ToEnum_string_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            string v = fnArgs[0].EvalString(scope, inst, cstack);
            string typeName = fnArgs[1].EvalString(scope, inst, cstack);
            Type t = Type.GetType(typeName, true);
            return Enum.Parse(t, v, true);
        }
        private static object ToEnum_string_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            string v = fnArgs[0].EvalString(scope, inst, cstack);
            Type t = (Type)fnArgs[1].EvalObject(scope, inst, cstack);

            return Enum.Parse(t, v, true);
        }
        private static object ToEnum_object_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            object v = fnArgs[0].EvalObject(scope, inst, cstack);
            Type t = (Type)fnArgs[1].EvalObject(scope, inst, cstack);

            return Enum.ToObject(t, v);
        }
        private static object ToEnum_object_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            object v = fnArgs[0].EvalObject(scope, inst, cstack);
            string typeName = fnArgs[1].EvalString(scope, inst, cstack);
            Type t = Type.GetType(typeName, true);

            return Enum.ToObject(t, v);
        }


        private static string UrlEncode_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => HttpUtility.UrlEncode(fnArgs[0].EvalString(scope, inst, cstack));
        private static string UrlEncode_string_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => HttpUtility.UrlEncode(fnArgs[0].EvalString(scope, inst, cstack), Encoding.GetEncoding(fnArgs[1].EvalString(scope, inst, cstack)));
        private static string UrlDecode_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => HttpUtility.UrlDecode(fnArgs[0].EvalString(scope, inst, cstack));
        private static string UrlDecode_string_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => HttpUtility.UrlDecode(fnArgs[0].EvalString(scope, inst, cstack), Encoding.GetEncoding(fnArgs[1].EvalString(scope, inst, cstack)));
        private static string UrlEncode_string_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => HttpUtility.UrlEncode(fnArgs[0].EvalString(scope, inst, cstack), (Encoding)fnArgs[1].EvalObject(scope, inst, cstack));
        private static string UrlDecode_string_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => HttpUtility.UrlDecode(fnArgs[0].EvalString(scope, inst, cstack), (Encoding)fnArgs[1].EvalObject(scope, inst, cstack));
        private static object Uri_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => new Uri(fnArgs[0].EvalString(scope, inst, cstack));

        private static string Fetch_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetFetchResult(Fetch(fnArgs[0].EvalString(scope, inst, cstack)));
        private static string Fetch_string_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetFetchResult(Fetch(fnArgs[0].EvalString(scope, inst, cstack), fnArgs[1].EvalString(scope, inst, cstack)));
        private static string Fetch_string_string_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetFetchResult(Fetch(fnArgs[0].EvalString(scope, inst, cstack), fnArgs[1].EvalString(scope, inst, cstack), fnArgs[2].EvalString(scope, inst, cstack)));
        private static string Fetch_string_string_string_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetFetchResult(Fetch(fnArgs[0].EvalString(scope, inst, cstack), fnArgs[1].EvalString(scope, inst, cstack), fnArgs[2].EvalString(scope, inst, cstack), fnArgs[3].EvalInt(scope, inst, cstack)));

        private static string Fetch_string_string_string_int_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetFetchResult(Fetch(fnArgs[0].EvalString(scope, inst, cstack), fnArgs[1].EvalString(scope, inst, cstack), fnArgs[2].EvalString(scope, inst, cstack), fnArgs[3].EvalInt(scope, inst, cstack), StrToHttpClientHandler(fnArgs[4].EvalString(scope, inst, cstack))));
        private static string Fetch_string_string_string_int_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetFetchResult(Fetch(fnArgs[0].EvalString(scope, inst, cstack), fnArgs[1].EvalString(scope, inst, cstack), fnArgs[2].EvalString(scope, inst, cstack), fnArgs[3].EvalInt(scope, inst, cstack), (HttpClientHandler)fnArgs[4].EvalObject(scope, inst, cstack)));

        private static string Fetch_string_string_string_int_object_string_string_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {

            string uri = fnArgs[0].EvalString(scope, inst, cstack);

            string headers = ArgExists(fnArgs, 1) ? fnArgs[1].EvalString(scope, inst, cstack) : "";
            string encodingName = ArgExists(fnArgs, 2) ? fnArgs[2].EvalString(scope, inst, cstack) : "";
            int timeout = ArgExists(fnArgs, 3) ? fnArgs[3].EvalInt(scope, inst, cstack) : 0;
            HttpClientHandler handler = null;
            if (ArgExists(fnArgs, 4))
            {
                handler = fnArgs[4].Type.ID == TypeID.String ? StrToHttpClientHandler(fnArgs[4].EvalString(scope, inst, cstack)) : (HttpClientHandler)fnArgs[4].EvalObject(scope, inst, cstack);
            }
            string postData = ArgExists(fnArgs, 5) ? fnArgs[5].EvalString(scope, inst, cstack) : "";
            string postDataEncoding = ArgExists(fnArgs, 6) ? fnArgs[6].EvalString(scope, inst, cstack) : "UTF-8";
            string postDataType = ArgExists(fnArgs, 7) ? fnArgs[7].EvalString(scope, inst, cstack) : "application/x-www-form-urlencoded";
            if (postDataEncoding.Length == 0) postDataEncoding = "UTF-8";

            return GetFetchResult(Fetch(uri, headers, encodingName, timeout, handler, postData, postDataEncoding, postDataType));
        }
        private static string GetFetchResult(Task<string> fetchTask)
        {
            try
            {
                return fetchTask.Result;
            }
            catch (AggregateException ex)
            {
                throw ex.InnerException;
            }
        }

        private static HttpClientHandler StrToHttpClientHandler(string str)
        {
            var handler = new HttpClientHandler();
            Type type = handler.GetType();
            KeyValuePair<string, string>[] props = str.Split(',').Select(x => { string[] s = x.Split('='); return (new KeyValuePair<string, string>(s[0].Trim(), s[1].Trim())); }).ToArray();
            object value = null;
            foreach (var p in props)
            {
                if (p.Value.Equals(TrueStr, StringComparison.OrdinalIgnoreCase) || p.Value.Equals(FalseStr, StringComparison.OrdinalIgnoreCase)) value = Convert.ToBoolean(p.Value);
                else if (!p.Value.StartsWith('"')) value = Convert.ToInt32(p.Value);
                else if (p.Key == "Proxy")
                {
                    string proxyStr = p.Value.Substring(1, p.Value.Length - 2);
                    string[] parts = proxyStr.Split(':');

                    System.Net.ICredentials credentials = null;

                    if (parts.Length > 3)
                    {
                        int i = proxyStr.LastIndexOf(':', proxyStr.LastIndexOf(':') - 1);
                        proxyStr = proxyStr.Substring(0, i);
                        credentials = new System.Net.NetworkCredential(parts[parts.Length - 2], parts[parts.Length - 1]);
                    }
                    value = new System.Net.WebProxy(proxyStr, true, null, credentials);
                }
                else value = p.Value.Substring(1, p.Value.Length - 2);

                type.InvokeMember(p.Key, BindingFlags.SetProperty, null, handler, new object[] { value });

            }

            return handler;
        }

        private static async Task<string> Fetch(string uri, string headers = "", string encName = "", int timeout = 0, HttpClientHandler handler = null, string postData = "", string postDataEncoding = "UTF-8", string postDataType = "application/x-www-form-urlencoded")
        {
            string data = "";
            using (var client = handler != null ? new HttpClient(handler) : new HttpClient())
            {
                if (timeout > 0) client.Timeout = TimeSpan.FromSeconds(timeout);

                if (!string.IsNullOrEmpty(headers))
                {
                    string[][] s = headers.Split("\r\n", StringSplitOptions.RemoveEmptyEntries).Select(x => x.Split(": ", 2)).ToArray();
                    for (int i = 0; i < s.Length; i++)
                        if (s[i].Length > 1) client.DefaultRequestHeaders.Add(s[i][0], s[i][1]);
                }

                HttpResponseMessage response;
                if (postData.Length > 0)
                {
                    StringContent postContent = new StringContent(postData, Encoding.GetEncoding(postDataEncoding), postDataType);
                    response = await client.PostAsync(uri, postContent);
                }
                else response = await client.GetAsync(uri);

                data = $"HTTP/{response.Version} {(int)response.StatusCode} {response.ReasonPhrase}\r\n";
                foreach (var item in response.Headers)
                    data += $"{item.Key}: {String.Join(",", item.Value)}\r\n";

                data += "\r\n";

                var bytes = await response.Content.ReadAsByteArrayAsync();

                if (string.IsNullOrEmpty(encName) && response.Content.Headers.ContentEncoding != null) encName = response.Content.Headers.ContentEncoding.FirstOrDefault();
                if (string.IsNullOrEmpty(encName) && response.Content.Headers.ContentType != null) encName = response.Content.Headers.ContentType.CharSet;

                Encoding enc = string.IsNullOrEmpty(encName) ? Encoding.Latin1 : Encoding.GetEncoding(encName);

                data += enc.GetString(bytes);

            }

            return data;
        }

        private static string[] RegexMatches_string_string_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RegexMatches(fnArgs[0].EvalString(scope, inst, cstack), fnArgs[1].EvalString(scope, inst, cstack), (RegexOptions)fnArgs[2].EvalInt(scope, inst, cstack));
        private static string[] RegexMatches_string_string_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RegexMatches(fnArgs[0].EvalString(scope, inst, cstack), fnArgs[1].EvalString(scope, inst, cstack), (RegexOptions)fnArgs[2].EvalObject(scope, inst, cstack));
        private static string[] RegexMatches_string_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RegexMatches(fnArgs[0].EvalString(scope, inst, cstack), fnArgs[1].EvalString(scope, inst, cstack));

        private static string[] RegexMatches(string input, string pattern, RegexOptions options = RegexOptions.None) => Regex.Matches(input, pattern, options).Select(m => m.Value).ToArray();

        private static string RegexReplace_string_string_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Regex.Replace(fnArgs[0].EvalString(scope, inst, cstack), fnArgs[1].EvalString(scope, inst, cstack), fnArgs[2].EvalString(scope, inst, cstack));
        private static string RegexReplace_string_string_string_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Regex.Replace(fnArgs[0].EvalString(scope, inst, cstack), fnArgs[1].EvalString(scope, inst, cstack), fnArgs[2].EvalString(scope, inst, cstack), (RegexOptions)fnArgs[3].EvalInt(scope, inst, cstack));
        private static string RegexReplace_string_string_string_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Regex.Replace(fnArgs[0].EvalString(scope, inst, cstack), fnArgs[1].EvalString(scope, inst, cstack), fnArgs[2].EvalString(scope, inst, cstack), (RegexOptions)fnArgs[3].EvalObject(scope, inst, cstack));

        private static string CombinePath_stringParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            string[] s = new string[fnArgs.Length];
            for (int i = 0; i < fnArgs.Length; i++) s[i] = fnArgs[i].EvalString(scope, inst, cstack);

            return Path.Combine(s);
        }

        private static object ReadKey_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => new PressedKey(Console.ReadKey(fnArgs[0].EvalBool(scope, inst, cstack)));
        private static object ReadKey(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => new PressedKey(Console.ReadKey());
        private static object ReadKey_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            Console.Write(fnArgs[0].EvalString(scope, inst, cstack));
            return new PressedKey(Console.ReadKey());
        }
        private static object ReadKey_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            Console.Write(fnArgs[0].EvalString(scope, inst, cstack));
            return new PressedKey(Console.ReadKey(fnArgs[1].EvalBool(scope, inst, cstack)));
        }
        private static object ReadKey_string_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            Console.Write(fnArgs[0].EvalString(scope, inst, cstack));
            var key = new PressedKey(Console.ReadKey(fnArgs[1].EvalBool(scope, inst, cstack)));
            if (fnArgs[2].EvalBool(scope, inst, cstack)) Console.WriteLine();
            return key;
        }
        private static object ReadKey_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            var key = new PressedKey(Console.ReadKey(fnArgs[0].EvalBool(scope, inst, cstack)));
            if (fnArgs[1].EvalBool(scope, inst, cstack)) Console.WriteLine();
            return key;
        }

        private static object KeyModifiers_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            PressedKey key = (PressedKey)fnArgs[0].EvalObject(scope, inst, cstack);

            return key.KeyInfo.Modifiers;
        }

        private static string ReadLine(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Console.ReadLine();
        private static string ReadLine_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { Console.Write(fnArgs[0].EvalString(scope, inst, cstack)); return Console.ReadLine(); }

        private static int If_bool_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalBool(scope, inst, cstack) ? fnArgs[1].EvalInt(scope, inst, cstack) : fnArgs[2].EvalInt(scope, inst, cstack);
        private static byte If_bool_byte_byte(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalBool(scope, inst, cstack) ? fnArgs[1].EvalByte(scope, inst, cstack) : fnArgs[2].EvalByte(scope, inst, cstack);
        private static string If_bool_string_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalBool(scope, inst, cstack) ? fnArgs[1].EvalString(scope, inst, cstack) : fnArgs[2].EvalString(scope, inst, cstack);
        private static char If_bool_char_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalBool(scope, inst, cstack) ? fnArgs[1].EvalChar(scope, inst, cstack) : fnArgs[2].EvalChar(scope, inst, cstack);
        private static long If_bool_long_long(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalBool(scope, inst, cstack) ? fnArgs[1].EvalLong(scope, inst, cstack) : fnArgs[2].EvalLong(scope, inst, cstack);
        private static double If_bool_double_double(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalBool(scope, inst, cstack) ? fnArgs[1].EvalDouble(scope, inst, cstack) : fnArgs[2].EvalDouble(scope, inst, cstack);
        private static float If_bool_float_float(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalBool(scope, inst, cstack) ? fnArgs[1].EvalFloat(scope, inst, cstack) : fnArgs[2].EvalFloat(scope, inst, cstack);
        private static bool If_bool_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalBool(scope, inst, cstack) ? fnArgs[1].EvalBool(scope, inst, cstack) : fnArgs[2].EvalBool(scope, inst, cstack);
        private static decimal If_bool_decimal_decimal(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalBool(scope, inst, cstack) ? fnArgs[1].EvalDecimal(scope, inst, cstack) : fnArgs[2].EvalDecimal(scope, inst, cstack);
        private static object If_bool_object_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalBool(scope, inst, cstack) ? fnArgs[1].EvalObject(scope, inst, cstack) : fnArgs[2].EvalObject(scope, inst, cstack);
        private static short If_bool_short_short(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalBool(scope, inst, cstack) ? fnArgs[1].EvalShort(scope, inst, cstack) : fnArgs[2].EvalShort(scope, inst, cstack);
        private static DateTime If_bool_date_date(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalBool(scope, inst, cstack) ? fnArgs[1].EvalDate(scope, inst, cstack) : fnArgs[2].EvalDate(scope, inst, cstack);
        private static CustomObject If_bool_custom_custom(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalBool(scope, inst, cstack) ? fnArgs[1].EvalCustom(scope, inst, cstack) : fnArgs[2].EvalCustom(scope, inst, cstack);
        private static int[] If_bool_intArray_intArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalBool(scope, inst, cstack) ? fnArgs[1].EvalIntArray(scope, inst, cstack) : fnArgs[2].EvalIntArray(scope, inst, cstack);
        private static byte[] If_bool_byteArray_byteArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalBool(scope, inst, cstack) ? fnArgs[1].EvalByteArray(scope, inst, cstack) : fnArgs[2].EvalByteArray(scope, inst, cstack);
        private static string[] If_bool_stringArray_stringArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalBool(scope, inst, cstack) ? fnArgs[1].EvalStringArray(scope, inst, cstack) : fnArgs[2].EvalStringArray(scope, inst, cstack);
        private static char[] If_bool_charArray_charArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalBool(scope, inst, cstack) ? fnArgs[1].EvalCharArray(scope, inst, cstack) : fnArgs[2].EvalCharArray(scope, inst, cstack);
        private static long[] If_bool_longArray_longArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalBool(scope, inst, cstack) ? fnArgs[1].EvalLongArray(scope, inst, cstack) : fnArgs[2].EvalLongArray(scope, inst, cstack);
        private static double[] If_bool_doubleArray_doubleArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalBool(scope, inst, cstack) ? fnArgs[1].EvalDoubleArray(scope, inst, cstack) : fnArgs[2].EvalDoubleArray(scope, inst, cstack);
        private static float[] If_bool_floatArray_floatArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalBool(scope, inst, cstack) ? fnArgs[1].EvalFloatArray(scope, inst, cstack) : fnArgs[2].EvalFloatArray(scope, inst, cstack);
        private static bool[] If_bool_boolArray_boolArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalBool(scope, inst, cstack) ? fnArgs[1].EvalBoolArray(scope, inst, cstack) : fnArgs[2].EvalBoolArray(scope, inst, cstack);
        private static decimal[] If_bool_decimalArray_decimalArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalBool(scope, inst, cstack) ? fnArgs[1].EvalDecimalArray(scope, inst, cstack) : fnArgs[2].EvalDecimalArray(scope, inst, cstack);
        private static object[] If_bool_objectArray_objectArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalBool(scope, inst, cstack) ? fnArgs[1].EvalObjectArray(scope, inst, cstack) : fnArgs[2].EvalObjectArray(scope, inst, cstack);
        private static short[] If_bool_shortArray_shortArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalBool(scope, inst, cstack) ? fnArgs[1].EvalShortArray(scope, inst, cstack) : fnArgs[2].EvalShortArray(scope, inst, cstack);
        private static DateTime[] If_bool_dateArray_dateArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalBool(scope, inst, cstack) ? fnArgs[1].EvalDateArray(scope, inst, cstack) : fnArgs[2].EvalDateArray(scope, inst, cstack);
        private static CustomObject If_bool_customArray_customArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].EvalBool(scope, inst, cstack) ? fnArgs[1].EvalCustomArray(scope, inst, cstack) : fnArgs[2].EvalCustomArray(scope, inst, cstack);


        private static CustomObject IfNotNull_object_custom(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].Ev<object>(scope, inst, cstack) != null ? fnArgs[1].EvalCustom(scope, inst, cstack) : null;
        private static int[] IfNotNull_object_intArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].Ev<object>(scope, inst, cstack) != null ? fnArgs[1].EvalIntArray(scope, inst, cstack) : null;
        private static byte[] IfNotNull_object_byteArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].Ev<object>(scope, inst, cstack) != null ? fnArgs[1].EvalByteArray(scope, inst, cstack) : null;
        private static string[] IfNotNull_object_stringArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].Ev<object>(scope, inst, cstack) != null ? fnArgs[1].EvalStringArray(scope, inst, cstack) : null;
        private static char[] IfNotNull_object_charArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].Ev<object>(scope, inst, cstack) != null ? fnArgs[1].EvalCharArray(scope, inst, cstack) : null;
        private static long[] IfNotNull_object_longArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].Ev<object>(scope, inst, cstack) != null ? fnArgs[1].EvalLongArray(scope, inst, cstack) : null;
        private static double[] IfNotNull_object_doubleArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].Ev<object>(scope, inst, cstack) != null ? fnArgs[1].EvalDoubleArray(scope, inst, cstack) : null;
        private static float[] IfNotNull_object_floatArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].Ev<object>(scope, inst, cstack) != null ? fnArgs[1].EvalFloatArray(scope, inst, cstack) : null;
        private static bool[] IfNotNull_object_boolArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].Ev<object>(scope, inst, cstack) != null ? fnArgs[1].EvalBoolArray(scope, inst, cstack) : null;
        private static decimal[] IfNotNull_object_decimalArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].Ev<object>(scope, inst, cstack) != null ? fnArgs[1].EvalDecimalArray(scope, inst, cstack) : null;
        private static object[] IfNotNull_object_objectArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].Ev<object>(scope, inst, cstack) != null ? fnArgs[1].EvalObjectArray(scope, inst, cstack) : null;
        private static short[] IfNotNull_object_shortArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].Ev<object>(scope, inst, cstack) != null ? fnArgs[1].EvalShortArray(scope, inst, cstack) : null;
        private static DateTime[] IfNotNull_object_dateArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].Ev<object>(scope, inst, cstack) != null ? fnArgs[1].EvalDateArray(scope, inst, cstack) : null;
        private static CustomObject IfNotNull_object_customArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].Ev<object>(scope, inst, cstack) != null ? fnArgs[1].EvalCustomArray(scope, inst, cstack) : null;
        private static string IfNotNull_object_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].Ev<object>(scope, inst, cstack) != null ? fnArgs[1].EvalString(scope, inst, cstack) : null;
        private static object IfNotNull_object_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => fnArgs[0].Ev<object>(scope, inst, cstack) != null ? fnArgs[1].EvalObject(scope, inst, cstack) : null;


        private static string GetAppInfo_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            string key = fnArgs[0].EvalString(scope, inst, cstack);
            string v;
            if (!inst.Exec.ExecutedScript.AppInfo.TryGetValue(key, out v)) throw new ScriptExecutionException($"Property '{key}' not set.");
            return v;
        }
        private static string[] GetAppInfo(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {

            return inst.Exec.ExecutedScript.AppInfo.Select(x => x.Key + ": " + x.Value).ToArray();
        }

        private static object StartProcess_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            ProcessStartInfo info = (ProcessStartInfo)fnArgs[0].EvalObject(scope, inst, cstack);
            Process process = Process.Start(info);
            return process;
        }
        private static object StartProcess_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            ProcessStartInfo info = (ProcessStartInfo)fnArgs[0].EvalObject(scope, inst, cstack);
            bool waitForExit = fnArgs[1].EvalBool(scope, inst, cstack);
            Process process = Process.Start(info);
            if (waitForExit) process.WaitForExit();
            return process;
        }
        private static object StartProcess_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => StartProcess(GetFullPath(fnArgs[0].EvalString(scope, inst, cstack), cstack, csrc));

        private static object StartProcess_string_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => StartProcess(GetFullPath(fnArgs[0].EvalString(scope, inst, cstack), cstack, csrc), fnArgs[1].EvalString(scope, inst, cstack));
        private static object StartProcess_string_string_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => StartProcess(GetFullPath(fnArgs[0].EvalString(scope, inst, cstack), cstack, csrc), fnArgs[1].EvalString(scope, inst, cstack), (ProcessWindowStyle)fnArgs[2].EvalInt(scope, inst, cstack));

        private static object StartProcess_string_string_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => StartProcess(GetFullPath(fnArgs[0].EvalString(scope, inst, cstack), cstack, csrc), fnArgs[1].EvalString(scope, inst, cstack), (ProcessWindowStyle)fnArgs[2].EvalObject(scope, inst, cstack));
        private static object StartProcess_string_string_int_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => StartProcess(GetFullPath(fnArgs[0].EvalString(scope, inst, cstack), cstack, csrc), fnArgs[1].EvalString(scope, inst, cstack), (ProcessWindowStyle)fnArgs[2].EvalInt(scope, inst, cstack), fnArgs[3].EvalBool(scope, inst, cstack));
        private static object StartProcess_string_string_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => StartProcess(GetFullPath(fnArgs[0].EvalString(scope, inst, cstack), cstack, csrc), fnArgs[1].EvalString(scope, inst, cstack), (ProcessWindowStyle)fnArgs[2].EvalObject(scope, inst, cstack), fnArgs[3].EvalBool(scope, inst, cstack));
        private static Process StartProcess(string fileName, string arguments = "", ProcessWindowStyle windowStyle = ProcessWindowStyle.Normal, bool waitForExit = true)
        {
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = fileName;
            info.Arguments = arguments;
            info.WindowStyle = windowStyle;

            info.UseShellExecute = true;

            Process process = Process.Start(info);
            if (waitForExit) process.WaitForExit();
            return process;
        }

        private static double DateDiff_date_date(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => DateDiff(fnArgs[0].EvalDate(scope, inst, cstack), fnArgs[1].EvalDate(scope, inst, cstack));
        private static double DateDiff_date_date_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => DateDiff(fnArgs[0].EvalDate(scope, inst, cstack), fnArgs[1].EvalDate(scope, inst, cstack), fnArgs[2].EvalChar(scope, inst, cstack));

        private static double DateDiff(DateTime date1, DateTime date2, char dateInterval = 'd')
        {
            var d = date2 - date1;

            switch (dateInterval)
            {
                case 'y':
                    DateTime zeroTime = new DateTime(1, 1, 1);
                    return (zeroTime + d).Year - 1;
                case 'M':
                    return (date2.Month - date1.Month) + 12 * (date2.Year - date1.Year);

                case 'd':
                    return d.TotalDays;
                case 'h':
                    return d.TotalHours;
                case 'm':
                    return d.TotalMinutes;
                case 's':
                    return d.TotalSeconds;
                case 'f':
                    return d.TotalMilliseconds;

                default:
                    return d.TotalDays;
            }
        }
        private static DateTime DateAdd_date_double(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => DateAdd(fnArgs[0].EvalDate(scope, inst, cstack), fnArgs[1].EvalDouble(scope, inst, cstack));
        private static DateTime DateAdd_date_double_char(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => DateAdd(fnArgs[0].EvalDate(scope, inst, cstack), fnArgs[1].EvalDouble(scope, inst, cstack), fnArgs[2].EvalChar(scope, inst, cstack));

        private static DateTime DateAdd(DateTime date, double num, char dateInterval = 'd')
        {
            switch (dateInterval)
            {
                case 'y':
                    return date.AddYears((int)num);
                case 'M':
                    return date.AddMonths((int)num);

                case 'd':
                    return date.AddDays(num);
                case 'h':
                    return date.AddHours(num);
                case 'm':
                    return date.AddMinutes(num);
                case 's':
                    return date.AddSeconds(num);
                case 'f':
                    return date.AddMilliseconds(num);

                default:
                    return date.AddDays(num);
            }
        }
        private static object GetLoopRange_int_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => new LoopRange(fnArgs[0].EvalInt(scope, inst, cstack), fnArgs[1].EvalInt(scope, inst, cstack), fnArgs[2].EvalInt(scope, inst, cstack));
        private static object GetLoopRange_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => new LoopRange(fnArgs[0].EvalInt(scope, inst, cstack), fnArgs[1].EvalInt(scope, inst, cstack), 1);
        private static object GetLoopRange_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => new LoopRange(0, fnArgs[0].EvalInt(scope, inst, cstack), 1);



        private static object For_int_int_int_int_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            EvalUnit varArg = fnArgs[3];

            int from = fnArgs[0].EvalInt(scope, inst, cstack);
            int to = fnArgs[1].EvalInt(scope, inst, cstack);
            int step = fnArgs[2].EvalInt(scope, inst, cstack);

            VarInfo varInfo = new VarInfo() { Inst = null, Scope = -1 };
            bool varIsNotEmpty = varArg.Kind != EvalUnitKind.Empty;
            Executor exec = inst.Exec;
            if (varIsNotEmpty)
            {
                varInfo = varArg.GetVarInfo(scope, inst, cstack);
                if (varArg.Type.ID == TypeID.Int) ScriptVars<int>.Add(exec, varInfo.ID, varInfo.Scope);
            }


            if (step >= 0)
            {

                for (int i = from; i <= to; i += step)
                {
                    if (varIsNotEmpty) ScriptVars<int>.Set(exec, varInfo.ID, varInfo.Scope, ref i);
                    for (int j = 4; j < fnArgs.Length; j++) fnArgs[j].ProcessUnit(scope, inst, cstack);

                }
            }
            else
            {
                for (int i = from; i >= to; i += step)
                {
                    if (varIsNotEmpty) ScriptVars<int>.Set(exec, varInfo.ID, varInfo.Scope, ref i);
                    for (int j = 4; j < fnArgs.Length; j++) fnArgs[j].ProcessUnit(scope, inst, cstack);

                }
            }
            return null;
        }

        private static object While_bool_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            var cond = fnArgs[0];
            int l = fnArgs.Length;
            while (cond.EvalBool(scope, inst, cstack))
            {
                for (int i = 1; i < l; i++)
                    fnArgs[i].ProcessUnit(scope, inst, cstack);
            }
            return null;
        }
        private static object DoWhile_bool_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            var cond = fnArgs[0];
            int l = fnArgs.Length;
            do
            {
                for (int i = 1; i < l; i++)
                    fnArgs[i].ProcessUnit(scope, inst, cstack);
            } while (cond.EvalBool(scope, inst, cstack));
            return null;
        }


        private static byte[] Select_intArray_int_byte_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<int, byte>(fnArgs, scope, inst, cstack, false);
        private static short[] Select_intArray_int_short_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<int, short>(fnArgs, scope, inst, cstack, false);
        private static int[] Select_intArray_int_int_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<int, int>(fnArgs, scope, inst, cstack, false);
        private static long[] Select_intArray_int_long_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<int, long>(fnArgs, scope, inst, cstack, false);
        private static float[] Select_intArray_int_float_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<int, float>(fnArgs, scope, inst, cstack, false);
        private static double[] Select_intArray_int_double_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<int, double>(fnArgs, scope, inst, cstack, false);
        private static decimal[] Select_intArray_int_decimal_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<int, decimal>(fnArgs, scope, inst, cstack, false);
        private static char[] Select_intArray_int_char_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<int, char>(fnArgs, scope, inst, cstack, false);
        private static string[] Select_intArray_int_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<int, string>(fnArgs, scope, inst, cstack, false);
        private static object[] Select_intArray_int_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<int, object>(fnArgs, scope, inst, cstack, false);
        private static bool[] Select_intArray_int_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<int, bool>(fnArgs, scope, inst, cstack, false);
        private static DateTime[] Select_intArray_int_date_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<int, DateTime>(fnArgs, scope, inst, cstack, false);
        private static CustomObject Select_intArray_int_custom_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetCustomObjectFromArr(Select<int, CustomObject>(fnArgs, scope, inst, cstack, false), fnArgs[2].Type.CType.Class, inst);
        private static byte[] Select_byteArray_byte_byte_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<byte, byte>(fnArgs, scope, inst, cstack, false);
        private static short[] Select_byteArray_byte_short_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<byte, short>(fnArgs, scope, inst, cstack, false);
        private static int[] Select_byteArray_byte_int_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<byte, int>(fnArgs, scope, inst, cstack, false);
        private static long[] Select_byteArray_byte_long_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<byte, long>(fnArgs, scope, inst, cstack, false);
        private static float[] Select_byteArray_byte_float_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<byte, float>(fnArgs, scope, inst, cstack, false);
        private static double[] Select_byteArray_byte_double_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<byte, double>(fnArgs, scope, inst, cstack, false);
        private static decimal[] Select_byteArray_byte_decimal_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<byte, decimal>(fnArgs, scope, inst, cstack, false);
        private static char[] Select_byteArray_byte_char_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<byte, char>(fnArgs, scope, inst, cstack, false);
        private static string[] Select_byteArray_byte_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<byte, string>(fnArgs, scope, inst, cstack, false);
        private static object[] Select_byteArray_byte_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<byte, object>(fnArgs, scope, inst, cstack, false);
        private static bool[] Select_byteArray_byte_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<byte, bool>(fnArgs, scope, inst, cstack, false);
        private static DateTime[] Select_byteArray_byte_date_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<byte, DateTime>(fnArgs, scope, inst, cstack, false);
        private static CustomObject Select_byteArray_byte_custom_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetCustomObjectFromArr(Select<byte, CustomObject>(fnArgs, scope, inst, cstack, false), fnArgs[2].Type.CType.Class, inst);
        private static byte[] Select_shortArray_short_byte_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<short, byte>(fnArgs, scope, inst, cstack, false);
        private static short[] Select_shortArray_short_short_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<short, short>(fnArgs, scope, inst, cstack, false);
        private static int[] Select_shortArray_short_int_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<short, int>(fnArgs, scope, inst, cstack, false);
        private static long[] Select_shortArray_short_long_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<short, long>(fnArgs, scope, inst, cstack, false);
        private static float[] Select_shortArray_short_float_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<short, float>(fnArgs, scope, inst, cstack, false);
        private static double[] Select_shortArray_short_double_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<short, double>(fnArgs, scope, inst, cstack, false);
        private static decimal[] Select_shortArray_short_decimal_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<short, decimal>(fnArgs, scope, inst, cstack, false);
        private static char[] Select_shortArray_short_char_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<short, char>(fnArgs, scope, inst, cstack, false);
        private static string[] Select_shortArray_short_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<short, string>(fnArgs, scope, inst, cstack, false);
        private static object[] Select_shortArray_short_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<short, object>(fnArgs, scope, inst, cstack, false);
        private static bool[] Select_shortArray_short_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<short, bool>(fnArgs, scope, inst, cstack, false);
        private static DateTime[] Select_shortArray_short_date_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<short, DateTime>(fnArgs, scope, inst, cstack, false);
        private static CustomObject Select_shortArray_short_custom_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetCustomObjectFromArr(Select<short, CustomObject>(fnArgs, scope, inst, cstack, false), fnArgs[2].Type.CType.Class, inst);
        private static byte[] Select_longArray_long_byte_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<long, byte>(fnArgs, scope, inst, cstack, false);
        private static short[] Select_longArray_long_short_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<long, short>(fnArgs, scope, inst, cstack, false);
        private static int[] Select_longArray_long_int_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<long, int>(fnArgs, scope, inst, cstack, false);
        private static long[] Select_longArray_long_long_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<long, long>(fnArgs, scope, inst, cstack, false);
        private static float[] Select_longArray_long_float_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<long, float>(fnArgs, scope, inst, cstack, false);
        private static double[] Select_longArray_long_double_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<long, double>(fnArgs, scope, inst, cstack, false);
        private static decimal[] Select_longArray_long_decimal_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<long, decimal>(fnArgs, scope, inst, cstack, false);
        private static char[] Select_longArray_long_char_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<long, char>(fnArgs, scope, inst, cstack, false);
        private static string[] Select_longArray_long_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<long, string>(fnArgs, scope, inst, cstack, false);
        private static object[] Select_longArray_long_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<long, object>(fnArgs, scope, inst, cstack, false);
        private static bool[] Select_longArray_long_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<long, bool>(fnArgs, scope, inst, cstack, false);
        private static DateTime[] Select_longArray_long_date_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<long, DateTime>(fnArgs, scope, inst, cstack, false);
        private static CustomObject Select_longArray_long_custom_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetCustomObjectFromArr(Select<long, CustomObject>(fnArgs, scope, inst, cstack, false), fnArgs[2].Type.CType.Class, inst);
        private static byte[] Select_floatArray_float_byte_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<float, byte>(fnArgs, scope, inst, cstack, false);
        private static short[] Select_floatArray_float_short_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<float, short>(fnArgs, scope, inst, cstack, false);
        private static int[] Select_floatArray_float_int_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<float, int>(fnArgs, scope, inst, cstack, false);
        private static long[] Select_floatArray_float_long_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<float, long>(fnArgs, scope, inst, cstack, false);
        private static float[] Select_floatArray_float_float_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<float, float>(fnArgs, scope, inst, cstack, false);
        private static double[] Select_floatArray_float_double_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<float, double>(fnArgs, scope, inst, cstack, false);
        private static decimal[] Select_floatArray_float_decimal_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<float, decimal>(fnArgs, scope, inst, cstack, false);
        private static char[] Select_floatArray_float_char_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<float, char>(fnArgs, scope, inst, cstack, false);
        private static string[] Select_floatArray_float_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<float, string>(fnArgs, scope, inst, cstack, false);
        private static object[] Select_floatArray_float_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<float, object>(fnArgs, scope, inst, cstack, false);
        private static bool[] Select_floatArray_float_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<float, bool>(fnArgs, scope, inst, cstack, false);
        private static DateTime[] Select_floatArray_float_date_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<float, DateTime>(fnArgs, scope, inst, cstack, false);
        private static CustomObject Select_floatArray_float_custom_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetCustomObjectFromArr(Select<float, CustomObject>(fnArgs, scope, inst, cstack, false), fnArgs[2].Type.CType.Class, inst);
        private static byte[] Select_doubleArray_double_byte_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<double, byte>(fnArgs, scope, inst, cstack, false);
        private static short[] Select_doubleArray_double_short_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<double, short>(fnArgs, scope, inst, cstack, false);
        private static int[] Select_doubleArray_double_int_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<double, int>(fnArgs, scope, inst, cstack, false);
        private static long[] Select_doubleArray_double_long_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<double, long>(fnArgs, scope, inst, cstack, false);
        private static float[] Select_doubleArray_double_float_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<double, float>(fnArgs, scope, inst, cstack, false);
        private static double[] Select_doubleArray_double_double_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<double, double>(fnArgs, scope, inst, cstack, false);
        private static decimal[] Select_doubleArray_double_decimal_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<double, decimal>(fnArgs, scope, inst, cstack, false);
        private static char[] Select_doubleArray_double_char_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<double, char>(fnArgs, scope, inst, cstack, false);
        private static string[] Select_doubleArray_double_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<double, string>(fnArgs, scope, inst, cstack, false);
        private static object[] Select_doubleArray_double_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<double, object>(fnArgs, scope, inst, cstack, false);
        private static bool[] Select_doubleArray_double_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<double, bool>(fnArgs, scope, inst, cstack, false);
        private static DateTime[] Select_doubleArray_double_date_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<double, DateTime>(fnArgs, scope, inst, cstack, false);
        private static CustomObject Select_doubleArray_double_custom_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetCustomObjectFromArr(Select<double, CustomObject>(fnArgs, scope, inst, cstack, false), fnArgs[2].Type.CType.Class, inst);
        private static byte[] Select_decimalArray_decimal_byte_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<decimal, byte>(fnArgs, scope, inst, cstack, false);
        private static short[] Select_decimalArray_decimal_short_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<decimal, short>(fnArgs, scope, inst, cstack, false);
        private static int[] Select_decimalArray_decimal_int_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<decimal, int>(fnArgs, scope, inst, cstack, false);
        private static long[] Select_decimalArray_decimal_long_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<decimal, long>(fnArgs, scope, inst, cstack, false);
        private static float[] Select_decimalArray_decimal_float_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<decimal, float>(fnArgs, scope, inst, cstack, false);
        private static double[] Select_decimalArray_decimal_double_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<decimal, double>(fnArgs, scope, inst, cstack, false);
        private static decimal[] Select_decimalArray_decimal_decimal_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<decimal, decimal>(fnArgs, scope, inst, cstack, false);
        private static char[] Select_decimalArray_decimal_char_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<decimal, char>(fnArgs, scope, inst, cstack, false);
        private static string[] Select_decimalArray_decimal_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<decimal, string>(fnArgs, scope, inst, cstack, false);
        private static object[] Select_decimalArray_decimal_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<decimal, object>(fnArgs, scope, inst, cstack, false);
        private static bool[] Select_decimalArray_decimal_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<decimal, bool>(fnArgs, scope, inst, cstack, false);
        private static DateTime[] Select_decimalArray_decimal_date_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<decimal, DateTime>(fnArgs, scope, inst, cstack, false);
        private static CustomObject Select_decimalArray_decimal_custom_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetCustomObjectFromArr(Select<decimal, CustomObject>(fnArgs, scope, inst, cstack, false), fnArgs[2].Type.CType.Class, inst);
        private static byte[] Select_charArray_char_byte_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<char, byte>(fnArgs, scope, inst, cstack, false);
        private static short[] Select_charArray_char_short_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<char, short>(fnArgs, scope, inst, cstack, false);
        private static int[] Select_charArray_char_int_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<char, int>(fnArgs, scope, inst, cstack, false);
        private static long[] Select_charArray_char_long_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<char, long>(fnArgs, scope, inst, cstack, false);
        private static float[] Select_charArray_char_float_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<char, float>(fnArgs, scope, inst, cstack, false);
        private static double[] Select_charArray_char_double_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<char, double>(fnArgs, scope, inst, cstack, false);
        private static decimal[] Select_charArray_char_decimal_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<char, decimal>(fnArgs, scope, inst, cstack, false);
        private static char[] Select_charArray_char_char_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<char, char>(fnArgs, scope, inst, cstack, false);
        private static string[] Select_charArray_char_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<char, string>(fnArgs, scope, inst, cstack, false);
        private static object[] Select_charArray_char_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<char, object>(fnArgs, scope, inst, cstack, false);
        private static bool[] Select_charArray_char_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<char, bool>(fnArgs, scope, inst, cstack, false);
        private static DateTime[] Select_charArray_char_date_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<char, DateTime>(fnArgs, scope, inst, cstack, false);
        private static CustomObject Select_charArray_char_custom_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetCustomObjectFromArr(Select<char, CustomObject>(fnArgs, scope, inst, cstack, false), fnArgs[2].Type.CType.Class, inst);
        private static byte[] Select_dateArray_date_byte_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<DateTime, byte>(fnArgs, scope, inst, cstack, false);
        private static short[] Select_dateArray_date_short_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<DateTime, short>(fnArgs, scope, inst, cstack, false);
        private static int[] Select_dateArray_date_int_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<DateTime, int>(fnArgs, scope, inst, cstack, false);
        private static long[] Select_dateArray_date_long_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<DateTime, long>(fnArgs, scope, inst, cstack, false);
        private static float[] Select_dateArray_date_float_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<DateTime, float>(fnArgs, scope, inst, cstack, false);
        private static double[] Select_dateArray_date_double_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<DateTime, double>(fnArgs, scope, inst, cstack, false);
        private static decimal[] Select_dateArray_date_decimal_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<DateTime, decimal>(fnArgs, scope, inst, cstack, false);
        private static char[] Select_dateArray_date_char_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<DateTime, char>(fnArgs, scope, inst, cstack, false);
        private static string[] Select_dateArray_date_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<DateTime, string>(fnArgs, scope, inst, cstack, false);
        private static object[] Select_dateArray_date_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<DateTime, object>(fnArgs, scope, inst, cstack, false);
        private static bool[] Select_dateArray_date_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<DateTime, bool>(fnArgs, scope, inst, cstack, false);
        private static DateTime[] Select_dateArray_date_date_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<DateTime, DateTime>(fnArgs, scope, inst, cstack, false);
        private static CustomObject Select_dateArray_date_custom_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetCustomObjectFromArr(Select<DateTime, CustomObject>(fnArgs, scope, inst, cstack, false), fnArgs[2].Type.CType.Class, inst);
        private static byte[] Select_stringArray_string_byte_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<string, byte>(fnArgs, scope, inst, cstack, false);
        private static short[] Select_stringArray_string_short_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<string, short>(fnArgs, scope, inst, cstack, false);
        private static int[] Select_stringArray_string_int_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<string, int>(fnArgs, scope, inst, cstack, false);
        private static long[] Select_stringArray_string_long_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<string, long>(fnArgs, scope, inst, cstack, false);
        private static float[] Select_stringArray_string_float_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<string, float>(fnArgs, scope, inst, cstack, false);
        private static double[] Select_stringArray_string_double_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<string, double>(fnArgs, scope, inst, cstack, false);
        private static decimal[] Select_stringArray_string_decimal_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<string, decimal>(fnArgs, scope, inst, cstack, false);
        private static char[] Select_stringArray_string_char_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<string, char>(fnArgs, scope, inst, cstack, false);
        private static string[] Select_stringArray_string_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<string, string>(fnArgs, scope, inst, cstack, false);
        private static object[] Select_stringArray_string_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<string, object>(fnArgs, scope, inst, cstack, false);
        private static bool[] Select_stringArray_string_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<string, bool>(fnArgs, scope, inst, cstack, false);
        private static DateTime[] Select_stringArray_string_date_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<string, DateTime>(fnArgs, scope, inst, cstack, false);
        private static CustomObject Select_stringArray_string_custom_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetCustomObjectFromArr(Select<string, CustomObject>(fnArgs, scope, inst, cstack, false), fnArgs[2].Type.CType.Class, inst);
        private static byte[] Select_boolArray_bool_byte_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<bool, byte>(fnArgs, scope, inst, cstack, false);
        private static short[] Select_boolArray_bool_short_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<bool, short>(fnArgs, scope, inst, cstack, false);
        private static int[] Select_boolArray_bool_int_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<bool, int>(fnArgs, scope, inst, cstack, false);
        private static long[] Select_boolArray_bool_long_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<bool, long>(fnArgs, scope, inst, cstack, false);
        private static float[] Select_boolArray_bool_float_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<bool, float>(fnArgs, scope, inst, cstack, false);
        private static double[] Select_boolArray_bool_double_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<bool, double>(fnArgs, scope, inst, cstack, false);
        private static decimal[] Select_boolArray_bool_decimal_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<bool, decimal>(fnArgs, scope, inst, cstack, false);
        private static char[] Select_boolArray_bool_char_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<bool, char>(fnArgs, scope, inst, cstack, false);
        private static string[] Select_boolArray_bool_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<bool, string>(fnArgs, scope, inst, cstack, false);
        private static object[] Select_boolArray_bool_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<bool, object>(fnArgs, scope, inst, cstack, false);
        private static bool[] Select_boolArray_bool_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<bool, bool>(fnArgs, scope, inst, cstack, false);
        private static DateTime[] Select_boolArray_bool_date_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<bool, DateTime>(fnArgs, scope, inst, cstack, false);
        private static CustomObject Select_boolArray_bool_custom_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetCustomObjectFromArr(Select<bool, CustomObject>(fnArgs, scope, inst, cstack, false), fnArgs[2].Type.CType.Class, inst);
        private static byte[] Select_objectArray_object_byte_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<object, byte>(fnArgs, scope, inst, cstack, false);
        private static short[] Select_objectArray_object_short_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<object, short>(fnArgs, scope, inst, cstack, false);
        private static int[] Select_objectArray_object_int_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<object, int>(fnArgs, scope, inst, cstack, false);
        private static long[] Select_objectArray_object_long_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<object, long>(fnArgs, scope, inst, cstack, false);
        private static float[] Select_objectArray_object_float_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<object, float>(fnArgs, scope, inst, cstack, false);
        private static double[] Select_objectArray_object_double_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<object, double>(fnArgs, scope, inst, cstack, false);
        private static decimal[] Select_objectArray_object_decimal_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<object, decimal>(fnArgs, scope, inst, cstack, false);
        private static char[] Select_objectArray_object_char_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<object, char>(fnArgs, scope, inst, cstack, false);
        private static string[] Select_objectArray_object_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<object, string>(fnArgs, scope, inst, cstack, false);
        private static object[] Select_objectArray_object_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<object, object>(fnArgs, scope, inst, cstack, false);
        private static bool[] Select_objectArray_object_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<object, bool>(fnArgs, scope, inst, cstack, false);
        private static DateTime[] Select_objectArray_object_date_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<object, DateTime>(fnArgs, scope, inst, cstack, false);
        private static CustomObject Select_objectArray_object_custom_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetCustomObjectFromArr(Select<object, CustomObject>(fnArgs, scope, inst, cstack, false), fnArgs[2].Type.CType.Class, inst);
        private static byte[] Select_customArray_custom_byte_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<CustomObject, byte>(fnArgs, scope, inst, cstack, false);
        private static short[] Select_customArray_custom_short_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<CustomObject, short>(fnArgs, scope, inst, cstack, false);
        private static int[] Select_customArray_custom_int_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<CustomObject, int>(fnArgs, scope, inst, cstack, false);
        private static long[] Select_customArray_custom_long_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<CustomObject, long>(fnArgs, scope, inst, cstack, false);
        private static float[] Select_customArray_custom_float_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<CustomObject, float>(fnArgs, scope, inst, cstack, false);
        private static double[] Select_customArray_custom_double_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<CustomObject, double>(fnArgs, scope, inst, cstack, false);
        private static decimal[] Select_customArray_custom_decimal_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<CustomObject, decimal>(fnArgs, scope, inst, cstack, false);
        private static char[] Select_customArray_custom_char_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<CustomObject, char>(fnArgs, scope, inst, cstack, false);
        private static string[] Select_customArray_custom_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<CustomObject, string>(fnArgs, scope, inst, cstack, false);
        private static object[] Select_customArray_custom_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<CustomObject, object>(fnArgs, scope, inst, cstack, false);
        private static bool[] Select_customArray_custom_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<CustomObject, bool>(fnArgs, scope, inst, cstack, false);
        private static DateTime[] Select_customArray_custom_date_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<CustomObject, DateTime>(fnArgs, scope, inst, cstack, false);
        private static CustomObject Select_customArray_custom_custom_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetCustomObjectFromArr(Select<CustomObject, CustomObject>(fnArgs, scope, inst, cstack, false), fnArgs[2].Type.CType.Class, inst);

        private static CustomObject GetCustomObjectFromArr(CustomObject[] arr, ScriptClass sc, ClassInstance inst) => new CustomObject(inst.Exec, arr, sc, true);


        private static byte[] Select_object_int_byte_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<int, byte>(fnArgs, scope, inst, cstack, true);
        private static short[] Select_object_int_short_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<int, short>(fnArgs, scope, inst, cstack, true);
        private static int[] Select_object_int_int_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<int, int>(fnArgs, scope, inst, cstack, true);
        private static long[] Select_object_int_long_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<int, long>(fnArgs, scope, inst, cstack, true);
        private static float[] Select_object_int_float_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<int, float>(fnArgs, scope, inst, cstack, true);
        private static double[] Select_object_int_double_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<int, double>(fnArgs, scope, inst, cstack, true);
        private static decimal[] Select_object_int_decimal_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<int, decimal>(fnArgs, scope, inst, cstack, true);
        private static char[] Select_object_int_char_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<int, char>(fnArgs, scope, inst, cstack, true);
        private static string[] Select_object_int_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<int, string>(fnArgs, scope, inst, cstack, true);
        private static object[] Select_object_int_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<int, object>(fnArgs, scope, inst, cstack, true);
        private static bool[] Select_object_int_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<int, bool>(fnArgs, scope, inst, cstack, true);
        private static DateTime[] Select_object_int_date_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<int, DateTime>(fnArgs, scope, inst, cstack, true);
        private static CustomObject Select_object_int_custom_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetCustomObjectFromArr(Select<int, CustomObject>(fnArgs, scope, inst, cstack, true), fnArgs[2].Type.CType.Class, inst);
        private static byte[] Select_object_byte_byte_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<byte, byte>(fnArgs, scope, inst, cstack, true);
        private static short[] Select_object_byte_short_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<byte, short>(fnArgs, scope, inst, cstack, true);
        private static int[] Select_object_byte_int_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<byte, int>(fnArgs, scope, inst, cstack, true);
        private static long[] Select_object_byte_long_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<byte, long>(fnArgs, scope, inst, cstack, true);
        private static float[] Select_object_byte_float_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<byte, float>(fnArgs, scope, inst, cstack, true);
        private static double[] Select_object_byte_double_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<byte, double>(fnArgs, scope, inst, cstack, true);
        private static decimal[] Select_object_byte_decimal_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<byte, decimal>(fnArgs, scope, inst, cstack, true);
        private static char[] Select_object_byte_char_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<byte, char>(fnArgs, scope, inst, cstack, true);
        private static string[] Select_object_byte_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<byte, string>(fnArgs, scope, inst, cstack, true);
        private static object[] Select_object_byte_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<byte, object>(fnArgs, scope, inst, cstack, true);
        private static bool[] Select_object_byte_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<byte, bool>(fnArgs, scope, inst, cstack, true);
        private static DateTime[] Select_object_byte_date_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<byte, DateTime>(fnArgs, scope, inst, cstack, true);
        private static CustomObject Select_object_byte_custom_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetCustomObjectFromArr(Select<byte, CustomObject>(fnArgs, scope, inst, cstack, true), fnArgs[2].Type.CType.Class, inst);
        private static byte[] Select_object_short_byte_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<short, byte>(fnArgs, scope, inst, cstack, true);
        private static short[] Select_object_short_short_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<short, short>(fnArgs, scope, inst, cstack, true);
        private static int[] Select_object_short_int_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<short, int>(fnArgs, scope, inst, cstack, true);
        private static long[] Select_object_short_long_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<short, long>(fnArgs, scope, inst, cstack, true);
        private static float[] Select_object_short_float_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<short, float>(fnArgs, scope, inst, cstack, true);
        private static double[] Select_object_short_double_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<short, double>(fnArgs, scope, inst, cstack, true);
        private static decimal[] Select_object_short_decimal_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<short, decimal>(fnArgs, scope, inst, cstack, true);
        private static char[] Select_object_short_char_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<short, char>(fnArgs, scope, inst, cstack, true);
        private static string[] Select_object_short_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<short, string>(fnArgs, scope, inst, cstack, true);
        private static object[] Select_object_short_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<short, object>(fnArgs, scope, inst, cstack, true);
        private static bool[] Select_object_short_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<short, bool>(fnArgs, scope, inst, cstack, true);
        private static DateTime[] Select_object_short_date_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<short, DateTime>(fnArgs, scope, inst, cstack, true);
        private static CustomObject Select_object_short_custom_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetCustomObjectFromArr(Select<short, CustomObject>(fnArgs, scope, inst, cstack, true), fnArgs[2].Type.CType.Class, inst);
        private static byte[] Select_object_long_byte_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<long, byte>(fnArgs, scope, inst, cstack, true);
        private static short[] Select_object_long_short_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<long, short>(fnArgs, scope, inst, cstack, true);
        private static int[] Select_object_long_int_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<long, int>(fnArgs, scope, inst, cstack, true);
        private static long[] Select_object_long_long_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<long, long>(fnArgs, scope, inst, cstack, true);
        private static float[] Select_object_long_float_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<long, float>(fnArgs, scope, inst, cstack, true);
        private static double[] Select_object_long_double_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<long, double>(fnArgs, scope, inst, cstack, true);
        private static decimal[] Select_object_long_decimal_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<long, decimal>(fnArgs, scope, inst, cstack, true);
        private static char[] Select_object_long_char_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<long, char>(fnArgs, scope, inst, cstack, true);
        private static string[] Select_object_long_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<long, string>(fnArgs, scope, inst, cstack, true);
        private static object[] Select_object_long_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<long, object>(fnArgs, scope, inst, cstack, true);
        private static bool[] Select_object_long_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<long, bool>(fnArgs, scope, inst, cstack, true);
        private static DateTime[] Select_object_long_date_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<long, DateTime>(fnArgs, scope, inst, cstack, true);
        private static CustomObject Select_object_long_custom_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetCustomObjectFromArr(Select<long, CustomObject>(fnArgs, scope, inst, cstack, true), fnArgs[2].Type.CType.Class, inst);
        private static byte[] Select_object_float_byte_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<float, byte>(fnArgs, scope, inst, cstack, true);
        private static short[] Select_object_float_short_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<float, short>(fnArgs, scope, inst, cstack, true);
        private static int[] Select_object_float_int_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<float, int>(fnArgs, scope, inst, cstack, true);
        private static long[] Select_object_float_long_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<float, long>(fnArgs, scope, inst, cstack, true);
        private static float[] Select_object_float_float_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<float, float>(fnArgs, scope, inst, cstack, true);
        private static double[] Select_object_float_double_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<float, double>(fnArgs, scope, inst, cstack, true);
        private static decimal[] Select_object_float_decimal_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<float, decimal>(fnArgs, scope, inst, cstack, true);
        private static char[] Select_object_float_char_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<float, char>(fnArgs, scope, inst, cstack, true);
        private static string[] Select_object_float_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<float, string>(fnArgs, scope, inst, cstack, true);
        private static object[] Select_object_float_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<float, object>(fnArgs, scope, inst, cstack, true);
        private static bool[] Select_object_float_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<float, bool>(fnArgs, scope, inst, cstack, true);
        private static DateTime[] Select_object_float_date_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<float, DateTime>(fnArgs, scope, inst, cstack, true);
        private static CustomObject Select_object_float_custom_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetCustomObjectFromArr(Select<float, CustomObject>(fnArgs, scope, inst, cstack, true), fnArgs[2].Type.CType.Class, inst);
        private static byte[] Select_object_double_byte_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<double, byte>(fnArgs, scope, inst, cstack, true);
        private static short[] Select_object_double_short_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<double, short>(fnArgs, scope, inst, cstack, true);
        private static int[] Select_object_double_int_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<double, int>(fnArgs, scope, inst, cstack, true);
        private static long[] Select_object_double_long_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<double, long>(fnArgs, scope, inst, cstack, true);
        private static float[] Select_object_double_float_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<double, float>(fnArgs, scope, inst, cstack, true);
        private static double[] Select_object_double_double_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<double, double>(fnArgs, scope, inst, cstack, true);
        private static decimal[] Select_object_double_decimal_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<double, decimal>(fnArgs, scope, inst, cstack, true);
        private static char[] Select_object_double_char_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<double, char>(fnArgs, scope, inst, cstack, true);
        private static string[] Select_object_double_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<double, string>(fnArgs, scope, inst, cstack, true);
        private static object[] Select_object_double_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<double, object>(fnArgs, scope, inst, cstack, true);
        private static bool[] Select_object_double_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<double, bool>(fnArgs, scope, inst, cstack, true);
        private static DateTime[] Select_object_double_date_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<double, DateTime>(fnArgs, scope, inst, cstack, true);
        private static CustomObject Select_object_double_custom_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetCustomObjectFromArr(Select<double, CustomObject>(fnArgs, scope, inst, cstack, true), fnArgs[2].Type.CType.Class, inst);
        private static byte[] Select_object_decimal_byte_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<decimal, byte>(fnArgs, scope, inst, cstack, true);
        private static short[] Select_object_decimal_short_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<decimal, short>(fnArgs, scope, inst, cstack, true);
        private static int[] Select_object_decimal_int_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<decimal, int>(fnArgs, scope, inst, cstack, true);
        private static long[] Select_object_decimal_long_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<decimal, long>(fnArgs, scope, inst, cstack, true);
        private static float[] Select_object_decimal_float_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<decimal, float>(fnArgs, scope, inst, cstack, true);
        private static double[] Select_object_decimal_double_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<decimal, double>(fnArgs, scope, inst, cstack, true);
        private static decimal[] Select_object_decimal_decimal_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<decimal, decimal>(fnArgs, scope, inst, cstack, true);
        private static char[] Select_object_decimal_char_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<decimal, char>(fnArgs, scope, inst, cstack, true);
        private static string[] Select_object_decimal_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<decimal, string>(fnArgs, scope, inst, cstack, true);
        private static object[] Select_object_decimal_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<decimal, object>(fnArgs, scope, inst, cstack, true);
        private static bool[] Select_object_decimal_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<decimal, bool>(fnArgs, scope, inst, cstack, true);
        private static DateTime[] Select_object_decimal_date_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<decimal, DateTime>(fnArgs, scope, inst, cstack, true);
        private static CustomObject Select_object_decimal_custom_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetCustomObjectFromArr(Select<decimal, CustomObject>(fnArgs, scope, inst, cstack, true), fnArgs[2].Type.CType.Class, inst);
        private static byte[] Select_object_char_byte_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<char, byte>(fnArgs, scope, inst, cstack, true);
        private static short[] Select_object_char_short_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<char, short>(fnArgs, scope, inst, cstack, true);
        private static int[] Select_object_char_int_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<char, int>(fnArgs, scope, inst, cstack, true);
        private static long[] Select_object_char_long_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<char, long>(fnArgs, scope, inst, cstack, true);
        private static float[] Select_object_char_float_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<char, float>(fnArgs, scope, inst, cstack, true);
        private static double[] Select_object_char_double_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<char, double>(fnArgs, scope, inst, cstack, true);
        private static decimal[] Select_object_char_decimal_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<char, decimal>(fnArgs, scope, inst, cstack, true);
        private static char[] Select_object_char_char_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<char, char>(fnArgs, scope, inst, cstack, true);
        private static string[] Select_object_char_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<char, string>(fnArgs, scope, inst, cstack, true);
        private static object[] Select_object_char_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<char, object>(fnArgs, scope, inst, cstack, true);
        private static bool[] Select_object_char_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<char, bool>(fnArgs, scope, inst, cstack, true);
        private static DateTime[] Select_object_char_date_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<char, DateTime>(fnArgs, scope, inst, cstack, true);
        private static CustomObject Select_object_char_custom_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetCustomObjectFromArr(Select<char, CustomObject>(fnArgs, scope, inst, cstack, true), fnArgs[2].Type.CType.Class, inst);
        private static byte[] Select_object_date_byte_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<DateTime, byte>(fnArgs, scope, inst, cstack, true);
        private static short[] Select_object_date_short_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<DateTime, short>(fnArgs, scope, inst, cstack, true);
        private static int[] Select_object_date_int_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<DateTime, int>(fnArgs, scope, inst, cstack, true);
        private static long[] Select_object_date_long_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<DateTime, long>(fnArgs, scope, inst, cstack, true);
        private static float[] Select_object_date_float_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<DateTime, float>(fnArgs, scope, inst, cstack, true);
        private static double[] Select_object_date_double_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<DateTime, double>(fnArgs, scope, inst, cstack, true);
        private static decimal[] Select_object_date_decimal_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<DateTime, decimal>(fnArgs, scope, inst, cstack, true);
        private static char[] Select_object_date_char_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<DateTime, char>(fnArgs, scope, inst, cstack, true);
        private static string[] Select_object_date_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<DateTime, string>(fnArgs, scope, inst, cstack, true);
        private static object[] Select_object_date_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<DateTime, object>(fnArgs, scope, inst, cstack, true);
        private static bool[] Select_object_date_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<DateTime, bool>(fnArgs, scope, inst, cstack, true);
        private static DateTime[] Select_object_date_date_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<DateTime, DateTime>(fnArgs, scope, inst, cstack, true);
        private static CustomObject Select_object_date_custom_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetCustomObjectFromArr(Select<DateTime, CustomObject>(fnArgs, scope, inst, cstack, true), fnArgs[2].Type.CType.Class, inst);
        private static byte[] Select_object_string_byte_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<string, byte>(fnArgs, scope, inst, cstack, true);
        private static short[] Select_object_string_short_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<string, short>(fnArgs, scope, inst, cstack, true);
        private static int[] Select_object_string_int_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<string, int>(fnArgs, scope, inst, cstack, true);
        private static long[] Select_object_string_long_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<string, long>(fnArgs, scope, inst, cstack, true);
        private static float[] Select_object_string_float_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<string, float>(fnArgs, scope, inst, cstack, true);
        private static double[] Select_object_string_double_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<string, double>(fnArgs, scope, inst, cstack, true);
        private static decimal[] Select_object_string_decimal_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<string, decimal>(fnArgs, scope, inst, cstack, true);
        private static char[] Select_object_string_char_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<string, char>(fnArgs, scope, inst, cstack, true);
        private static string[] Select_object_string_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<string, string>(fnArgs, scope, inst, cstack, true);
        private static object[] Select_object_string_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<string, object>(fnArgs, scope, inst, cstack, true);
        private static bool[] Select_object_string_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<string, bool>(fnArgs, scope, inst, cstack, true);
        private static DateTime[] Select_object_string_date_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<string, DateTime>(fnArgs, scope, inst, cstack, true);
        private static CustomObject Select_object_string_custom_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetCustomObjectFromArr(Select<string, CustomObject>(fnArgs, scope, inst, cstack, true), fnArgs[2].Type.CType.Class, inst);
        private static byte[] Select_object_bool_byte_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<bool, byte>(fnArgs, scope, inst, cstack, true);
        private static short[] Select_object_bool_short_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<bool, short>(fnArgs, scope, inst, cstack, true);
        private static int[] Select_object_bool_int_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<bool, int>(fnArgs, scope, inst, cstack, true);
        private static long[] Select_object_bool_long_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<bool, long>(fnArgs, scope, inst, cstack, true);
        private static float[] Select_object_bool_float_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<bool, float>(fnArgs, scope, inst, cstack, true);
        private static double[] Select_object_bool_double_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<bool, double>(fnArgs, scope, inst, cstack, true);
        private static decimal[] Select_object_bool_decimal_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<bool, decimal>(fnArgs, scope, inst, cstack, true);
        private static char[] Select_object_bool_char_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<bool, char>(fnArgs, scope, inst, cstack, true);
        private static string[] Select_object_bool_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<bool, string>(fnArgs, scope, inst, cstack, true);
        private static object[] Select_object_bool_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<bool, object>(fnArgs, scope, inst, cstack, true);
        private static bool[] Select_object_bool_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<bool, bool>(fnArgs, scope, inst, cstack, true);
        private static DateTime[] Select_object_bool_date_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<bool, DateTime>(fnArgs, scope, inst, cstack, true);
        private static CustomObject Select_object_bool_custom_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetCustomObjectFromArr(Select<bool, CustomObject>(fnArgs, scope, inst, cstack, true), fnArgs[2].Type.CType.Class, inst);
        private static byte[] Select_object_object_byte_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<object, byte>(fnArgs, scope, inst, cstack, true);
        private static short[] Select_object_object_short_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<object, short>(fnArgs, scope, inst, cstack, true);
        private static int[] Select_object_object_int_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<object, int>(fnArgs, scope, inst, cstack, true);
        private static long[] Select_object_object_long_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<object, long>(fnArgs, scope, inst, cstack, true);
        private static float[] Select_object_object_float_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<object, float>(fnArgs, scope, inst, cstack, true);
        private static double[] Select_object_object_double_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<object, double>(fnArgs, scope, inst, cstack, true);
        private static decimal[] Select_object_object_decimal_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<object, decimal>(fnArgs, scope, inst, cstack, true);
        private static char[] Select_object_object_char_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<object, char>(fnArgs, scope, inst, cstack, true);
        private static string[] Select_object_object_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<object, string>(fnArgs, scope, inst, cstack, true);
        private static object[] Select_object_object_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<object, object>(fnArgs, scope, inst, cstack, true);
        private static bool[] Select_object_object_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<object, bool>(fnArgs, scope, inst, cstack, true);
        private static DateTime[] Select_object_object_date_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<object, DateTime>(fnArgs, scope, inst, cstack, true);
        private static CustomObject Select_object_object_custom_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetCustomObjectFromArr(Select<object, CustomObject>(fnArgs, scope, inst, cstack, true), fnArgs[2].Type.CType.Class, inst);
        private static byte[] Select_object_custom_byte_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<CustomObject, byte>(fnArgs, scope, inst, cstack, true);
        private static short[] Select_object_custom_short_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<CustomObject, short>(fnArgs, scope, inst, cstack, true);
        private static int[] Select_object_custom_int_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<CustomObject, int>(fnArgs, scope, inst, cstack, true);
        private static long[] Select_object_custom_long_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<CustomObject, long>(fnArgs, scope, inst, cstack, true);
        private static float[] Select_object_custom_float_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<CustomObject, float>(fnArgs, scope, inst, cstack, true);
        private static double[] Select_object_custom_double_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<CustomObject, double>(fnArgs, scope, inst, cstack, true);
        private static decimal[] Select_object_custom_decimal_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<CustomObject, decimal>(fnArgs, scope, inst, cstack, true);
        private static char[] Select_object_custom_char_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<CustomObject, char>(fnArgs, scope, inst, cstack, true);
        private static string[] Select_object_custom_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<CustomObject, string>(fnArgs, scope, inst, cstack, true);
        private static object[] Select_object_custom_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<CustomObject, object>(fnArgs, scope, inst, cstack, true);
        private static bool[] Select_object_custom_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<CustomObject, bool>(fnArgs, scope, inst, cstack, true);
        private static DateTime[] Select_object_custom_date_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Select<CustomObject, DateTime>(fnArgs, scope, inst, cstack, true);
        private static CustomObject Select_object_custom_custom_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetCustomObjectFromArr(Select<CustomObject, CustomObject>(fnArgs, scope, inst, cstack, true), fnArgs[2].Type.CType.Class, inst);
        private static T2[] Select<T1, T2>(EvalUnit[] fnArgs, int scope, ClassInstance inst, CallStack cstack, bool common)
        {
            EvalUnit varArg = fnArgs[1];
            EvalUnit arrArg = fnArgs[0];

            EvalUnit valArg = fnArgs[2];
            EvalUnit condArg = null;
            bool noCond = true;
            if (fnArgs.Length > 3)
            {
                condArg = fnArgs[3];
                noCond = false;
            }



            Executor exec = inst.Exec;

            var varInfo = varArg.GetVarInfo(scope, inst, cstack);
            if (varArg.Define) ScriptVars<T1>.Add(exec, varInfo.ID, varInfo.Scope);
            int c = 0;
            T2[] result = null;
            T1 val;
            if (!common)
            {
                var en = (IEnumerable<T1>)arrArg.EvalArray(scope, inst, cstack);


                result = new T2[en.Count()];

                foreach (var v in en)
                {

                    val = v;
                    ScriptVars<T1>.Set(exec, varInfo.ID, varInfo.Scope, ref val);
                    if (noCond || condArg.EvalBool(scope, inst, cstack))
                        result[c++] = valArg.Eval<T2>(scope, inst, cstack);
                }
            }
            else
            {
                var en = (System.Collections.IEnumerable)arrArg.EvalObject(scope, inst, cstack);

                result = new T2[GetCountOfUnknown(en)];

                foreach (var v in en)
                {
                    val = (T1)v;
                    ScriptVars<T1>.Set(exec, varInfo.ID, varInfo.Scope, ref val);
                    if (noCond || condArg.EvalBool(scope, inst, cstack))
                        result[c++] = valArg.Eval<T2>(scope, inst, cstack);
                }
            }

            Array.Resize(ref result, c);
            return result;
        }

        private static CustomObject Find_customArray_custom_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Find<CustomObject>(fnArgs, scope, inst, cstack, false).item;
        private static int FindIndex_customArray_custom_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Find<CustomObject>(fnArgs, scope, inst, cstack, false).index;
        private static CustomObject Find_object_custom_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Find<CustomObject>(fnArgs, scope, inst, cstack, true).item;
        private static int FindIndex_object_custom_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Find<CustomObject>(fnArgs, scope, inst, cstack, true).index;

        private static int Find_intArray_int_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Find<int>(fnArgs, scope, inst, cstack, false).item;
        private static int FindIndex_intArray_int_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Find<int>(fnArgs, scope, inst, cstack, false).index;
        private static int Find_object_int_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Find<int>(fnArgs, scope, inst, cstack, true).item;
        private static int FindIndex_object_int_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Find<int>(fnArgs, scope, inst, cstack, true).index;

        private static long Find_longArray_long_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Find<long>(fnArgs, scope, inst, cstack, false).item;
        private static int FindIndex_longArray_long_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Find<long>(fnArgs, scope, inst, cstack, false).index;
        private static long Find_object_long_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Find<long>(fnArgs, scope, inst, cstack, true).item;
        private static int FindIndex_object_long_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Find<long>(fnArgs, scope, inst, cstack, true).index;

        private static float Find_floatArray_float_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Find<float>(fnArgs, scope, inst, cstack, false).item;
        private static int FindIndex_floatArray_float_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Find<float>(fnArgs, scope, inst, cstack, false).index;
        private static float Find_object_float_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Find<float>(fnArgs, scope, inst, cstack, true).item;
        private static int FindIndex_object_float_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Find<float>(fnArgs, scope, inst, cstack, true).index;

        private static double Find_doubleArray_double_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Find<double>(fnArgs, scope, inst, cstack, false).item;
        private static int FindIndex_doubleArray_double_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Find<double>(fnArgs, scope, inst, cstack, false).index;
        private static double Find_object_double_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Find<double>(fnArgs, scope, inst, cstack, true).item;
        private static int FindIndex_object_double_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Find<double>(fnArgs, scope, inst, cstack, true).index;

        private static decimal Find_decimalArray_decimal_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Find<decimal>(fnArgs, scope, inst, cstack, false).item;
        private static int FindIndex_decimalArray_decimal_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Find<decimal>(fnArgs, scope, inst, cstack, false).index;
        private static decimal Find_object_decimal_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Find<decimal>(fnArgs, scope, inst, cstack, true).item;
        private static int FindIndex_object_decimal_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Find<decimal>(fnArgs, scope, inst, cstack, true).index;

        private static bool Find_boolArray_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Find<bool>(fnArgs, scope, inst, cstack, false).item;
        private static int FindIndex_boolArray_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Find<bool>(fnArgs, scope, inst, cstack, false).index;
        private static bool Find_object_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Find<bool>(fnArgs, scope, inst, cstack, true).item;
        private static int FindIndex_object_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Find<bool>(fnArgs, scope, inst, cstack, true).index;

        private static char Find_charArray_char_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Find<char>(fnArgs, scope, inst, cstack, false).item;
        private static int FindIndex_charArray_char_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Find<char>(fnArgs, scope, inst, cstack, false).index;
        private static char Find_object_char_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Find<char>(fnArgs, scope, inst, cstack, true).item;
        private static int FindIndex_object_char_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Find<char>(fnArgs, scope, inst, cstack, true).index;

        private static string Find_stringArray_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Find<string>(fnArgs, scope, inst, cstack, false).item;
        private static int FindIndex_stringArray_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Find<string>(fnArgs, scope, inst, cstack, false).index;
        private static string Find_object_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Find<string>(fnArgs, scope, inst, cstack, true).item;
        private static int FindIndex_object_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Find<string>(fnArgs, scope, inst, cstack, true).index;

        private static object Find_objectArray_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Find<object>(fnArgs, scope, inst, cstack, false).item;
        private static int FindIndex_objectArray_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Find<object>(fnArgs, scope, inst, cstack, false).index;
        private static object Find_object_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Find<object>(fnArgs, scope, inst, cstack, true).item;
        private static int FindIndex_object_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Find<object>(fnArgs, scope, inst, cstack, true).index;

        private static short Find_shortArray_short_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Find<short>(fnArgs, scope, inst, cstack, false).item;
        private static int FindIndex_shortArray_short_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Find<short>(fnArgs, scope, inst, cstack, false).index;
        private static short Find_object_short_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Find<short>(fnArgs, scope, inst, cstack, true).item;
        private static int FindIndex_object_short_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Find<short>(fnArgs, scope, inst, cstack, true).index;

        private static byte Find_byteArray_byte_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Find<byte>(fnArgs, scope, inst, cstack, false).item;
        private static int FindIndex_byteArray_byte_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Find<byte>(fnArgs, scope, inst, cstack, false).index;
        private static byte Find_object_byte_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Find<byte>(fnArgs, scope, inst, cstack, true).item;
        private static int FindIndex_object_byte_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Find<byte>(fnArgs, scope, inst, cstack, true).index;

        private static DateTime Find_dateArray_date_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Find<DateTime>(fnArgs, scope, inst, cstack, false).item;
        private static int FindIndex_dateArray_date_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Find<DateTime>(fnArgs, scope, inst, cstack, false).index;
        private static DateTime Find_object_date_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Find<DateTime>(fnArgs, scope, inst, cstack, true).item;
        private static int FindIndex_object_date_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Find<DateTime>(fnArgs, scope, inst, cstack, true).index;


        private static (int index, T item) Find<T>(EvalUnit[] fnArgs, int scope, ClassInstance inst, CallStack cstack, bool common)
        {
            EvalUnit varArg = fnArgs[1];
            EvalUnit arrArg = fnArgs[0];
            EvalUnit condArg = fnArgs[2];




            Executor exec = inst.Exec;
            var varInfo = varArg.GetVarInfo(scope, inst, cstack);
            if (varArg.Define) ScriptVars<T>.Add(exec, varInfo.ID, varInfo.Scope);


            T val = default(T);

            int c = -1, index = -1;
            if (!common)
            {
                var en = (IEnumerable<T>)arrArg.EvalArray(scope, inst, cstack);



                foreach (var v in en)
                {
                    val = v;
                    c++;
                    ScriptVars<T>.Set(exec, varInfo.ID, varInfo.Scope, ref val);
                    if (condArg.EvalBool(scope, inst, cstack)) { index = c; break; }
                }
            }
            else
            {
                var en = (System.Collections.IEnumerable)arrArg.EvalObject(scope, inst, cstack);


                foreach (var v in en)
                {
                    val = (T)v;
                    c++;
                    ScriptVars<T>.Set(exec, varInfo.ID, varInfo.Scope, ref val);
                    if (condArg.EvalBool(scope, inst, cstack)) { index = c; break; }

                }
            }

            return (index, index >= 0 ? val : default(T));
        }

        public static int GetCountOfUnknown(object source)
        {
            var col = source as System.Collections.ICollection;
            if (col != null)
                return col.Count;
            else
                throw new ScriptLoadingException($"Can't count the number of elements in object of type '{source.GetType().Name}' because it is not an 'ICollection'.");
        }

        private static CustomObject FindAll_customArray_custom_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetCustomObjectFromArr(FindAll<CustomObject>(fnArgs, scope, inst, cstack, false), fnArgs[1].Type.CType.Class, inst);

        private static CustomObject FindAll_object_custom_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetCustomObjectFromArr(FindAll<CustomObject>(fnArgs, scope, inst, cstack, true), fnArgs[1].Type.CType.Class, inst);

        private static int[] FindAll_intArray_int_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FindAll<int>(fnArgs, scope, inst, cstack, false);
        private static int[] FindAll_object_int_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FindAll<int>(fnArgs, scope, inst, cstack, true);




        private static long[] FindAll_longArray_long_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FindAll<long>(fnArgs, scope, inst, cstack, false);
        private static long[] FindAll_object_long_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FindAll<long>(fnArgs, scope, inst, cstack, true);

        private static float[] FindAll_floatArray_float_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FindAll<float>(fnArgs, scope, inst, cstack, false);
        private static float[] FindAll_object_float_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FindAll<float>(fnArgs, scope, inst, cstack, true);

        private static double[] FindAll_doubleArray_double_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FindAll<double>(fnArgs, scope, inst, cstack, false);
        private static double[] FindAll_object_double_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FindAll<double>(fnArgs, scope, inst, cstack, true);


        private static decimal[] FindAll_decimalArray_decimal_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FindAll<decimal>(fnArgs, scope, inst, cstack, false);
        private static decimal[] FindAll_object_decimal_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FindAll<decimal>(fnArgs, scope, inst, cstack, true);


        private static bool[] FindAll_boolArray_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FindAll<bool>(fnArgs, scope, inst, cstack, false);
        private static bool[] FindAll_object_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FindAll<bool>(fnArgs, scope, inst, cstack, true);

        private static char[] FindAll_charArray_char_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FindAll<char>(fnArgs, scope, inst, cstack, false);
        private static char[] FindAll_object_char_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FindAll<char>(fnArgs, scope, inst, cstack, true);

        private static string[] FindAll_stringArray_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FindAll<string>(fnArgs, scope, inst, cstack, false);
        private static string[] FindAll_object_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FindAll<string>(fnArgs, scope, inst, cstack, true);

        private static object[] FindAll_objectArray_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FindAll<object>(fnArgs, scope, inst, cstack, false);


        private static short[] FindAll_shortArray_short_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FindAll<short>(fnArgs, scope, inst, cstack, false);
        private static short[] FindAll_object_short_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FindAll<short>(fnArgs, scope, inst, cstack, true);

        private static byte[] FindAll_byteArray_byte_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FindAll<byte>(fnArgs, scope, inst, cstack, false);
        private static byte[] FindAll_object_byte_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FindAll<byte>(fnArgs, scope, inst, cstack, true);

        private static DateTime[] FindAll_dateArray_date_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FindAll<DateTime>(fnArgs, scope, inst, cstack, false);
        private static DateTime[] FindAll_object_date_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FindAll<DateTime>(fnArgs, scope, inst, cstack, true);


        private static object FindAll_object_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FindAllInObj<object>(fnArgs, scope, inst, cstack);

        private static T[] FindAll<T>(EvalUnit[] fnArgs, int scope, ClassInstance inst, CallStack cstack, bool common, bool equals = true)
        {
            EvalUnit varArg = fnArgs[1];
            EvalUnit arrArg = fnArgs[0];
            EvalUnit condArg = fnArgs[2];




            Executor exec = inst.Exec;
            var varInfo = varArg.GetVarInfo(scope, inst, cstack);
            if (varArg.Define) ScriptVars<T>.Add(exec, varInfo.ID, varInfo.Scope);

            T[] result = null;

            T val = default(T);

            int c = 0;
            if (!common)
            {
                var en = (IEnumerable<T>)arrArg.EvalArray(scope, inst, cstack);

                result = new T[en.Count()];

                foreach (var v in en)
                {
                    val = v;

                    ScriptVars<T>.Set(exec, varInfo.ID, varInfo.Scope, ref val);
                    if (condArg.EvalBool(scope, inst, cstack) == equals) result[c++] = val;
                }
            }
            else
            {
                var en = (System.Collections.IEnumerable)arrArg.EvalObject(scope, inst, cstack);

                result = new T[GetCountOfUnknown(en)];

                foreach (var v in en)
                {
                    val = (T)v;

                    ScriptVars<T>.Set(exec, varInfo.ID, varInfo.Scope, ref val);
                    if (condArg.EvalBool(scope, inst, cstack) == equals) result[c++] = val;

                }
            }

            Array.Resize(ref result, c);
            return result;
        }
        private static object FindAllInObj<T>(EvalUnit[] fnArgs, int scope, ClassInstance inst, CallStack cstack, bool equals = true)
        {
            EvalUnit varArg = fnArgs[1];
            EvalUnit arrArg = fnArgs[0];
            EvalUnit condArg = fnArgs[2];




            Executor exec = inst.Exec;
            var varInfo = varArg.GetVarInfo(scope, inst, cstack);
            if (varArg.Define) ScriptVars<T>.Add(exec, varInfo.ID, varInfo.Scope);



            T val = default(T);

            int c = 0;

            var en = (System.Collections.IEnumerable)arrArg.EvalObject(scope, inst, cstack);




            Type arrType = en is CustomObject ? typeof(CustomObject) : en.GetType().GetElementType();
            Array result = Array.CreateInstance(arrType, GetCountOfUnknown(en));
            foreach (var v in en)
            {
                val = (T)v;

                ScriptVars<T>.Set(exec, varInfo.ID, varInfo.Scope, ref val);
                if (condArg.EvalBool(scope, inst, cstack) == equals) result.SetValue(v, c++);

            }
            Array result2 = Array.CreateInstance(arrType, c);
            Array.Copy(result, result2, c);


            object obj;
            if (result2 is CustomObject[] coArr) obj = GetCustomObjectFromArr(coArr, ((CustomObject)en).Type.Class, inst);
            else obj = result2;

            return obj;
        }

        private static int RemoveAll_intArray_int_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveAll<int>(fnArgs, scope, inst, cstack, false);
        private static int RemoveAll_object_int_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveAll<int>(fnArgs, scope, inst, cstack, true);
        private static int RemoveAll_longArray_long_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveAll<long>(fnArgs, scope, inst, cstack, false);
        private static int RemoveAll_object_long_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveAll<long>(fnArgs, scope, inst, cstack, true);
        private static int RemoveAll_floatArray_float_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveAll<float>(fnArgs, scope, inst, cstack, false);
        private static int RemoveAll_object_float_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveAll<float>(fnArgs, scope, inst, cstack, true);
        private static int RemoveAll_doubleArray_double_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveAll<double>(fnArgs, scope, inst, cstack, false);
        private static int RemoveAll_object_double_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveAll<double>(fnArgs, scope, inst, cstack, true);
        private static int RemoveAll_decimalArray_decimal_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveAll<decimal>(fnArgs, scope, inst, cstack, false);
        private static int RemoveAll_object_decimal_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveAll<decimal>(fnArgs, scope, inst, cstack, true);
        private static int RemoveAll_boolArray_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveAll<bool>(fnArgs, scope, inst, cstack, false);
        private static int RemoveAll_object_bool_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveAll<bool>(fnArgs, scope, inst, cstack, true);
        private static int RemoveAll_charArray_char_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveAll<char>(fnArgs, scope, inst, cstack, false);
        private static int RemoveAll_object_char_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveAll<char>(fnArgs, scope, inst, cstack, true);
        private static int RemoveAll_stringArray_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveAll<string>(fnArgs, scope, inst, cstack, false);
        private static int RemoveAll_object_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveAll<string>(fnArgs, scope, inst, cstack, true);
        private static int RemoveAll_objectArray_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveAll<object>(fnArgs, scope, inst, cstack, false);
        private static int RemoveAll_object_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveAll<object>(fnArgs, scope, inst, cstack, true);
        private static int RemoveAll_shortArray_short_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveAll<short>(fnArgs, scope, inst, cstack, false);
        private static int RemoveAll_object_short_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveAll<short>(fnArgs, scope, inst, cstack, true);
        private static int RemoveAll_byteArray_byte_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveAll<byte>(fnArgs, scope, inst, cstack, false);
        private static int RemoveAll_object_byte_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveAll<byte>(fnArgs, scope, inst, cstack, true);
        private static int RemoveAll_dateArray_date_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveAll<DateTime>(fnArgs, scope, inst, cstack, false);
        private static int RemoveAll_object_date_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveAll<DateTime>(fnArgs, scope, inst, cstack, true);

        private static int RemoveAll_customArray_custom_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveAll<CustomObject>(fnArgs, scope, inst, cstack, false);
        private static int RemoveAll_object_custom_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveAll<CustomObject>(fnArgs, scope, inst, cstack, true);

        private static int RemoveAll<T>(EvalUnit[] fnArgs, int scope, ClassInstance inst, CallStack cstack, bool common = false)
        {
            var obj = fnArgs[0].EvalObject(scope, inst, cstack);
            bool inObj = fnArgs[0].Type.ID == TypeID.Object && common;
            bool custom = false;
            CustomObject customArr = null;

            if (obj is CustomObject co) { customArr = co; obj = (CustomObject[])customArr.Object; custom = true; }
            var list = (System.Collections.IList)obj;
            int c = list.Count;
            if (list.IsFixedSize)
            {
                VarInfo varInfo = fnArgs[0].GetVarInfo(scope, inst, cstack);



                object newArr = common ? FindAllInObj<T>(fnArgs, scope, inst, cstack, false) : FindAll<T>(fnArgs, scope, inst, cstack, common, false);
                if (inObj) { object arrObj = newArr; ScriptVars<object>.Set(inst.Exec, varInfo.ID, varInfo.Scope, ref arrObj); }
                else if (!custom) { T[] arr = (T[])newArr; ScriptVars<T[]>.Set(inst.Exec, varInfo.ID, varInfo.Scope, ref arr); }
                else customArr.Object = newArr;

                return c - ((System.Collections.ICollection)newArr).Count;

            }
            else
            {


                int[] indexes = FindAllIndexes<T>(fnArgs, scope, inst, cstack, true, false);
                for (int i = indexes.Length - 1; i >= 0; i--)

                    list.RemoveAt(indexes[i]);
                return c - list.Count;
            }

        }


        private static int[] FindAllIndexes<T>(EvalUnit[] fnArgs, int scope, ClassInstance inst, CallStack cstack, bool common, bool equals = true)
        {
            EvalUnit varArg = fnArgs[1];
            EvalUnit arrArg = fnArgs[0];
            EvalUnit condArg = fnArgs[2];




            Executor exec = inst.Exec;
            var varInfo = varArg.GetVarInfo(scope, inst, cstack);
            if (varArg.Define) ScriptVars<T>.Add(exec, varInfo.ID, varInfo.Scope);

            int[] result = null;

            T val = default(T);

            int c = 0, index = -1;
            if (!common)
            {
                var en = (IEnumerable<T>)arrArg.EvalArray(scope, inst, cstack);

                result = new int[en.Count()];

                foreach (var v in en)
                {
                    val = v;
                    index++;
                    ScriptVars<T>.Set(exec, varInfo.ID, varInfo.Scope, ref val);
                    if (condArg.EvalBool(scope, inst, cstack) == equals) result[c++] = index;
                }
            }
            else
            {
                var en = (System.Collections.IEnumerable)arrArg.EvalObject(scope, inst, cstack);

                result = new int[GetCountOfUnknown(en)];

                foreach (var v in en)
                {
                    val = (T)v;
                    index++;
                    ScriptVars<T>.Set(exec, varInfo.ID, varInfo.Scope, ref val);
                    if (condArg.EvalBool(scope, inst, cstack) == equals) result[c++] = index;

                }
            }

            Array.Resize(ref result, c);
            return result;
        }

        private static object ForEach_intArray_int_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { ForEach<int>(fnArgs, scope, inst, cstack, false); return null; }
        private static object ForEach_byteArray_byte_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { ForEach<byte>(fnArgs, scope, inst, cstack, false); return null; }
        private static object ForEach_shortArray_short_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { ForEach<short>(fnArgs, scope, inst, cstack, false); return null; }
        private static object ForEach_longArray_long_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { ForEach<long>(fnArgs, scope, inst, cstack, false); return null; }
        private static object ForEach_floatArray_float_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { ForEach<float>(fnArgs, scope, inst, cstack, false); return null; }
        private static object ForEach_doubleArray_double_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { ForEach<double>(fnArgs, scope, inst, cstack, false); return null; }
        private static object ForEach_decimalArray_decimal_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { ForEach<decimal>(fnArgs, scope, inst, cstack, false); return null; }
        private static object ForEach_charArray_char_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { ForEach<char>(fnArgs, scope, inst, cstack, false); return null; }
        private static object ForEach_dateArray_date_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { ForEach<DateTime>(fnArgs, scope, inst, cstack, false); return null; }
        private static object ForEach_stringArray_string_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { ForEach<string>(fnArgs, scope, inst, cstack, false); return null; }
        private static object ForEach_boolArray_bool_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { ForEach<bool>(fnArgs, scope, inst, cstack, false); return null; }
        private static object ForEach_objectArray_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { ForEach<object>(fnArgs, scope, inst, cstack, false); return null; }
        private static object ForEach_customArray_custom_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { ForEach<CustomObject>(fnArgs, scope, inst, cstack, false); return null; }

        private static object ForEach_object_int_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { ForEach<int>(fnArgs, scope, inst, cstack, true); return null; }
        private static object ForEach_object_byte_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { ForEach<byte>(fnArgs, scope, inst, cstack, true); return null; }
        private static object ForEach_object_short_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { ForEach<short>(fnArgs, scope, inst, cstack, true); return null; }
        private static object ForEach_object_long_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { ForEach<long>(fnArgs, scope, inst, cstack, true); return null; }
        private static object ForEach_object_float_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { ForEach<float>(fnArgs, scope, inst, cstack, true); return null; }
        private static object ForEach_object_double_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { ForEach<double>(fnArgs, scope, inst, cstack, true); return null; }
        private static object ForEach_object_decimal_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { ForEach<decimal>(fnArgs, scope, inst, cstack, true); return null; }
        private static object ForEach_object_char_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { ForEach<char>(fnArgs, scope, inst, cstack, true); return null; }
        private static object ForEach_object_date_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { ForEach<DateTime>(fnArgs, scope, inst, cstack, true); return null; }
        private static object ForEach_object_string_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { ForEach<string>(fnArgs, scope, inst, cstack, true); return null; }
        private static object ForEach_object_bool_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { ForEach<bool>(fnArgs, scope, inst, cstack, true); return null; }
        private static object ForEach_object_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { ForEach<object>(fnArgs, scope, inst, cstack, true); return null; }
        private static object ForEach_object_custom_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { ForEach<CustomObject>(fnArgs, scope, inst, cstack, true); return null; }
        private static void ForEach<T1>(EvalUnit[] fnArgs, int scope, ClassInstance inst, CallStack cstack, bool common)
        {
            EvalUnit varArg = fnArgs[1];
            EvalUnit arrArg = fnArgs[0];



            Executor exec = inst.Exec;
            var varInfo = varArg.GetVarInfo(scope, inst, cstack);
            if (varArg.Define) ScriptVars<T1>.Add(exec, varInfo.ID, varInfo.Scope);

            T1 val;
            if (!common)
            {
                var en = (IEnumerable<T1>)arrArg.EvalArray(scope, inst, cstack);

                foreach (var v in en)
                {
                    val = v;
                    ScriptVars<T1>.Set(exec, varInfo.ID, varInfo.Scope, ref val);

                    for (int j = 2; j < fnArgs.Length; j++) fnArgs[j].ProcessUnit(scope, inst, cstack);
                }

            }
            else
            {
                var en = (System.Collections.IEnumerable)arrArg.EvalObject(scope, inst, cstack);
                foreach (var v in en)
                {
                    val = (T1)v;
                    ScriptVars<T1>.Set(exec, varInfo.ID, varInfo.Scope, ref val);

                    for (int j = 2; j < fnArgs.Length; j++) fnArgs[j].ProcessUnit(scope, inst, cstack);
                }
            }
        }

        private static object ClearConsole(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { Console.Clear(); return null; }

        private static string CommandLine(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Environment.CommandLine;
        private static string[] StartArgs(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => inst.Exec.Args;
        private static string AppPath(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => inst.Exec.ExecutedScript.ScriptDir;
        class FuncReference
        {
            public object Func;
            public ClassInstance Inst;
            public EvalUnit EU;
            public FuncReference(object func, ClassInstance inst, EvalUnit eu)
            {
                Func = func;
                Inst = inst;
                EU = eu;
            }
            public override string ToString()
            {
                return RestoreCode(EU.Code);
            }
        }
        private static object FuncRef_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            return GetFuncRef(fnArgs[0], scope, inst, cstack, csrc);


        }
        static FuncReference GetFuncRef(EvalUnit eu, int scope, ClassInstance inst, CallStack cstack, EvalUnit csrc, bool pure = false)
        {
            if (eu.Func == null) throw new ArgumentException($"Failed to get function reference because expression '{RestoreCode(eu.Code)}' is not a function.");
            if (pure) return new FuncReference(eu.Func, null, csrc);
            var varInfo = eu.GetVarInfo(scope, inst, cstack);
            return new FuncReference(eu.Func, varInfo.Inst, csrc);
        }
        private static object FuncPureRef_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            return GetFuncRef(fnArgs[0], scope, inst, cstack, csrc, true);
        }
        private static object CallFunc(object funcToCall, ClassInstance onInst, EvalUnit[] args, int scope, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            switch (funcToCall)
            {
                case FuncToCall<byte> fn: return fn(args, scope, inst, onInst, cstack, csrc);
                case FuncToCall<short> fn: return fn(args, scope, inst, onInst, cstack, csrc);
                case FuncToCall<int> fn: return fn(args, scope, inst, onInst, cstack, csrc);
                case FuncToCall<long> fn: return fn(args, scope, inst, onInst, cstack, csrc);
                case FuncToCall<decimal> fn: return fn(args, scope, inst, onInst, cstack, csrc);
                case FuncToCall<float> fn: return fn(args, scope, inst, onInst, cstack, csrc);
                case FuncToCall<double> fn: return fn(args, scope, inst, onInst, cstack, csrc);
                case FuncToCall<bool> fn: return fn(args, scope, inst, onInst, cstack, csrc);
                case FuncToCall<object> fn: return fn(args, scope, inst, onInst, cstack, csrc);
                case FuncToCall<string> fn: return fn(args, scope, inst, onInst, cstack, csrc);
                case FuncToCall<char> fn: return fn(args, scope, inst, onInst, cstack, csrc);
                case FuncToCall<DateTime> fn: return fn(args, scope, inst, onInst, cstack, csrc);
                case FuncToCall<CustomObject> fn: return fn(args, scope, inst, onInst, cstack, csrc);

                case FuncToCall<byte[]> fn: return fn(args, scope, inst, onInst, cstack, csrc);
                case FuncToCall<short[]> fn: return fn(args, scope, inst, onInst, cstack, csrc);
                case FuncToCall<int[]> fn: return fn(args, scope, inst, onInst, cstack, csrc);
                case FuncToCall<long[]> fn: return fn(args, scope, inst, onInst, cstack, csrc);
                case FuncToCall<decimal[]> fn: return fn(args, scope, inst, onInst, cstack, csrc);
                case FuncToCall<float[]> fn: return fn(args, scope, inst, onInst, cstack, csrc);
                case FuncToCall<double[]> fn: return fn(args, scope, inst, onInst, cstack, csrc);
                case FuncToCall<bool[]> fn: return fn(args, scope, inst, onInst, cstack, csrc);
                case FuncToCall<object[]> fn: return fn(args, scope, inst, onInst, cstack, csrc);
                case FuncToCall<string[]> fn: return fn(args, scope, inst, onInst, cstack, csrc);
                case FuncToCall<char[]> fn: return fn(args, scope, inst, onInst, cstack, csrc);
                case FuncToCall<DateTime[]> fn: return fn(args, scope, inst, onInst, cstack, csrc);

                default:
                    throw new ScriptExecutionException($"Type '{funcToCall.GetType().Name}' not supported.");
            }
        }
        private static object Call_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            var fnRef = (FuncReference)fnArgs[0].EvalObject(scope, inst, cstack);
            EvalUnit[] args = fnArgs.Skip(1).ToArray();

            return CallFunc(fnRef.Func, fnRef.Inst, args, scope, inst, cstack, csrc);
        }
        private static object CallOn_object_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            var fnRef = (FuncReference)fnArgs[0].EvalObject(scope, inst, cstack);

            bool isStatic;
            var onInst = GetCI(fnArgs[1].EvalObject(scope, inst, cstack), out isStatic, inst.Exec);

            EvalUnit[] args = fnArgs.Skip(2).ToArray();

            return CallFunc(fnRef.Func, onInst, args, scope, inst, cstack, csrc);
        }
        private static ClassInstance GetCI(object obj, out bool isStatic, Executor exec)
        {
            if (obj is CustomObject co)
            {
                isStatic = false;
                return (ClassInstance)co;
            }
            else if (obj is CustomType ct && ct.Class != null)
            {
                isStatic = true;
                return exec.GetStaticInstance(ct.Class);
            }
            else
                throw new ArgumentException($"Invalid argument of type '{obj.GetType()}'.");
        }
        private static (ScriptFunction fn, ClassInstance inst, EvalUnit[] args) DynGetFunc(EvalUnit[] fnArgs, int scope, ClassInstance inst, CallStack cstack)
        {
            var obj = fnArgs[0].EvalObject(scope, inst, cstack);
            bool isStatic;
            ClassInstance onInst = GetCI(obj, out isStatic, inst.Exec);


            var fnName = fnArgs[1].EvalString(scope, inst, cstack);
            var args = fnArgs.Skip(2).ToArray();
            var fn = onInst.Class.GetFunc(fnName, args, false, null, isStatic ? true : null);
            if (fn == null)
                throw new ArgumentException($"Function '{FormatFuncSign(GetFuncSign(fnName, args))}' not found at '{onInst.Class.ClassFullName}'.");
            return (fn, onInst, args);
        }
        private static object DynCall_object_string_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            var fnData = DynGetFunc(fnArgs, scope, inst, cstack);
            return ExecuteFunction<object>(fnData.fn, fnData.args, scope, inst, fnData.inst, cstack, csrc);

        }

        private static object FuncRef_object_string_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            var fnData = DynGetFunc(fnArgs, scope, inst, cstack);
            var ftc = GetFuncToCall<object>(fnData.fn);
            return new FuncReference(ftc, fnData.inst, csrc);
        }
        private static object FuncPureRef_object_string_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            var fnData = DynGetFunc(fnArgs, scope, inst, cstack);
            var ftc = GetFuncToCall<object>(fnData.fn);
            return new FuncReference(ftc, null, csrc);
        }
        private static object Lock_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            var obj = fnArgs[0].EvalObject(scope, inst, cstack);
            Monitor.Enter(obj);
            return null;
        }
        private static object Lock_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            var obj = fnArgs[0].EvalObject(scope, inst, cstack);
            VarInfo varInfo = fnArgs[1].GetVarInfo(scope, inst, cstack);
            bool lockTaken = false;
            Monitor.Enter(obj, ref lockTaken);
            ScriptVars<bool>.Set(inst.Exec, varInfo.ID, varInfo.Scope, ref lockTaken);
            return null;
        }
        private static object Unlock_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            var obj = fnArgs[0].EvalObject(scope, inst, cstack);
            Monitor.Exit(obj);
            return null;
        }
        private static object Enumerator_object_object_object_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Enumerator_object_object_object_object(fnArgs, scope, inst, cstack, csrc, false);
        private static object EnumeratorByFuncRefs_object_object_object_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Enumerator_object_object_object_object(fnArgs, scope, inst, cstack, csrc, true);
        private static object Enumerator_object_object_object_object(EvalUnit[] fnArgs, int scope, ClassInstance inst, CallStack cstack, EvalUnit csrc, bool byFnRef)
        {
            var moveNextFnRef = GetFuncRefArg(fnArgs[0], byFnRef, scope, inst, cstack, csrc);
            var currentFnRef = GetFuncRefArg(fnArgs[1], byFnRef, scope, inst, cstack, csrc);
            var resetFnRef = GetFuncRefArg(fnArgs[2], byFnRef, scope, inst, cstack, csrc);
            var disposeFnRef = ArgExists(fnArgs, 3) ? GetFuncRefArg(fnArgs[3], byFnRef, scope, inst, cstack, csrc) : null;

            Func<bool> moveNext = () => (bool)CallFunc(moveNextFnRef.Func, moveNextFnRef.Inst, null, scope, inst, cstack, csrc);
            Func<object> current = () => CallFunc(currentFnRef.Func, currentFnRef.Inst, null, scope, inst, cstack, csrc);
            Action reset = () => CallFunc(resetFnRef.Func, resetFnRef.Inst, null, scope, inst, cstack, csrc);
            Action dispose = disposeFnRef != null ? () => CallFunc(disposeFnRef.Func, disposeFnRef.Inst, null, scope, inst, cstack, csrc) : null;
            return new Enumerator(moveNext, current, reset, dispose);
        }
        private static object Enumerator_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            var co = fnArgs[0].EvalCustom(scope, inst, cstack);
            var c = co.Type.Class;
            var ci = (ClassInstance)co.Object;
            if (c.EnumeratorMoveNextFunc == null) throw new ScriptExecutionException("Enumerator class must contain the function 'bool MoveNext()'.");
            if (c.EnumeratorCurrentFunc == null) throw new ScriptExecutionException("Enumerator class must contain the function 'object Current()'.");
            if (c.EnumeratorResetFunc == null) throw new ScriptExecutionException("Enumerator class must contain the function 'void Reset()'.");

            Func<bool> moveNext = () => (bool)CallFunc(c.EnumeratorMoveNextFunc, ci, null, scope, inst, cstack, csrc);
            Func<object> current = () => CallFunc(c.EnumeratorCurrentFunc, ci, null, scope, inst, cstack, csrc);
            Action reset = () => CallFunc(c.EnumeratorResetFunc, ci, null, scope, inst, cstack, csrc);
            Action dispose = c.DisposeFunc != null ? () => CallFunc(c.DisposeFunc, ci, null, scope, inst, cstack, csrc) : null;
            return new Enumerator(moveNext, current, reset, dispose);
        }
        private static object EventHandler_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => EventHandler_object(fnArgs, scope, inst, cstack, csrc, false);
        private static object EventHandlerByFuncRef_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => EventHandler_object(fnArgs, scope, inst, cstack, csrc, true);
        private static object EventHandler_object(EvalUnit[] fnArgs, int scope, ClassInstance inst, CallStack cstack, EvalUnit csrc, bool byFnRef)
        {
            var fnRef = GetFuncRefArg(fnArgs[0], byFnRef, scope, inst, cstack, csrc);
            EventHandler eh = (object sender, EventArgs e) =>
            {
                EvalUnit arg1 = EvalUnit.GetEUWithSpecificValue(sender);
                EvalUnit arg2 = EvalUnit.GetEUWithSpecificValue(e);
                EvalUnit[] args = new EvalUnit[] { arg1, arg2 };
                CallFunc(fnRef.Func, fnRef.Inst, args, -1, null, null, csrc);
            };

            return eh;
        }
        static FuncReference GetFuncRefArg(EvalUnit eu, bool byFnRef, int scope, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            FuncReference fnRef;
            if (byFnRef)
                fnRef = (FuncReference)eu.EvalObject(scope, inst, cstack);
            else
                fnRef = GetFuncRef(eu, scope, inst, cstack, csrc);

            if (fnRef == null) throw new ArgumentException("Function reference missing.");
            return fnRef;
        }

        class GenEventHandler
        {

            public static EventHandler<T> GetEventHandler<T>(FuncReference fnRef, EvalUnit csrc)
            {
                EventHandler<T> eh = (object sender, T e) =>
                {
                    EvalUnit arg1 = EvalUnit.GetEUWithSpecificValue(sender);
                    EvalUnit arg2 = EvalUnit.GetEUWithSpecificValue(e);
                    EvalUnit[] args = new EvalUnit[] { arg1, arg2 };
                    CallFunc(fnRef.Func, fnRef.Inst, args, -1, null, null, csrc);
                };
                return eh;
            }
        }
        private static object EventHandler_object_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => EventHandler_object_object(fnArgs, scope, inst, cstack, csrc, false);
        private static object EventHandlerByFuncRef_object_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => EventHandler_object_object(fnArgs, scope, inst, cstack, csrc, true);
        private static object EventHandler_object_object(EvalUnit[] fnArgs, int scope, ClassInstance inst, CallStack cstack, EvalUnit csrc, bool byFnRef)
        {
            var fnRef = GetFuncRefArg(fnArgs[0], byFnRef, scope, inst, cstack, csrc);
            var type = fnArgs[1].Type.ID == TypeID.String ? GetTypeArgFromStr(fnArgs[1].EvalString(scope, inst, cstack)) : GetTypeArgFromObj(fnArgs[1].EvalObject(scope, inst, cstack));

            var met = typeof(GenEventHandler).GetMethod("GetEventHandler");
            var gmet = met.MakeGenericMethod(type.Type);
            object eh = gmet.Invoke(null, new object[] { fnRef, csrc });



            return eh;
        }


        private static object Raise_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {



            var h = fnArgs[0].EvalObject(scope, inst, cstack);
            if (h is EventHandler eh)
            {
                object sender = fnArgs.Length > 1 ? fnArgs[1].EvalObject(scope, inst, cstack) : null;


                var arg = fnArgs.Length > 2 ? (EventArgs)fnArgs[2].EvalObject(scope, inst, cstack) : EventArgs.Empty;
                eh.Invoke(sender, arg);

            }
            else if (h is Delegate d)
            {
                object[] args = EvalArgs(1, fnArgs, scope, inst, cstack);

                return d.DynamicInvoke(args);
            }
            else
                throw new ScriptExecutionException($"Raise function not supported for object of type '{h.GetType()}'.");

            return null;
        }




        class DelegateCaller
        {
            public const int ArgLimit = 7;
            FuncReference FnRef;
            EvalUnit FnEU;
            public DelegateCaller(FuncReference fnRef, EvalUnit csrc)
            {
                FnRef = fnRef;
                FnEU = csrc;
            }

            public void CallAction0()
            {
                EvalUnit[] args = null;
                CallFunc(FnRef.Func, FnRef.Inst, args, -1, null, null, FnEU);
            }
            public void CallAction1<T>(T a1)
            {
                EvalUnit[] args = new EvalUnit[] { EvalUnit.GetEUWithSpecificValue(a1) };
                CallFunc(FnRef.Func, FnRef.Inst, args, -1, null, null, FnEU);
            }
            public void CallAction2<T, T2>(T a1, T2 a2)
            {
                EvalUnit[] args = new EvalUnit[] { EvalUnit.GetEUWithSpecificValue(a1), EvalUnit.GetEUWithSpecificValue(a2) };
                CallFunc(FnRef.Func, FnRef.Inst, args, -1, null, null, FnEU);
            }
            public void CallAction3<T, T2, T3>(T a1, T2 a2, T3 a3)
            {
                EvalUnit[] args = new EvalUnit[] { EvalUnit.GetEUWithSpecificValue(a1), EvalUnit.GetEUWithSpecificValue(a2), EvalUnit.GetEUWithSpecificValue(a3) };
                CallFunc(FnRef.Func, FnRef.Inst, args, -1, null, null, FnEU);
            }
            public void CallAction4<T, T2, T3, T4>(T a1, T2 a2, T3 a3, T4 a4)
            {
                EvalUnit[] args = new EvalUnit[] { EvalUnit.GetEUWithSpecificValue(a1), EvalUnit.GetEUWithSpecificValue(a2), EvalUnit.GetEUWithSpecificValue(a3), EvalUnit.GetEUWithSpecificValue(a4) };
                CallFunc(FnRef.Func, FnRef.Inst, args, -1, null, null, FnEU);
            }
            public void CallAction5<T, T2, T3, T4, T5>(T a1, T2 a2, T3 a3, T4 a4, T5 a5)
            {
                EvalUnit[] args = new EvalUnit[] { EvalUnit.GetEUWithSpecificValue(a1), EvalUnit.GetEUWithSpecificValue(a2), EvalUnit.GetEUWithSpecificValue(a3), EvalUnit.GetEUWithSpecificValue(a4), EvalUnit.GetEUWithSpecificValue(a5) };
                CallFunc(FnRef.Func, FnRef.Inst, args, -1, null, null, FnEU);
            }
            public void CallAction6<T, T2, T3, T4, T5, T6>(T a1, T2 a2, T3 a3, T4 a4, T5 a5, T6 a6)
            {
                EvalUnit[] args = new EvalUnit[] { EvalUnit.GetEUWithSpecificValue(a1), EvalUnit.GetEUWithSpecificValue(a2), EvalUnit.GetEUWithSpecificValue(a3), EvalUnit.GetEUWithSpecificValue(a4), EvalUnit.GetEUWithSpecificValue(a5), EvalUnit.GetEUWithSpecificValue(a6) };
                CallFunc(FnRef.Func, FnRef.Inst, args, -1, null, null, FnEU);
            }
            public void CallAction7<T, T2, T3, T4, T5, T6, T7>(T a1, T2 a2, T3 a3, T4 a4, T5 a5, T6 a6, T7 a7)
            {
                EvalUnit[] args = new EvalUnit[] { EvalUnit.GetEUWithSpecificValue(a1), EvalUnit.GetEUWithSpecificValue(a2), EvalUnit.GetEUWithSpecificValue(a3), EvalUnit.GetEUWithSpecificValue(a4), EvalUnit.GetEUWithSpecificValue(a5), EvalUnit.GetEUWithSpecificValue(a6), EvalUnit.GetEUWithSpecificValue(a7) };
                CallFunc(FnRef.Func, FnRef.Inst, args, -1, null, null, FnEU);
            }

            public T CallFunction0<T>()
            {
                EvalUnit[] args = null;
                return (T)CallFunc(FnRef.Func, FnRef.Inst, args, -1, null, null, FnEU);
            }
            public T2 CallFunction1<T, T2>(T a1)
            {
                EvalUnit[] args = new EvalUnit[] { EvalUnit.GetEUWithSpecificValue(a1) };
                return (T2)CallFunc(FnRef.Func, FnRef.Inst, args, -1, null, null, FnEU);
            }
            public T3 CallFunction2<T, T2, T3>(T a1, T2 a2)
            {


                EvalUnit[] args = new EvalUnit[] { EvalUnit.GetEUWithSpecificValue(a1), EvalUnit.GetEUWithSpecificValue(a2) };
                return (T3)CallFunc(FnRef.Func, FnRef.Inst, args, -1, null, null, FnEU);
            }
            public T4 CallFunction3<T, T2, T3, T4>(T a1, T2 a2, T3 a3)
            {
                EvalUnit[] args = new EvalUnit[] { EvalUnit.GetEUWithSpecificValue(a1), EvalUnit.GetEUWithSpecificValue(a2), EvalUnit.GetEUWithSpecificValue(a3) };
                return (T4)CallFunc(FnRef.Func, FnRef.Inst, args, -1, null, null, FnEU);
            }
            public T5 CallFunction4<T, T2, T3, T4, T5>(T a1, T2 a2, T3 a3, T4 a4)
            {
                EvalUnit[] args = new EvalUnit[] { EvalUnit.GetEUWithSpecificValue(a1), EvalUnit.GetEUWithSpecificValue(a2), EvalUnit.GetEUWithSpecificValue(a3), EvalUnit.GetEUWithSpecificValue(a4) };
                return (T5)CallFunc(FnRef.Func, FnRef.Inst, args, -1, null, null, FnEU);
            }
            public T6 CallFunction5<T, T2, T3, T4, T5, T6>(T a1, T2 a2, T3 a3, T4 a4, T5 a5)
            {
                EvalUnit[] args = new EvalUnit[] { EvalUnit.GetEUWithSpecificValue(a1), EvalUnit.GetEUWithSpecificValue(a2), EvalUnit.GetEUWithSpecificValue(a3), EvalUnit.GetEUWithSpecificValue(a4), EvalUnit.GetEUWithSpecificValue(a5) };
                return (T6)CallFunc(FnRef.Func, FnRef.Inst, args, -1, null, null, FnEU);
            }
            public T7 CallFunction6<T, T2, T3, T4, T5, T6, T7>(T a1, T2 a2, T3 a3, T4 a4, T5 a5, T6 a6)
            {
                EvalUnit[] args = new EvalUnit[] { EvalUnit.GetEUWithSpecificValue(a1), EvalUnit.GetEUWithSpecificValue(a2), EvalUnit.GetEUWithSpecificValue(a3), EvalUnit.GetEUWithSpecificValue(a4), EvalUnit.GetEUWithSpecificValue(a5), EvalUnit.GetEUWithSpecificValue(a6) };
                return (T7)CallFunc(FnRef.Func, FnRef.Inst, args, -1, null, null, FnEU);
            }
            public T8 CallFunction7<T, T2, T3, T4, T5, T6, T7, T8>(T a1, T2 a2, T3 a3, T4 a4, T5 a5, T6 a6, T7 a7)
            {
                EvalUnit[] args = new EvalUnit[] { EvalUnit.GetEUWithSpecificValue(a1), EvalUnit.GetEUWithSpecificValue(a2), EvalUnit.GetEUWithSpecificValue(a3), EvalUnit.GetEUWithSpecificValue(a4), EvalUnit.GetEUWithSpecificValue(a5), EvalUnit.GetEUWithSpecificValue(a6), EvalUnit.GetEUWithSpecificValue(a7) };
                return (T8)CallFunc(FnRef.Func, FnRef.Inst, args, -1, null, null, FnEU);
            }
        }

        private static object Delegate_object_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Delegate_object_object(fnArgs, scope, inst, cstack, csrc, false);
        private static object DelegateByFuncRef_object_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Delegate_object_object(fnArgs, scope, inst, cstack, csrc, true);
        private static object Delegate_object_object(EvalUnit[] fnArgs, int scope, ClassInstance inst, CallStack cstack, EvalUnit csrc, bool byFnRef)
        {
            FuncReference fnRef = GetFuncRefArg(fnArgs[0], byFnRef, scope, inst, cstack, csrc);

            var type = fnArgs[1].Type.ID == TypeID.String ? GetTypeArgFromStr(fnArgs[1].EvalString(scope, inst, cstack)).Type : (Type)fnArgs[1].EvalObject(scope, inst, cstack);


            var c = new DelegateCaller(fnRef, csrc);



            var met = type.GetMethod("Invoke");
            bool isVoid = met.ReturnType == typeof(void);
            var paramTypes = met.GetParameters().Select(x => x.ParameterType).ToArray();
            if (!isVoid)
            {
                Array.Resize(ref paramTypes, paramTypes.Length + 1);
                paramTypes[paramTypes.Length - 1] = met.ReturnType;
            }
            int n = paramTypes.Length;
            if (!isVoid) n--;



            var methodInfo = isVoid ? c.GetType().GetMethod("CallAction" + n.ToString()) : c.GetType().GetMethod("CallFunction" + n.ToString());
            if (methodInfo == null)
                throw new ScriptExecutionException($"Failed to create a delegate. " + (n > DelegateCaller.ArgLimit ? $"Too many parameters ({n}). Limit is {DelegateCaller.ArgLimit}." : ""));

            if (methodInfo.IsGenericMethod)
                methodInfo = methodInfo.MakeGenericMethod(paramTypes);

            Delegate handler = Delegate.CreateDelegate(type, c, methodInfo);


            return handler;
        }


        private static bool Is_object_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ObjIs(GetTypeArgFromStr(fnArgs[1].EvalString(scope, inst, cstack)), fnArgs[0].EvalObject(scope, inst, cstack));
        private static bool Is_object_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ObjIs(GetTypeArgFromObj(fnArgs[1].EvalObject(scope, inst, cstack)), fnArgs[0].EvalObject(scope, inst, cstack));
        private static bool Is_object_customType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ObjIs(GetTypeArgFromCustomType((CustomType)fnArgs[1].EvalObject(scope, inst, cstack)), fnArgs[0].EvalObject(scope, inst, cstack));
        private static bool Is_object_hintArrayType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ObjIsByHintArray(GetTypeArgFromObj(fnArgs[1].EvalObject(scope, inst, cstack)), fnArgs[0].EvalObject(scope, inst, cstack));

        private static bool ObjIs(TypeArg typeArg, object obj)
        {
            if (typeArg.CType != null)
            {
                if (!(obj is CustomObject co)) return false;
                var ct = typeArg.CType;

                var t = co.Type;
                if (t == ct) return true;
                if (ct.IsArray != co.Type.IsArray) return false;

                return t.Class.Is(ct.Class);
            }
            else
                return typeArg.Type.IsInstanceOfType(obj);
        }
        private static bool ObjIsByHintArray(TypeArg typeArg, object obj)
        {
            var et = typeArg.Type.GetElementType();
            if (!et.IsValueType && typeArg.Type.IsInstanceOfType(obj)) return true;
            if (obj is object[] arr)
            {
                int n = FindMismatchedElement(arr, et);
                return n < 0;
            }
            else return false;
        }

        private static string GetAbsoluteUri_string_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetAbsoluteUri(fnArgs[0].EvalString(scope, inst, cstack), fnArgs[1].EvalString(scope, inst, cstack)).AbsoluteUri;
        private static string GetAbsoluteUri_string_string_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            var u = GetAbsoluteUri(fnArgs[0].EvalString(scope, inst, cstack), fnArgs[1].EvalString(scope, inst, cstack));
            return fnArgs[2].EvalBool(scope, inst, cstack) ? u.PathAndQuery : u.AbsoluteUri;
        }
        private static Uri GetAbsoluteUri(string basePath, string uri) => new Uri(new Uri(basePath), uri);


        private static string Slice_string_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            string str = fnArgs[0].EvalString(scope, inst, cstack);
            int start = fnArgs[1].EvalInt(scope, inst, cstack);
            int stop = fnArgs[2].EvalInt(scope, inst, cstack);
            if (start < 0) start = str.Length + start;
            if (stop < 0) stop = str.Length + stop;

            if (stop <= start || start >= str.Length || stop < 1) return "";
            if (start < 0) start = 0;
            if (stop > str.Length) stop = str.Length;
            return str.Substring(start, stop - start);

        }
        private static string Slice_string_empty_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            string str = fnArgs[0].EvalString(scope, inst, cstack);
            int stop = fnArgs[2].EvalInt(scope, inst, cstack);
            if (stop < 0) stop = str.Length + stop;

            if (stop < 1) return "";
            if (stop > str.Length) stop = str.Length;
            return str.Substring(0, stop);
        }
        private static string Slice_string_int_empty(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            string str = fnArgs[0].EvalString(scope, inst, cstack);
            int start = fnArgs[1].EvalInt(scope, inst, cstack);
            if (start < 0) start = str.Length + start;

            if (start >= str.Length) return "";
            if (start < 0) start = 0;
            return str.Substring(start);
        }

        private static int[] Slice_intArray_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Slice<int>(fnArgs, scope, inst, cstack);
        private static int[] Slice_intArray_empty_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => SliceTake<int>(fnArgs, scope, inst, cstack);
        private static int[] Slice_intArray_int_empty(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => SliceSkip<int>(fnArgs, scope, inst, cstack);

        private static string[] Slice_stringArray_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Slice<string>(fnArgs, scope, inst, cstack);
        private static string[] Slice_stringArray_empty_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => SliceTake<string>(fnArgs, scope, inst, cstack);
        private static string[] Slice_stringArray_int_empty(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => SliceSkip<string>(fnArgs, scope, inst, cstack);

        private static long[] Slice_longArray_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Slice<long>(fnArgs, scope, inst, cstack);
        private static long[] Slice_longArray_empty_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => SliceTake<long>(fnArgs, scope, inst, cstack);
        private static long[] Slice_longArray_int_empty(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => SliceSkip<long>(fnArgs, scope, inst, cstack);

        private static float[] Slice_floatArray_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Slice<float>(fnArgs, scope, inst, cstack);
        private static float[] Slice_floatArray_empty_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => SliceTake<float>(fnArgs, scope, inst, cstack);
        private static float[] Slice_floatArray_int_empty(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => SliceSkip<float>(fnArgs, scope, inst, cstack);

        private static double[] Slice_doubleArray_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Slice<double>(fnArgs, scope, inst, cstack);
        private static double[] Slice_doubleArray_empty_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => SliceTake<double>(fnArgs, scope, inst, cstack);
        private static double[] Slice_doubleArray_int_empty(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => SliceSkip<double>(fnArgs, scope, inst, cstack);

        private static decimal[] Slice_decimalArray_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Slice<decimal>(fnArgs, scope, inst, cstack);
        private static decimal[] Slice_decimalArray_empty_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => SliceTake<decimal>(fnArgs, scope, inst, cstack);
        private static decimal[] Slice_decimalArray_int_empty(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => SliceSkip<decimal>(fnArgs, scope, inst, cstack);

        private static bool[] Slice_boolArray_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Slice<bool>(fnArgs, scope, inst, cstack);
        private static bool[] Slice_boolArray_empty_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => SliceTake<bool>(fnArgs, scope, inst, cstack);
        private static bool[] Slice_boolArray_int_empty(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => SliceSkip<bool>(fnArgs, scope, inst, cstack);

        private static char[] Slice_charArray_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Slice<char>(fnArgs, scope, inst, cstack);
        private static char[] Slice_charArray_empty_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => SliceTake<char>(fnArgs, scope, inst, cstack);
        private static char[] Slice_charArray_int_empty(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => SliceSkip<char>(fnArgs, scope, inst, cstack);

        private static object[] Slice_objectArray_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Slice<object>(fnArgs, scope, inst, cstack);
        private static object[] Slice_objectArray_empty_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => SliceTake<object>(fnArgs, scope, inst, cstack);
        private static object[] Slice_objectArray_int_empty(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => SliceSkip<object>(fnArgs, scope, inst, cstack);

        private static byte[] Slice_byteArray_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Slice<byte>(fnArgs, scope, inst, cstack);
        private static byte[] Slice_byteArray_empty_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => SliceTake<byte>(fnArgs, scope, inst, cstack);
        private static byte[] Slice_byteArray_int_empty(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => SliceSkip<byte>(fnArgs, scope, inst, cstack);

        private static short[] Slice_shortArray_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Slice<short>(fnArgs, scope, inst, cstack);
        private static short[] Slice_shortArray_empty_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => SliceTake<short>(fnArgs, scope, inst, cstack);
        private static short[] Slice_shortArray_int_empty(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => SliceSkip<short>(fnArgs, scope, inst, cstack);

        private static DateTime[] Slice_dateArray_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Slice<DateTime>(fnArgs, scope, inst, cstack);
        private static DateTime[] Slice_dateArray_empty_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => SliceTake<DateTime>(fnArgs, scope, inst, cstack);
        private static DateTime[] Slice_dateArray_int_empty(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => SliceSkip<DateTime>(fnArgs, scope, inst, cstack);

        private static CustomObject Slice_customArray_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetCustomObjectFromArr(Slice<CustomObject>(fnArgs, scope, inst, cstack, true), fnArgs[0].Type.CType.Class, inst);
        private static CustomObject Slice_customArray_empty_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetCustomObjectFromArr(SliceTake<CustomObject>(fnArgs, scope, inst, cstack, true), fnArgs[0].Type.CType.Class, inst);
        private static CustomObject Slice_customArray_int_empty(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetCustomObjectFromArr(SliceSkip<CustomObject>(fnArgs, scope, inst, cstack, true), fnArgs[0].Type.CType.Class, inst);


        private static T[] Slice<T>(EvalUnit[] fnArgs, int scope, ClassInstance inst, CallStack cstack, bool custom = false)
        {
            T[] arr;
            if (custom)
                arr = (T[])(object)(CustomObject[])fnArgs[0].EvalCustomArray(scope, inst, cstack);
            else
                arr = fnArgs[0].Eval<T[]>(scope, inst, cstack);

            int start = fnArgs[1].EvalInt(scope, inst, cstack);
            int stop = fnArgs[2].EvalInt(scope, inst, cstack);
            if (start < 0) start = arr.Length + start;
            if (stop < 0) stop = arr.Length + stop;

            if (stop <= start || start >= arr.Length || stop < 1) return new T[0];
            if (start < 0) start = 0;
            if (stop > arr.Length) stop = arr.Length;
            T[] result = new T[stop - start];
            Array.Copy(arr, start, result, 0, stop - start);
            return result;
        }
        private static T[] SliceTake<T>(EvalUnit[] fnArgs, int scope, ClassInstance inst, CallStack cstack, bool custom = false)
        {

            T[] arr;
            if (custom)
                arr = (T[])(object)(CustomObject[])fnArgs[0].EvalCustomArray(scope, inst, cstack);
            else
                arr = fnArgs[0].Eval<T[]>(scope, inst, cstack);

            int stop = fnArgs[2].EvalInt(scope, inst, cstack);
            if (stop < 0) stop = arr.Length + stop;

            if (stop < 1) return new T[0];
            if (stop > arr.Length) stop = arr.Length;

            return arr.Take(stop).ToArray();
        }
        private static T[] SliceSkip<T>(EvalUnit[] fnArgs, int scope, ClassInstance inst, CallStack cstack, bool custom = false)
        {

            T[] arr;
            if (custom)
                arr = (T[])(object)(CustomObject[])fnArgs[0].EvalCustomArray(scope, inst, cstack);
            else
                arr = fnArgs[0].Eval<T[]>(scope, inst, cstack);

            int start = fnArgs[1].EvalInt(scope, inst, cstack);
            if (start < 0) start = arr.Length + start;

            if (start >= arr.Length) return new T[0];
            if (start < 0) start = 0;

            return arr.Skip(start).ToArray();
        }

        private static int[] Splice_intArray_int_int_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Splice<int>(fnArgs, scope, inst, cstack);

        private static string[] Splice_stringArray_int_int_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Splice<string>(fnArgs, scope, inst, cstack);

        private static long[] Splice_longArray_int_int_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Splice<long>(fnArgs, scope, inst, cstack);

        private static float[] Splice_floatArray_int_int_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Splice<float>(fnArgs, scope, inst, cstack);

        private static double[] Splice_doubleArray_int_int_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Splice<double>(fnArgs, scope, inst, cstack);

        private static decimal[] Splice_decimalArray_int_int_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Splice<decimal>(fnArgs, scope, inst, cstack);

        private static bool[] Splice_boolArray_int_int_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Splice<bool>(fnArgs, scope, inst, cstack);

        private static char[] Splice_charArray_int_int_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Splice<char>(fnArgs, scope, inst, cstack);

        private static object[] Splice_objectArray_int_int_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Splice<object>(fnArgs, scope, inst, cstack);

        private static byte[] Splice_byteArray_int_int_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Splice<byte>(fnArgs, scope, inst, cstack);

        private static short[] Splice_shortArray_int_int_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Splice<short>(fnArgs, scope, inst, cstack);

        private static DateTime[] Splice_dateArray_int_int_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Splice<DateTime>(fnArgs, scope, inst, cstack);

        private static CustomObject Splice_customArray_int_int_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => GetCustomObjectFromArr(Splice<CustomObject>(fnArgs, scope, inst, cstack, true), fnArgs[0].Type.CType.Class, inst);

        private static T[] Splice<T>(EvalUnit[] fnArgs, int scope, ClassInstance inst, CallStack cstack, bool custom = false)
        {
            VarInfo varInfo = fnArgs[0].GetVarInfo(scope, inst, cstack);
            Executor exec = inst.Exec;
            CustomObject customArr = null;
            T[] arr = custom ? (T[])(customArr = ScriptVars<CustomObject>.Get(exec, varInfo.ID, varInfo.Scope)).Object : ScriptVars<T[]>.Get(exec, varInfo.ID, varInfo.Scope);


            T[] vals = EvalArgs<T>(3, fnArgs, scope, inst, cstack);
            int valCount = vals == null ? 0 : vals.Length;

            int start = fnArgs[1].EvalInt(scope, inst, cstack);


            if (start < 0) start = arr.Length + start;
            else if (start > arr.Length) start = arr.Length;

            if (start < 0) start = 0;

            int deleteCount = ArgExists(fnArgs, 2) ? fnArgs[2].EvalInt(scope, inst, cstack) : arr.Length - start;
            int stop = start + deleteCount;
            if (stop > arr.Length)
            {
                stop = arr.Length;
                deleteCount = stop - start;
            }

            T[] result = new T[deleteCount];
            if (deleteCount > 0)
            {
                result = new T[deleteCount];

                Array.Copy(arr, start, result, 0, deleteCount);
            }

            int diff = valCount - deleteCount;
            if (diff != 0)
            {
                if (diff > 0)
                {
                    Array.Resize(ref arr, arr.Length + diff);

                    Array.Copy(arr, start, arr, start + diff, arr.Length - (start + diff));

                }
                else
                {
                    diff = Math.Abs(diff);
                    Array.Copy(arr, start + diff, arr, start, arr.Length - (start + diff));
                    Array.Resize(ref arr, arr.Length - diff);
                }
            }
            if (valCount > 0) vals.CopyTo(arr, start);



            if (!custom) ScriptVars<T[]>.Set(exec, varInfo.ID, varInfo.Scope, ref arr);
            else customArr.Object = arr;


            return result;
        }


        private static int Push_intArray_intParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Insert<int>(fnArgs, scope, inst, cstack, InsertMode.AtEnd, false);
        private static int Push_longArray_longParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Insert<long>(fnArgs, scope, inst, cstack, InsertMode.AtEnd, false);
        private static int Push_floatArray_floatParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Insert<float>(fnArgs, scope, inst, cstack, InsertMode.AtEnd, false);
        private static int Push_doubleArray_doubleParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Insert<double>(fnArgs, scope, inst, cstack, InsertMode.AtEnd, false);
        private static int Push_decimalArray_decimalParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Insert<decimal>(fnArgs, scope, inst, cstack, InsertMode.AtEnd, false);
        private static int Push_boolArray_boolParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Insert<bool>(fnArgs, scope, inst, cstack, InsertMode.AtEnd, false);
        private static int Push_stringArray_stringParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Insert<string>(fnArgs, scope, inst, cstack, InsertMode.AtEnd, false);
        private static int Push_charArray_charParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Insert<char>(fnArgs, scope, inst, cstack, InsertMode.AtEnd, false);
        private static int Push_objectArray_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Insert<object>(fnArgs, scope, inst, cstack, InsertMode.AtEnd, false);
        private static int Push_byteArray_byteParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Insert<byte>(fnArgs, scope, inst, cstack, InsertMode.AtEnd, false);
        private static int Push_shortArray_shortParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Insert<short>(fnArgs, scope, inst, cstack, InsertMode.AtEnd, false);
        private static int Push_dateArray_dateParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Insert<DateTime>(fnArgs, scope, inst, cstack, InsertMode.AtEnd, false);
        private static int Push_customArray_customParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Insert<CustomObject>(fnArgs, scope, inst, cstack, InsertMode.AtEnd, false, true);


        private static int ConvPush_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => InsertIntoObject(fnArgs, scope, inst, cstack, InsertMode.AtEnd, false, true);

        private static int Push_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => InsertIntoObject(fnArgs, scope, inst, cstack, InsertMode.AtEnd, false);


        private static int Peek_intArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Peek<int>(fnArgs, scope, inst, cstack);
        private static long Peek_longArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Peek<long>(fnArgs, scope, inst, cstack);
        private static float Peek_floatArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Peek<float>(fnArgs, scope, inst, cstack);
        private static double Peek_doubleArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Peek<double>(fnArgs, scope, inst, cstack);
        private static decimal Peek_decimalArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Peek<decimal>(fnArgs, scope, inst, cstack);
        private static bool Peek_boolArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Peek<bool>(fnArgs, scope, inst, cstack);
        private static string Peek_stringArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Peek<string>(fnArgs, scope, inst, cstack);
        private static char Peek_charArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Peek<char>(fnArgs, scope, inst, cstack);
        private static object Peek_objectArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Peek<object>(fnArgs, scope, inst, cstack);
        private static byte Peek_byteArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Peek<byte>(fnArgs, scope, inst, cstack);
        private static short Peek_shortArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Peek<short>(fnArgs, scope, inst, cstack);
        private static DateTime Peek_dateArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Peek<DateTime>(fnArgs, scope, inst, cstack);
        private static CustomObject Peek_customArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Peek<CustomObject>(fnArgs, scope, inst, cstack, true);


        private static T Peek<T>(EvalUnit[] fnArgs, int scope, ClassInstance inst, CallStack cstack, bool custom = false)
        {
            T[] arr = custom ? (T[])fnArgs[0].EvalCustomArray(scope, inst, cstack) : (T[])fnArgs[0].EvalObject(scope, inst, cstack);

            return arr.Last();
        }
        private static object Peek_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {

            object obj = fnArgs[0].EvalObject(scope, inst, cstack);
            switch (obj)
            {
                case Array arr:
                    return arr.GetValue(arr.Length - 1);
                case System.Collections.IList list:
                    return list[list.Count - 1];
                case System.Collections.Stack stack:
                    return stack.Peek();
                case System.Collections.Queue queue:
                    return queue.Peek();
                case CustomObject co:
                    return ((CustomObject[])co).Last();
            }
            throw new ScriptExecutionException($"The Peek function cannot be applied to the object of type '{obj.GetType()}'.");
        }

        private static int Pop_intArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Pop<int>(fnArgs, scope, inst, cstack);
        private static long Pop_longArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Pop<long>(fnArgs, scope, inst, cstack);
        private static float Pop_floatArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Pop<float>(fnArgs, scope, inst, cstack);
        private static double Pop_doubleArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Pop<double>(fnArgs, scope, inst, cstack);
        private static decimal Pop_decimalArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Pop<decimal>(fnArgs, scope, inst, cstack);
        private static bool Pop_boolArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Pop<bool>(fnArgs, scope, inst, cstack);
        private static string Pop_stringArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Pop<string>(fnArgs, scope, inst, cstack);
        private static char Pop_charArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Pop<char>(fnArgs, scope, inst, cstack);
        private static object Pop_objectArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Pop<object>(fnArgs, scope, inst, cstack);
        private static byte Pop_byteArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Pop<byte>(fnArgs, scope, inst, cstack);
        private static short Pop_shortArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Pop<short>(fnArgs, scope, inst, cstack);
        private static DateTime Pop_dateArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Pop<DateTime>(fnArgs, scope, inst, cstack);
        private static CustomObject Pop_customArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Pop<CustomObject>(fnArgs, scope, inst, cstack, true);


        private static object PopFromArray(Array arr, VarInfo varInfo, ClassInstance inst)
        {
            int n = arr.Length - 1;
            if (n < 0) throw new InvalidOperationException("Array empty.");

            object v = arr.GetValue(n);
            var et = arr.GetType().GetElementType();
            var arr2 = Array.CreateInstance(et, n);
            Array.Copy(arr, arr2, arr2.Length);

            object arr3 = arr2;
            ScriptVars<object>.Set(inst.Exec, varInfo.ID, varInfo.Scope, ref arr3);
            return v;
        }

        private static object Pop_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {

            object obj = fnArgs[0].EvalObject(scope, inst, cstack);
            switch (obj)
            {
                case Array arr:
                    return PopFromArray(arr, fnArgs[0].GetVarInfo(scope, inst, cstack), inst);
                case System.Collections.IList list:
                    int n = list.Count - 1;
                    if (n < 0) throw new InvalidOperationException("List empty.");
                    object v = list[n];
                    list.RemoveAt(n);
                    return v;
                case System.Collections.Stack stack:
                    return stack.Pop();
                case System.Collections.Queue queue:
                    return queue.Dequeue();
                case CustomObject co:
                    return ((CustomObject[])co).Last();
            }
            throw new ScriptExecutionException($"The Pop function cannot be applied to the object of type '{obj.GetType()}'.");
        }

        private static T Pop<T>(EvalUnit[] fnArgs, int scope, ClassInstance inst, CallStack cstack, bool custom = false)
        {
            VarInfo varInfo = fnArgs[0].GetVarInfo(scope, inst, cstack);

            Executor exec = inst.Exec;
            CustomObject customArr = null;
            T[] arr = custom ? (T[])(customArr = ScriptVars<CustomObject>.Get(exec, varInfo.ID, varInfo.Scope)).Object : ScriptVars<T[]>.Get(exec, varInfo.ID, varInfo.Scope);
            int len = arr.Length - 1;
            if (len < 0) throw new InvalidOperationException("Array empty.");
            T v = arr.Last();

            Array.Resize(ref arr, len);

            if (!custom) ScriptVars<T[]>.Set(inst.Exec, varInfo.ID, varInfo.Scope, ref arr);
            else customArr.Object = arr;

            return v;
        }


        private static int At_intArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => At<int>(fnArgs, scope, inst, cstack);
        private static long At_longArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => At<long>(fnArgs, scope, inst, cstack);
        private static float At_floatArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => At<float>(fnArgs, scope, inst, cstack);
        private static double At_doubleArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => At<double>(fnArgs, scope, inst, cstack);
        private static decimal At_decimalArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => At<decimal>(fnArgs, scope, inst, cstack);
        private static bool At_boolArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => At<bool>(fnArgs, scope, inst, cstack);
        private static string At_stringArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => At<string>(fnArgs, scope, inst, cstack);
        private static char At_charArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => At<char>(fnArgs, scope, inst, cstack);
        private static object At_objectArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => At<object>(fnArgs, scope, inst, cstack);
        private static byte At_byteArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => At<byte>(fnArgs, scope, inst, cstack);
        private static short At_shortArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => At<short>(fnArgs, scope, inst, cstack);
        private static DateTime At_dateArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => At<DateTime>(fnArgs, scope, inst, cstack);
        private static CustomObject At_customArray(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => At<CustomObject>(fnArgs, scope, inst, cstack, true);

        private static object At_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            object obj = fnArgs[0].EvalObject(scope, inst, cstack);
            System.Collections.IList list;

            if (obj is CustomObject co) list = (CustomObject[])co;
            else list = (System.Collections.IList)obj;

            int index = fnArgs[1].EvalInt(scope, inst, cstack);
            if (index < 0) index = list.Count + index;
            return list[index];
        }
        private static object ElementAt_object_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            var en = (System.Collections.IEnumerable)fnArgs[0].EvalObject(scope, inst, cstack);
            int index = fnArgs[1].EvalInt(scope, inst, cstack);

            int n = 0;
            if (index >= 0)
            {

                foreach (var item in en)
                {
                    if (n == index) return item;
                    n++;
                }
            }

            throw new ArgumentOutOfRangeException($"Index {index} was out of range. " + (index < 0 ? "Must be non-negative." : $"Must be less than {n}."), (Exception)null);
        }
        private static T At<T>(EvalUnit[] fnArgs, int scope, ClassInstance inst, CallStack cstack, bool custom = false)
        {
            T[] arr = custom ? (T[])fnArgs[0].EvalCustomArray(scope, inst, cstack) : (T[])fnArgs[0].EvalObject(scope, inst, cstack);
            int index = fnArgs[1].EvalInt(scope, inst, cstack);
            if (index < 0) index = arr.Length + index;
            return arr[index];
        }
        private static char At_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            string str = fnArgs[0].EvalString(scope, inst, cstack);
            int index = fnArgs[1].EvalInt(scope, inst, cstack);
            if (index < 0) index = str.Length + index;
            return str[index];
        }

        private static int Insert_intArray_int_intParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Insert<int>(fnArgs, scope, inst, cstack, InsertMode.AtIndex, false);
        private static int Insert_longArray_int_longParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Insert<long>(fnArgs, scope, inst, cstack, InsertMode.AtIndex, false);
        private static int Insert_floatArray_int_floatParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Insert<float>(fnArgs, scope, inst, cstack, InsertMode.AtIndex, false);
        private static int Insert_doubleArray_int_doubleParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Insert<double>(fnArgs, scope, inst, cstack, InsertMode.AtIndex, false);
        private static int Insert_decimalArray_int_decimalParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Insert<decimal>(fnArgs, scope, inst, cstack, InsertMode.AtIndex, false);
        private static int Insert_boolArray_int_boolParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Insert<bool>(fnArgs, scope, inst, cstack, InsertMode.AtIndex, false);
        private static int Insert_stringArray_int_stringParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Insert<string>(fnArgs, scope, inst, cstack, InsertMode.AtIndex, false);
        private static int Insert_charArray_int_charParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Insert<char>(fnArgs, scope, inst, cstack, InsertMode.AtIndex, false);
        private static int Insert_objectArray_int_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Insert<object>(fnArgs, scope, inst, cstack, InsertMode.AtIndex, false);
        private static int Insert_byteArray_int_byteParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Insert<byte>(fnArgs, scope, inst, cstack, InsertMode.AtIndex, false);
        private static int Insert_shortArray_int_shortParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Insert<short>(fnArgs, scope, inst, cstack, InsertMode.AtIndex, false);
        private static int Insert_dateArray_int_dateParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Insert<DateTime>(fnArgs, scope, inst, cstack, InsertMode.AtIndex, false);
        private static int Insert_customArray_int_customParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Insert<CustomObject>(fnArgs, scope, inst, cstack, InsertMode.AtIndex, false, true);


        private static int Insert_object_int_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => InsertIntoObject(fnArgs, scope, inst, cstack, InsertMode.AtIndex, false, false);
        private static int ConvInsert_object_int_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => InsertIntoObject(fnArgs, scope, inst, cstack, InsertMode.AtIndex, false, true);


        private static int InsertRange_intArray_int_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Insert<int>(fnArgs, scope, inst, cstack, InsertMode.AtIndex, true);
        private static int InsertRange_longArray_int_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Insert<long>(fnArgs, scope, inst, cstack, InsertMode.AtIndex, true);
        private static int InsertRange_floatArray_int_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Insert<float>(fnArgs, scope, inst, cstack, InsertMode.AtIndex, true);
        private static int InsertRange_doubleArray_int_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Insert<double>(fnArgs, scope, inst, cstack, InsertMode.AtIndex, true);
        private static int InsertRange_decimalArray_int_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Insert<decimal>(fnArgs, scope, inst, cstack, InsertMode.AtIndex, true);
        private static int InsertRange_boolArray_int_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Insert<bool>(fnArgs, scope, inst, cstack, InsertMode.AtIndex, true);
        private static int InsertRange_stringArray_int_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Insert<string>(fnArgs, scope, inst, cstack, InsertMode.AtIndex, true);
        private static int InsertRange_charArray_int_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Insert<char>(fnArgs, scope, inst, cstack, InsertMode.AtIndex, true);
        private static int InsertRange_objectArray_int_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Insert<object>(fnArgs, scope, inst, cstack, InsertMode.AtIndex, true);
        private static int InsertRange_byteArray_int_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Insert<byte>(fnArgs, scope, inst, cstack, InsertMode.AtIndex, true);
        private static int InsertRange_shortArray_int_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Insert<short>(fnArgs, scope, inst, cstack, InsertMode.AtIndex, true);
        private static int InsertRange_dateArray_int_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Insert<DateTime>(fnArgs, scope, inst, cstack, InsertMode.AtIndex, true);
        private static int InsertRange_customArray_int_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => Insert<CustomObject>(fnArgs, scope, inst, cstack, InsertMode.AtIndex, true, true);

        private static int InsertRange_object_int_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => InsertIntoObject(fnArgs, scope, inst, cstack, InsertMode.AtIndex, true);


        private static int InsertIntoObject(EvalUnit[] fnArgs, int scope, ClassInstance inst, CallStack cstack, InsertMode mode, bool range, bool conv = false)
        {

            int c = 2;
            object obj = fnArgs[0].EvalObject(scope, inst, cstack);
            int index = 0;
            if (mode == InsertMode.AtIndex) index = fnArgs[1].EvalInt(scope, inst, cstack); else c--;

            bool singleVal = !range;

            object val;
            if (fnArgs.Length == c + 1 || range)
                val = fnArgs[c].Ev<object>(scope, inst, cstack);
            else
            {
                if (fnArgs.Length <= c) throw new ArgumentException("No values to insert.");
                val = EvalArgs(c, fnArgs, scope, inst, cstack);
                singleVal = false;
            }




            if (range)
            {
                if (fnArgs.Length == c + 2) conv = fnArgs[c + 1].EvalBool(scope, inst, cstack);
                if (val is CustomObject cobj) val = cobj.Object;
            }


            switch (obj)
            {

                case Array arr:
                    VarInfo varInfo = fnArgs[0].GetVarInfo(scope, inst, cstack);
                    Array newArr = InsertIntoArray(arr, index, val, mode, singleVal, conv);
                    object arr2 = newArr;
                    ScriptVars<object>.Set(inst.Exec, varInfo.ID, varInfo.Scope, ref arr2);
                    return newArr.Length;

                case CustomObject co:

                    newArr = InsertIntoArray((CustomObject[])co, index, val, mode, singleVal, conv);


                    co.Object = newArr;
                    return newArr.Length;

                case System.Collections.IList list:



                    if (mode == InsertMode.AtEnd) index = list.Count;
                    InsertIntoList(list, index, val, singleVal, conv);
                    return list.Count;

                case System.Collections.Stack stack:
                    if (!singleVal && val is Array valArr)
                        foreach (object v in valArr) stack.Push(v);
                    else
                        stack.Push(val);

                    return stack.Count;
                case System.Collections.Queue queue:
                    if (!singleVal && val is Array valArr2)
                        foreach (object v in valArr2) queue.Enqueue(v);
                    else
                        queue.Enqueue(val);

                    return queue.Count;


            }
            throw new ScriptExecutionException($"Cannot insert items into an object of type '{obj.GetType()}'.");
        }
        private static void InsertIntoList(System.Collections.IList list, int index, object val, bool singleVal, bool conv)
        {
            if (conv)
            {
                Type listType = list.GetType();
                Type et = listType.GetGenericArguments().SingleOrDefault();
                if (et == null) throw new NotSupportedException($"Insert with conversion is not supported for object of type '{listType}'.");
                if (!singleVal && val is Array vals)
                {
                    foreach (object v in vals)
                        list.Insert(index++, ConvertValue(v, et));
                }
                else
                    list.Insert(index, ConvertValue(val, et));
            }
            else
            {
                if (!singleVal && val is Array vals)
                {
                    foreach (object v in vals)
                        list.Insert(index++, v);
                }
                else
                    list.Insert(index, val);
            }
        }
        private static Array InsertIntoArray(Array arr, int index, object val, InsertMode mode, bool singleVal, bool conv = false)
        {
            int len = arr.Length;
            if (mode == InsertMode.AtEnd) index = len;
            int valCount;
            Array vals = null;
            var arrType = arr.GetType();
            var et = arrType.GetElementType();
            if (singleVal)
            {
                valCount = 1;
                if (conv) val = ConvertValue(val, et);
            }
            else
            {

                vals = (Array)val;


                valCount = vals.Length;
            }


            var arr2 = Array.CreateInstance(et, len + valCount);
            Array.Copy(arr, arr2, index);
            Array.Copy(arr, index, arr2, index + valCount, arr2.Length - (index + valCount));
            if (singleVal)
                arr2.SetValue(val, index);
            else
            {

                if (conv && arrType != vals.GetType())
                {
                    for (int i = 0; i < valCount; i++)
                    {
                        object v = vals.GetValue(i);
                        arr2.SetValue(ConvertValue(v, et), index + i);

                    }
                }
                else
                {
                    try { vals.CopyTo(arr2, index); }
                    catch (ArrayTypeMismatchException) { throw new ArrayTypeMismatchException($"Could not insert elements of type '{vals.GetType().GetElementType()}' into array of type '{arr2.GetType()}'."); }
                }
            }

            return arr2;
        }
        enum InsertMode : byte { AtIndex, AtStart, AtEnd }
        private static int Insert<T>(EvalUnit[] fnArgs, int scope, ClassInstance inst, CallStack cstack, InsertMode mode, bool range, bool custom = false)
        {
            VarInfo varInfo = fnArgs[0].GetVarInfo(scope, inst, cstack);

            Executor exec = inst.Exec;
            CustomObject customArr = null;
            T[] arr = custom ? (T[])(customArr = ScriptVars<CustomObject>.Get(exec, varInfo.ID, varInfo.Scope)).Object : ScriptVars<T[]>.Get(exec, varInfo.ID, varInfo.Scope);

            int index = 0;
            int c = 2;
            if (mode == InsertMode.AtIndex) index = fnArgs[1].EvalInt(scope, inst, cstack); else c--;


            bool singleVal = !range;
            bool conv = false;
            if (range && fnArgs.Length == c + 2) conv = fnArgs[c + 1].EvalBool(scope, inst, cstack);

            object val;
            if (fnArgs.Length == c + 1 || range)
                val = range ? fnArgs[c].Ev<object>(scope, inst, cstack) : fnArgs[c].Ev<T>(scope, inst, cstack);
            else
            {
                if (fnArgs.Length <= c) throw new ArgumentException("No values to insert.");
                val = EvalArgs<T>(c, fnArgs, scope, inst, cstack);

                singleVal = false;
            }


            if (range && val is CustomObject co) val = co.Object;

            T[] newArr = (T[])InsertIntoArray(arr, index, val, mode, singleVal, conv);
            if (!custom) ScriptVars<T[]>.Set(exec, varInfo.ID, varInfo.Scope, ref newArr);
            else customArr.Object = newArr;

            return newArr.Length;
        }


        private static int RemoveRange_intArray_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveRange<int>(fnArgs, scope, inst, cstack);
        private static int RemoveRange_longArray_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveRange<long>(fnArgs, scope, inst, cstack);
        private static int RemoveRange_floatArray_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveRange<float>(fnArgs, scope, inst, cstack);
        private static int RemoveRange_doubleArray_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveRange<double>(fnArgs, scope, inst, cstack);
        private static int RemoveRange_decimalArray_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveRange<decimal>(fnArgs, scope, inst, cstack);
        private static int RemoveRange_boolArray_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveRange<bool>(fnArgs, scope, inst, cstack);
        private static int RemoveRange_stringArray_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveRange<string>(fnArgs, scope, inst, cstack);
        private static int RemoveRange_charArray_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveRange<char>(fnArgs, scope, inst, cstack);
        private static int RemoveRange_objectArray_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveRange<object>(fnArgs, scope, inst, cstack);
        private static int RemoveRange_byteArray_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveRange<byte>(fnArgs, scope, inst, cstack);
        private static int RemoveRange_shortArray_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveRange<short>(fnArgs, scope, inst, cstack);
        private static int RemoveRange_dateArray_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveRange<DateTime>(fnArgs, scope, inst, cstack);
        private static int RemoveRange_customArray_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveRange<CustomObject>(fnArgs, scope, inst, cstack, true);

        private static int RemoveRange<T>(EvalUnit[] fnArgs, int scope, ClassInstance inst, CallStack cstack, bool custom = false)
        {
            VarInfo varInfo = fnArgs[0].GetVarInfo(scope, inst, cstack);

            Executor exec = inst.Exec;
            CustomObject customArr = null;
            T[] arr = custom ? (T[])(customArr = ScriptVars<CustomObject>.Get(exec, varInfo.ID, varInfo.Scope)).Object : ScriptVars<T[]>.Get(exec, varInfo.ID, varInfo.Scope);

            int index = fnArgs[1].EvalInt(scope, inst, cstack);
            int count = fnArgs.Length > 2 ? fnArgs[2].EvalInt(scope, inst, cstack) : 1;
            T[] newArr = (T[])RemoveRangeFromArray(arr, index, count);

            if (!custom) ScriptVars<T[]>.Set(exec, varInfo.ID, varInfo.Scope, ref newArr);
            else customArr.Object = newArr;

            return newArr.Length;
        }
        private static int RemoveRange_object_int_int(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            object obj = fnArgs[0].EvalObject(scope, inst, cstack);
            int index = fnArgs[1].EvalInt(scope, inst, cstack);
            int count = fnArgs.Length > 2 ? fnArgs[2].EvalInt(scope, inst, cstack) : 1;

            switch (obj)
            {

                case Array arr:

                    VarInfo varInfo = fnArgs[0].GetVarInfo(scope, inst, cstack);
                    Array newArr = RemoveRangeFromArray(arr, index, count);
                    object arr2 = newArr;
                    ScriptVars<object>.Set(inst.Exec, varInfo.ID, varInfo.Scope, ref arr2);
                    return newArr.Length;

                case CustomObject co:

                    newArr = RemoveRangeFromArray((CustomObject[])co, index, count);
                    co.Object = newArr;
                    return newArr.Length;

                case System.Collections.IList list:

                    for (int i = 0; i < count; i++)
                        list.RemoveAt(index);
                    return list.Count;

            }
            throw new ScriptExecutionException($"Cannot remove items from an object of type '{obj.GetType()}'.");
        }

        private static Array RemoveRangeFromArray(Array arr, int index, int count)
        {
            int len = arr.Length;
            var et = arr.GetType().GetElementType();
            var arr2 = Array.CreateInstance(et, len - count);

            Array.Copy(arr, arr2, index);
            Array.Copy(arr, index + count, arr2, index, arr.Length - (index + count));

            return arr2;
        }
        private static int RemoveValue_intArray_intParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveValueFromArray<int>(fnArgs, scope, inst, cstack);
        private static int RemoveValue_longArray_longParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveValueFromArray<long>(fnArgs, scope, inst, cstack);
        private static int RemoveValue_floatArray_floatParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveValueFromArray<float>(fnArgs, scope, inst, cstack);
        private static int RemoveValue_doubleArray_doubleParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveValueFromArray<double>(fnArgs, scope, inst, cstack);
        private static int RemoveValue_decimalArray_decimalParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveValueFromArray<decimal>(fnArgs, scope, inst, cstack);
        private static int RemoveValue_boolArray_boolParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveValueFromArray<bool>(fnArgs, scope, inst, cstack);
        private static int RemoveValue_stringArray_stringParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveValueFromArray<string>(fnArgs, scope, inst, cstack);
        private static int RemoveValue_charArray_charParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveValueFromArray<char>(fnArgs, scope, inst, cstack);
        private static int RemoveValue_objectArray_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveValueFromArray<object>(fnArgs, scope, inst, cstack);
        private static int RemoveValue_byteArray_byteParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveValueFromArray<byte>(fnArgs, scope, inst, cstack);
        private static int RemoveValue_shortArray_shortParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveValueFromArray<short>(fnArgs, scope, inst, cstack);
        private static int RemoveValue_dateArray_dateParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveValueFromArray<DateTime>(fnArgs, scope, inst, cstack);
        private static int RemoveValue_customArray_customParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveValueFromArray<CustomObject>(fnArgs, scope, inst, cstack, true);

        private static int RemoveValue_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RemoveValueFromObject(fnArgs, scope, inst, cstack);


        private static int RemoveValueFromArray<T>(EvalUnit[] fnArgs, int scope, ClassInstance inst, CallStack cstack, bool custom = false)
        {
            VarInfo varInfo = fnArgs[0].GetVarInfo(scope, inst, cstack);

            CustomObject customArr = null;
            T[] arr = custom ? (T[])(customArr = ScriptVars<CustomObject>.Get(inst.Exec, varInfo.ID, varInfo.Scope)).Object : ScriptVars<T[]>.Get(inst.Exec, varInfo.ID, varInfo.Scope);


            object v;
            bool singleVal = true;
            if (fnArgs.Length == 2) v = fnArgs[1].Ev<T>(scope, inst, cstack);
            else { v = EvalArgs<T>(1, fnArgs, scope, inst, cstack); singleVal = false; }

            T[] newArr = (T[])RemoveValue(arr, v, singleVal);

            if (!custom) ScriptVars<T[]>.Set(inst.Exec, varInfo.ID, varInfo.Scope, ref newArr);
            else customArr.Object = newArr;

            return newArr.Length;
        }
        private static Array RemoveValue(Array arr, object val, bool singleVal = true)
        {
            int len = arr.Length;
            var et = arr.GetType().GetElementType();

            bool[] rem = new bool[len];
            int c = 0;
            if (!singleVal && val is Array vals)
            {

                for (int i = 0; i < len; i++)
                {
                    var v = arr.GetValue(i);

                    if (Array.IndexOf(vals, v) >= 0) { c++; rem[i] = true; }
                }
            }
            else
            {

                for (int i = 0; i < len; i++)
                {
                    var v = arr.GetValue(i);
                    if (v == val || (v != null && v.Equals(val))) { c++; rem[i] = true; }
                }
            }
            var arr2 = Array.CreateInstance(et, len - c);
            int n = 0;
            for (int i = 0; i < len; i++)
                if (!rem[i]) arr2.SetValue(arr.GetValue(i), n++);


            return arr2;
        }

        private static int RemoveValueFromObject(EvalUnit[] fnArgs, int scope, ClassInstance inst, CallStack cstack)
        {
            object obj = fnArgs[0].EvalObject(scope, inst, cstack);

            object val;
            bool singleVal = true;
            if (fnArgs.Length == 2) val = fnArgs[1].Ev<object>(scope, inst, cstack);
            else { val = EvalArgs(1, fnArgs, scope, inst, cstack); singleVal = false; }

            if (val is CustomObject cobj) val = cobj.Object;

            switch (obj)
            {

                case Array arr:
                    Type et = arr.GetType().GetElementType();
                    val = val is object[] valArr ? ConvVals(valArr, et) : ConvertValue(val, et);
                    VarInfo varInfo = fnArgs[0].GetVarInfo(scope, inst, cstack);
                    Array newArr = RemoveValue(arr, val, singleVal);
                    object arr2 = newArr;
                    ScriptVars<object>.Set(inst.Exec, varInfo.ID, varInfo.Scope, ref arr2);
                    return newArr.Length;

                case CustomObject co:

                    newArr = RemoveValue((Array)co, val, singleVal);


                    co.Object = newArr;
                    return newArr.Length;

                case System.Collections.IList list:
                    Type listType = list.GetType();
                    et = listType.GetGenericArguments().SingleOrDefault();


                    if (!singleVal && val is Array vals)
                    {
                        foreach (object v in vals)
                            list.Remove(et != null ? ConvertValue(v, et) : v);
                    }
                    else
                        list.Remove(et != null ? ConvertValue(val, et) : val);

                    return list.Count;




            }
            throw new ScriptExecutionException($"Cannot insert items into an object of type '{obj.GetType()}'.");
        }


        static Array ConvVals(Array arr, Type t)
        {
            for (int i = 0; i < arr.Length; i++)
                arr.SetValue(ConvertValue(arr.GetValue(i), t), i);

            return arr;
        }



        private static void CopyVars(Executor exec, List<VarName> vars, int fromScope, int toScope)
        {


            for (int i = 0; i < vars.Count; i++)
            {
                var v = vars[i];
                int varId = v.Id;

                switch (v.Type.ID)
                {
                    case TypeID.Int: { int val = ScriptVars<int>.Get(exec, varId, fromScope); ScriptVars<int>.Set(exec, varId, toScope, ref val); break; }
                    case TypeID.Long: { long val = ScriptVars<long>.Get(exec, varId, fromScope); ScriptVars<long>.Set(exec, varId, toScope, ref val); break; }
                    case TypeID.Byte: { byte val = ScriptVars<byte>.Get(exec, varId, fromScope); ScriptVars<byte>.Set(exec, varId, toScope, ref val); break; }
                    case TypeID.Short: { short val = ScriptVars<short>.Get(exec, varId, fromScope); ScriptVars<short>.Set(exec, varId, toScope, ref val); break; }
                    case TypeID.Float: { float val = ScriptVars<float>.Get(exec, varId, fromScope); ScriptVars<float>.Set(exec, varId, toScope, ref val); break; }
                    case TypeID.Double: { double val = ScriptVars<double>.Get(exec, varId, fromScope); ScriptVars<double>.Set(exec, varId, toScope, ref val); break; }
                    case TypeID.Decimal: { decimal val = ScriptVars<decimal>.Get(exec, varId, fromScope); ScriptVars<decimal>.Set(exec, varId, toScope, ref val); break; }
                    case TypeID.Bool: { bool val = ScriptVars<bool>.Get(exec, varId, fromScope); ScriptVars<bool>.Set(exec, varId, toScope, ref val); break; }
                    case TypeID.Date: { DateTime val = ScriptVars<DateTime>.Get(exec, varId, fromScope); ScriptVars<DateTime>.Set(exec, varId, toScope, ref val); break; }
                    case TypeID.String: { string val = ScriptVars<string>.Get(exec, varId, fromScope); ScriptVars<string>.Set(exec, varId, toScope, ref val); break; }
                    case TypeID.Char: { char val = ScriptVars<char>.Get(exec, varId, fromScope); ScriptVars<char>.Set(exec, varId, toScope, ref val); break; }
                    case TypeID.Object: { object val = ScriptVars<object>.Get(exec, varId, fromScope); ScriptVars<object>.Set(exec, varId, toScope, ref val); break; }
                    case TypeID.Custom:
                    case TypeID.CustomArray: { CustomObject val = ScriptVars<CustomObject>.Get(exec, varId, fromScope); ScriptVars<CustomObject>.Set(exec, varId, toScope, ref val); break; }

                    case TypeID.IntArray: { int[] val = ScriptVars<int[]>.Get(exec, varId, fromScope); ScriptVars<int[]>.Set(exec, varId, toScope, ref val); break; }
                    case TypeID.LongArray: { long[] val = ScriptVars<long[]>.Get(exec, varId, fromScope); ScriptVars<long[]>.Set(exec, varId, toScope, ref val); break; }
                    case TypeID.ByteArray: { byte[] val = ScriptVars<byte[]>.Get(exec, varId, fromScope); ScriptVars<byte[]>.Set(exec, varId, toScope, ref val); break; }
                    case TypeID.ShortArray: { short[] val = ScriptVars<short[]>.Get(exec, varId, fromScope); ScriptVars<short[]>.Set(exec, varId, toScope, ref val); break; }
                    case TypeID.FloatArray: { float[] val = ScriptVars<float[]>.Get(exec, varId, fromScope); ScriptVars<float[]>.Set(exec, varId, toScope, ref val); break; }
                    case TypeID.DoubleArray: { double[] val = ScriptVars<double[]>.Get(exec, varId, fromScope); ScriptVars<double[]>.Set(exec, varId, toScope, ref val); break; }
                    case TypeID.DecimalArray: { decimal[] val = ScriptVars<decimal[]>.Get(exec, varId, fromScope); ScriptVars<decimal[]>.Set(exec, varId, toScope, ref val); break; }
                    case TypeID.BoolArray: { bool[] val = ScriptVars<bool[]>.Get(exec, varId, fromScope); ScriptVars<bool[]>.Set(exec, varId, toScope, ref val); break; }
                    case TypeID.DateArray: { DateTime[] val = ScriptVars<DateTime[]>.Get(exec, varId, fromScope); ScriptVars<DateTime[]>.Set(exec, varId, toScope, ref val); break; }
                    case TypeID.StringArray: { string[] val = ScriptVars<string[]>.Get(exec, varId, fromScope); ScriptVars<string[]>.Set(exec, varId, toScope, ref val); break; }
                    case TypeID.CharArray: { char[] val = ScriptVars<char[]>.Get(exec, varId, fromScope); ScriptVars<char[]>.Set(exec, varId, toScope, ref val); break; }
                    case TypeID.ObjectArray: { object[] val = ScriptVars<object[]>.Get(exec, varId, fromScope); ScriptVars<object[]>.Set(exec, varId, toScope, ref val); break; }

                    default: throw new ScriptExecutionException($"Wrong type '{v.Type.ID}'.");
                }
            }

        }

        private static CustomObject As_object_customType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (CustomObject)ObjAs(GetTypeArgFromCustomType((CustomType)fnArgs[1].EvalObject(scope, inst, cstack)), fnArgs[0].EvalObject(scope, inst, cstack));
        private static object As_object_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ObjAs(GetTypeArgFromObj(fnArgs[1].EvalObject(scope, inst, cstack)), fnArgs[0].EvalObject(scope, inst, cstack));
        private static object As_object_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ObjAs(GetTypeArgFromStr(fnArgs[1].EvalString(scope, inst, cstack)), fnArgs[0].EvalObject(scope, inst, cstack));

        private static object[] As_object_hintArrayType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ObjAsByHintArray(GetTypeArgFromObj(fnArgs[1].EvalObject(scope, inst, cstack)), fnArgs[0].EvalObject(scope, inst, cstack));

        private static object ObjAs(TypeArg typeArg, object obj)
        {
            if (typeArg.CType != null)
            {
                if (!(obj is CustomObject co)) return null;
                if (co.Type.IsArray != typeArg.CType.IsArray) return null;
                if (!co.Type.Class.Is(typeArg.CType.Class)) return null;

                return obj;
            }
            else
                return AsType(obj, typeArg.Type);

        }
        private static object[] ObjAsByHintArray(TypeArg typeArg, object obj)
        {
            return ObjIsByHintArray(typeArg, obj) ? (object[])obj : null;
        }
        public static object AsType(object value, Type type)
        {

            try
            {
                if (type.IsInstanceOfType(value))
                    return value;
                else
                    return value is IConvertible ? Convert.ChangeType(value, type) : null;

            }
            catch
            {
                return null;
            }
        }



        private static object Clone_object_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => CloneObj(GetTypeArgFromStr(fnArgs[1].EvalString(scope, inst, cstack)), fnArgs[0].EvalObject(scope, inst, cstack), scope, inst);
        private static object Clone_object_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => CloneObj(GetTypeArgFromObj(fnArgs[1].EvalObject(scope, inst, cstack)), fnArgs[0].EvalObject(scope, inst, cstack), scope, inst);
        private static CustomObject Clone_object_customType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => (CustomObject)CloneObj(GetTypeArgFromCustomType((CustomType)fnArgs[1].EvalObject(scope, inst, cstack)), fnArgs[0].EvalObject(scope, inst, cstack), scope, inst);

        private static object CloneObj(TypeArg typeArg, object obj, int scope, ClassInstance inst)
        {
            if (obj == null) return null;

            if (typeArg.CType != null)
            {
                if (!(obj is CustomObject co)) throw new ScriptExecutionException($"Unable to clone object. The object is not an {(typeArg.CType.IsArray ? "array of custom class instances" : "instance of custom class")}.");
                if (co.Type.IsArray != typeArg.CType.IsArray) throw new ScriptExecutionException($"Unable to clone object. The object must{(typeArg.CType.IsArray ? "" : " not")} be an array.");
                if (!co.Type.Class.Is(typeArg.CType.Class)) throw new ScriptExecutionException($"Unable to clone object. The object does not inherit from {typeArg.CType.Class.ClassName}.");

                ArgBlocks[] nest = new ArgBlocks[] { new ArgBlocks() };

                List<VarName> vars = GetVarNames(typeArg.CType.Class);
                Executor exec = inst.Exec;
                if (typeArg.CType.IsArray)
                {
                    CustomObject[] arr = (CustomObject[])co;

                    for (int i = 0; i < arr.Length; i++)
                    {
                        ClassInstance o = (ClassInstance)NewClassInstance(typeArg.CType.Class, nest, scope, inst, true);
                        int newScope = o.Scope;

                        ClassInstance ci = (ClassInstance)arr[i];

                        CopyVars(exec, vars, ci.Scope, newScope);
                        arr[i] = new CustomObject(inst.Exec, o, o.Class);
                    }
                    return new CustomObject(inst.Exec, arr, typeArg.CType.Class, true);
                }
                else
                {
                    ClassInstance o = (ClassInstance)NewClassInstance(typeArg.CType.Class, nest, scope, inst, true);
                    int newScope = o.Scope;

                    ClassInstance ci = (ClassInstance)co.Object;

                    CopyVars(exec, vars, ci.Scope, newScope);

                    return new CustomObject(inst.Exec, o, o.Class);
                }
            }
            else
            {
                if (obj is Array arr)
                {

                    Type itemType = typeArg.Type.GetElementType();
                    Array newArr = Array.CreateInstance(itemType, arr.Length);

                    int i = 0;

                    foreach (var item in arr)
                        newArr.SetValue(Clone(item, itemType), i++);

                    return newArr;
                }
                else
                    return Clone(obj, typeArg.Type);
            }

        }

        static object Clone(object obj, Type type = null)
        {
            try
            {
                Type objType = obj.GetType();
                bool convRequired = objType != type;

                if (type == null) type = obj.GetType();
                var newObj = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(type);

                var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var field in fields)
                {
                    object value;
                    if (convRequired)
                    {
                        var f = objType.GetField(field.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        value = f.GetValue(obj);
                        value = ConvertValue(value, type);
                    }
                    else
                        value = field.GetValue(obj);

                    field.SetValue(newObj, value);
                }
                return newObj;
            }
            catch (Exception ex)
            {
                throw new ScriptExecutionException("Unable to clone object. " + ex.Message);
            }

        }

        private static object Tuple_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => CreateTuple(fnArgs, scope, inst, cstack, "System.Tuple");
        private static object ValueTuple_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => CreateTuple(fnArgs, scope, inst, cstack, "System.ValueTuple");
        private static object CreateTuple(EvalUnit[] fnArgs, int scope, ClassInstance inst, CallStack cstack, string typeName)
        {
            object[] vals = EvalArgs(0, fnArgs, scope, inst, cstack);


            string argTypes = null;
            int last = fnArgs.Length - 1;
            for (int i = 0; i <= last; i++)
            {
                argTypes += fnArgs[i].Type.T.FullName;
                if (i < last) argTypes += ",";
            }

            string tupleType = $"{typeName}`{fnArgs.Length}[{argTypes}]";

            return Activator.CreateInstance(Type.GetType(tupleType, true), vals);
        }


        private static object SequentialEval_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            int last = fnArgs.Length - 1;
            for (int i = 0; i < last; i++)
                fnArgs[i].ProcessUnit(scope, inst, cstack);





            return fnArgs[last].EvalObject(scope, inst, cstack);
        }


        private static object FirstValAfterSequentialEval_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FirstValAfterSequentialEval<object>(fnArgs, scope, inst, cstack, csrc);
        private static int FirstValAfterSequentialEval_int_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FirstValAfterSequentialEval<int>(fnArgs, scope, inst, cstack, csrc);
        private static long FirstValAfterSequentialEval_long_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FirstValAfterSequentialEval<long>(fnArgs, scope, inst, cstack, csrc);
        private static float FirstValAfterSequentialEval_float_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FirstValAfterSequentialEval<float>(fnArgs, scope, inst, cstack, csrc);
        private static double FirstValAfterSequentialEval_double_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FirstValAfterSequentialEval<double>(fnArgs, scope, inst, cstack, csrc);
        private static decimal FirstValAfterSequentialEval_decimal_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FirstValAfterSequentialEval<decimal>(fnArgs, scope, inst, cstack, csrc);
        private static bool FirstValAfterSequentialEval_bool_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FirstValAfterSequentialEval<bool>(fnArgs, scope, inst, cstack, csrc);
        private static char FirstValAfterSequentialEval_char_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FirstValAfterSequentialEval<char>(fnArgs, scope, inst, cstack, csrc);
        private static string FirstValAfterSequentialEval_string_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FirstValAfterSequentialEval<string>(fnArgs, scope, inst, cstack, csrc);
        private static short FirstValAfterSequentialEval_short_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FirstValAfterSequentialEval<short>(fnArgs, scope, inst, cstack, csrc);
        private static byte FirstValAfterSequentialEval_byte_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FirstValAfterSequentialEval<byte>(fnArgs, scope, inst, cstack, csrc);
        private static DateTime FirstValAfterSequentialEval_date_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FirstValAfterSequentialEval<DateTime>(fnArgs, scope, inst, cstack, csrc);
        private static CustomObject FirstValAfterSequentialEval_custom_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FirstValAfterSequentialEval<CustomObject>(fnArgs, scope, inst, cstack, csrc);

        private static object[] FirstValAfterSequentialEval_objectArray_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FirstValAfterSequentialEval<object[]>(fnArgs, scope, inst, cstack, csrc);
        private static int[] FirstValAfterSequentialEval_intArray_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FirstValAfterSequentialEval<int[]>(fnArgs, scope, inst, cstack, csrc);
        private static long[] FirstValAfterSequentialEval_longArray_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FirstValAfterSequentialEval<long[]>(fnArgs, scope, inst, cstack, csrc);
        private static float[] FirstValAfterSequentialEval_floatArray_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FirstValAfterSequentialEval<float[]>(fnArgs, scope, inst, cstack, csrc);
        private static double[] FirstValAfterSequentialEval_doubleArray_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FirstValAfterSequentialEval<double[]>(fnArgs, scope, inst, cstack, csrc);
        private static decimal[] FirstValAfterSequentialEval_decimalArray_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FirstValAfterSequentialEval<decimal[]>(fnArgs, scope, inst, cstack, csrc);
        private static bool[] FirstValAfterSequentialEval_boolArray_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FirstValAfterSequentialEval<bool[]>(fnArgs, scope, inst, cstack, csrc);
        private static char[] FirstValAfterSequentialEval_charArray_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FirstValAfterSequentialEval<char[]>(fnArgs, scope, inst, cstack, csrc);
        private static string[] FirstValAfterSequentialEval_stringArray_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FirstValAfterSequentialEval<string[]>(fnArgs, scope, inst, cstack, csrc);
        private static short[] FirstValAfterSequentialEval_shortArray_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FirstValAfterSequentialEval<short[]>(fnArgs, scope, inst, cstack, csrc);
        private static byte[] FirstValAfterSequentialEval_byteArray_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FirstValAfterSequentialEval<byte[]>(fnArgs, scope, inst, cstack, csrc);
        private static DateTime[] FirstValAfterSequentialEval_dateArray_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FirstValAfterSequentialEval<DateTime[]>(fnArgs, scope, inst, cstack, csrc);



        private static T FirstValAfterSequentialEval<T>(EvalUnit[] fnArgs, int scope, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {

            for (int i = 1; i < fnArgs.Length; i++)
                fnArgs[i].ProcessUnit(scope, inst, cstack);

            return fnArgs[0].Eval<T>(scope, inst, cstack);
        }
        private static object FirstValSequentialEval_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FirstValSequentialEval<object>(fnArgs, scope, inst, cstack, csrc);
        private static int FirstValSequentialEval_int_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FirstValSequentialEval<int>(fnArgs, scope, inst, cstack, csrc);
        private static long FirstValSequentialEval_long_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FirstValSequentialEval<long>(fnArgs, scope, inst, cstack, csrc);
        private static float FirstValSequentialEval_float_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FirstValSequentialEval<float>(fnArgs, scope, inst, cstack, csrc);
        private static double FirstValSequentialEval_double_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FirstValSequentialEval<double>(fnArgs, scope, inst, cstack, csrc);
        private static decimal FirstValSequentialEval_decimal_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FirstValSequentialEval<decimal>(fnArgs, scope, inst, cstack, csrc);
        private static bool FirstValSequentialEval_bool_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FirstValSequentialEval<bool>(fnArgs, scope, inst, cstack, csrc);
        private static char FirstValSequentialEval_char_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FirstValSequentialEval<char>(fnArgs, scope, inst, cstack, csrc);
        private static string FirstValSequentialEval_string_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FirstValSequentialEval<string>(fnArgs, scope, inst, cstack, csrc);
        private static short FirstValSequentialEval_short_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FirstValSequentialEval<short>(fnArgs, scope, inst, cstack, csrc);
        private static byte FirstValSequentialEval_byte_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FirstValSequentialEval<byte>(fnArgs, scope, inst, cstack, csrc);
        private static DateTime FirstValSequentialEval_date_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FirstValSequentialEval<DateTime>(fnArgs, scope, inst, cstack, csrc);
        private static CustomObject FirstValSequentialEval_custom_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FirstValSequentialEval<CustomObject>(fnArgs, scope, inst, cstack, csrc);

        private static object[] FirstValSequentialEval_objectArray_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FirstValSequentialEval<object[]>(fnArgs, scope, inst, cstack, csrc);
        private static int[] FirstValSequentialEval_intArray_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FirstValSequentialEval<int[]>(fnArgs, scope, inst, cstack, csrc);
        private static long[] FirstValSequentialEval_longArray_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FirstValSequentialEval<long[]>(fnArgs, scope, inst, cstack, csrc);
        private static float[] FirstValSequentialEval_floatArray_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FirstValSequentialEval<float[]>(fnArgs, scope, inst, cstack, csrc);
        private static double[] FirstValSequentialEval_doubleArray_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FirstValSequentialEval<double[]>(fnArgs, scope, inst, cstack, csrc);
        private static decimal[] FirstValSequentialEval_decimalArray_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FirstValSequentialEval<decimal[]>(fnArgs, scope, inst, cstack, csrc);
        private static bool[] FirstValSequentialEval_boolArray_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FirstValSequentialEval<bool[]>(fnArgs, scope, inst, cstack, csrc);
        private static char[] FirstValSequentialEval_charArray_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FirstValSequentialEval<char[]>(fnArgs, scope, inst, cstack, csrc);
        private static string[] FirstValSequentialEval_stringArray_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FirstValSequentialEval<string[]>(fnArgs, scope, inst, cstack, csrc);
        private static short[] FirstValSequentialEval_shortArray_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FirstValSequentialEval<short[]>(fnArgs, scope, inst, cstack, csrc);
        private static byte[] FirstValSequentialEval_byteArray_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FirstValSequentialEval<byte[]>(fnArgs, scope, inst, cstack, csrc);
        private static DateTime[] FirstValSequentialEval_dateArray_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => FirstValSequentialEval<DateTime[]>(fnArgs, scope, inst, cstack, csrc);


        private static T FirstValSequentialEval<T>(EvalUnit[] fnArgs, int scope, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            T v = fnArgs[0].Eval<T>(scope, inst, cstack);
            for (int i = 1; i < fnArgs.Length; i++)
                fnArgs[i].ProcessUnit(scope, inst, cstack);

            return v;
        }



        private static object Expr_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => new Expr(CopyEU(fnArgs[0]));
        private static object Expr_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {

            var varIds = GetArgVarIds(fnArgs, 1, "to involve");


            var eu = CopyEU(fnArgs[0]);

            InvolveVars(ref eu, varIds, scope, inst);
            CheckInvBoxResult(varIds, fnArgs);
            return new Expr(eu);
        }
        private static object Involve_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {

            var varIds = GetArgVarIds(fnArgs, 1, "to involve");

            var e = (Expr)fnArgs[0].EvalObject(scope, inst, cstack);


            InvolveVars(ref e.EU, varIds, scope, inst);
            CheckInvBoxResult(varIds, fnArgs);
            return e;
        }
        private static VarIdWithType[] GetArgVarIds(EvalUnit[] fnArgs, int skip, string kind)
        {
            VarIdWithType[] varIds = fnArgs.Skip(skip).Select(x => new VarIdWithType(x.VarID, x.Type.ID)).ToArray();
            if (varIds.Any(x => x.ID < 0)) throw new ScriptExecutionException($"One or several variables {kind} are invalid (" + string.Join(", ", fnArgs.Skip(skip).Where(x => x.VarID < 0).Select(x => $"'{x.Code}'")) + ").");
            return varIds;
        }
        private static object FixOrigExpr_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            var e = (Expr)fnArgs[0].EvalObject(scope, inst, cstack);
            e.OrigEU = CopyEU(e.EU);
            return e;
        }
        private static object OrigExpr_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            var e = (Expr)fnArgs[0].EvalObject(scope, inst, cstack);
            if (e.OrigEU == null) throw new ScriptExecutionException("Original expression was not captured.");
            return new Expr(CopyEU(e.OrigEU), e.OrigEU);
        }
        private static object RenewExpr_object_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RenewExpr((Expr)fnArgs[0].EvalObject(scope, inst, cstack), (Expr)fnArgs[1].EvalObject(scope, inst, cstack));
        private static object RenewExpr_object_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => RenewExpr((Expr)fnArgs[0].EvalObject(scope, inst, cstack), (Expr)fnArgs[1].EvalObject(scope, inst, cstack), fnArgs[2].EvalBool(scope, inst, cstack));

        private static Expr RenewExpr(Expr e, Expr e2, bool hot = false)
        {
            if (hot) e2.EU.ShallowCopyTo(e.EU);
            else e.EU = e2.EU;
            return e;
        }
        private static object CopyExpr_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            var e = (Expr)fnArgs[0].EvalObject(scope, inst, cstack);
            return new Expr(CopyEU(e.EU), e.OrigEU);
        }
        private static object OrigExpr_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            var e = (Expr)fnArgs[0].EvalObject(scope, inst, cstack);
            if (e.OrigEU == null) throw new ScriptExecutionException("Original expression was not captured.");
            var varIds = GetArgVarIds(fnArgs, 1, "to involve");
            e = new Expr(CopyEU(e.OrigEU), e.OrigEU);

            InvolveVars(ref e.EU, varIds, scope, inst);
            CheckInvBoxResult(varIds, fnArgs);
            return e;
        }
        private static void CheckInvBoxResult(VarIdWithType[] varIds, EvalUnit[] fnArgs, bool box = false, int skip = 1)
        {
            int c = 0;
            for (int i = 0; i < varIds.Length; i++)
                if (varIds[i].Result) c++;

            if (c < varIds.Length)
            {
                var errIds = varIds.Where(x => !x.Result).ToArray();
                string errArgs = string.Join(", ", fnArgs.Skip(skip).Where(x => errIds.Contains(new VarIdWithType(x.VarID, x.Type.ID))).Select(x => $"'{x.Code}'"));


                throw new ScriptExecutionException($"Not all of the specified variables were {(box ? "boxed" : "involved")} ({c}/{varIds.Length}). Failed: {errArgs}.");
            }

        }


        private static EvalUnit CopyEU(EvalUnit source)
        {
            EvalUnit eu = source.ShallowCopy();
            ResetProcessUnit(eu);


            if (eu.Path_Unit != null) eu.Path_Unit = CopyEU(eu.Path_Unit);
            if (eu.Op1_Unit != null) eu.Op1_Unit = CopyEU(eu.Op1_Unit);
            if (eu.Op2_Unit != null) eu.Op2_Unit = CopyEU(eu.Op2_Unit);
            if (eu.Nested != null)
            {
                eu.Nested = (ArgBlocks[])eu.Nested.Clone();

                for (int b = 0; b < eu.Nested.Length; b++)
                {

                    EvalUnit[] args = eu.Nested[b].Args;
                    if (args != null)
                    {
                        eu.Nested[b].Args = args = (EvalUnit[])args.Clone();
                        for (int i = 0; i < args.Length; i++)
                            args[i] = CopyEU(args[i]);
                    }
                }
            }

            return eu;
        }

        private static void InvolveVars(ref EvalUnit eu, VarIdWithType[] varIds, int scope, ClassInstance inst)
        {
            bool isArrayItem = eu.Kind == EvalUnitKind.ArrayItem;

            if ((isArrayItem || eu.Kind == EvalUnitKind.Variable || eu.Kind == EvalUnitKind.SpecificValue) && eu.ScopeKind != VarScopeKind.Inst)
            {
                TypeID tid = !isArrayItem ? eu.Type.ID : GetArrayTypeId(eu.Type.ID);
                var n = VarIdWithType.Find(varIds, eu.VarID, tid);

                if (n >= 0)
                {
                    if (eu.ScopeKind == VarScopeKind.Ref) throw new ScriptLoadingException($"Ref-variable '{eu}' cannot be involved.");
                    else if (eu.ScopeKind == VarScopeKind.Static) throw new ScriptLoadingException($"Static variable '{eu}' do not need to be involved.");

                    if (eu.ScopeKind == VarScopeKind.Involved) eu.ScopeKind = eu.InvolveData.RealScopeKind;
                    var vi = eu.GetVarInfo(scope, inst, null);

                    eu.Kind = isArrayItem ? EvalUnitKind.ArrayItem : EvalUnitKind.Variable;



                    eu.InvolveData = new InvolveInfo(vi, eu.ScopeKind);
                    eu.ScopeKind = VarScopeKind.Involved;
                    eu.Op1_Unit = null;

                    varIds[n].Result = true;

                }
            }

            if (eu.Path_Unit != null) InvolveVars(ref eu.Path_Unit, varIds, scope, inst);
            if (eu.Op1_Unit != null) InvolveVars(ref eu.Op1_Unit, varIds, scope, inst);
            if (eu.Op2_Unit != null) InvolveVars(ref eu.Op2_Unit, varIds, scope, inst);
            if (eu.Nested != null)
            {


                foreach (var argBlock in eu.Nested)
                    if (argBlock.Args != null)
                        for (int i = 0; i < argBlock.Args.Length; i++)
                            InvolveVars(ref argBlock.Args[i], varIds, scope, inst);
            }


        }
        private static object SetVarScopeKind_object_string_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {


            var varIds = GetArgVarIds(fnArgs, 2, "to change");
            var e = (Expr)fnArgs[0].EvalObject(scope, inst, cstack);
            string kindStr = fnArgs[1].EvalString(scope, inst, cstack);

            if (Enum.Parse(typeof(VarScopeKind), kindStr) is VarScopeKind kind)
                SetVarScopeKind(ref e.EU, varIds, kind);
            else throw new ScriptExecutionException($"Invalid scope kind name '{kindStr}'.");
            return e;
        }
        private static void SetVarScopeKind(ref EvalUnit eu, VarIdWithType[] varIds, VarScopeKind kind)
        {

            bool isArrayItem = eu.Kind == EvalUnitKind.ArrayItem;
            if ((isArrayItem || eu.Kind == EvalUnitKind.Variable) && eu.ScopeKind != VarScopeKind.Inst)
            {
                TypeID tid = !isArrayItem ? eu.Type.ID : GetArrayTypeId(eu.Type.ID);
                var n = Array.IndexOf(varIds, new VarIdWithType(eu.VarID, tid));
                if (n >= 0)
                {
                    eu.ScopeKind = kind;
                    if (kind == VarScopeKind.Static) eu.ClassLink = null;
                }

            }


            if (eu.Path_Unit != null) SetVarScopeKind(ref eu.Path_Unit, varIds, kind);
            if (eu.Op1_Unit != null) SetVarScopeKind(ref eu.Op1_Unit, varIds, kind);
            if (eu.Op2_Unit != null) SetVarScopeKind(ref eu.Op2_Unit, varIds, kind);
            if (eu.Nested != null)
            {
                foreach (var argBlock in eu.Nested)
                    if (argBlock.Args != null)
                        for (int i = 0; i < argBlock.Args.Length; i++)
                            SetVarScopeKind(ref argBlock.Args[i], varIds, kind);
            }


        }
        private static object BoxValues_object_objectParams(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {


            var varIds = GetArgVarIds(fnArgs, 1, "to box");
            object[] varVals = EvalArgs(1, fnArgs, scope, inst, cstack);
            Expr e = (Expr)fnArgs[0].EvalObject(scope, inst, cstack);

            BoxValues(ref e.EU, varIds, varVals);
            CheckInvBoxResult(varIds, fnArgs, true);
            return e;
        }
        private static object BoxAll_object_bool(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            Expr e = (Expr)fnArgs[0].EvalObject(scope, inst, cstack);
            if (e == null) throw new ArgumentException("Expression is null.");
            bool onlyLocal = fnArgs.Length > 1 ? fnArgs[1].EvalBool(scope, inst, cstack) : false;
            BoxAll(ref e.EU, scope, inst, cstack, onlyLocal);

            return e;
        }
        private static void BoxValues(ref EvalUnit eu, VarIdWithType[] varIds, object[] varVals, bool assign = false)
        {
            bool isArrayItem = eu.Kind == EvalUnitKind.ArrayItem;
            if ((isArrayItem || eu.Kind == EvalUnitKind.Variable || eu.Kind == EvalUnitKind.SpecificValue) && eu.ScopeKind != VarScopeKind.Inst)
            {
                TypeID tid = !isArrayItem ? eu.Type.ID : GetArrayTypeId(eu.Type.ID);

                var n = VarIdWithType.Find(varIds, eu.VarID, tid);

                if (n >= 0)
                {
                    if (assign) throw new ScriptLoadingException($"Left operand variable '{eu}' in an assignment expression cannot be boxed.");

                    if (!isArrayItem)
                    {
                        eu.Kind = EvalUnitKind.SpecificValue;
                        eu.SpecificValue = varVals[n];
                    }
                    else
                    {
                        eu.Op1_Unit = EvalUnit.GetEUWithSpecificValue(varVals[n]);


                    }

                    varIds[n].Result = true;

                }

            }


            if (eu.Path_Unit != null) BoxValues(ref eu.Path_Unit, varIds, varVals);
            if (eu.Op1_Unit != null) BoxValues(ref eu.Op1_Unit, varIds, varVals, eu.IsAssignment ? true : false);
            if (eu.Op2_Unit != null) BoxValues(ref eu.Op2_Unit, varIds, varVals);
            if (eu.Nested != null)
                foreach (var argBlock in eu.Nested)
                    if (argBlock.Args != null)
                        for (int i = 0; i < argBlock.Args.Length; i++)
                            BoxValues(ref argBlock.Args[i], varIds, varVals);



        }
        struct VarIdWithType
        {
            public int ID;
            public TypeID Type;
            public bool Result;

            public VarIdWithType(int id, TypeID typeId)
            {
                ID = id;
                Type = typeId;
                Result = false;

            }
            public static int Find(VarIdWithType[] arr, int id, TypeID typeId)
            {
                for (int i = 0; i < arr.Length; i++)
                    if (arr[i].ID == id && arr[i].Type == typeId) return i;

                return -1;
            }
        }


        private static void BoxAll(ref EvalUnit eu, int scope, ClassInstance inst, CallStack cstack, bool onlyLocal, bool assign = false)
        {
            bool isArrayItem = eu.Kind == EvalUnitKind.ArrayItem;
            if ((isArrayItem || eu.Kind == EvalUnitKind.Variable) && (!onlyLocal || eu.ScopeKind == VarScopeKind.Local || eu.ScopeKind == VarScopeKind.Ref))
            {
                if (assign) throw new ScriptLoadingException($"Left operand variable '{eu}' in an assignment expression cannot be boxed.");
                object v = eu.Ev<object>(scope, inst, cstack);

                if (!isArrayItem)
                {
                    eu.Kind = EvalUnitKind.SpecificValue;
                    eu.SpecificValue = v;
                }
                else
                {
                    eu.Op1_Unit = EvalUnit.GetEUWithSpecificValue(v);
                }

            }
            if (eu.Path_Unit != null) BoxAll(ref eu.Path_Unit, scope, inst, cstack, onlyLocal);
            if (eu.Op1_Unit != null) BoxAll(ref eu.Op1_Unit, scope, inst, cstack, onlyLocal, eu.IsAssignment ? true : false);
            if (eu.Op2_Unit != null) BoxAll(ref eu.Op2_Unit, scope, inst, cstack, onlyLocal);
            if (eu.Nested != null)
                foreach (var argBlock in eu.Nested)
                    if (argBlock.Args != null)
                        for (int i = 0; i < argBlock.Args.Length; i++)
                            BoxAll(ref argBlock.Args[i], scope, inst, cstack, onlyLocal);


        }


        private static object Eval_object_objectType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ((Expr)fnArgs[0].EvalObject(scope, inst, cstack)).EU.EvalObject(scope, inst, cstack);
        private static int Eval_object_intType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ((Expr)fnArgs[0].EvalObject(scope, inst, cstack)).EU.EvalInt(scope, inst, cstack);
        private static long Eval_object_longType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ((Expr)fnArgs[0].EvalObject(scope, inst, cstack)).EU.EvalLong(scope, inst, cstack);
        private static float Eval_object_floatType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ((Expr)fnArgs[0].EvalObject(scope, inst, cstack)).EU.EvalFloat(scope, inst, cstack);
        private static double Eval_object_doubleType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ((Expr)fnArgs[0].EvalObject(scope, inst, cstack)).EU.EvalDouble(scope, inst, cstack);
        private static decimal Eval_object_decimalType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ((Expr)fnArgs[0].EvalObject(scope, inst, cstack)).EU.EvalDecimal(scope, inst, cstack);
        private static bool Eval_object_boolType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ((Expr)fnArgs[0].EvalObject(scope, inst, cstack)).EU.EvalBool(scope, inst, cstack);
        private static char Eval_object_charType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ((Expr)fnArgs[0].EvalObject(scope, inst, cstack)).EU.EvalChar(scope, inst, cstack);
        private static string Eval_object_stringType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ((Expr)fnArgs[0].EvalObject(scope, inst, cstack)).EU.EvalString(scope, inst, cstack);
        private static short Eval_object_shortType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ((Expr)fnArgs[0].EvalObject(scope, inst, cstack)).EU.EvalShort(scope, inst, cstack);
        private static byte Eval_object_byteType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ((Expr)fnArgs[0].EvalObject(scope, inst, cstack)).EU.EvalByte(scope, inst, cstack);
        private static DateTime Eval_object_dateType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ((Expr)fnArgs[0].EvalObject(scope, inst, cstack)).EU.EvalDate(scope, inst, cstack);
        private static CustomObject Eval_object_customType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ((Expr)fnArgs[0].EvalObject(scope, inst, cstack)).EU.EvalCustom(scope, inst, cstack);

        private static object[] Eval_object_objectArrayType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ((Expr)fnArgs[0].EvalObject(scope, inst, cstack)).EU.EvalObjectArray(scope, inst, cstack);
        private static int[] Eval_object_intArrayType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ((Expr)fnArgs[0].EvalObject(scope, inst, cstack)).EU.EvalIntArray(scope, inst, cstack);
        private static long[] Eval_object_longArrayType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ((Expr)fnArgs[0].EvalObject(scope, inst, cstack)).EU.EvalLongArray(scope, inst, cstack);
        private static float[] Eval_object_floatArrayType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ((Expr)fnArgs[0].EvalObject(scope, inst, cstack)).EU.EvalFloatArray(scope, inst, cstack);
        private static double[] Eval_object_doubleArrayType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ((Expr)fnArgs[0].EvalObject(scope, inst, cstack)).EU.EvalDoubleArray(scope, inst, cstack);
        private static decimal[] Eval_object_decimalArrayType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ((Expr)fnArgs[0].EvalObject(scope, inst, cstack)).EU.EvalDecimalArray(scope, inst, cstack);
        private static bool[] Eval_object_boolArrayType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ((Expr)fnArgs[0].EvalObject(scope, inst, cstack)).EU.EvalBoolArray(scope, inst, cstack);
        private static char[] Eval_object_charArrayType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ((Expr)fnArgs[0].EvalObject(scope, inst, cstack)).EU.EvalCharArray(scope, inst, cstack);
        private static string[] Eval_object_stringArrayType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ((Expr)fnArgs[0].EvalObject(scope, inst, cstack)).EU.EvalStringArray(scope, inst, cstack);
        private static short[] Eval_object_shortArrayType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ((Expr)fnArgs[0].EvalObject(scope, inst, cstack)).EU.EvalShortArray(scope, inst, cstack);
        private static byte[] Eval_object_byteArrayType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ((Expr)fnArgs[0].EvalObject(scope, inst, cstack)).EU.EvalByteArray(scope, inst, cstack);
        private static DateTime[] Eval_object_dateArrayType(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) => ((Expr)fnArgs[0].EvalObject(scope, inst, cstack)).EU.EvalDateArray(scope, inst, cstack);

        private static object Eval_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc) { ((Expr)fnArgs[0].EvalObject(scope, inst, cstack)).EU.ProcessUnit(scope, inst, cstack); return null; }

        private static object Throw_object_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            Exception ex;
            try
            {
                ex = GetThrowEx(fnArgs, scope, inst, cstack, inst.Exec);
            }
            catch
            {
                throw new ArgumentException("Failed to throw an exception due to invalid arguments.");
            }
            throw ex;

        }
        private static object Throw(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            throw cstack.GetThrownException();

        }

        private static string IniGet_string_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            string str = fnArgs[0].EvalString(scope, inst, cstack);
            string name = fnArgs[1].EvalString(scope, inst, cstack);
            var pos = IniPos(str, name);
            return IniGet(str, pos.startPos, pos.stopPos);
        }
        private static string IniGet_string_string_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            string str = fnArgs[0].EvalString(scope, inst, cstack);
            string name = fnArgs[1].EvalString(scope, inst, cstack);
            string section = fnArgs[2].EvalString(scope, inst, cstack);

            var pos = IniPos(str, name, section);
            return IniGet(str, pos.startPos, pos.stopPos);
        }
        private static string IniSet_string_string_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            string str = fnArgs[0].EvalString(scope, inst, cstack);
            string name = fnArgs[1].EvalString(scope, inst, cstack);
            object value = fnArgs[2].EvalObject(scope, inst, cstack);

            return IniSet(str, name, null, value);
        }

        private static string IniSet_string_string_string_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            string str = fnArgs[0].EvalString(scope, inst, cstack);
            string name = fnArgs[1].EvalString(scope, inst, cstack);
            string section = fnArgs[2].EvalString(scope, inst, cstack);
            object value = fnArgs[3].EvalObject(scope, inst, cstack);
            return IniSet(str, name, section, value);
        }
        private static string IniSet(string str, string name, string section, object value)
        {
            string v = ToIniVal(value);

            var pos = IniPos(str, name, section);
            if (pos.startPos >= 0)
                return str.Remove(pos.startPos, pos.stopPos - pos.startPos).Insert(pos.startPos, v);
            int start = str.Length;
            bool addSection = false;
            if (section != null)
            {
                var sc = GetIniSection(str, section);
                if (sc.exists)
                {
                    int p = start = sc.sectionEnd;

                    char c = str[p - 1];
                    while (c == '\n' || c == '\r')
                    {
                        start = p;
                        c = str[--p];
                    }

                }
                else
                    addSection = true;
            }

            if (start > 0 && str[start - 1] != '\n') { str = str.Insert(start, Environment.NewLine); start += Environment.NewLine.Length; }
            if (addSection)
            {
                string s = $"{Environment.NewLine}[{section}]{Environment.NewLine}";

                str = str.Insert(start, s); start += s.Length;
            }

            string newLine = $"{name}={v}";
            str = str.Insert(start, newLine);
            start += newLine.Length;
            if (start + Environment.NewLine.Length > str.Length || str.Substring(start, Environment.NewLine.Length) != Environment.NewLine)
                str = str.Insert(start, Environment.NewLine);

            return str;
        }

        static string ToIniVal(object value)
        {
            if (value == null) return "";
            string v;
            if (value is string s)
            {
                string se = HttpUtility.JavaScriptStringEncode(s);
                v = se != s ? "\"" + se + "\"" : s;
            }
            else v = value.ToString();
            return v;
        }
        private static string IniGet(string str, int startPos, int stopPos)
        {
            if (startPos >= 0)
            {
                string v = str.Substring(startPos, stopPos - startPos).Trim(IniTrimChars);

                if (v.StartsWith("\"") && v.EndsWith("\"") && v.Length > 1)
                {
                    v = v.Substring(1, v.Length - 2);
                    v = Regex.Unescape(v);
                }
                return v;
            }
            return null;
        }

        static char[] IniTrimChars = { ' ', '\t' };
        static char[] IniCommentChars = new char[] { '#', ';' };

        static bool AtLineBeginning(string str, int i)
        {
            if (i == 0) return true;
            else
            {
                int i2 = str.LastIndexOf('\n', i) + 1;
                return str.Substring(i2, i - i2).Trim(IniTrimChars).Length == 0;
            }
        }
        static (bool exists, int sectionStart, int sectionEnd) GetIniSection(string str, string section)
        {
            int i, i2;
            int sectionStart = 0, sectionEnd = str.Length;

            if (section != null)
            {
                string tag = $"[{section}]";
                i = str.IndexOf(tag);
                while (i >= 0 && !AtLineBeginning(str, i))
                    i = str.IndexOf(tag, i + 1);

                if (i < 0) return (false, -1, -1);
                else
                {

                    sectionStart = i + tag.Length;

                    i2 = str.IndexOf('[', i + 1);
                    while (i2 >= 0 && !AtLineBeginning(str, i2))
                        i2 = str.IndexOf('[', i2 + 1);

                    if (i2 >= 0) sectionEnd = i2;
                }
            }
            return (true, sectionStart, sectionEnd);
        }
        static (int startPos, int stopPos) IniPos(string str, string name, string section = null)
        {
            int i, i2, i3;
            int sectionStart = 0, sectionEnd = str.Length;
            if (section != null)
            {
                var sc = GetIniSection(str, section);
                if (!sc.exists) return (-1, -1);
                sectionStart = sc.sectionStart;
                sectionEnd = sc.sectionEnd;
            }

            int sl = str.Length;
            int nl = name.Length;
            i3 = -1;
            i = str.IndexOf(name, sectionStart);
            while (i >= 0)
            {
                if (i >= sectionStart)
                {
                    if (i >= sectionEnd) return (-1, -1);

                    i2 = i + nl;
                    if (AtLineBeginning(str, i) && i2 < sl)
                    {

                        if (str[i2] != '=')
                        {
                            i3 = str.IndexOf('=', i2);
                            if (i3 < 0) return (-1, -1);
                            i2 = str.Substring(i2, i3 - i2).Trim(IniTrimChars).Length == 0 ? i2 = i3 : -1;
                        }
                        if (i2 >= 0)
                        {
                            i3 = str.IndexOf('\n', i2);
                            if (i3 < 0) i3 = sl;
                            if (str[i3 - 1] == '\r') i3--;

                            i2++;
                            if (IniTrimChars.Contains(str[i2])) i2++;

                            int i4 = str.IndexOf('\"', i2, i3 - i2);

                            bool isStr = i4 >= 0;
                            if (isStr && str.Substring(i2, i4 - i2).Trim(IniTrimChars).Length > 0) isStr = false;
                            int commentCharPos = -1;

                            if (isStr)
                            {
                                int i5 = FindEndOfLiteral(str, i4, true);

                                if (i5 >= 0 && i5 < i3) i3 = i5 + 1;
                            }
                            else if ((commentCharPos = str.IndexOfAny(IniCommentChars, i2, i3 - i2)) >= 0)
                            {
                                char c = str[commentCharPos - 1];


                                i3 = commentCharPos;
                                while (i3 > i2 && IniTrimChars.Contains(str[i3 - 1]))
                                    i3--;

                            }

                            return (i2, i3);

                        }

                    }

                }
                i = str.IndexOf(name, i + 1);
            }
            return (-1, -1);

        }

        private static object GetGlobal_string(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            string key = fnArgs[0].EvalString(scope, inst, cstack);
            return inst.Exec.GetGlobal(key);
        }
        private static object SetGlobal_string_object(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            string key = fnArgs[0].EvalString(scope, inst, cstack);
            object val = fnArgs[1].EvalObject(scope, inst, cstack);
            inst.Exec.SetGlobal(key, val);
            return null;
        }
        private static object Executor(EvalUnit[] fnArgs, int scope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            return inst.Exec;
        }

        struct TypeArg
        {
            public Type Type;
            public CustomType CType;
            public TypeID Id;
            public TypeArg(Type type, CustomType cType, TypeID id)
            {
                Type = type;
                CType = cType;
                Id = id;
            }

        }
    }


}
