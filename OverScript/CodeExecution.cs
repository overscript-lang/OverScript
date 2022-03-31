using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using static OverScript.Executor;

namespace OverScript
{
    public partial class ScriptClass
    {

        public static void ResetProcessUnit(EvalUnit eu)
        {
            switch (eu.Type.ID)
            {
                case TypeID.Int: SetProcessUnit<int>(eu); return;
                case TypeID.String: SetProcessUnit<string>(eu); return;
                case TypeID.Char: SetProcessUnit<char>(eu); return;
                case TypeID.Long: SetProcessUnit<long>(eu); return;
                case TypeID.Float: SetProcessUnit<float>(eu); return;
                case TypeID.Double: SetProcessUnit<double>(eu); return;
                case TypeID.Decimal: SetProcessUnit<decimal>(eu); return;
                case TypeID.Bool: SetProcessUnit<bool>(eu); return;
                case TypeID.Void:
                case TypeID.Object: SetProcessUnit<object>(eu); return;
                case TypeID.Short: SetProcessUnit<short>(eu); return;
                case TypeID.Byte: SetProcessUnit<byte>(eu); return;
                case TypeID.Date: SetProcessUnit<DateTime>(eu); return;

                case TypeID.ObjectArray: SetProcessUnit<object[]>(eu); return;
                case TypeID.IntArray: SetProcessUnit<int[]>(eu); return;
                case TypeID.LongArray: SetProcessUnit<long[]>(eu); return;
                case TypeID.StringArray: SetProcessUnit<string[]>(eu); return;
                case TypeID.CharArray: SetProcessUnit<char[]>(eu); return;
                case TypeID.DoubleArray: SetProcessUnit<double[]>(eu); return;
                case TypeID.FloatArray: SetProcessUnit<float[]>(eu); return;
                case TypeID.BoolArray: SetProcessUnit<bool[]>(eu); return;
                case TypeID.DecimalArray: SetProcessUnit<decimal[]>(eu); return;
                case TypeID.ShortArray: SetProcessUnit<short[]>(eu); return;
                case TypeID.ByteArray: SetProcessUnit<byte[]>(eu); return;
                case TypeID.DateArray: SetProcessUnit<DateTime[]>(eu); return;
                case TypeID.CustomArray:
                case TypeID.Custom: SetProcessUnit<CustomObject>(eu); return;
                default: throw new Exception($"Can't to process type '{eu.Type}'.");
            }
        }
        static void SetProcessUnit<T>(EvalUnit eu)
        {
            eu.ProcessUnit = (int scope, ClassInstance inst, CallStack cstack) => eu.Eval<T>(scope, inst, cstack);
        }
        private static void SetParamVar(ScriptFunction.FuncParam fnParam, EvalUnit fnArg, int scope, int baseScope, ClassInstance srcInst, ClassInstance inst, CallStack cstack)
        {
            if (srcInst == null) srcInst = inst;

            switch (fnParam.ParamType.ID)
            {
                case TypeID.Int: { var v = fnArg.EvalInt(baseScope, srcInst, cstack); ScriptVars<int>.AddSet(inst.Exec, fnParam.VarId, scope, ref v); break; }
                case TypeID.String: { var v = fnArg.EvalString(baseScope, srcInst, cstack); ScriptVars<string>.AddSet(inst.Exec, fnParam.VarId, scope, ref v); break; }
                case TypeID.Char: { var v = fnArg.EvalChar(baseScope, srcInst, cstack); ScriptVars<char>.AddSet(inst.Exec, fnParam.VarId, scope, ref v); break; }
                case TypeID.Long: { var v = fnArg.EvalLong(baseScope, srcInst, cstack); ScriptVars<long>.AddSet(inst.Exec, fnParam.VarId, scope, ref v); break; }
                case TypeID.Double: { var v = fnArg.EvalDouble(baseScope, srcInst, cstack); ScriptVars<double>.AddSet(inst.Exec, fnParam.VarId, scope, ref v); break; }
                case TypeID.Float: { var v = fnArg.EvalFloat(baseScope, srcInst, cstack); ScriptVars<float>.AddSet(inst.Exec, fnParam.VarId, scope, ref v); break; }
                case TypeID.Bool: { var v = fnArg.EvalBool(baseScope, srcInst, cstack); ScriptVars<bool>.AddSet(inst.Exec, fnParam.VarId, scope, ref v); break; }
                case TypeID.Decimal: { var v = fnArg.EvalDecimal(baseScope, srcInst, cstack); ScriptVars<decimal>.AddSet(inst.Exec, fnParam.VarId, scope, ref v); break; }
                case TypeID.Object: { var v = fnArg.EvalObject(baseScope, srcInst, cstack); ScriptVars<object>.AddSet(inst.Exec, fnParam.VarId, scope, ref v); break; }
                case TypeID.Short: { var v = fnArg.EvalShort(baseScope, srcInst, cstack); ScriptVars<short>.AddSet(inst.Exec, fnParam.VarId, scope, ref v); break; }
                case TypeID.Byte: { var v = fnArg.EvalByte(baseScope, srcInst, cstack); ScriptVars<byte>.AddSet(inst.Exec, fnParam.VarId, scope, ref v); break; }
                case TypeID.Date: { var v = fnArg.EvalDate(baseScope, srcInst, cstack); ScriptVars<DateTime>.AddSet(inst.Exec, fnParam.VarId, scope, ref v); break; }

                case TypeID.IntArray: { var v = fnArg.EvalIntArray(baseScope, srcInst, cstack); ScriptVars<int[]>.AddSet(inst.Exec, fnParam.VarId, scope, ref v); break; }
                case TypeID.StringArray: { var v = fnArg.EvalStringArray(baseScope, srcInst, cstack); ScriptVars<string[]>.AddSet(inst.Exec, fnParam.VarId, scope, ref v); break; }
                case TypeID.CharArray: { var v = fnArg.EvalCharArray(baseScope, srcInst, cstack); ScriptVars<char[]>.AddSet(inst.Exec, fnParam.VarId, scope, ref v); break; }
                case TypeID.LongArray: { var v = fnArg.EvalLongArray(baseScope, srcInst, cstack); ScriptVars<long[]>.AddSet(inst.Exec, fnParam.VarId, scope, ref v); break; }
                case TypeID.DoubleArray: { var v = fnArg.EvalDoubleArray(baseScope, srcInst, cstack); ScriptVars<double[]>.AddSet(inst.Exec, fnParam.VarId, scope, ref v); break; }
                case TypeID.FloatArray: { var v = fnArg.EvalFloatArray(baseScope, srcInst, cstack); ScriptVars<float[]>.AddSet(inst.Exec, fnParam.VarId, scope, ref v); break; }
                case TypeID.BoolArray: { var v = fnArg.EvalBoolArray(baseScope, srcInst, cstack); ScriptVars<bool[]>.AddSet(inst.Exec, fnParam.VarId, scope, ref v); break; }
                case TypeID.DecimalArray: { var v = fnArg.EvalDecimalArray(baseScope, srcInst, cstack); ScriptVars<decimal[]>.AddSet(inst.Exec, fnParam.VarId, scope, ref v); break; }
                case TypeID.ObjectArray: { var v = fnArg.EvalObjectArray(baseScope, srcInst, cstack); ScriptVars<object[]>.AddSet(inst.Exec, fnParam.VarId, scope, ref v); break; }
                case TypeID.ShortArray: { var v = fnArg.EvalShortArray(baseScope, srcInst, cstack); ScriptVars<short[]>.AddSet(inst.Exec, fnParam.VarId, scope, ref v); break; }
                case TypeID.ByteArray: { var v = fnArg.EvalByteArray(baseScope, srcInst, cstack); ScriptVars<byte[]>.AddSet(inst.Exec, fnParam.VarId, scope, ref v); break; }
                case TypeID.DateArray: { var v = fnArg.EvalDateArray(baseScope, srcInst, cstack); ScriptVars<DateTime[]>.AddSet(inst.Exec, fnParam.VarId, scope, ref v); break; }

                case TypeID.CustomArray:
                case TypeID.Custom:
                    { var v = fnArg.EvalCustom(baseScope, srcInst, cstack); ScriptVars<CustomObject>.AddSet(inst.Exec, fnParam.VarId, scope, ref v); break; }
            }

        }

        private static void SetParamsArray(ScriptFunction.FuncParam fnParam, EvalUnit[] fnArgs, int skip, int scope, int baseScope, ClassInstance srcInst, CallStack cstack)
        {

            switch (fnParam.ParamType.ID)
            {

                case TypeID.IntArray: { var v = GetParamsVals<int>(fnArgs, skip, baseScope, srcInst, cstack); ScriptVars<int[]>.AddSet(srcInst.Exec, fnParam.VarId, scope, ref v); break; }
                case TypeID.StringArray: { var v = GetParamsVals<string>(fnArgs, skip, baseScope, srcInst, cstack); ScriptVars<string[]>.AddSet(srcInst.Exec, fnParam.VarId, scope, ref v); break; }
                case TypeID.CharArray: { var v = GetParamsVals<char>(fnArgs, skip, baseScope, srcInst, cstack); ScriptVars<char[]>.AddSet(srcInst.Exec, fnParam.VarId, scope, ref v); break; }
                case TypeID.LongArray: { var v = GetParamsVals<long>(fnArgs, skip, baseScope, srcInst, cstack); ScriptVars<long[]>.AddSet(srcInst.Exec, fnParam.VarId, scope, ref v); break; }
                case TypeID.DoubleArray: { var v = GetParamsVals<double>(fnArgs, skip, baseScope, srcInst, cstack); ScriptVars<double[]>.AddSet(srcInst.Exec, fnParam.VarId, scope, ref v); break; }
                case TypeID.FloatArray: { var v = GetParamsVals<float>(fnArgs, skip, baseScope, srcInst, cstack); ScriptVars<float[]>.AddSet(srcInst.Exec, fnParam.VarId, scope, ref v); break; }
                case TypeID.BoolArray: { var v = GetParamsVals<bool>(fnArgs, skip, baseScope, srcInst, cstack); ScriptVars<bool[]>.AddSet(srcInst.Exec, fnParam.VarId, scope, ref v); break; }
                case TypeID.DecimalArray: { var v = GetParamsVals<decimal>(fnArgs, skip, baseScope, srcInst, cstack); ScriptVars<decimal[]>.AddSet(srcInst.Exec, fnParam.VarId, scope, ref v); break; }
                case TypeID.ObjectArray: { var v = GetParamsVals<object>(fnArgs, skip, baseScope, srcInst, cstack); ScriptVars<object[]>.AddSet(srcInst.Exec, fnParam.VarId, scope, ref v); break; }
                case TypeID.ShortArray: { var v = GetParamsVals<short>(fnArgs, skip, baseScope, srcInst, cstack); ScriptVars<short[]>.AddSet(srcInst.Exec, fnParam.VarId, scope, ref v); break; }
                case TypeID.ByteArray: { var v = GetParamsVals<byte>(fnArgs, skip, baseScope, srcInst, cstack); ScriptVars<byte[]>.AddSet(srcInst.Exec, fnParam.VarId, scope, ref v); break; }
                case TypeID.DateArray: { var v = GetParamsVals<DateTime>(fnArgs, skip, baseScope, srcInst, cstack); ScriptVars<DateTime[]>.AddSet(srcInst.Exec, fnParam.VarId, scope, ref v); break; }

                case TypeID.CustomArray:
                case TypeID.Custom:
                    { var v = new CustomObject(srcInst.Exec, GetParamsVals<CustomObject>(fnArgs, skip, baseScope, srcInst, cstack), fnParam.ParamType.CType.Class, true); ScriptVars<CustomObject>.AddSet(srcInst.Exec, fnParam.VarId, scope, ref v); break; }
            }
        }

        static T[] GetParamsVals<T>(EvalUnit[] args, int skip, int baseScope, ClassInstance srcInst, CallStack cstack)
        {
            if (args == null) return new T[0];
            int c = args.Length - skip;
            if (c < 1) return new T[0];

            T[] arr = new T[c];
            int n = 0;
            TypeID tid = GetTypeID(typeof(T));
            for (int i = skip; i < args.Length; i++)
                arr[n++] = args[i].Ev<T>(tid, baseScope, srcInst, cstack);

            return arr;
        }

        class ForEachEnumeratorAndVarInfo
        {
            public System.Collections.IEnumerator Enm;
            public VarInfo VI;
            public ForEachEnumeratorAndVarInfo(System.Collections.IEnumerator enm, VarInfo vi)
            {
                Enm = enm;
                VI = vi;
            }
        }


        public static T ExecuteFunction<T>(ScriptFunction fn, EvalUnit[] fnArgs, int baseScope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc)
        {
            if (cstack == null) cstack = new CallStack();

            cstack.Push(new CallStack.Item(fn, inst.Class));

            int scope = fn.IsInstanceFunc || fn.IsStaticFunc ? inst.Scope : inst.Exec.NewScope();
            int currentUnit = 0;

            Executor exec = inst.Exec;
            T returnValue = default(T);
            CodeUnit u, prev;

            var units = fn.Units;
            int totalUnits = units.Length;

            if (fn.Params.Length > 0)
            {
                try
                {
                    bool OneRef = fn.RefCount == 1;

                    if (fn.RefCount > 1)
                        cstack.CreateRefs(fn.RefCount);


                    for (currentUnit = 0; currentUnit < fn.Params.Length; currentUnit++)
                    {
                        var prm = fn.Params[currentUnit];
                        if (prm.ByRef)
                        {
                            CallStack.Item.RefParam v;
                            try { v = new CallStack.Item.RefParam(prm.VarId, fnArgs[currentUnit].GetVarInfo(baseScope, srcInst, cstack, true)); }
                            catch { throw new InvalidByRefArgumentException($"Failed to execute function '{FormatFuncSign(fn.Signature)}'. Argument for parameter '{prm.ParamName}' cannot be used by reference."); }
                            if (OneRef)
                                cstack.CreateRef(v);
                            else
                                cstack.AddRef(v);

                            continue;
                        }
                        u = units[currentUnit];


                        if (fnArgs == null || currentUnit >= fnArgs.Length || fnArgs[currentUnit].Kind == EvalUnitKind.Empty)
                        {
                            if (u.EU0.IsAssignment)
                                u.EU0.ProcessUnit(scope, inst, cstack);
                            else
                                u.EU0.DefineVar(scope, inst.Exec);

                        }
                    }
                }
                catch (ExecutingForciblyCanceledException) { throw; }
                catch (Exception ex)
                {

                    var lm = ex is InvalidByRefArgumentException ? csrc?.CU?.CodeLocation : fn.LocationMark;

                    string st = ErrMsgWithLoc(null, lm, null, inst.Class, fn);
                    ex.Data[ExceptionVarName.StackTrace] += st;


                    ClearVars();
                    cstack.Pop(); throw;
                }

                int c = 0;
                if (fnArgs != null)
                {
                    if (fn.HasParams)
                    {
                        c = fn.ParamsIndex;
                        if (c > fnArgs.Length) c = fnArgs.Length;
                    }
                    else
                    {
                        c = fnArgs.Length;
                        if (c > fn.Params.Length) c = fn.Params.Length;
                    }

                    for (int n = 0; n < c; n++)
                    {
                        if (fnArgs[n].Kind == EvalUnitKind.Empty || fn.Params[n].ByRef) continue;

                        SetParamVar(fn.Params[n], fnArgs[n], scope, baseScope, srcInst, inst, cstack);
                    }


                }
                if (fn.HasParams)
                {
                    c = fnArgs.Length - 1;
                    if (c == fn.ParamsIndex && fnArgs[c].Type.ID == fn.Params[c].ParamType.ID)
                        SetParamVar(fn.Params[c], fnArgs[c], scope, baseScope, srcInst, inst, cstack);
                    else
                        SetParamsArray(fn.Params[fn.ParamsIndex], fnArgs, fn.ParamsIndex, scope, baseScope, srcInst, cstack);

                }

            }

            ForEachEnumeratorAndVarInfo[] ForEachInfo = null;
            EvalUnit[] ReapplyEU = null;

            u = currentUnit < units.Length ? units[currentUnit] : null;
            prev = null;
            while (u != null)
            {

                while (u.Forward) u = u.Next;

#if EXON
                try
                {
#endif
                    if (inst.Exec.Canceled && (inst.Exec.ForciblyCanceled || (!u.InCatch && !u.InFinally && !fn.IsDisposeFunc && Thread.CurrentThread.Priority != ThreadPriority.Highest))) throw ExecutingCanceledException.GetCanceledException(inst.Exec.ForciblyCanceled);
                    switch (u.Type)
                    {
                        case UnitType.Expression:

                            u.EU0.ProcessUnit(scope, inst, cstack);
                            u = u.TrueNext;

                            break;

                        case UnitType.ForEach:
                        case UnitType.EndForEach:
                            CodeUnit ForEachUnit;
                            System.Collections.IEnumerator enumerator = null;
                            ForEachEnumeratorAndVarInfo fei = default(ForEachEnumeratorAndVarInfo);

                            if (u.Type == UnitType.ForEach)
                            {
                                ForEachUnit = u;

                                System.Collections.IEnumerable en = (System.Collections.IEnumerable)u.EU1.EvalObject(scope, inst, cstack);

                                enumerator = en.GetEnumerator();
                                if (ForEachInfo == null) ForEachInfo = new ForEachEnumeratorAndVarInfo[fn.ForEachCount];
                                ForEachInfo[u.Order] = fei = new ForEachEnumeratorAndVarInfo(enumerator, u.EU0.GetVarInfo(scope, inst, cstack));
                                if (u.EU0.Define)
                                {

                                    int VScope = fei.VI.Scope;
                                    int VId = fei.VI.ID;
                                    DefineNonArray(exec, u.EU0.Type.ID, VId, VScope);

                                }
                            }
                            else
                            {
                                ForEachUnit = u.TrueNext;

                                try
                                {
                                    fei = ForEachInfo[ForEachUnit.Order];
                                    enumerator = fei.Enm;
                                }
                                catch (NullReferenceException) { throw new ScriptExecutionException("Loop error due to missing foreach enumerator."); }

                            }
                            if (enumerator.MoveNext())
                            {
                                object curVal = enumerator.Current;
                                int VScope = fei.VI.Scope;
                                int VId = fei.VI.ID;
                                switch (ForEachUnit.EU0.Type.ID)
                                {
                                    case TypeID.Int: { int val = (int)curVal; ScriptVars<int>.Set(exec, VId, VScope, ref val); } break;
                                    case TypeID.Long: { long val = (long)curVal; ScriptVars<long>.Set(exec, VId, VScope, ref val); } break;
                                    case TypeID.Float: { float val = (float)curVal; ScriptVars<float>.Set(exec, VId, VScope, ref val); } break;
                                    case TypeID.Double: { double val = (double)curVal; ScriptVars<double>.Set(exec, VId, VScope, ref val); } break;
                                    case TypeID.Decimal: { decimal val = (decimal)curVal; ScriptVars<decimal>.Set(exec, VId, VScope, ref val); } break;
                                    case TypeID.Bool: { bool val = (bool)curVal; ScriptVars<bool>.Set(exec, VId, VScope, ref val); } break;
                                    case TypeID.String: { string val = (string)curVal; ScriptVars<string>.Set(exec, VId, VScope, ref val); } break;
                                    case TypeID.Char: { char val = (char)curVal; ScriptVars<char>.Set(exec, VId, VScope, ref val); } break;
                                    case TypeID.Byte: { byte val = (byte)curVal; ScriptVars<byte>.Set(exec, VId, VScope, ref val); } break;
                                    case TypeID.Short: { short val = (short)curVal; ScriptVars<short>.Set(exec, VId, VScope, ref val); } break;
                                    case TypeID.Date: { DateTime val = (DateTime)curVal; ScriptVars<DateTime>.Set(exec, VId, VScope, ref val); } break;
                                    case TypeID.CustomArray:
                                    case TypeID.Custom: { CustomObject val = (CustomObject)curVal; ScriptVars<CustomObject>.Set(exec, VId, VScope, ref val); } break;
                                    case TypeID.Object:
                                    default:
                                        ScriptVars<object>.Set(exec, VId, VScope, ref curVal);
                                        break;
                                }
                                u = u.Type == UnitType.ForEach ? u.TrueNext : u.TrueNext.TrueNext;
                            }
                            else { u = u.FalseNext; }

                            break;

                        case UnitType.For:
                            u.EU0.ProcessUnit(scope, inst, cstack);
                            if (u.EU1.EvalBool(scope, inst, cstack)) u = u.TrueNext; else u = u.FalseNext;

                            break;

                        case UnitType.EndFor:

                            u.TrueNext.EU2.ProcessUnit(scope, inst, cstack);
                            if (u.TrueNext.EU1.EvalBool(scope, inst, cstack)) u = u.TrueNext.TrueNext; else u = u.FalseNext;

                            break;
                        case UnitType.If:
                        case UnitType.ElseIf:

                            if (u.EU0.EvalBool(scope, inst, cstack)) u = u.TrueNext;
                            else u = u.FalseNext;

                            break;

                        case UnitType.While:
                            if (u.EU0.EvalBool(scope, inst, cstack)) u = u.TrueNext;
                            else u = u.FalseNext;
                            break;
                        case UnitType.EndDo:
                            if (u.EU0.EvalBool(scope, inst, cstack)) u = u.TrueNext;
                            else u = u.FalseNext;
                            break;

                        case UnitType.Switch:
                            object v = u.EU0.EvalObject(scope, inst, cstack);

                            CodeUnit next;
                            if (u.SwitchDict.TryGetValue(v, out next)) u = next;
                            else u = u.FalseNext;
                            break;

                        case UnitType.Return:
                            if (u.EU0 != null) returnValue = u.EU0.Ev<T>(scope, inst, cstack);

                            if (u.TrueNext != null)
                            {
                                prev = u;
                                u = u.TrueNext;
                            }
                            else
                                goto endFunc;

                            break;
                        case UnitType.Try:
                            if (cstack.SetThrownException(null))
                                SetExVars(exec, scope, null, null, null, null);

                            u = u.Next;
                            break;
                        case UnitType.EndTry:
                        case UnitType.EndCatch:

                            if (cstack.SetThrownException(null))
                                SetExVars(exec, scope, null, null, null, null);

                            u = u.TrueNext;

                            break;
                        case UnitType.Break:
                        case UnitType.Continue:
                        case UnitType.GoTo:
                            prev = u;
                            u = u.TrueNext;

                            break;

                        case UnitType.EndFinally:
                            var tex = cstack.GetThrownException();
                            if (tex != null) throw tex;

                            if (prev != null)
                            {
                                if (prev.Type == UnitType.Return) goto endFunc;
                                else u = prev.FalseNext;

                                prev = null;
                            }
                            else
                                u = u.TrueNext;

                            break;
                        case UnitType.Throw:
                            if (u.EU0 != null)
                                throw GetThrowEx(u.EU, scope, inst, cstack, exec);
                            else
                                throw cstack.GetThrownException();

                        case UnitType.Apply:

                            var applyEU = GetApplyEU(u.EU0);
                            try { applyEU.ProcessUnit(scope, inst, cstack); }
                            catch (Exception ex) { throw new ScriptExecutionException($"Error on applying expression '{applyEU.Code}'. " + ex.Message); }
                            u = u.TrueNext;
                            break;
                        case UnitType.Reapply:
                            if (ReapplyEU == null) ReapplyEU = new EvalUnit[fn.ReapplyCount];
                            applyEU = ReapplyEU[u.Order];
                            if (applyEU == null)
                                ReapplyEU[u.Order] = applyEU = GetApplyEU(u.EU0);

                            try { applyEU.ProcessUnit(scope, inst, cstack); }
                            catch (Exception ex) { throw new ScriptExecutionException($"Error on applying expression '{applyEU.Code}'. " + ex.Message); }

                            u = u.TrueNext;
                            break;
                        default:

                            if (u.TrueNext != null) throw new Exception($"UnitType '{u.Type}' not supported.");
                            u = null;
                            break;

                    }

#if EXON
                }
#endif

#if DEBUG
                try
                {

                }
#endif
                catch (ExecutingForciblyCanceledException) { throw; }
                catch (Exception ex)
                {

                    if (u.Type != UnitType.EndFinally && cstack.GetThrownException() != ex)
                    {

                        bool dataIsNull = ex.Data[ExceptionVarName.StackTrace] == null;
                        string st = ErrMsgWithLoc(null, u.CodeLocation, dataIsNull ? RestoreCode(u.Code) : null, inst.Class, fn);
                        ex.Data[ExceptionVarName.StackTrace] += st;

                    }

                    string exType = "", exMsg = "";
                    object exObj = null;
                    bool re = false;
                doEx:
                    if (u.Try == null)
                    {
                        ClearVars();
                        cstack.Pop(); throw;
                    }

                    if (!re)
                    {
                        if (cstack.SetThrownException(ex))
                        {
                            GetExInfo(ex, out exType, out exMsg, out exObj);
                            SetExVars(exec, scope, ex, exType, exMsg, exObj);
                        }
                    }

                    bool caseNotFound = false;
                    CodeUnit next;

                findNext:
                    if (u.InCatch || caseNotFound)
                    {
                        if (u.Try.FalseNext == null)
                        {
                            u = units.Last();
                            re = true;
                            goto doEx;
                        }
                        if (units[u.Try.FalseNext.Index - 1].Type == UnitType.Finally) u = u.Try.FalseNext;
                        else
                        {
                            u = units[u.Try.FalseNext.Index - 1];
                            re = true;
                            goto doEx;
                        }
                    }
                    else if (FindEx(u.Try.CaseList, ex, out next) || (exType != null && u.Try.CaseDict.TryGetValue(exType, out next))) u = next;
                    else if (u.Try.TrueNext != null) u = u.Try.TrueNext;
                    else { caseNotFound = true; goto findNext; }

                }
            }

        endFunc:


            ClearVars();
            cstack.Pop();

            return returnValue;

            void ClearVars()
            {

                if (cstack.GetThrownException() != null) SetExVars(exec, scope, null, null, null, null);

                if (fn.ForCleaning != null) inst.Exec.RemVars(scope, fn.ForCleaning);
                if (!fn.IsInstanceOrStaticFunc) inst.Exec.FreeScope(scope);
            }
            EvalUnit GetApplyEU(EvalUnit eu)
            {
                var varInfo = eu.GetVarInfo(scope, inst, cstack);
                object euObj = ScriptVars<object>.Get(exec, varInfo.ID, varInfo.Scope);
                if (euObj is Expr result)
                    return result.EU;
                else throw new ScriptExecutionException("Argument is not an expression.");
            }
        }

        public static Exception GetThrowEx(EvalUnit[] args, int scope, ClassInstance inst, CallStack cstack, Executor exec)
        {
            Exception ex;
            object exVal = args[0].Ev<object>(scope, inst, cstack);
            if (exVal is Exception exc)
                return exc;
            else if (exVal is CustomObject custExObj)
            {

                string name = "", msg = "";
                GetExVarsFromCustomEx(exec, out name, out msg, ((ClassInstance)custExObj).Scope);

                ex = new CustomThrownException();
                ex.Data[ExceptionVarName.TypeName] = name;
                ex.Data[ExceptionVarName.Message] = msg;
                ex.Data[ExceptionVarName.CustomExObj] = custExObj;
                return ex;

            }
            else if (exVal is string exStr)
            {
                ex = new CustomThrownException();
                ex.Data[ExceptionVarName.TypeName] = exStr;
                ex.Data[ExceptionVarName.Message] = args.Length > 1 ? args[1].EvalString(scope, inst, cstack) : null;
                return ex;
            }
            else
                throw new ScriptExecutionException($"Invalid throw argument of type '{exVal.GetType()}'.");

        }
        static bool FindEx(List<KeyValuePair<object, CodeUnit>> caseList, Exception ex, out CodeUnit cu)
        {
            object exObj = ex is CustomThrownException && ex.Data.Contains(ExceptionVarName.CustomExObj) ? ex.Data[ExceptionVarName.CustomExObj] : ex;

            for (int i = 0; i < caseList.Count; i++)
            {
                var key = caseList[i].Key;
                var value = caseList[i].Value;

                bool ok = false;
                if (key is Type type) ok = type.IsInstanceOfType(ex);
                else if (key is CustomType ct) ok = ct.IsOfType(exObj);


                if (ok)
                {
                    cu = value;
                    return true;
                }

            }

            cu = null;
            return false;

        }

        public static void SetExVars(Executor exec, int scope, object ex, string exType, string exMsg, object exObj)
        {

            Script script = exec.ExecutedScript;
            ScriptVars<string>.AddSet(exec, script.ExVarID.TypeName, scope, ref exType);
            ScriptVars<string>.AddSet(exec, script.ExVarID.Message, scope, ref exMsg);

            ScriptVars<object>.AddSet(exec, script.ExVarID.Object, scope, ref ex);
            ScriptVars<object>.AddSet(exec, script.ExVarID.CustomExObj, scope, ref exObj);

        }
        static void GetExVarsFromCustomEx(Executor exec, out string exType, out string exMsg, int scope)
        {
            Script script = exec.ExecutedScript;

            exType = ScriptVars<string>.Get(exec, script.ExVarID.NameVarInCustomExClass, scope);
            exMsg = ScriptVars<string>.Get(exec, script.ExVarID.MessageVarInCustomExClass, scope);

        }
        public static void GetExInfo(Exception ex, out string exType, out string exMsg, out object exObj)
        {
            exType = ex.GetType().Name;
            exMsg = ex.Message;
            exObj = null;
            if (ex.Data.Contains(ExceptionVarName.TypeName)) exType = (string)ex.Data[ExceptionVarName.TypeName];
            if (ex.Data.Contains(ExceptionVarName.Message)) exMsg = (string)ex.Data[ExceptionVarName.Message];
            if (ex.Data.Contains(ExceptionVarName.CustomExObj)) exObj = ex.Data[ExceptionVarName.CustomExObj];

        }

        public static CustomObject NewClassInstance(EvalUnit eu, int scope, ClassInstance inst, bool ignoreConstructor = false)
        {
            ClassInstance result = new ClassInstance(inst.Exec, eu.Type.CType.Class, eu.Nested[0].Args, inst, scope, false, ignoreConstructor, eu.Func);

            return new CustomObject(inst.Exec, result, eu.Type.CType.Class);
        }

        public static CustomObject NewClassInstance(ScriptClass sc, ArgBlocks[] nested, int scope, ClassInstance inst, bool ignoreConstructor = false)
        {
            ClassInstance result = new ClassInstance(inst.Exec, sc, nested[0].Args, inst, scope, false, ignoreConstructor, null);
            return new CustomObject(inst.Exec, result, sc);
        }

        private static object NewArray(TypeID arrType, ScriptClass custom, ArgBlocks[] nested, int scope, ClassInstance inst, CallStack cstack)
        {
            bool withInit = nested.Length > 1;
            int length = withInit ? nested[1].Args.Length : nested[0].Args[0].EvalInt(scope, inst, cstack);

            Type t = custom == null ? GetTypeByID(arrType).GetElementType() : TypeOfCustom;

            Array arr = Array.CreateInstance(t, length);
            if (withInit)
                InitArrayWithValues(arr, arrType, nested[1].Args, scope, inst, cstack);

            return custom != null ? new CustomObject(inst.Exec, arr, custom, true) : arr;
        }

        private static void InitArrayWithValues(Array arr, TypeID typeId, EvalUnit[] args, int scope, ClassInstance inst, CallStack cstack)
        {
            switch (typeId)
            {
                case TypeID.IntArray: FillWithArgs<int>((int[])arr, args, scope, inst, cstack); break;
                case TypeID.StringArray: FillWithArgs<string>((string[])arr, args, scope, inst, cstack); ; break;
                case TypeID.CharArray: FillWithArgs<char>((char[])arr, args, scope, inst, cstack); break;
                case TypeID.LongArray: FillWithArgs<long>((long[])arr, args, scope, inst, cstack); break;
                case TypeID.DoubleArray: FillWithArgs<double>((double[])arr, args, scope, inst, cstack); break;
                case TypeID.FloatArray: FillWithArgs<float>((float[])arr, args, scope, inst, cstack); break;
                case TypeID.ShortArray: FillWithArgs<short>((short[])arr, args, scope, inst, cstack); break;
                case TypeID.ByteArray: FillWithArgs<byte>((byte[])arr, args, scope, inst, cstack); break;
                case TypeID.BoolArray: FillWithArgs<bool>((bool[])arr, args, scope, inst, cstack); break;
                case TypeID.DecimalArray: FillWithArgs<decimal>((decimal[])arr, args, scope, inst, cstack); break;
                case TypeID.DateArray: FillWithArgs<DateTime>((DateTime[])arr, args, scope, inst, cstack); break;
                case TypeID.CustomArray: FillWithArgs<CustomObject>((CustomObject[])arr, args, scope, inst, cstack); break;
                case TypeID.ObjectArray: FillWithArgs<object>((object[])arr, args, scope, inst, cstack); break;
                default: throw new ScriptExecutionException($"Bad array type '{GetTypeName(typeId)}'.");
            }

        }

        private static void FillWithArgs<T>(T[] arr, EvalUnit[] args, int scope, ClassInstance inst, CallStack cstack)
        {
            for (int i = 0; i < args.Length; i++)
            {
                T v = args[i].Ev<T>(scope, inst, cstack);
                arr[i] = v;
            }
        }

        private static object NewBasic(TypeID type, ArgBlocks[] nested, int scope, ClassInstance inst, CallStack cstack, int length = -1)
        {

            if (nested[0].Args == null) return Activator.CreateInstance(GetTypeByID(type));

            object[] prms = null;
            prms = new object[nested[0].Args.Length];
            for (int i = 0; i < prms.Length; i++)
                prms[i] = nested[0].Args[i].EvalObject(scope, inst, cstack);

            return Activator.CreateInstance(GetTypeByID(type), prms);
        }
        public static object NewObj(EvalUnit eu, int scope, ClassInstance inst, CallStack cstack)
        {
            if (eu.Type.ID == TypeID.Custom)
                return NewClassInstance(eu, scope, inst);

            if (!TypeIsArray(eu.Type.ID))
                return NewBasic(eu.Type.ID, eu.Nested, scope, inst, cstack);
            else
                return NewArray(eu.Type.ID, eu.Type.CType?.Class, eu.Nested, scope, inst, cstack);

        }

        private static void DefineNonArray(Executor exec, TypeID typeId, int varId, int scope)
        {
            switch (typeId)
            {
                case TypeID.Int: ScriptVars<int>.Add(exec, varId, scope); break;
                case TypeID.Long: ScriptVars<long>.Add(exec, varId, scope); break;
                case TypeID.Float: ScriptVars<float>.Add(exec, varId, scope); break;
                case TypeID.Double: ScriptVars<double>.Add(exec, varId, scope); break;
                case TypeID.Decimal: ScriptVars<decimal>.Add(exec, varId, scope); break;
                case TypeID.Bool: ScriptVars<bool>.Add(exec, varId, scope); break;
                case TypeID.String: ScriptVars<string>.Add(exec, varId, scope); break;
                case TypeID.Char: ScriptVars<char>.Add(exec, varId, scope); break;
                case TypeID.Byte: ScriptVars<byte>.Add(exec, varId, scope); break;
                case TypeID.Short: ScriptVars<short>.Add(exec, varId, scope); break;
                case TypeID.Date: ScriptVars<DateTime>.Add(exec, varId, scope); break;
                case TypeID.Custom: ScriptVars<CustomObject>.Add(exec, varId, scope); break;
                case TypeID.Object: ScriptVars<object>.Add(exec, varId, scope); break;
                default: throw new ScriptExecutionException("Non-array variable required.");
            }
        }

        public static object DynFuncCall(ClassInstance inst, string fnName, object[] args)
        {

            EvalUnit[] svArgs = null;
            int c = args == null ? 0 : args.Length;
            if (c > 0)
            {
                svArgs = new EvalUnit[args.Length];
                for (int i = 0; i < c; i++)
                {
                    var v = args[i];
                    var t = GetTypeID(v.GetType(), true);
                    svArgs[i] = t == TypeID.None ? EvalUnit.GetEUWithSpecificValue(v) : EvalUnit.GetEUWithSpecificValue(v, GetVarTypeByID(t));
                }
            }
            var fn = inst.Class.GetFunc(fnName, svArgs, true, null, inst.IsStatic ? true : null);
            if (fn == null)
                throw new ArgumentException($"{(inst.IsStatic ? "Static f" : "F")}unction '{FormatFuncSign(GetFuncSign(fnName, svArgs))}' not found at '{inst.Class.ClassFullName}'.");

            return ExecuteFunction<object>(fn, svArgs, -1, null, inst, null, null);

        }

        public struct VarInfo
        {
            public ClassInstance Inst;

            public int Scope;
            public int ID;
            public VarInfo(int scope, ClassInstance inst, int id)
            {
                Inst = inst;

                Scope = scope;
                ID = id;
            }
        }

    }

}