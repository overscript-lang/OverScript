using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static OverScript.Literals;

namespace OverScript
{

    public partial class ScriptClass
    {


        public enum EvalUnitKind : byte { Empty = 0, Expression, Variable, Literal, ArrayItem, Function, New, This, SpecificValue, BasicType, CustomType, Assignment }

        public struct ArgBlocks
        {
            public EvalUnit[] Args;

        }
        public delegate T FuncToCall<T>(EvalUnit[] fnArgs, int baseScope, ClassInstance srcInst, ClassInstance inst, CallStack cstack, EvalUnit csrc);

        public const string InstanceFuncName = "Instance";
        public const string StaticFuncName = "Static";
        public const string CtorFuncName = "New";
        public const string FinalizeFuncName = "Finalize";
        public const string ToStringFuncName = "ToString";
        public const string ArrayToStringFuncName = "ArrayToString";
        public const string GetHashCodeFuncName = "GetHashCode";
        public const string EqualsFuncName = "Equals";
        public const string CompareToFuncName = "CompareTo";
        public const string DisposeFuncName = "Dispose";
        public const string GetEnumeratorFuncName = "GetEnumerator";
        public const string EnumeratorMoveNextFuncName = "MoveNext";
        public const string EnumeratorCurrentFuncName = "Current";
        public const string EnumeratorResetFuncName = "Reset";

        public const char BasicFunctionPrefix = '@';
        public const char FunctionLayerPrefix = '$';
        public const string OrLocalFunctionPrefix = "@@";
        public const string OrBasicFunctionPrefix = "@@@";
        public const string ClassSign = "class";
        public const string NullStr = "null";
        public const string TrueStr = "true";
        public const string FalseStr = "false";
        public const string ThisStr = "this";
        public const char RedefinePrefix = '$';
        public const string OperatorFunctionPrefix = "op_";
        public const char AtRuntimeTypeMethodPrefix = '@';
        public class CodeUnit
        {
            public EvalUnit EU0, EU1, EU2;
            public EvalUnit[] EU;
            public string[] Str;
            public int Nesting;
            public UnitType Type;
            public string Code;

            public bool InCatch, InFinally;
            public Dictionary<NullableObject<object>, CodeUnit> SwitchDict;
            public Dictionary<string, CodeUnit> CaseDict;
            public List<KeyValuePair<object, CodeUnit>> CaseList;

            public CodeUnit Next, TrueNext, FalseNext, Try, Finally;
            public int Index;
            public LocMark CodeLocation;
            public ScriptFunction Fn;
            public int Order = -1;

            public bool Forward;
            public override string ToString() => RestoreCode(Code);

        }
        public enum VarScopeKind : byte
        {
            None = 0,
            Local = 1,
            This = 2,
            Inst = 3,
            Static = 4,
            Ref = 5,
            Involved = 6
        }

        public enum UnitType : ushort
        {
            Expression = 0,
            If = 1,
            For = 2,
            While = 3,
            ElseIf = 4,
            Switch = 5,
            Case = 6,
            Do = 7,
            Else = 8,
            Label = 9,
            EndIfTrueBlock = 12,
            EndIf = 11,
            EndFor = 21,
            EndWhile = 31,
            EndDo = 41,
            EndSwitch = 51,
            Break = 90,
            Continue = 91,
            DefaultCase = 92,
            GoTo = 93,
            Return = 94,
            Throw = 95,
            Apply = 96,
            Reapply = 97,

            Try = 100,
            EndTry = 1001,
            Catch = 101,
            EndCatch = 1011,
            Finally = 102,
            EndFinally = 1021,
            CatchByName = 103,
            EndCatchByName = 1031,
            ForEach = 200,
            EndForEach = 2001
        }
        static class UnitTypes
        {
            public static UnitType[] EndFor = new UnitType[] { UnitType.EndFor };
            public static UnitType[] For = new UnitType[] { UnitType.For };
            public static UnitType[] EndForEach = new UnitType[] { UnitType.EndForEach };
            public static UnitType[] ForEach = new UnitType[] { UnitType.ForEach };
            public static UnitType[] ElseOrEndif = new UnitType[] { UnitType.ElseIf, UnitType.Else, UnitType.EndIf };
            public static UnitType[] Endif = new UnitType[] { UnitType.EndIf };
            public static UnitType[] EndWhile = new UnitType[] { UnitType.EndWhile };
            public static UnitType[] While = new UnitType[] { UnitType.While };
            public static UnitType[] Do = new UnitType[] { UnitType.Do };
            public static UnitType[] EndLoopOrEndSwitch = new UnitType[] { UnitType.EndDo, UnitType.EndWhile, UnitType.EndFor, UnitType.EndSwitch };
            public static UnitType[] EndLoop = new UnitType[] { UnitType.EndDo, UnitType.EndWhile, UnitType.EndFor };
            public static UnitType[] Case = new UnitType[] { UnitType.Case };
            public static UnitType[] EndSwitch = new UnitType[] { UnitType.EndSwitch };
            public static UnitType[] DefaultCase = new UnitType[] { UnitType.DefaultCase };
            public static UnitType[] Switch = new UnitType[] { UnitType.Switch };
            public static UnitType[] Label = new UnitType[] { UnitType.Label };
            public static UnitType[] EndFinally = new UnitType[] { UnitType.EndFinally };
            public static UnitType[] EndCatch = new UnitType[] { UnitType.EndCatch, UnitType.EndCatchByName };
            public static UnitType[] Finally = new UnitType[] { UnitType.Finally };
            public static UnitType[] Catch = new UnitType[] { UnitType.Catch };
            public static UnitType[] CatchByName = new UnitType[] { UnitType.CatchByName };
            public static UnitType[] Try = new UnitType[] { UnitType.Try };
            public static UnitType[] TryCatchFinally = new UnitType[] { UnitType.Try, UnitType.Catch, UnitType.Finally };
            public static UnitType[] EndTryEndCatchEndFinally = new UnitType[] { UnitType.EndTry, UnitType.EndCatch, UnitType.EndCatchByName, UnitType.EndFinally };
        }
        public enum OperationKind : byte
        {
            None = 0,
            ForcedAssignment = 1,
            Assignment = 2,
            AdditionAssignment = 3,
            SubtractionAssignment = 4,
            MultiplyAssignment = 5,
            DivisionAssignment = 6,
            ModulusAssignment = 7,
            BitwiseAndAssignment = 8,
            ExclusiveOrAssignment = 9,
            BitwiseOrAssignment = 10,

            AdditionForcedAssignment = 11,
            SubtractionForcedAssignment = 12,
            MultiplyForcedAssignment = 13,
            DivisionForcedAssignment = 14,
            ModulusForcedAssignment = 15,
            BitwiseAndForcedAssignment = 16,
            ExclusiveOrForcedAssignment = 17,
            BitwiseOrForcedAssignment = 18,

            LogicalNot = 20,
            OnesComplement = 21,
            Casting = 22,

            Increment = 23,
            Decrement = 24,
            Addition = 25,
            Subtraction = 26,
            Multiply = 27,
            Division = 28,
            Modulus = 29,

            LeftShift = 31,
            RightShift = 32,

            BitwiseAnd = 34,
            ExclusiveOr = 35,
            BitwiseOr = 36,

            LessThan = 41,
            LessThanOrEqual = 42,
            GreaterThan = 43,
            GreaterThanOrEqual = 44,
            Equality = 45,
            Inequality = 46,

            LogicalAnd = 48,
            LogicalOr = 49,

            Coalescing = 50,

        }
        static bool OpIsAssignment(OperationKind opKind)
        {
            return opKind >= OperationKind.ForcedAssignment && opKind <= OperationKind.BitwiseOrForcedAssignment;
        }
        public enum OperandPosition : byte
        {
            None = 0,
            Left = 1,
            Right = 2
        }
        public static bool OpIsAnyAssign(OperationKind opKind) => (opKind > 0 && opKind <= OperationKind.BitwiseOrForcedAssignment) || opKind == OperationKind.Increment || opKind == OperationKind.Decrement;

        public static DateTime NotNullDate = default(DateTime).AddSeconds(1);

        public static Type TypeOfBool = typeof(bool);
        public static Type TypeOfByte = typeof(byte);
        public static Type TypeOfShort = typeof(short);
        public static Type TypeOfInt = typeof(int);
        public static Type TypeOfLong = typeof(long);
        public static Type TypeOfFloat = typeof(float);
        public static Type TypeOfDouble = typeof(double);
        public static Type TypeOfDecimal = typeof(decimal);
        public static Type TypeOfString = typeof(string);
        public static Type TypeOfChar = typeof(char);
        public static Type TypeOfObject = typeof(object);
        public static Type TypeOfDate = typeof(DateTime);
        public static Type TypeOfCustom = typeof(CustomObject);

        public static Type TypeOfBoolArray = typeof(bool[]);
        public static Type TypeOfByteArray = typeof(byte[]);
        public static Type TypeOfShortArray = typeof(short[]);
        public static Type TypeOfIntArray = typeof(int[]);
        public static Type TypeOfLongArray = typeof(long[]);
        public static Type TypeOfFloatArray = typeof(float[]);
        public static Type TypeOfDoubleArray = typeof(double[]);
        public static Type TypeOfDecimalArray = typeof(decimal[]);
        public static Type TypeOfStringArray = typeof(string[]);
        public static Type TypeOfCharArray = typeof(char[]);
        public static Type TypeOfObjectArray = typeof(object[]);
        public static Type TypeOfDateArray = typeof(DateTime[]);
        public static Type TypeOfCustomArray = typeof(CustomObject);
        public static Type TypeOfVoid = typeof(void);

        public static class TypeStr
        {
            public const string Int = "int";
            public const string Long = "long";
            public const string String = "string";
            public const string Char = "char";
            public const string Float = "float";
            public const string Double = "double";
            public const string Decimal = "decimal";
            public const string Bool = "bool";
            public const string Object = "object";
            public const string Short = "short";
            public const string Byte = "byte";
            public const string DateTime = "date";
            public const string Void = "void";

            public const string IntArray = "int[]";
            public const string LongArray = "long[]";
            public const string StringArray = "string[]";
            public const string CharArray = "char[]";
            public const string FloatArray = "float[]";
            public const string DoubleArray = "double[]";
            public const string DecimalArray = "decimal[]";
            public const string BoolArray = "bool[]";
            public const string ObjectArray = "object[]";
            public const string ShortArray = "short[]";
            public const string ByteArray = "byte[]";
            public const string DateTimeArray = "date[]";

        }
        public static Dictionary<Type, TypeID> TypeIds = new Dictionary<Type, TypeID>()
            {
                { TypeOfBool,TypeID.Bool},
                { TypeOfByte,TypeID.Byte},
                { TypeOfShort,TypeID.Short},
                { TypeOfInt,TypeID.Int},
                { TypeOfLong,TypeID.Long},
                { TypeOfFloat,TypeID.Float},
                { TypeOfDouble,TypeID.Double},
                { TypeOfDecimal,TypeID.Decimal},
                { TypeOfString,TypeID.String},
                { TypeOfChar,TypeID.Char},
                { TypeOfObject,TypeID.Object},
                { TypeOfDate,TypeID.Date},
                { TypeOfCustom,TypeID.Custom},

                { TypeOfBoolArray,TypeID.BoolArray},
                { TypeOfByteArray,TypeID.ByteArray},
                { TypeOfShortArray,TypeID.ShortArray},
                { TypeOfIntArray,TypeID.IntArray},
                { TypeOfLongArray,TypeID.LongArray},
                { TypeOfFloatArray,TypeID.FloatArray},
                { TypeOfDoubleArray,TypeID.DoubleArray},
                { TypeOfDecimalArray,TypeID.DecimalArray},
                { TypeOfStringArray,TypeID.StringArray},
                { TypeOfCharArray,TypeID.CharArray},
                { TypeOfObjectArray,TypeID.ObjectArray},
                { TypeOfDateArray,TypeID.DateArray},
                { typeof(CustomObject[]),TypeID.CustomArray},

                { TypeOfVoid,TypeID.Void}

         };

        [Flags]
        public enum TypeFlag
        {
            None = 0,
            Bool = 1 << 0,
            Byte = 1 << 1,
            Short = 1 << 2,
            Int = 1 << 3,
            Long = 1 << 4,
            Float = 1 << 5,
            Double = 1 << 6,
            Decimal = 1 << 7,
            String = 1 << 8,
            Char = 1 << 9,
            Object = 1 << 10,
            Date = 1 << 11,
            Custom = 1 << 12,

            BoolArray = 1 << 13,
            ByteArray = 1 << 14,
            ShortArray = 1 << 15,
            IntArray = 1 << 16,
            LongArray = 1 << 17,
            FloatArray = 1 << 18,
            DoubleArray = 1 << 19,
            DecimalArray = 1 << 20,
            StringArray = 1 << 21,
            CharArray = 1 << 22,
            ObjectArray = 1 << 23,
            DateArray = 1 << 24,
            CustomArray = 1 << 25
        }

        public static Type[] Types = ((TypeID[])Enum.GetValues(typeof(TypeID))).Select(x => TypeIds.FirstOrDefault(y => y.Value == x).Key).ToArray();


        public static string GetTypeName(TypeID type) => type == TypeID.Custom || type == TypeID.CustomArray ? type.ToString() : BasicTypes.Where(x => x.Value.ID == type).FirstOrDefault().Key;



        public static VarType GetVarTypeByID(TypeID type) => type >= TypeID.Type || type == TypeID.Custom || type == TypeID.CustomArray ? new VarType(type) : BasicTypes.Where(x => x.Value.ID == type).FirstOrDefault().Value;

        public static Dictionary<string, VarType> BasicTypes = new Dictionary<string, VarType>() {
            { TypeStr.Int,new VarType( TypeID.Int) },
            { TypeStr.Long, new VarType( TypeID.Long) },
            { TypeStr.String, new VarType( TypeID.String) },
            { TypeStr.Char,new VarType( TypeID.Char) },
            { TypeStr.Float, new VarType(TypeID.Float) },
            { TypeStr.Double, new VarType(TypeID.Double) },
            { TypeStr.Decimal, new VarType( TypeID.Decimal)},
            { TypeStr.Bool,  new VarType(TypeID.Bool)},
            { TypeStr.Object, new VarType(TypeID.Object)},
            { TypeStr.Short, new VarType(TypeID.Short)},
            { TypeStr.Byte, new VarType(  TypeID.Byte)},
            { TypeStr.DateTime, new VarType( TypeID.Date)},

            { TypeStr.IntArray, new VarType(  TypeID.IntArray)},
            { TypeStr.ObjectArray, new VarType(TypeID.ObjectArray)},
            { TypeStr.StringArray, new VarType( TypeID.StringArray)},
            { TypeStr.CharArray, new VarType(TypeID.CharArray )},
            { TypeStr.LongArray,  new VarType(TypeID.LongArray)},
            { TypeStr.DoubleArray, new VarType( TypeID.DoubleArray)},
            { TypeStr.FloatArray, new VarType( TypeID.FloatArray)},
            { TypeStr.DecimalArray, new VarType( TypeID.DecimalArray)},
            { TypeStr.BoolArray, new VarType( TypeID.BoolArray)},
            { TypeStr.ShortArray,new VarType( TypeID.ShortArray )},
            { TypeStr.ByteArray, new VarType( TypeID.ByteArray )},
            { TypeStr.DateTimeArray, new VarType(  TypeID.DateArray)},
            { TypeStr.Void, new VarType( TypeID.Void)},

        };

        public static string[] BasicTypeNames = BasicTypes.Keys.ToArray();
        public static bool IsBasic(string str) => Array.IndexOf(BasicTypeNames, str) >= 0;

        public bool IsCustom(string str, out object obj)
        {
            bool isArray = false;
            if (str.EndsWith("[]")) { str = str.Remove(str.Length - 2, 2); isArray = true; }
            var c = GetClassByName(str, this);
            if (c != null) obj = CustomType.Get(c, isArray); else obj = null;
            return obj != null;

        }

        public static TypeID[] TypesByPriority = { TypeID.Object, TypeID.Bool, TypeID.Byte , TypeID.Short , TypeID.Char , TypeID.Int, TypeID.Long,
        TypeID.Float,TypeID.Double,TypeID.Decimal, TypeID.Date, TypeID.String, TypeID.Custom};

        public static TypeID GetMaxType(TypeID t1, TypeID t2) => Array.IndexOf(TypesByPriority, t1) > Array.IndexOf(TypesByPriority, t2) ? t1 : t2;


        static char[] Brackets = { '[', '(', '{' };

        static bool TypeIdIsType(TypeID id) => id >= TypeID.BoolType;
        public static bool TypeIsArray(TypeID t) => t >= TypeID.BoolArray;
        public static Type GetTypeByID(TypeID id) => Types[(byte)id];
        public static TypeID GetTypeID(Type type, bool noException = false)
        {
            TypeID result;
            bool exists = TypeIds.TryGetValue(type, out result);
            if (!exists && !noException)
                throw new ScriptExecutionException($"Type '{type}' not found.");

            return result;
        }
        public static TypeID SubTypeIdToTypeId(TypeID tid)
        {
            string type = tid.ToString();
            type = type.Remove(type.Length - 4);
            var en = Enum.Parse(typeof(TypeID), type);
            return (TypeID)en;
        }
        public static TypeID GetArrayTypeId(TypeID tid)
        {
            string type = tid.ToString() + "Array";

            var en = Enum.Parse(typeof(TypeID), type);
            return (TypeID)en;
        }
        public static TypeID GetElementType(TypeID type) => GetTypeID(GetTypeByID(type).GetElementType());
        public static bool TypeIsNumeric(TypeID type) => type == TypeID.Byte || type == TypeID.Short || type == TypeID.Int || type == TypeID.Long || type == TypeID.Decimal || type == TypeID.Float || type == TypeID.Double;

        public VarType GetElementVarType(VarType vt)
        {
            if (vt.ID == TypeID.CustomArray)
                vt = CurScript.AllTypes[vt.CType.Class.ClassFullName];
            else
            {
                var hint = vt.TypeHint;
                vt = GetVarTypeByID(GetElementType(vt.ID));

                if (hint != null)
                    vt.AddHint(hint.GetElementType());
            }
            return vt;
        }

        public static Dictionary<TypeID, int> TypeOrder = TypeIds.ToDictionary(x => x.Value, x => Array.IndexOf(TypesByPriority, x.Value));



        public static string[] ExceptionVarNames = { ExceptionVarName.TypeName, ExceptionVarName.Message, ExceptionVarName.CustomExObj, ExceptionVarName.Object };
        public class ExceptionVarID
        {
            public int TypeName;
            public int Message;
            public int CustomExObj;
            public int Object;


            public int NameVarInCustomExClass;
            public int MessageVarInCustomExClass;

            public ExceptionVarID(Script script)
            {
                TypeName = script.GetVarID<string>(ExceptionVarName.TypeName);
                Message = script.GetVarID<string>(ExceptionVarName.Message);
                CustomExObj = script.GetVarID<object>(ExceptionVarName.CustomExObj);
                Object = script.GetVarID<object>(ExceptionVarName.Object);


                NameVarInCustomExClass = script.GetVarID<string>("Name");
                MessageVarInCustomExClass = script.GetVarID<string>("Message");
            }
        }
        public class Enumerator : IEnumerator, IDisposable
        {
            Func<bool> MoveNextFn;
            Func<object> CurrentFn;
            Action ResetFn, DisposeFn;

            public Enumerator(Func<bool> moveNextFn, Func<object> currentFn, Action resetFn, Action disposeFn)
            {
                MoveNextFn = moveNextFn;
                CurrentFn = currentFn;
                ResetFn = resetFn;
                DisposeFn = disposeFn;
            }

            public object Current => CurrentFn();
            public bool MoveNext() => MoveNextFn();
            public void Reset() => ResetFn();
            public void Dispose()
            {
                if (DisposeFn != null) DisposeFn();
            }
        }




        private HashSet<string> FuncNames, PublicNonStaticFuncNames, PublicStaticFuncNames;
        public List<ScriptClass> SubClasses = new List<ScriptClass>();
        private List<ScriptFunction> Functions = new List<ScriptFunction>();
        public string ClassName;
        public string ClassFullName;
        public ScriptClass OuterClass;


        public int ID;
        public FuncToCall<object>[] InstanceFuncs;
        public FuncToCall<object> CtorFunc;
        public FuncToCall<string> ToStringFunc;
        public FuncToCall<string> ArrayToStringFunc;
        public FuncToCall<int> GetHashCodeFunc;
        public FuncToCall<bool> EqualsFunc;
        public FuncToCall<int> CompareToFunc;
        public FuncToCall<object> DisposeFunc;
        public FuncToCall<object> GetEnumeratorFunc;
        public FuncToCall<bool> EnumeratorMoveNextFunc;
        public FuncToCall<object> EnumeratorCurrentFunc;
        public FuncToCall<object> EnumeratorResetFunc;

        public FuncToCall<object>[] StaticFuncs;
        public FuncToCall<object>[] FinalizeFuncs;
        public Dictionary<ScriptFunction, int> FnLayers = new Dictionary<ScriptFunction, int>();
        public List<VarName> InstVars = new List<VarName>();
        public List<VarName> StatVars = new List<VarName>();

        public VarsToClear ForCleaning;
        public List<ScriptClass> InheritedClasses = new List<ScriptClass>();

        public List<string> PublicVars = new List<string>();
        public bool HasCtors;
        public bool IsPublic, IsStatic, IsSealed;
        public string Code;
        public static ScriptClass RootClass;
        public List<(string code, ScriptClass sc, bool? staticFunc)> FnCodes = new List<(string code, ScriptClass sc, bool? staticFunc)>();
     
        public Script CurScript;

   
        public ScriptClass(string code, string name, ScriptClass outerClass, Script script)
        {
            bool isRoot = outerClass == null;
            OuterClass = outerClass;
            ID = script.ClassCount++;
            CurScript = script;

#if EXON
            try
            {
#endif

                if (isRoot)
                    RootClass = this;

                string inhs = null;
                int i = name.IndexOf(':');
                if (i > 0)
                {
                    inhs = name.Substring(i + 2);
                    name = name.Substring(0, i);

                }

                if (!CheckCharsInVarName(name))
                    throw new ScriptLoadingException($"Invalid class name '{name}'. A valid name starts with a letter or underscore, followed by any number of letters, numbers, or underscores.");

                ClassName = name;


                ClassFullName = GetFullName();


                if (inhs != null) AddInhs(inhs);

                if (isRoot)
                {
                    PrepareCode(ref code, CurScript);
                    code = UnitSeparator + code;

                }

                GetSubClasses(ref code);
                Code = code;


#if EXON
            }




            catch (Exception ex)
            {


                throw new ScriptLoadingException($"Class '{name}' loading error. " + ex.Message);
            }
#endif
            if (isRoot)
            {

                GetTypeLiterals();
                AddCustomTypes();

                GetClassFuncCodes();
                GetClassStaticFuncs();
                GetClassConstantTypes();
                GetConstants();

                GetClassFuncs();

                GetConstants(false, true);

                CopyInheritedConsts();

                CopyInheritedFunctions();
                CheckOverrides();
                SetFnLayers();
                GetFuncNames();
                GetCtors();



                ReverseClasses();
                GetClassVarTypes();

                BuildClass();
                GetVarsToClear();
                TrimLit();


            }

        }

        public override string ToString() => $"ScriptClass {ClassFullName}";
        string GetFullName() => OuterClass == null ? ClassName : OuterClass.ClassFullName + "." + ClassName;
        void AddInhs(string inhs)
        {
            string[] inhClasses = inhs.Split(',');
            ScriptClass inh;
            for (int j = 0; j < inhClasses.Length; j++)
            {

                inh = GetClassByName(inhClasses[j], OuterClass, true);
                if (inh == null) throw new ScriptLoadingException($"Couldn't find class '{inhClasses[j]}' to inherit.");
                if (inh.IsSealed) throw new ScriptLoadingException($"Can't inherit from sealed class '{inhClasses[j]}'.");
                InheritedClasses.Add(inh);
                AddSubClasses(inh.SubClasses);

            }

            InheritedClasses.Reverse();
        }
        private void CopyInheritedConsts()
        {

            var d = new Dictionary<string, int>();
            foreach (var c in Constants)
                d.Add(c.Key, c.Value);

            Constants.Clear();





            for (int i = InheritedClasses.Count - 1; i >= 0; i--)
                CopyConsts(InheritedClasses[i]);


            foreach (var c in d)
                Constants[c.Key] = c.Value;

            foreach (var c in SubClasses)
                c.CopyInheritedConsts();

        }
        void CopyConsts(ScriptClass sc)
        {

            if (sc.Constants != null)
            {
                foreach (var c in sc.Constants)
                    Constants[c.Key] = c.Value;

            }

            foreach (var c in sc.StaticVarTypes)
                StaticVarTypes[c.Key] = c.Value;

            foreach (var c in sc.PublicVars)
                if (!PublicVars.Contains(c)) PublicVars.Add(c);
        }


        public void GetTypeLiterals()
        {
            foreach (var c in SubClasses)
                c.GetTypeLiterals();

            ExtractTypeLiterals(ref Code);

        }
        public void AddCustomTypes()
        {
            string className;

            className = ClassFullName;
            if (!CurScript.AllTypes.ContainsKey(className))
            {
                CurScript.AllTypes.Add(className, new VarType(TypeID.Custom, CustomType.Get(this)));
                className += "[]";
                CurScript.AllTypes.Add(className, new VarType(TypeID.CustomArray, CustomType.Get(this, true)));
            }

            foreach (var c in SubClasses)
                c.AddCustomTypes();
        }
        private void GetCtors()
        {
            EvalUnit[] args = null;
            ScriptClass sc = this;
            HasCtors = sc.FuncNames.Contains("New");
            InstanceFuncs = sc.GetServiceFuncs<object>(InstanceFuncName, args);
            StaticFuncs = sc.GetServiceFuncs<object>(StaticFuncName, args);
            CtorFunc = sc.GetFunc<object>(CtorFuncName, args, true);
            FinalizeFuncs = sc.GetServiceFuncs<object>(FinalizeFuncName, args);
            ToStringFunc = sc.GetFunc<string>(ToStringFuncName, args, true, null, false, TypeID.String, 0);
            GetHashCodeFunc = sc.GetFunc<int>(GetHashCodeFuncName, args, true, null, false, TypeID.Int, 0);
            DisposeFunc = sc.GetFunc<object>(DisposeFuncName, args, true, null, false, TypeID.Void, 0);

            GetEnumeratorFunc = sc.GetFunc<object>(GetEnumeratorFuncName, args, true, null, false, TypeID.Object, 0);
            EnumeratorMoveNextFunc = sc.GetFunc<bool>(EnumeratorMoveNextFuncName, args, true, null, false, TypeID.Bool, 0);
            EnumeratorResetFunc = sc.GetFunc<object>(EnumeratorResetFuncName, args, true, null, false, TypeID.Void, 0);
            EnumeratorCurrentFunc = sc.GetFunc<object>(EnumeratorCurrentFuncName, args, true, null, false, TypeID.Object, 0);

            EvalUnit arg = new EvalUnit();
            arg.Type = GetVarTypeByID(TypeID.CustomArray);
            arg.Kind = EvalUnitKind.Variable;
            args = new EvalUnit[] { arg };
            ArrayToStringFunc = sc.GetFunc<string>(ArrayToStringFuncName, args, true, null, true, TypeID.String, 1);


            args[0].Type = GetVarTypeByID(TypeID.Object);
            CompareToFunc = sc.GetFunc<int>(CompareToFuncName, args, true, null, false, TypeID.Int, 1);


            EqualsFunc = sc.GetFunc<bool>(EqualsFuncName, args, true, null, false, TypeID.Bool, 1);



            foreach (var c in SubClasses) c.GetCtors();
        }
        private void AddFunctions(List<ScriptFunction> funcs)
        {
            foreach (var f in funcs)
            {
                if (f.IsExclusive) continue;
                var fn = f.IsFixed ? f : f.ShallowCopy();


                Functions.Add(fn);
            }
        }

        private void AddSubClasses(List<ScriptClass> classes)
        {
            foreach (var sc in classes)
                SubClasses.Add(sc);

        }
        private void GetConstants(bool withoutSubClasses = false, bool all = false)
        {
            if (all || Constants == null)
            {
                Constants = Constants ?? new Dictionary<string, int>();
                foreach (var fn in Functions)
                {

                    bool isStatic = fn.IsStaticFunc;


                    GetConsts(fn, isStatic ? StaticVarTypes : fn.VarTypes, isStatic ? Constants : fn.Constants, isStatic ? PublicVars : null);
                }
            }

            if (!withoutSubClasses)
            {
                foreach (var c in SubClasses) c.GetConstants(withoutSubClasses, all);
            }

        }
        private void GetClassConstantTypes()
        {

            foreach (var c in SubClasses) c.GetClassConstantTypes();

            foreach (var fn in Functions)
            {
                if (fn.IsStaticFunc)
                    GetConsts(fn, StaticVarTypes, Constants, PublicVars, true);


            }

        }




        private void GetFuncNames()
        {
            FuncNames = Functions.Select(x => x.Name).ToHashSet();
            PublicNonStaticFuncNames = Functions.Where(x => x.IsPublic && !x.IsStatic).Select(x => x.Name).ToHashSet();
            PublicStaticFuncNames = Functions.Where(x => x.IsPublic && x.IsStatic).Select(x => x.Name).ToHashSet();
        

            foreach (var c in SubClasses) c.GetFuncNames();
        }
        private void ReverseClasses()
        {

            SubClasses.Reverse();

            foreach (var c in SubClasses) c.ReverseClasses();
        }
        private void CopyInheritedFunctions()
        {
            foreach (var c in InheritedClasses)
                AddFunctions(c.Functions);

            foreach (var c in SubClasses)
                c.CopyInheritedFunctions();
        }
        private void GetClassStaticFuncs()
        {
            foreach (var c in SubClasses)
                c.GetClassStaticFuncs();

            GetFunctions(true);



        }
        private void GetClassFuncs()
        {
            foreach (var c in SubClasses)
                c.GetClassFuncs();

            GetFunctions(false);

            Functions.Reverse();
            foreach (var f in Functions) f.CheckForInconsistentAccessibility();

            FnCodes = null;
        }
        private void GetClassFuncCodes()
        {
            foreach (var c in SubClasses)
                c.GetClassFuncCodes();

            GetClassFuncCodes(ref Code);



        }
        void GetFunctions(bool staticFn = false)
        {
            ScriptFunction newFn;
            if (staticFn)
            {
                foreach (var fnCode in FnCodes.Where(x => x.staticFunc == true))
                {
                    newFn = new ScriptFunction($"{StaticFuncName}(){{{fnCode.code}}}", this);
                    Functions.Add(newFn);
                }
            }
            else
            {

                foreach (var fnCode in FnCodes.Where(x => x.staticFunc == false))
                {
                    newFn = new ScriptFunction($"{InstanceFuncName}(){{{fnCode.code}}}", this);
                    Functions.Add(newFn);
                }
                foreach (var fnCode in FnCodes.Where(x => x.staticFunc == null))
                {
                    newFn = new ScriptFunction(fnCode.code, this);
                    if (IsStatic && !newFn.IsStatic) throw new ScriptLoadingException($"Static class '{ClassFullName}' can't contain an instance function '{newFn.Name}'.");
                    Functions.Add(newFn);
                }
            }
        }

        private void GetClassVarTypes()
        {
            foreach (var c in SubClasses)
                c.GetClassVarTypes();

            GetVarTypes();
        }


        private void BuildClass()
        {


            foreach (var f in Functions) BuildFunc(f);

            ExprTypeCache = null;
            EUCache = null;

            foreach (var c in SubClasses) c.BuildClass();

        }
        private void GetVarsToClear()
        {
            ForCleaning = GetForCleaning(VarTypes);
            foreach (var fn in Functions)
                if (!fn.IsInstanceOrStaticFunc) fn.ForCleaning = GetForCleaning(fn.VarTypes.Skip(ScriptFunction.DefaultVarTypeCount).Where(x => x.Key != ThisStr && fn.Params.Where(y => y.ByRef && y.ParamName == x.Key).FirstOrDefault() == null).ToDictionary(x => x.Key, x => x.Value));

            foreach (var c in SubClasses) c.GetVarsToClear();

        }
        private void GetSubClasses(ref string code)
        {

            int l = ClassSign.Length + 1;
            string cs = UnitSeparator + ClassSign + " ";
            int i2, i3, i = code.IndexOf(cs);
            string classCode = "", className = "";
            while (i >= 0)
            {
                i2 = code.IndexOf('{', i);
                i3 = FindClosing(code, i, "{", "}");
                classCode = code.Substring(i2 + 1, i3 - (i2 + 1));
                classCode = classCode.Trim(UnitSeparator[0]);
                if (!classCode.StartsWith(CodeLocationChar))
                {
                    var lm = GetLocationMark(CurScript, code.Substring(0, i2));
                    if (lm != null)
                        classCode = ToLocationMarkStr(lm.CFile.Num, lm.Line2) + classCode;
                }
                className = code.Substring(i + l, i2 - (i + l));
                className = RemLocationMarks(className).TrimEnd();
                classCode = UnitSeparator[0] + classCode;
                var newClass = new ScriptClass(classCode, className, this, CurScript);
                SubClasses.Add(newClass);

                i--;
                if (code[i] != UnitSeparator[0])
                {
                    i = code.LastIndexOf(UnitSeparator[0], i);
                    string h = code.Substring(i + 1, i2 - (i + 1));
                    h = RemLocationMarks(h);
                    bool isFixed = false, IsVirtual = false, IsOverride = false, IsExclusive = false, IsPrivate = false;
                    GetModifiers(ref h, ref newClass.IsPublic, ref newClass.IsStatic, ref newClass.IsSealed, ref isFixed, ref IsVirtual, ref IsOverride, ref IsExclusive, ref IsPrivate);
                    if (isFixed) throw new ScriptLoadingException("Modifier 'fixed' not supported for classes.");
                    if (IsVirtual) throw new ScriptLoadingException("Modifier 'virtual' not supported for classes.");
                    if (IsOverride) throw new ScriptLoadingException("Modifier 'override' not supported for classes.");
                    if (IsExclusive) throw new ScriptLoadingException("Modifier 'exclusive' not supported for classes.");
                }

                code = code.Remove(i, i3 + 1 - i);

                i = code.IndexOf(cs, i);
            }
        }

        static string[] Modifiers = { "public ", "static ", "sealed ", "fixed ", "virtual ", "override ", "private ", "exclusive " };
        static void GetModifiers(ref string h, ref bool IsPublic, ref bool IsStatic, ref bool IsSealed, ref bool IsFixed, ref bool IsVirtual, ref bool IsOverride, ref bool IsExclusive, ref bool IsPrivate)
        {
            bool redo;
            do
            {
                redo = false;
                for (int i = 0; i < Modifiers.Length; i++)
                {
                    string m = Modifiers[i];
                    if (h.StartsWith(m))
                    {
                        if (m == "public ") IsPublic = true;
                        else if (m == "static ") IsStatic = true;
                        else if (m == "sealed ") IsSealed = true;
                        else if (m == "fixed ") IsFixed = true;
                        else if (m == "virtual ") IsVirtual = true;
                        else if (m == "override ") IsOverride = true;
                        else if (m == "private ") { IsPublic = false; IsPrivate = true; }
                        else if (m == "exclusive ") IsExclusive = true;

                        h = h.Remove(0, m.Length);
                        redo = true;
                    }
                }
            } while (redo);

        }

        public static bool CanCovert(Type fromType, Type toType)
        {
            var converter = System.ComponentModel.TypeDescriptor.GetConverter(fromType);
            return converter.CanConvertTo(toType);
        }


        private void GetClassFuncCodes(ref string code)
        {

            int i2, i3, i = code.IndexOf("){");
            string funcCode = "", funcHead = "";
            while (i >= 0)
            {
                i2 = code.LastIndexOf(UnitSeparator, i, StringComparison.Ordinal);
                funcHead = code.Substring(i2 + 1, i + 1 - (i2 + 1));

                if (!funcHead.StartsWith("if(") && !funcHead.StartsWith("elseif(") && !funcHead.StartsWith("for(") && !funcHead.StartsWith("foreach(") && !funcHead.StartsWith("while(") && !funcHead.StartsWith("switch(") && !funcHead.StartsWith("catch(") && !funcHead.StartsWith("catch$(") && !funcHead.StartsWith("lock(") && !funcHead.StartsWith("using("))
                {
                    i3 = FindClosing(code, i, "{", "}");
                    funcCode = code.Substring(i2 + 1, i3 - i2);

                    funcCode = funcCode.Trim(UnitSeparator[0]);
                    if (!funcCode.StartsWith(CodeLocationChar))
                    {
                        var lm = GetLocationMark(CurScript, code.Substring(0, i2));

                        if (lm != null)
                            funcCode = ToLocationMarkStr(lm.CFile.Num, lm.Line2) + funcCode;

                    }
                    else

                        i2 = SkipLocationMarks(code, i2 + 1);

                    FnCodes.Add((funcCode, this, null));


                    code = code.Remove(i2, i3 + 1 - i2);
                }
                else { i2 = i + 1; }

                i = code.IndexOf("){", i2);

            }

            if (RemLocationMarks(code).Trim(UnitSeparator[0]).Length > 0)
            {
                int i0, l;
                string staticCode = "";
                string[] find = { UnitSeparator + "const ", UnitSeparator + "static " };
                int n = 0;
                foreach (string fs in find)
                {
                    l = n++ == 0 ? 0 : fs.Length - 1;
                    i = code.IndexOf(fs);

                    while (i >= 0)
                    {

                        var pc = code[i - 1];
                        if (pc == ' ' || pc == CodeLocationChar || pc == UnitSeparator[0])
                        {
                            i2 = code.IndexOf(';', i);
                            if (i2 < 0) break;
                            i0 = i;
                            i--;
                            if (code[i] != UnitSeparator[0]) i = code.LastIndexOf(UnitSeparator[0], i);
                            string s = code.Substring(i, i0 - i) + code.Substring(i0 + l, i2 + 1 - (i0 + l));
                            s = s.TrimStart(UnitSeparator[0]);
                            if (!s.StartsWith(CodeLocationChar))
                            {
                                var lm = GetLocationMark(CurScript, code.Substring(0, i));
                                if (lm != null)
                                    s = ToLocationMarkStr(lm.CFile.Num, lm.Line2) + s;

                            }

                            staticCode += s + UnitSeparator;

                            code = code.Remove(i, i2 + 1 - i);

                        }
                        i = code.IndexOf(fs, i);
                    }
                }
                if (staticCode.Length > 0)
                {


                    FnCodes.Add((staticCode, this, true));
                }

                if (RemLocationMarks(code).Trim(UnitSeparator[0]).Length > 0)
                {
                    if (IsStatic) throw new ScriptLoadingException($"Static class '{ClassFullName}' can't contain an instance members.");


                    FnCodes.Add((code, this, false));
                }
            }



        }
        void GetVarTypes()
        {
            ScriptFunction[] f = Functions.Where(x => x.IsInstanceOrStaticFunc).ToArray();
            for (int j = f.Length - 1; j >= 0; j--)
            {
                if (f[j].Units == null) f[j].Units = ParseCode(f[j]);

                if (!f[j].IsStaticFunc)
                    AddVarTypes(f[j], VarTypes, Constants, PublicVars);
                else
                    AddVarTypes(f[j], StaticVarTypes, Constants, PublicVars);
            }


            foreach (var v in VarTypes)
                InstVars.Add(new VarName(CurScript, v.Key, v.Value, PublicVars.Contains(v.Key)));

            foreach (var v in StaticVarTypes)
                StatVars.Add(new VarName(CurScript, v.Key, v.Value, PublicVars.Contains(v.Key)));
        }
        private void BuildFunc(ScriptFunction fn)
        {

            if (fn.Compiled || (fn.IsFixed && fn.Class != this)) return;


            if (!fn.IsInstanceOrStaticFunc)
            {
                fn.Units = ParseCode(fn);
                AddVarTypes(fn, fn.VarTypes, fn.Constants);

            }

            BuildEvalUnits(fn);
            CalcJumps(fn);

            fn.Compiled = true;
        }

        void SetFnLayers()
        {
            foreach (var fn in Functions)
                FnLayers[fn] = Array.IndexOf(Functions.Where(x => x.Signature == fn.Signature).ToArray(), fn);

            foreach (var c in SubClasses)
                c.SetFnLayers();
        }



        public static (string file, string[] args) GetFileAndArgs(string str)
        {
            var strVals = new List<string>();

            char[] q = { '"', '\'' };
            int i = str.IndexOfAny(q);
            while (i >= 0)
            {
                int i2 = FindEndOfLiteral(str, i, true, str[i]);
                if (i2 < 0) throw new FormatException("Invalid command line format.");
                string v = str.Substring(i + 1, i2 - (i + 1));

                if (str[i] == '"')
                    v = System.Text.RegularExpressions.Regex.Unescape(v);


                strVals.Add(v);

                str = str.Remove(i, i2 + 1 - i).Insert(i, $"{LiteralMark}{(strVals.Count - 1)}{LiteralMark}");

                i = str.IndexOfAny(q, i);
            }

            string[] s = str.Split(' ').Where(x => x.Length > 0).ToArray();
            for (i = 0; i < s.Length; i++)
            {
                string item = s[i];
                int j = item.IndexOf(LiteralMark);
                while (j >= 0)
                {
                    int j2 = item.IndexOf(LiteralMark, j + 1);
                    int n = int.Parse(item.Substring(j + 1, j2 - (j + 1)));
                    item = item.Remove(j, j2 + 1 - j).Insert(j, strVals[n]);

                    j = item.IndexOf(LiteralMark, j);
                }
                s[i] = item;
            }


            string file = System.IO.Path.GetFullPath(s[0]);
            string[] args = s.Skip(1).ToArray();
            return (file, args);
        }


        public Dictionary<string, VarType> VarTypes = new Dictionary<string, VarType>();
        public Dictionary<string, VarType> StaticVarTypes = new Dictionary<string, VarType>();
        public Dictionary<string, int> Constants = null;


        public const int MaxTypeId = (int)TypeID.CustomArray;
        public class VarsToClear
        {
            public int[][] VarIDs = new int[MaxTypeId + 1][];

            public TypeFlag UsedTypes = TypeFlag.None;
            public int UsedTypeCount = 0;

            public bool StringUsed, ObjectUsed, CustomUsed, IntArrayUsed, StringArrayUsed, ByteArrayUsed, LongArrayUsed, DoubleArrayUsed, FloatArrayUsed, BoolArrayUsed, DecimalArrayUsed, CharArrayUsed, ShortArrayUsed, DateArrayUsed, ObjectArrayUsed;
            public void SetUsedTypes()
            {

                StringUsed = (UsedTypes & TypeFlag.String) != 0;
                ObjectUsed = (UsedTypes & TypeFlag.Object) != 0;
                CustomUsed = (UsedTypes & TypeFlag.Custom) != 0;
                IntArrayUsed = (UsedTypes & TypeFlag.IntArray) != 0;
                StringArrayUsed = (UsedTypes & TypeFlag.StringArray) != 0;
                ByteArrayUsed = (UsedTypes & TypeFlag.ByteArray) != 0;
                LongArrayUsed = (UsedTypes & TypeFlag.LongArray) != 0;
                DoubleArrayUsed = (UsedTypes & TypeFlag.DoubleArray) != 0;
                FloatArrayUsed = (UsedTypes & TypeFlag.FloatArray) != 0;
                BoolArrayUsed = (UsedTypes & TypeFlag.BoolArray) != 0;
                DecimalArrayUsed = (UsedTypes & TypeFlag.DecimalArray) != 0;
                CharArrayUsed = (UsedTypes & TypeFlag.CharArray) != 0;
                ShortArrayUsed = (UsedTypes & TypeFlag.ShortArray) != 0;
                DateArrayUsed = (UsedTypes & TypeFlag.DateArray) != 0;
                ObjectArrayUsed = (UsedTypes & TypeFlag.ObjectArray) != 0;

                UsedTypeCount = System.Numerics.BitOperations.PopCount((ulong)UsedTypes);
            }
        }
        public class ScriptFunction
        {
            public string Name;
            public VarType ReturnType;
            public string TypeName;
            public FuncParam[] Params;
            public string Signature;
            public CodeUnit[] Units;
            public string Code;
            public Dictionary<string, VarType> VarTypes = new Dictionary<string, VarType>();
            public Dictionary<string, int> Constants = new Dictionary<string, int>();


            public bool IsInstanceFunc, IsStaticFunc, IsFinalizeFunc, IsNewFunc, IsDisposeFunc, IsInstanceOrStaticFunc;

            public bool Compiled;
            public ScriptClass Class;
            public VarsToClear ForCleaning;
            public LocMark LocationMark;

            public bool IsPublic, IsStatic, IsSealed, IsFixed, IsVirtual, IsOverride, IsExclusive;

            public int RefCount = 0;
            public string CodeFile, BasePath;
            public int ForEachCount = 0, ReapplyCount = 0;
            public bool HasParams;
            public int ParamsIndex = -1;
            public VarType ParamsType;
            public int MinArgNum = 0;
            public ScriptFunction OrigFn;

            public class FuncParam
            {
                public string ParamName;
                public string ParamTypeName;
                public VarType ParamType;
                public int VarId;
                public bool ByRef;
                public bool Optional;
                public FuncParam(string name, string typeName, VarType type, bool byRef, bool optional, ScriptClass location)
                {
                    ParamName = name;
                    ParamTypeName = typeName;
                    ParamType = type;
                    VarId = location.CurScript.GetVarID(name, type.ID);
                    ByRef = byRef;
                    Optional = optional;
                }

                public FuncParam ShallowCopy()
                {
                    var fp = (FuncParam)this.MemberwiseClone();
                    return fp;
                }
            }

            public ScriptFunction ShallowCopy()
            {
                var f = (ScriptFunction)this.MemberwiseClone();

                return f;
            }
            public void CheckForInconsistentAccessibility()
            {
                if (ReturnType.ID == TypeID.Custom || ReturnType.ID == TypeID.CustomArray)
                {
                    string t = TypeName;
                    if (t.EndsWith("[]")) t = t.Remove(t.Length - 2, 2);
                    var c = GetClassByNameOrException(t, this.Class);
                    if (InconsistentAccessibility(this, c)) throw new ScriptLoadingException($"Inconsistent accessibility. Return type '{TypeName}' is less accessible than function '{Name}' at '{this.Class.ClassFullName}'.");
                }
            }

            public const int DefaultVarTypeCount = 4;
            void AddDefaultVars()
            {

                VarTypes.Clear();
                VarTypes.Add(ExceptionVarName.Object, new VarType(TypeID.Object));
                VarTypes.Add(ExceptionVarName.TypeName, new VarType(TypeID.String));
                VarTypes.Add(ExceptionVarName.Message, new VarType(TypeID.String));
                VarTypes.Add(ExceptionVarName.CustomExObj, new VarType(TypeID.Object));
            }
            public override string ToString() => Signature;



            public ScriptFunction(string code, ScriptClass location)
            {
                string h = "", h2 = "";
                Script script = location.CurScript;

#if EXON
                try
                {
#endif
                    OrigFn = this;
                    Class = location;

                    if (code.StartsWith(CodeLocationChar))
                        LocationMark = GetLocationMark(script, code, true);


                    int i = code.IndexOf('(');

                    h = code.Substring(0, i);
                    h = RemLocationMarks(h);

                    int i2 = FindClosing(code, i, "(", ")");
                    h2 = code.Substring(i + 1, i2 - (i + 1));
                    h2 = RemLocationMarks(h2);
                    bool isPrivate = false;
                    GetModifiers(ref h, ref IsPublic, ref IsStatic, ref IsSealed, ref IsFixed, ref IsVirtual, ref IsOverride, ref IsExclusive, ref isPrivate);


                    string[] p = h.Split(' ');

                    if (p.Length > 1)
                    {
                        Name = p[1];
                        TypeName = p[0];

                        ReturnType = Class.GetTypeFromDict(TypeName, Class);
                    }
                    else
                    {
                        Name = p[0];
                        TypeName = TypeStr.Void;
                        ReturnType = script.AllTypes[TypeName];

                    }

                    if (!CheckCharsInVarName(Name))
                        throw new ScriptLoadingException($"Invalid function name '{Name}'. A valid name starts with a letter or underscore, followed by any number of letters, numbers, or underscores.");

                    IsInstanceFunc = Name == InstanceFuncName;
                    IsStaticFunc = Name == StaticFuncName;
                    IsFinalizeFunc = Name == FinalizeFuncName;
                    IsNewFunc = Name == CtorFuncName;
                    IsDisposeFunc = Name == DisposeFuncName;
                    IsInstanceOrStaticFunc = IsInstanceFunc || IsStaticFunc;
                    if ((IsInstanceFunc || IsNewFunc || IsFinalizeFunc) && IsStatic) throw new ScriptLoadingException($"Function '{Name}' cannot be static.");
                    if (IsStaticFunc) IsStatic = true;
                    else if (IsNewFunc && !isPrivate) IsPublic = true;
                    AddDefaultVars();

                    Signature = Name + " ";
                    p = SmartSplit(h2);

                    if (p.Length > 1 || p[0].Length > 0)
                    {
                        if (p.Length > byte.MaxValue) throw new ScriptLoadingException($"Too many parameters ({p.Length}). Limit is {byte.MaxValue}.");
                        string prmName, prmTypeName;
                        bool byRef;
                        HasParams = false;
                        VarType prmType;
                        Params = new FuncParam[p.Length];
                        int opPos;
                        for (int n = 0; n < p.Length; n++)
                        {
                            if (HasParams) throw new ScriptLoadingException("A params parameter must be the last parameter.");
                            string prm = p[n];
                            byRef = prm.StartsWith("ref ");
                            if (byRef) { p[n] = prm = prm.Substring(4); byRef = true; RefCount++; }
                            else if (prm.StartsWith("params ")) { p[n] = prm = prm.Substring(7); HasParams = true; ParamsIndex = n; }

                            i = prm.IndexOf(' ');
                            if (i < 1) throw new ScriptLoadingException($"Invalid parameter at position {n + 1}.");

                            prmName = prm.Substring(i + 1);
                            prmTypeName = prm.Substring(0, i);

                            opPos = prmName.IndexOf(OperatorChar);
                            bool optional = opPos >= 0;
                            if (optional)
                            {
                                if (byRef) throw new ScriptLoadingException("Ref-parameter cannot have a default value.");
                                if (HasParams) throw new ScriptLoadingException("Params-parameter cannot have a default value.");
                                prmName = prmName.Substring(0, opPos);

                            }
                            else if (!HasParams) MinArgNum = n + 1;
                            else optional = true;



                            prmType = Class.GetTypeFromDict(prmTypeName, Class);
                            if (HasParams && !TypeIsArray(prmType.ID)) throw new ScriptLoadingException("When using the params keyword, must specify an array of the data type.");
                            Params[n] = new FuncParam(prmName, prmTypeName, prmType, byRef, optional, location);

                            if (byRef) Signature += "ref*";
                            else if (HasParams) Signature += "params*";
                            else if (optional) Signature += "optional*";

                            Signature += prmType.Name + " ";
                        }
                        if (HasParams)
                            ParamsType = location.GetElementVarType(Params[ParamsIndex].ParamType);

                    }
                    else Params = new FuncParam[0];

                    i = code.IndexOf("){");
                    i = code.IndexOf('{', i);
                    i2 = code.LastIndexOf('}');
                    Code = code.Substring(i + 1, i2 - (i + 1));
                    Code = Code.Trim(UnitSeparator[0]);
                    if (!Code.StartsWith(CodeLocationChar))
                        if (LocationMark != null)
                            Code = ToLocationMarkStr(LocationMark.CFile.Num, LocationMark.Line2) + Code;

                    if (p[0].Length > 0) Code = string.Join(UnitSeparator, p) + UnitSeparator + Code;




                    if (!IsStatic) VarTypes.Add(ThisStr, new VarType(TypeID.Custom, CustomType.Get(Class)));

                    var lm = LocationMark ?? GetLocationMark(script, code, true);

                    if (lm != null) CodeFile = lm.File;
                    else throw new ScriptLoadingException($"Could not find a service label with data about the file in which the function '{Name}' is located.");
                    BasePath = lm.CFile.Base;

                    var d = PPDirective.CuteDirectiveLine(ref Code, "base", 0, CodeLocationChar);
                    if (d.str != null)
                    {

                        var ppd = PPDirective.Get(d.str, lm.File, script);

                        BasePath = TrimLastSlash(ppd.Path) ?? System.IO.Path.GetDirectoryName(lm.File);


                    }

#if EXON
                }
                catch (Exception ex)
                {
                    var lm = GetLocationMark(script, code, true);
                    string msg = ErrMsgWithLoc($"Function '{h}' at '{Class.ClassFullName}' loading error. " + ex.Message, lm);
                    throw new ScriptLoadingException(msg);
                }
#endif
            }

        }

        static bool InconsistentAccessibility(ScriptFunction f, ScriptClass typeClass)
        {
            var fac = FuncAccessibilityClass(f);
            var tac = AccessibilityClass(typeClass);
            bool isNested = ClassIsNested(tac, fac);
            return isNested;
        }
        static bool ClassIsNested(ScriptClass c, ScriptClass where)
        {
            if (c == where) return false;
            while (c.OuterClass != null)
            {
                c = c.OuterClass;
                if (c == where) return true;

            }
            return false;
        }
        static ScriptClass AccessibilityClass(ScriptClass c)
        {

            while (c.OuterClass != null)
            {
                bool pub = c.IsPublic;
                c = c.OuterClass;
                if (!pub) break;
            }
            return c;
        }
        static ScriptClass FuncAccessibilityClass(ScriptFunction f)
        {
            if (!f.IsPublic) return f.Class;
            return AccessibilityClass(f.Class);
        }



        public bool Is(ScriptClass f)
        {
            if (this == f || InheritedClasses.Contains(f)) return true;
            for (int i = 0; i < InheritedClasses.Count; i++)
                if (InheritedClasses[i].Is(f)) return true;

            return false;
        }


    }






}
