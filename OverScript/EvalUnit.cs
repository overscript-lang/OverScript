using System;
using System.Collections.Generic;
using static OverScript.Executor;
using static OverScript.Literals;
using static OverScript.ScriptClass;

namespace OverScript
{

    public class EvalUnit
    {
        public bool IsAssignment;
        public EvalUnit Parent;
        public VarType Type;
        public OperationKind OpKind;
        public string Code;
        public EvalUnit Op1_Unit, Op2_Unit, Path_Unit;

        public ScriptClass ClassLink;
        public ArgBlocks[] Nested;

        public EvalUnitKind Kind;

        public int VarID = -1;


        public VarScopeKind ScopeKind = VarScopeKind.None;
        public object Func;

        public object SpecificValue;
        public byte RefParamNum = 0;
        public bool Postfix = false;


        public bool ScopeKindIsThisOrStatic = false;
        public bool Define = false;
        public bool IsArrayItem = false;
        public CodeUnit CU = null;
        public InvolveInfo InvolveData = null;

        public delegate void PU(int scope, ClassInstance inst, CallStack cstack);
        public PU ProcessUnit;

        public class FuncOnExData
        {
            public EvalUnit ValueEU;
            public EvalUnit ExConditionEU;
            public EvalUnit TriesEU;
            public EvalUnit[] RetryEU;
            public EvalUnit RetryWhileEU;
            public bool Retry;
            public FuncOnExData(EvalUnit value, EvalUnit exConditionEU, bool retry, EvalUnit triesEU, EvalUnit retryWhileEU, EvalUnit[] retryEU)
            {
                ValueEU = value;
                ExConditionEU = exConditionEU;
                TriesEU = triesEU;
                RetryEU = retryEU;
                RetryWhileEU = retryWhileEU;
                Retry = retry;
            }
        }

        public List<FuncOnExData> FuncOnEx = null;


        public bool EvalBool(int scope, ClassInstance inst, CallStack cstack) => Ev<bool>(TypeID.Bool, scope, inst, cstack);
        public byte EvalByte(int scope, ClassInstance inst, CallStack cstack) => Ev<byte>(TypeID.Byte, scope, inst, cstack);
        public short EvalShort(int scope, ClassInstance inst, CallStack cstack) => Ev<short>(TypeID.Short, scope, inst, cstack);
        public int EvalInt(int scope, ClassInstance inst, CallStack cstack) => Ev<int>(TypeID.Int, scope, inst, cstack);
        public long EvalLong(int scope, ClassInstance inst, CallStack cstack) => Ev<long>(TypeID.Long, scope, inst, cstack);
        public float EvalFloat(int scope, ClassInstance inst, CallStack cstack) => Ev<float>(TypeID.Float, scope, inst, cstack);
        public double EvalDouble(int scope, ClassInstance inst, CallStack cstack) => Ev<double>(TypeID.Double, scope, inst, cstack);
        public decimal EvalDecimal(int scope, ClassInstance inst, CallStack cstack) => Ev<decimal>(TypeID.Decimal, scope, inst, cstack);
        public string EvalString(int scope, ClassInstance inst, CallStack cstack) => Ev<string>(TypeID.String, scope, inst, cstack);
        public char EvalChar(int scope, ClassInstance inst, CallStack cstack) => Ev<char>(TypeID.Char, scope, inst, cstack);
        public DateTime EvalDate(int scope, ClassInstance inst, CallStack cstack) => Ev<DateTime>(TypeID.Date, scope, inst, cstack);
        public object EvalObject(int scope, ClassInstance inst, CallStack cstack) => Ev<object>(TypeID.Object, scope, inst, cstack);
        public CustomObject EvalCustom(int scope, ClassInstance inst, CallStack cstack) => Ev<CustomObject>(TypeID.Custom, scope, inst, cstack);

        public bool[] EvalBoolArray(int scope, ClassInstance inst, CallStack cstack) => Ev<bool[]>(TypeID.BoolArray, scope, inst, cstack);
        public byte[] EvalByteArray(int scope, ClassInstance inst, CallStack cstack) => Ev<byte[]>(TypeID.ByteArray, scope, inst, cstack);
        public short[] EvalShortArray(int scope, ClassInstance inst, CallStack cstack) => Ev<short[]>(TypeID.ShortArray, scope, inst, cstack);
        public int[] EvalIntArray(int scope, ClassInstance inst, CallStack cstack) => Ev<int[]>(TypeID.IntArray, scope, inst, cstack);
        public long[] EvalLongArray(int scope, ClassInstance inst, CallStack cstack) => Ev<long[]>(TypeID.LongArray, scope, inst, cstack);
        public float[] EvalFloatArray(int scope, ClassInstance inst, CallStack cstack) => Ev<float[]>(TypeID.FloatArray, scope, inst, cstack);
        public double[] EvalDoubleArray(int scope, ClassInstance inst, CallStack cstack) => Ev<double[]>(TypeID.DoubleArray, scope, inst, cstack);
        public decimal[] EvalDecimalArray(int scope, ClassInstance inst, CallStack cstack) => Ev<decimal[]>(TypeID.DecimalArray, scope, inst, cstack);
        public string[] EvalStringArray(int scope, ClassInstance inst, CallStack cstack) => Ev<string[]>(TypeID.StringArray, scope, inst, cstack);
        public char[] EvalCharArray(int scope, ClassInstance inst, CallStack cstack) => Ev<char[]>(TypeID.CharArray, scope, inst, cstack);
        public DateTime[] EvalDateArray(int scope, ClassInstance inst, CallStack cstack) => Ev<DateTime[]>(TypeID.DateArray, scope, inst, cstack);
        public object[] EvalObjectArray(int scope, ClassInstance inst, CallStack cstack) => Ev<object[]>(TypeID.ObjectArray, scope, inst, cstack);

        public CustomObject EvalCustomArray(int scope, ClassInstance inst, CallStack cstack) => Ev<CustomObject>(TypeID.CustomArray, scope, inst, cstack);
        public Array EvalArray(int scope, ClassInstance inst, CallStack cstack) => Ev<Array>(scope, inst, cstack);


        public T Ev<T>(TypeID tid, int scope, ClassInstance inst, CallStack cstack)
        {

            if (Type.ID == tid)
                return Eval<T>(scope, inst, cstack);
            else
                return Ev<T>(scope, inst, cstack);

        }

        public T Ev<T>(int scope, ClassInstance inst, CallStack cstack)
        {
            switch (Type.ID)
            {
                case TypeID.Int: return Eval<T, int>(scope, inst, cstack);
                case TypeID.Bool: return Eval<T, bool>(scope, inst, cstack);
                case TypeID.String: return Eval<T, string>(scope, inst, cstack);
                case TypeID.Char: return Eval<T, char>(scope, inst, cstack);
                case TypeID.Decimal: return Eval<T, decimal>(scope, inst, cstack);
                case TypeID.Long: return Eval<T, long>(scope, inst, cstack);
                case TypeID.Void:
                case TypeID.Object: return Eval<T, object>(scope, inst, cstack);
                case TypeID.Double: return Eval<T, double>(scope, inst, cstack);
                case TypeID.Float: return Eval<T, float>(scope, inst, cstack);
                case TypeID.Byte: return Eval<T, byte>(scope, inst, cstack);
                case TypeID.Short: return Eval<T, short>(scope, inst, cstack);
                case TypeID.Date: return Eval<T, DateTime>(scope, inst, cstack);

                case TypeID.IntArray: return Eval<T, int[]>(scope, inst, cstack);
                case TypeID.BoolArray: return Eval<T, bool[]>(scope, inst, cstack);
                case TypeID.StringArray: return Eval<T, string[]>(scope, inst, cstack);
                case TypeID.CharArray: return Eval<T, char[]>(scope, inst, cstack);
                case TypeID.DecimalArray: return Eval<T, decimal[]>(scope, inst, cstack);
                case TypeID.LongArray: return Eval<T, long[]>(scope, inst, cstack);
                case TypeID.ObjectArray: return Eval<T, object[]>(scope, inst, cstack);
                case TypeID.DoubleArray: return Eval<T, double[]>(scope, inst, cstack);
                case TypeID.FloatArray: return Eval<T, float[]>(scope, inst, cstack);
                case TypeID.ByteArray: return Eval<T, byte[]>(scope, inst, cstack);
                case TypeID.ShortArray: return Eval<T, short[]>(scope, inst, cstack);
                case TypeID.DateArray: return Eval<T, DateTime[]>(scope, inst, cstack);
                case TypeID.CustomArray:
                case TypeID.Custom: return Eval<T, CustomObject>(scope, inst, cstack);
                default:
                    throw new ScriptExecutionException($"Wrong type '{Type.Name}'.");
            }
        }
        private TR Eval<TR, TO>(int scope, ClassInstance inst, CallStack cstack) => TypeConverter.ConvertValue<TO, TR>(Eval<TO>(scope, inst, cstack));

        void AddVar<T>(int scope, Executor exec)
        {

            ScriptVars<T>.Add(exec, VarID, scope);
        }
        public void DefineVar(int scope, Executor exec)
        {


            switch (Type.ID)
            {

                case TypeID.IntArray: AddVar<int[]>(scope, exec); break;
                case TypeID.ObjectArray: AddVar<object[]>(scope, exec); break;
                case TypeID.LongArray: AddVar<long[]>(scope, exec); break;
                case TypeID.FloatArray: AddVar<float[]>(scope, exec); break;
                case TypeID.DoubleArray: AddVar<double[]>(scope, exec); break;
                case TypeID.DecimalArray: AddVar<decimal[]>(scope, exec); break;
                case TypeID.StringArray: AddVar<string[]>(scope, exec); break;
                case TypeID.CharArray: AddVar<char[]>(scope, exec); break;
                case TypeID.BoolArray: AddVar<bool[]>(scope, exec); break;
                case TypeID.ShortArray: AddVar<short[]>(scope, exec); break;
                case TypeID.ByteArray: AddVar<byte[]>(scope, exec); break;
                case TypeID.DateArray: AddVar<DateTime[]>(scope, exec); break;

                case TypeID.Int: AddVar<int>(scope, exec); break;
                case TypeID.String: AddVar<string>(scope, exec); break;
                case TypeID.Char: AddVar<char>(scope, exec); break;
                case TypeID.Double: AddVar<double>(scope, exec); break;
                case TypeID.Float: AddVar<float>(scope, exec); break;
                case TypeID.Long: AddVar<long>(scope, exec); break;
                case TypeID.Decimal: AddVar<decimal>(scope, exec); break;
                case TypeID.Bool: AddVar<bool>(scope, exec); break;
                case TypeID.Object: AddVar<object>(scope, exec); break;
                case TypeID.Short: AddVar<short>(scope, exec); break;
                case TypeID.Byte: AddVar<byte>(scope, exec); break;
                case TypeID.Date: AddVar<DateTime>(scope, exec); break;
                case TypeID.CustomArray:
                case TypeID.Custom: AddVar<CustomObject>(scope, exec); break;

                default: throw new ScriptExecutionException($"Type '{Type.Name}' not supported.");
            }
        }



        public T Eval<T>(int scope, ClassInstance inst, CallStack cstack)
        {
            if (inst.Exec.ForciblyCanceled) throw ExecutingCanceledException.GetCanceledException(true);


            T v, v1, v2;
            v1 = v2 = default(T);
            switch (Kind)
            {
                case EvalUnitKind.Assignment:

                    v2 = Op2_Unit.Ev<T>(scope, inst, cstack);

                    VarInfo varInfo;

                    varInfo = Op1_Unit.GetVarInfo(scope, inst, cstack);
                    if (!Op1_Unit.IsArrayItem)
                    {
                        if (Define) AddVar<T>(scope, inst.Exec);

                        v = ScriptVars<T>.SetAndReturnPrev(inst.Exec, varInfo.ID, varInfo.Scope, ref v2);
                    }
                    else
                    {
                        int index = Op1_Unit.Nested[0].Args[0].EvalInt(scope, inst, cstack);
                        T[] arr;
                        if (Op1_Unit.Op1_Unit == null)
                            arr = Type.ID != TypeID.Custom ? ScriptVars<T[]>.Get(inst.Exec, varInfo.ID, varInfo.Scope) : (T[])(object)(CustomObject[])ScriptVars<CustomObject>.Get(inst.Exec, varInfo.ID, varInfo.Scope);
                        else
                            arr = Type.ID != TypeID.Custom ? Op1_Unit.Op1_Unit.Eval<T[]>(scope, inst, cstack) : (T[])(object)(CustomObject[])Op1_Unit.Op1_Unit.Eval<CustomObject>(scope, inst, cstack);

                        v = SetArrayItemAndReturnPrev<T>(arr, index, ref v2);
                    }

                    if (Postfix) v2 = v;
                    return v2;
                case EvalUnitKind.Literal:
                    return GetLiteral<T>(VarID);

                case EvalUnitKind.ArrayItem:
                    {
                        int index = Nested[0].Args[0].EvalInt(scope, inst, cstack);
                        T[] arr;
                        if (Op1_Unit == null)
                        {
                            varInfo = GetVarInfo(scope, inst, cstack);
                            arr = Type.ID != TypeID.Custom ? ScriptVars<T[]>.Get(inst.Exec, varInfo.ID, varInfo.Scope) : (T[])(object)(CustomObject[])ScriptVars<CustomObject>.Get(inst.Exec, varInfo.ID, varInfo.Scope);


                        }
                        else
                            arr = Type.ID != TypeID.Custom ? Op1_Unit.Eval<T[]>(scope, inst, cstack) : (T[])(object)(CustomObject[])Op1_Unit.Eval<CustomObject>(scope, inst, cstack);

                        v = GetArrayItem<T>(arr, index);
                        return v;
                    }
                case EvalUnitKind.Function:
                    FuncToCall<T> fn = (FuncToCall<T>)Func;
                    varInfo = GetVarInfo(scope, inst, cstack);


                    try
                    {
                        v = fn(Nested[0].Args, scope, inst, varInfo.Inst, cstack, this);
                        return v;
                    }
                    catch (ExecutingForciblyCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        if (FuncOnEx == null) throw;

                        string exType, exMsg;
                        object exObj;
                        if (cstack.SetThrownException(ex))
                        {
                            GetExInfo(ex, out exType, out exMsg, out exObj);
                            SetExVars(inst.Exec, scope, ex, exType, exMsg, exObj);
                        }

                        int tryNum = 0;
                        FuncOnExData tryFOE = null;

                    findFOE:

                        for (int n = 0; n < FuncOnEx.Count; n++)
                        {
                            var curFOE = FuncOnEx[n];
                            bool cond = curFOE.ExConditionEU == null ? true : curFOE.ExConditionEU.EvalBool(scope, inst, cstack);
                            if (cond)
                            {
                                int triesNum = curFOE.TriesEU == null ? 0 : curFOE.TriesEU.EvalInt(scope, inst, cstack);
                                bool untilCancel = triesNum == 0 && curFOE.Retry;

                                if (tryFOE != curFOE) tryNum = 0;

                                tryNum += 1;

                                if (untilCancel || tryNum <= triesNum)

                                {
                                    try
                                    {
                                        if (curFOE.RetryWhileEU == null || curFOE.RetryWhileEU.EvalBool(scope, inst, cstack))
                                        {

                                            if (curFOE.RetryEU != null)
                                            {
                                                for (int actNum = 0; actNum < curFOE.RetryEU.Length; actNum++)
                                                    curFOE.RetryEU[actNum].ProcessUnit(scope, inst, cstack);
                                            }

                                            v = fn(Nested[0].Args, scope, inst, varInfo.Inst, cstack, this);
                                            cstack.SetThrownException(null);
                                            SetExVars(inst.Exec, scope, null, null, null, null);
                                            return v;
                                        }
                                    }
                                    catch (ExecutingForciblyCanceledException) { throw; }
                                    catch (Exception tryEx)
                                    {

                                        if (cstack.SetThrownException(tryEx))
                                        {
                                            GetExInfo(tryEx, out exType, out exMsg, out exObj);
                                            SetExVars(inst.Exec, scope, tryEx, exType, exMsg, exObj);
                                        }


                                        ex = tryEx;
                                        tryFOE = curFOE;
                                        goto findFOE;
                                    }
                                }
                                if (curFOE.ValueEU == null || curFOE.ValueEU.Kind == EvalUnitKind.Empty) throw ex;

                                string st = ErrMsgWithLoc(null, CU.CodeLocation, RestoreCode(CU.Code), inst.Class, CU.Fn);
                                ex.Data[ExceptionVarName.StackTrace] += st;

                                return curFOE.ValueEU.Ev<T>(Type.ID, scope, inst, cstack);
                            }
                        }
                        throw ex;

                    }

                case EvalUnitKind.Empty:
                    return default(T);

                case EvalUnitKind.New:
                    var obj = (T)NewObj(this, scope, inst, cstack);
                    return obj;
                case EvalUnitKind.This:
                    if (inst.ThisObj == null)
                        inst.ThisObj = new CustomObject(inst.Exec, inst, inst.Class);


                    return (T)(object)inst.ThisObj;

                case EvalUnitKind.Variable:
                    if (Define) { AddVar<T>(scope, inst.Exec); v = default(T); }
                    else
                    {
                        varInfo = GetVarInfo(scope, inst, cstack);
                        v = ScriptVars<T>.Get(inst.Exec, varInfo.ID, varInfo.Scope);
                    }
                    return v;
                default:

                    return (T)SpecificValue;

            }
        }



        public VarInfo GetVarInfo(int scope, ClassInstance inst, CallStack cstack, bool prevCall = false)
        {

            switch (ScopeKind)
            {
                case VarScopeKind.Local:
                    return new VarInfo(scope, inst, VarID);
                case VarScopeKind.This:
                    return new VarInfo(inst.Scope, inst, VarID);
                case VarScopeKind.Inst:
                    ClassInstance ci = (ClassInstance)Path_Unit.EvalCustom(scope, inst, cstack);
                    return new VarInfo(ci.Scope, ci, VarID);
                case VarScopeKind.Static:
                    ci = inst.Exec.GetStaticInstance(ClassLink ?? inst.Class);
                    return new VarInfo(ci.Scope, ci, VarID);
                case VarScopeKind.Ref:
                    int n = cstack.Items.Count - (prevCall ? 2 : 1);
                    if (n < 0) throw new ScriptExecutionException($"Failed to get variable '{Code}' by reference.");

                    var cs = cstack.Items[n];

                    return cs.Refs != null ? cs.Refs[RefParamNum].VI : cs.Ref.VI;
                case VarScopeKind.Involved:



                    return InvolveData.VI;

                default:
                    throw new ScriptExecutionException($"Invalid ScopeKind '{ScopeKind}'.");
            }

        }
        public static EvalUnit GetEUWithSpecificValue(object v)
        {
            var vt = new VarType(TypeID.Object);
            return new EvalUnit() { Kind = EvalUnitKind.SpecificValue, SpecificValue = v, Type = vt, Code = "" };
        }
        public static EvalUnit GetEUWithSpecificValue(object v, VarType vt)
        {

            return new EvalUnit() { Kind = EvalUnitKind.SpecificValue, SpecificValue = v, Type = vt, Code = "" };
        }
        public EvalUnit ShallowCopy()
        {
            var eu = (EvalUnit)this.MemberwiseClone();
            return eu;
        }
        public void ShallowCopyTo(EvalUnit eu)
        {

            eu.IsAssignment = IsAssignment;
            eu.Parent = Parent;
            eu.Type = Type;
            eu.OpKind = OpKind;
            eu.Code = Code;
            eu.Op1_Unit = Op1_Unit;
            eu.Op2_Unit = Op2_Unit;
            eu.Path_Unit = Path_Unit;
            eu.ClassLink = ClassLink;
            eu.Nested = Nested;
            eu.Kind = Kind;
            eu.VarID = VarID;
            eu.ScopeKind = ScopeKind;
            eu.Func = Func;
            eu.SpecificValue = SpecificValue;
            eu.RefParamNum = RefParamNum;
            eu.Postfix = Postfix;
            eu.ScopeKindIsThisOrStatic = ScopeKindIsThisOrStatic;
            eu.Define = Define;
            eu.IsArrayItem = IsArrayItem;
            eu.CU = CU;
            eu.InvolveData = InvolveData;
            eu.ProcessUnit = ProcessUnit;



        }
        public override string ToString() => RestoreCode(Code);


        static private T SetArrayItemAndReturnPrev<T>(T[] array, int pos, ref T value)
        {
            try
            {
                T prev = array[pos];
                array[pos] = value;
                return prev;
            }
            catch (IndexOutOfRangeException)
            {
                throw new IndexOutOfRangeException($"Array of size {array.Length} does not contain element with index {pos}.");
            }
            catch (NullReferenceException)
            {
                throw new NullReferenceException("Failed to set array element due to the array is null.");
            }
        }

        static private T GetArrayItem<T>(T[] array, int pos)
        {
            try
            {
                return array[pos];
            }
            catch (IndexOutOfRangeException)
            {
                throw new IndexOutOfRangeException($"Array of size {array.Length} does not contain element with index {pos}.");
            }
            catch (NullReferenceException)
            {
                throw new NullReferenceException("Failed to get array element due to the array is null.");
            }


        }


    }

    public class InvolveInfo
    {


        public VarScopeKind RealScopeKind;
        public VarInfo VI;

        public InvolveInfo(VarInfo vi, VarScopeKind kind = default)
        {
            VI = vi;
            RealScopeKind = kind;
        }

    }


}

