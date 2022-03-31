using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using static OverScript.ScriptClass;

namespace OverScript
{



    public struct BF
    {
        public List<OverloadVariant> OV;

        public BF Add<T>(FuncToCall<T> method, params TypeID[] prmTypes)
        {
            TypeID returnType = GetTypeID(typeof(T));
            if (OV == null) OV = new List<OverloadVariant>();
            OV.Add(new OverloadVariant(method, returnType, prmTypes));
            return this;
        }



        public BF HasParams(TypeID typeId, bool strict = false)
        {
            var last = GetOV();
            last.HasParams = true;
            last.TypeOfParams = new PrmType(GetVarTypeByID(typeId));
            last.TypeOfParams.IsStrict = strict;
            last.ParamsArrType = GetArrayTypeId(last.TypeOfParams.Type.ID);

            return this;
        }
        public BF FewerArgsAllowed(int count = 0)
        {

            var last = GetOV();
            last.FewerArgsAllowed = true;
            last.MinArgsRequired = count;
            return this;
        }
        public BF Return(int n, bool byTypeObj = false, bool elemArrConv = false, bool byHint = false, bool byHintArray = false)
        {

            var last = GetOV();
            last.ReturnTypeByArg = n;

            last.ReturnTypeArgIsTypeObj = byTypeObj;
            last.ReturnTypeArgElemArrConv = elemArrConv;
            last.ReturnTypeByHint = byHint;
            last.ReturnTypeByHintArray = byHintArray;
            return this;
        }
        public BF Return2(int n, int n2)
        {
            var last = GetOV();
            last.ReturnTypeByArg = n;
            last.ReturnTypeByArg2 = n2;
            return this;
        }
        public BF HReturn(int n, bool byHintTypeObj = false, bool elemArrConv = false)
        {
            Return(n, byHintTypeObj, elemArrConv, true);
            return this;
        }
        public BF HAReturn(int n, bool byHintTypeObj = false, bool elemArrConv = false)
        {
            Return(n, byHintTypeObj, elemArrConv, true, true);
            return this;
        }
        public BF Strict(params int[] prmIndexes)
        {

            var last = GetOV();

            if (prmIndexes.Length == 0)
            {
                for (int i = 0; i < last.ParamTypes.Length; i++)
                    last.ParamTypes[i].IsStrict = true;

            }
            else
            {


                for (int i = 0; i < prmIndexes.Length; i++)
                    last.ParamTypes[prmIndexes[i]].IsStrict = true;


            }

            return this;
        }
        public BF Kind(int n, EvalUnitKind kind)
        {
            var last = GetOV();
            last.ParamTypes[n].Kind = kind;
            return this;
        }



        int CurIndex;
        bool ByIndex;
        private OverloadVariant GetOV() => !ByIndex ? OV.Last() : OV[CurIndex];

        public BF ConvertAll(Action<BF> a)
        {
            ByIndex = true;
            for (int i = 0; i < OV.Count; i++)
            {
                CurIndex = i;
                a(this);
            }



            return this;
        }
    }

    public class OverloadVariant
    {
        public TypeID ReturnType;

        public PrmType[] ParamTypes;


        public bool HasParams;
        public TypeID ParamsArrType;
        public int ReturnTypeByArg, ReturnTypeByArg2;

        public bool ReturnTypeArgElemArrConv;

        public bool ReturnTypeArgIsTypeObj;
        public bool ReturnTypeByHint, ReturnTypeByHintArray;

        public bool FewerArgsAllowed;
        public int MinArgsRequired;
        public object Method;
        public PrmType TypeOfParams;
        public OverloadVariant(object method, TypeID returnType, TypeID[] paramTypes)
        {
            ReturnType = returnType;

            Method = method;
            HasParams = false;
            ReturnTypeByArg = ReturnTypeByArg2 = -1;

            ReturnTypeArgIsTypeObj = false;
            ReturnTypeArgElemArrConv = false;
            FewerArgsAllowed = false;
            MinArgsRequired = 0;
            ReturnTypeByHint = ReturnTypeByHintArray = false;
            ParamTypes = paramTypes.Select(x => new PrmType(GetVarTypeByID(x))).ToArray();

            TypeOfParams = null;
        }

    }

    public class PrmType
    {
        public VarType Type;
        public bool IsStrict;
        public EvalUnitKind? Kind;
        public PrmType(VarType type)
        {
            Type = type;
            IsStrict = false;
            Kind = null;
        }
    }


    public partial class BasicFunctions
    {
        public Dictionary<string, BF> BasicFuncs = new Dictionary<string, BF>();
        public BasicFunctions()
        {
            SetFuncs();
        }







        void SetFuncs()
        {

            AddFunc("Substring", new BF().Add(Substring_string_int, TypeID.String, TypeID.Int).Add(Substring_string_int_int, TypeID.String, TypeID.Int, TypeID.Int));
            AddFunc("Left", new BF().Add(Left_string_int, TypeID.String, TypeID.Int));
            AddFunc("Right", new BF().Add(Right_string_int, TypeID.String, TypeID.Int));
            AddFunc("Length", new BF().Add(StrLen_string, TypeID.String).Add(Count_array, TypeID.Array).Add(Count_object, TypeID.Object));

            AddFunc("Trim", new BF().Add(Trim_string, TypeID.String).Add(Trim_string_char, TypeID.String, TypeID.Char));

            AddFunc("TrimStart", new BF().Add(TrimStart_string, TypeID.String).Add(TrimStart_string_char, TypeID.String, TypeID.Char));

            AddFunc("TrimEnd", new BF().Add(TrimEnd_string, TypeID.String).Add(TrimEnd_string_char, TypeID.String, TypeID.Char));

            AddFunc("ToUpper", new BF().Add(ToUpper_string, TypeID.String));
            AddFunc("ToLower", new BF().Add(ToLower_string, TypeID.String));
            AddFunc("ToUpperFirst", new BF().Add(ToUpperFirst_string, TypeID.String));
            AddFunc("ToLowerFirst", new BF().Add(ToLowerFirst_string, TypeID.String));
            AddFunc("Replace", new BF().Add(Replace_string_char_char, TypeID.String, TypeID.Char, TypeID.Char)
                                            .Add(Replace_string_string_string, TypeID.String, TypeID.String, TypeID.String)
                                            .Add(Replace_string_string_string_int, TypeID.String, TypeID.String, TypeID.String, TypeID.Int)
                                            .Add(Replace_string_string_string_bool, TypeID.String, TypeID.String, TypeID.String, TypeID.Bool)
                                            .Add(Replace_string_string_string_object, TypeID.String, TypeID.String, TypeID.String, TypeID.Object));
            AddFunc("Remove", new BF().Add(RemoveStr_string_int, TypeID.String, TypeID.Int).Add(RemoveStr_string_int_int, TypeID.String, TypeID.Int, TypeID.Int)
                                        .Add(RemoveValue_intArray_intParams, TypeID.IntArray).HasParams(TypeID.Int)
                                        .Add(RemoveValue_longArray_longParams, TypeID.LongArray).HasParams(TypeID.Long)
                                        .Add(RemoveValue_floatArray_floatParams, TypeID.FloatArray).HasParams(TypeID.Float)
                                        .Add(RemoveValue_doubleArray_doubleParams, TypeID.DoubleArray).HasParams(TypeID.Double)
                                        .Add(RemoveValue_decimalArray_decimalParams, TypeID.DecimalArray).HasParams(TypeID.Decimal)
                                        .Add(RemoveValue_boolArray_boolParams, TypeID.BoolArray).HasParams(TypeID.Bool)
                                        .Add(RemoveValue_stringArray_stringParams, TypeID.StringArray).HasParams(TypeID.String)
                                        .Add(RemoveValue_charArray_charParams, TypeID.CharArray).HasParams(TypeID.Char)
                                        .Add(RemoveValue_objectArray_objectParams, TypeID.ObjectArray).HasParams(TypeID.Object)
                                        .Add(RemoveValue_byteArray_byteParams, TypeID.ByteArray).HasParams(TypeID.Byte)
                                        .Add(RemoveValue_shortArray_shortParams, TypeID.ShortArray).HasParams(TypeID.Short)
                                        .Add(RemoveValue_dateArray_dateParams, TypeID.DateArray).HasParams(TypeID.Date)
                                        .Add(RemoveValue_customArray_customParams, TypeID.CustomArray).HasParams(TypeID.Custom)
                                        .Add(RemoveValue_object_objectParams, TypeID.Object).HasParams(TypeID.Object).Strict(0)
                                         );
            AddFunc("Insert", new BF().Add(InsertStr_string_int_string, TypeID.String, TypeID.Int, TypeID.String).Add(InsertStr_string_int_string, TypeID.String, TypeID.Int, TypeID.Char)
                                            .Add(Insert_intArray_int_intParams, TypeID.IntArray, TypeID.Int).HasParams(TypeID.Int, true)
                                            .Add(Insert_longArray_int_longParams, TypeID.LongArray, TypeID.Int).HasParams(TypeID.Long, true)
                                            .Add(Insert_floatArray_int_floatParams, TypeID.FloatArray, TypeID.Int).HasParams(TypeID.Float, true)
                                            .Add(Insert_doubleArray_int_doubleParams, TypeID.DoubleArray, TypeID.Int).HasParams(TypeID.Double, true)
                                            .Add(Insert_decimalArray_int_decimalParams, TypeID.DecimalArray, TypeID.Int).HasParams(TypeID.Decimal, true)
                                            .Add(Insert_boolArray_int_boolParams, TypeID.BoolArray, TypeID.Int).HasParams(TypeID.Bool, true)
                                            .Add(Insert_stringArray_int_stringParams, TypeID.StringArray, TypeID.Int).HasParams(TypeID.String, true)
                                            .Add(Insert_charArray_int_charParams, TypeID.CharArray, TypeID.Int).HasParams(TypeID.Char, true)
                                            .Add(Insert_byteArray_int_byteParams, TypeID.ByteArray, TypeID.Int).HasParams(TypeID.Byte, true)
                                            .Add(Insert_shortArray_int_shortParams, TypeID.ShortArray, TypeID.Int).HasParams(TypeID.Short, true)
                                            .Add(Insert_dateArray_int_dateParams, TypeID.DateArray, TypeID.Int).HasParams(TypeID.Date, true)
                                            .Add(Insert_customArray_int_customParams, TypeID.CustomArray, TypeID.Int).HasParams(TypeID.Custom)
                                            .Add(Insert_objectArray_int_objectParams, TypeID.ObjectArray, TypeID.Int).HasParams(TypeID.Object)
                                            .Add(Insert_object_int_objectParams, TypeID.Object, TypeID.Int).HasParams(TypeID.Object).Strict(0)
                                            );
            AddFunc("ConvInsert", new BF().Add(Insert_intArray_int_intParams, TypeID.IntArray, TypeID.Int).HasParams(TypeID.Object)
                                            .Add(Insert_longArray_int_longParams, TypeID.LongArray, TypeID.Int).HasParams(TypeID.Object)
                                            .Add(Insert_floatArray_int_floatParams, TypeID.FloatArray, TypeID.Int).HasParams(TypeID.Object)
                                            .Add(Insert_doubleArray_int_doubleParams, TypeID.DoubleArray, TypeID.Int).HasParams(TypeID.Object)
                                            .Add(Insert_decimalArray_int_decimalParams, TypeID.DecimalArray, TypeID.Int).HasParams(TypeID.Object)
                                            .Add(Insert_boolArray_int_boolParams, TypeID.BoolArray, TypeID.Int).HasParams(TypeID.Object)
                                            .Add(Insert_stringArray_int_stringParams, TypeID.StringArray, TypeID.Int).HasParams(TypeID.Object)
                                            .Add(Insert_charArray_int_charParams, TypeID.CharArray, TypeID.Int).HasParams(TypeID.Object)
                                            .Add(Insert_byteArray_int_byteParams, TypeID.ByteArray, TypeID.Int).HasParams(TypeID.Object)
                                            .Add(Insert_shortArray_int_shortParams, TypeID.ShortArray, TypeID.Int).HasParams(TypeID.Object)
                                            .Add(Insert_dateArray_int_dateParams, TypeID.DateArray, TypeID.Int).HasParams(TypeID.Object)


                                            .Add(ConvInsert_object_int_objectParams, TypeID.Object, TypeID.Int).HasParams(TypeID.Object).Strict(0)
                                            );

            AddFunc("InsertRange", new BF().Add(InsertRange_intArray_int_object_bool, TypeID.IntArray, TypeID.Int, TypeID.Object, TypeID.Bool).FewerArgsAllowed(3)
                                                .Add(InsertRange_longArray_int_object_bool, TypeID.LongArray, TypeID.Int, TypeID.Object, TypeID.Bool).FewerArgsAllowed(3)
                                                .Add(InsertRange_floatArray_int_object_bool, TypeID.FloatArray, TypeID.Int, TypeID.Object, TypeID.Bool).FewerArgsAllowed(3)
                                                .Add(InsertRange_doubleArray_int_object_bool, TypeID.DoubleArray, TypeID.Int, TypeID.Object, TypeID.Bool).FewerArgsAllowed(3)
                                                .Add(InsertRange_decimalArray_int_object_bool, TypeID.DecimalArray, TypeID.Int, TypeID.Object, TypeID.Bool).FewerArgsAllowed(3)
                                                .Add(InsertRange_boolArray_int_object_bool, TypeID.BoolArray, TypeID.Int, TypeID.Object, TypeID.Bool).FewerArgsAllowed(3)
                                                .Add(InsertRange_stringArray_int_object_bool, TypeID.StringArray, TypeID.Int, TypeID.Object, TypeID.Bool).FewerArgsAllowed(3)
                                                .Add(InsertRange_charArray_int_object_bool, TypeID.CharArray, TypeID.Int, TypeID.Object, TypeID.Bool).FewerArgsAllowed(3)
                                                .Add(InsertRange_objectArray_int_object_bool, TypeID.ObjectArray, TypeID.Int, TypeID.Object, TypeID.Bool).FewerArgsAllowed(3)
                                                .Add(InsertRange_byteArray_int_object_bool, TypeID.ByteArray, TypeID.Int, TypeID.Object, TypeID.Bool).FewerArgsAllowed(3)
                                                .Add(InsertRange_shortArray_int_object_bool, TypeID.ShortArray, TypeID.Int, TypeID.Object, TypeID.Bool).FewerArgsAllowed(3)
                                                .Add(InsertRange_dateArray_int_object_bool, TypeID.DateArray, TypeID.Int, TypeID.Object, TypeID.Bool).FewerArgsAllowed(3)
                                                .Add(InsertRange_customArray_int_object_bool, TypeID.CustomArray, TypeID.Int, TypeID.Object, TypeID.Bool).FewerArgsAllowed(3)
                                                .Add(InsertRange_object_int_object_bool, TypeID.Object, TypeID.Int, TypeID.Object, TypeID.Bool).Strict(0).FewerArgsAllowed(3)
                                                 );

            AddFunc("ToString", new BF().Add(ToString, TypeID.Object).Add(ToString_byte_int, TypeID.Byte, TypeID.Int).Add(ToString_short_int, TypeID.Short, TypeID.Int).Add(ToString_int_int, TypeID.Int, TypeID.Int).Add(ToString_long_int, TypeID.Long, TypeID.Int));
            AddFunc("StartsWith", new BF().Add(StartsWith_string_char, TypeID.String, TypeID.Char)
                                                .Add(StartsWith_string_string, TypeID.String, TypeID.String)
                                                .Add(StartsWith_string_string_int, TypeID.String, TypeID.String, TypeID.Int)
                                                .Add(StartsWith_string_string_object, TypeID.String, TypeID.String, TypeID.Object)
                                                .Add(StartsWith_string_string_bool, TypeID.String, TypeID.String, TypeID.Bool)
                                                .Add(StartsWith_string_string_bool_object, TypeID.String, TypeID.String, TypeID.Bool, TypeID.Object));
            AddFunc("EndsWith", new BF().Add(EndsWith_string_char, TypeID.String, TypeID.Char)
                                              .Add(EndsWith_string_string, TypeID.String, TypeID.String)
                                              .Add(EndsWith_string_string_int, TypeID.String, TypeID.String, TypeID.Int)
                                              .Add(EndsWith_string_string_object, TypeID.String, TypeID.String, TypeID.Object)
                                              .Add(EndsWith_string_string_bool, TypeID.String, TypeID.String, TypeID.Bool)
                                              .Add(EndsWith_string_string_bool_object, TypeID.String, TypeID.String, TypeID.Bool, TypeID.Object));

            AddFunc("Format", new BF().Add(Format_string_object, TypeID.String, TypeID.Object).Add(Format_string_objectParams, TypeID.String).HasParams(TypeID.Object));
            AddFunc("Ord", new BF().Add(Ord_char, TypeID.Char).Add(Ord_string, TypeID.String).Add(Ord_string_int, TypeID.String, TypeID.Int));
            AddFunc("Asc", new BF().Add(Asc_char, TypeID.Char)
                                        .Add(Asc_string, TypeID.String)
                                        .Add(Asc_char_object, TypeID.Char, TypeID.Object)
                                        .Add(Asc_char_string, TypeID.Char, TypeID.String)
                                        .Add(Asc_string_int_string, TypeID.String, TypeID.Int, TypeID.String)
                                        .Add(Asc_string_int_object, TypeID.String, TypeID.Int, TypeID.Object)
                                        .Add(Asc_string_string, TypeID.String, TypeID.String)
                                        .Add(Asc_string_object, TypeID.String, TypeID.Object)
                                        );
            AddFunc("Chr", new BF().Add(Chr_int, TypeID.Int).Add(Chr_int_string, TypeID.Int, TypeID.String).Add(Chr_int_object, TypeID.Int, TypeID.Object));
            AddFunc("ToChar", new BF().Add(ToChar_object, TypeID.Object));
            AddFunc("CharAt", new BF().Add(CharAt_string_int, TypeID.String, TypeID.Int));
            AddFunc("IsControl", new BF().Add(CharIsControl_char, TypeID.Char));
            AddFunc("IsDigit", new BF().Add(CharIsDigit_char, TypeID.Char));
            AddFunc("IsHighSurrogate", new BF().Add(CharIsHighSurrogate_char, TypeID.Char));
            AddFunc("IsLetter", new BF().Add(CharIsLetter_char, TypeID.Char));
            AddFunc("IsLetterOrDigit", new BF().Add(CharIsLetterOrDigit_char, TypeID.Char));
            AddFunc("IsLower", new BF().Add(CharIsLower_char, TypeID.Char));
            AddFunc("IsLowSurrogate", new BF().Add(CharIsLowSurrogate_char, TypeID.Char));
            AddFunc("IsNumber", new BF().Add(CharIsNumber_char, TypeID.Char));
            AddFunc("IsPunctuation", new BF().Add(CharIsPunctuation_char, TypeID.Char));
            AddFunc("IsSeparator", new BF().Add(CharIsSeparator_char, TypeID.Char));
            AddFunc("IsSurrogate", new BF().Add(CharIsSurrogate_char, TypeID.Char));
            AddFunc("IsSurrogatePair", new BF().Add(CharIsSurrogatePair_char, TypeID.Char, TypeID.Char));
            AddFunc("IsSymbol", new BF().Add(CharIsSymbol_char, TypeID.Char));
            AddFunc("IsUpper", new BF().Add(CharIsUpper_char, TypeID.Char));
            AddFunc("IsWhiteSpace", new BF().Add(CharIsWhiteSpace_char, TypeID.Char));
            AddFunc("ToInt", new BF().Add(ToInt_object, TypeID.Object).Add(ToInt_string_int, TypeID.String, TypeID.Int));

            AddFunc("ToLong", new BF().Add(ToLong_object, TypeID.Object).Add(ToLong_string_int, TypeID.String, TypeID.Int));
            AddFunc("ToDouble", new BF().Add(ToDouble_object, TypeID.Object));
            AddFunc("ToFloat", new BF().Add(ToFloat_object, TypeID.Object));
            AddFunc("ToDecimal", new BF().Add(ToDecimal_object, TypeID.Object));
            AddFunc("ToBool", new BF().Add(ToBool_object, TypeID.Object));
            AddFunc("ToShort", new BF().Add(ToShort_object, TypeID.Object).Add(ToShort_string_int, TypeID.String, TypeID.Int));
            AddFunc("ToByte", new BF().Add(ToByte_object, TypeID.Object).Add(ToByte_string_int, TypeID.String, TypeID.Int));
            AddFunc("ToDate", new BF().Add(ToDate_object, TypeID.Object));
            AddFunc("Floor", new BF().Add(Floor_float, TypeID.Float).Add(Floor_double, TypeID.Double).Add(Floor_decimal, TypeID.Decimal));
            AddFunc("Ceiling", new BF().Add(Ceiling_float, TypeID.Float).Add(Ceiling_double, TypeID.Double).Add(Ceiling_decimal, TypeID.Decimal));
            AddFunc("Round", new BF().Add(Round_float, TypeID.Float)
                                            .Add(Round_double, TypeID.Double)
                                            .Add(Round_decimal, TypeID.Decimal)
                                            .Add(Round_float_int, TypeID.Float, TypeID.Int)
                                            .Add(Round_double_int, TypeID.Double, TypeID.Int)
                                            .Add(Round_decimal_int, TypeID.Decimal, TypeID.Int)
                                            .Add(Round_float_int_int, TypeID.Float, TypeID.Int, TypeID.Int)
                                            .Add(Round_double_int_int, TypeID.Double, TypeID.Int, TypeID.Int)
                                            .Add(Round_decimal_int_int, TypeID.Decimal, TypeID.Int, TypeID.Int)
                                            .Add(Round_float_int_object, TypeID.Float, TypeID.Int, TypeID.Object)
                                            .Add(Round_double_int_object, TypeID.Double, TypeID.Int, TypeID.Object)
                                            .Add(Round_decimal_int_object, TypeID.Decimal, TypeID.Int, TypeID.Object)
                                            );
            AddFunc("Truncate", new BF().Add(Truncate_double, TypeID.Double).Add(Truncate_decimal, TypeID.Decimal));

            AddFunc("Abs", new BF().Add(Abs_short, TypeID.Short).Add(Abs_int, TypeID.Int).Add(Abs_long, TypeID.Long)
                                        .Add(Abs_float, TypeID.Float).Add(Abs_double, TypeID.Double).Add(Abs_decimal, TypeID.Decimal));
            AddFunc("Pow", new BF().Add(Pow_double_double, TypeID.Double, TypeID.Double));

            AddFunc("Sin", new BF().Add(Sin_double, TypeID.Double));
            AddFunc("Cos", new BF().Add(Cos_double, TypeID.Double));
            AddFunc("Tan", new BF().Add(Tan_double, TypeID.Double));
            AddFunc("Exp", new BF().Add(Exp_double, TypeID.Double));
            AddFunc("Log", new BF().Add(Log_double, TypeID.Double).Add(Log_double_double, TypeID.Double, TypeID.Double));
            AddFunc("Log10", new BF().Add(Log10_double, TypeID.Double));
            AddFunc("Log2", new BF().Add(Log2_double, TypeID.Double));

            AddFunc("Acos", new BF().Add(Acos_double, TypeID.Double));
            AddFunc("Asin", new BF().Add(Asin_double, TypeID.Double));
            AddFunc("Atan", new BF().Add(Atan_double, TypeID.Double));
            AddFunc("Atan2", new BF().Add(Atan2_double_double, TypeID.Double, TypeID.Double));
        
          
            AddFunc("Resize", new BF().Add(Resize_intArray_int, TypeID.IntArray, TypeID.Int)
                                            .Add(Resize_longArray_int, TypeID.LongArray, TypeID.Int)
                                            .Add(Resize_floatArray_int, TypeID.FloatArray, TypeID.Int)
                                            .Add(Resize_doubleArray_int, TypeID.DoubleArray, TypeID.Int)
                                            .Add(Resize_decimalArray_int, TypeID.DecimalArray, TypeID.Int)
                                            .Add(Resize_boolArray_int, TypeID.BoolArray, TypeID.Int)
                                            .Add(Resize_stringArray_int, TypeID.StringArray, TypeID.Int)
                                            .Add(Resize_charArray_int, TypeID.CharArray, TypeID.Int)
                                            .Add(Resize_objectArray_int, TypeID.ObjectArray, TypeID.Int)
                                            .Add(Resize_byteArray_int, TypeID.ByteArray, TypeID.Int)
                                            .Add(Resize_shortArray_int, TypeID.ShortArray, TypeID.Int)
                                            .Add(Resize_dateArray_int, TypeID.DateArray, TypeID.Int)
                                            .Add(Resize_customArray_int, TypeID.CustomArray, TypeID.Int)
                                            .Add(Resize_object_int, TypeID.Object, TypeID.Int)
                                            .ConvertAll(x => x.Strict(0).Kind(0, EvalUnitKind.Variable))
                                            );

            AddFunc("IndexOf", new BF().Add(StrPos_string_char, TypeID.String, TypeID.Char)
                                            .Add(StrPos_string_string, TypeID.String, TypeID.String)
                                            .Add(StrPos_string_char_int, TypeID.String, TypeID.Char, TypeID.Int)
                                            .Add(StrPos_string_string_int, TypeID.String, TypeID.String, TypeID.Int)
                                            .Add(StrPos_string_char_int_int, TypeID.String, TypeID.Char, TypeID.Int, TypeID.Int)
                                            .Add(StrPos_string_string_int_int, TypeID.String, TypeID.String, TypeID.Int, TypeID.Int)
                                            .Add(StrPos_string_string_int_int_int, TypeID.String, TypeID.String, TypeID.Int, TypeID.Int, TypeID.Int)
                                            .Add(StrPos_string_string_int_int_object, TypeID.String, TypeID.String, TypeID.Int, TypeID.Int, TypeID.Object)
                                            .Add(StrPos_string_string_object, TypeID.String, TypeID.String, TypeID.Object)
                                            .Add(StrPos_string_string_int_int_bool, TypeID.String, TypeID.String, TypeID.Int, TypeID.Int, TypeID.Bool)
                                            .Add(StrPos_string_string_int_bool, TypeID.String, TypeID.String, TypeID.Int, TypeID.Bool)
                                            .Add(StrPos_string_string_bool, TypeID.String, TypeID.String, TypeID.Bool)
                                            .Add(StrPos_string_string_int_object, TypeID.String, TypeID.String, TypeID.Int, TypeID.Object)

                                            .Add(IndexOf_intArray_int, TypeID.IntArray, TypeID.Int)
                                            .Add(IndexOf_intArray_int_int, TypeID.IntArray, TypeID.Int, TypeID.Int)
                                            .Add(IndexOf_intArray_int_int_int, TypeID.IntArray, TypeID.Int, TypeID.Int, TypeID.Int)
                                            .Add(IndexOf_longArray_long, TypeID.LongArray, TypeID.Long)
                                            .Add(IndexOf_longArray_long_int, TypeID.LongArray, TypeID.Long, TypeID.Int)
                                            .Add(IndexOf_longArray_long_int_int, TypeID.LongArray, TypeID.Long, TypeID.Int, TypeID.Int)
                                            .Add(IndexOf_floatArray_float, TypeID.FloatArray, TypeID.Float)
                                            .Add(IndexOf_floatArray_float_int, TypeID.FloatArray, TypeID.Float, TypeID.Int)
                                            .Add(IndexOf_floatArray_float_int_int, TypeID.FloatArray, TypeID.Float, TypeID.Int, TypeID.Int)
                                            .Add(IndexOf_doubleArray_double, TypeID.DoubleArray, TypeID.Double)
                                            .Add(IndexOf_doubleArray_double_int, TypeID.DoubleArray, TypeID.Double, TypeID.Int)
                                            .Add(IndexOf_doubleArray_double_int_int, TypeID.DoubleArray, TypeID.Double, TypeID.Int, TypeID.Int)
                                            .Add(IndexOf_decimalArray_decimal, TypeID.DecimalArray, TypeID.Decimal)
                                            .Add(IndexOf_decimalArray_decimal_int, TypeID.DecimalArray, TypeID.Decimal, TypeID.Int)
                                            .Add(IndexOf_decimalArray_decimal_int_int, TypeID.DecimalArray, TypeID.Decimal, TypeID.Int, TypeID.Int)
                                            .Add(IndexOf_boolArray_bool, TypeID.BoolArray, TypeID.Bool)
                                            .Add(IndexOf_boolArray_bool_int, TypeID.BoolArray, TypeID.Bool, TypeID.Int)
                                            .Add(IndexOf_boolArray_bool_int_int, TypeID.BoolArray, TypeID.Bool, TypeID.Int, TypeID.Int)
                                            .Add(IndexOf_stringArray_string, TypeID.StringArray, TypeID.String)
                                            .Add(IndexOf_stringArray_string_int, TypeID.StringArray, TypeID.String, TypeID.Int)
                                            .Add(IndexOf_stringArray_string_int_int, TypeID.StringArray, TypeID.String, TypeID.Int, TypeID.Int)
                                            .Add(IndexOf_charArray_char, TypeID.CharArray, TypeID.Char)
                                            .Add(IndexOf_charArray_char_int, TypeID.CharArray, TypeID.Char, TypeID.Int)
                                            .Add(IndexOf_charArray_char_int_int, TypeID.CharArray, TypeID.Char, TypeID.Int, TypeID.Int)
                                            .Add(IndexOf_objectArray_object, TypeID.ObjectArray, TypeID.Object)
                                            .Add(IndexOf_objectArray_object_int, TypeID.ObjectArray, TypeID.Object, TypeID.Int)
                                            .Add(IndexOf_objectArray_object_int_int, TypeID.ObjectArray, TypeID.Object, TypeID.Int, TypeID.Int)
                                            .Add(IndexOf_shortArray_short, TypeID.ShortArray, TypeID.Short)
                                            .Add(IndexOf_shortArray_short_int, TypeID.ShortArray, TypeID.Short, TypeID.Int)
                                            .Add(IndexOf_shortArray_short_int_int, TypeID.ShortArray, TypeID.Short, TypeID.Int, TypeID.Int)
                                            .Add(IndexOf_byteArray_byte, TypeID.ByteArray, TypeID.Byte)
                                            .Add(IndexOf_byteArray_byte_int, TypeID.ByteArray, TypeID.Byte, TypeID.Int)
                                            .Add(IndexOf_byteArray_byte_int_int, TypeID.ByteArray, TypeID.Byte, TypeID.Int, TypeID.Int)
                                            .Add(IndexOf_dateArray_date, TypeID.DateArray, TypeID.Date)
                                            .Add(IndexOf_dateArray_date_int, TypeID.DateArray, TypeID.Date, TypeID.Int)
                                            .Add(IndexOf_dateArray_date_int_int, TypeID.DateArray, TypeID.Date, TypeID.Int, TypeID.Int)
                                            .Add(IndexOf_customArray_custom, TypeID.CustomArray, TypeID.Custom)
                                            .Add(IndexOf_customArray_custom_int, TypeID.CustomArray, TypeID.Custom, TypeID.Int)
                                            .Add(IndexOf_customArray_custom_int_int, TypeID.CustomArray, TypeID.Custom, TypeID.Int, TypeID.Int)
                                            .Add(IndexOf_object_object, TypeID.Object, TypeID.Object).Strict(0)
                                            );
            AddFunc("LastIndexOf", new BF().Add(StrRPos_string_char, TypeID.String, TypeID.Char)
                                            .Add(StrRPos_string_string, TypeID.String, TypeID.String)
                                            .Add(StrRPos_string_char_int, TypeID.String, TypeID.Char, TypeID.Int)
                                            .Add(StrRPos_string_string_int, TypeID.String, TypeID.String, TypeID.Int)
                                            .Add(StrRPos_string_char_int_int, TypeID.String, TypeID.Char, TypeID.Int, TypeID.Int)
                                            .Add(StrRPos_string_string_int_int, TypeID.String, TypeID.String, TypeID.Int, TypeID.Int)
                                            .Add(StrRPos_string_string_int_int_int, TypeID.String, TypeID.String, TypeID.Int, TypeID.Int, TypeID.Int)
                                            .Add(StrRPos_string_string_int_int_object, TypeID.String, TypeID.String, TypeID.Int, TypeID.Int, TypeID.Object)
                                            .Add(StrRPos_string_string_object, TypeID.String, TypeID.String, TypeID.Object)
                                            .Add(StrRPos_string_string_int_int_bool, TypeID.String, TypeID.String, TypeID.Int, TypeID.Int, TypeID.Bool)
                                            .Add(StrRPos_string_string_int_bool, TypeID.String, TypeID.String, TypeID.Int, TypeID.Bool)
                                            .Add(StrRPos_string_string_bool, TypeID.String, TypeID.String, TypeID.Bool)

                                            .Add(LastIndexOf_intArray_int, TypeID.IntArray, TypeID.Int)
                                            .Add(LastIndexOf_intArray_int_int, TypeID.IntArray, TypeID.Int, TypeID.Int)
                                            .Add(LastIndexOf_intArray_int_int_int, TypeID.IntArray, TypeID.Int, TypeID.Int, TypeID.Int)
                                            .Add(LastIndexOf_longArray_long, TypeID.LongArray, TypeID.Long)
                                            .Add(LastIndexOf_longArray_long_int, TypeID.LongArray, TypeID.Long, TypeID.Int)
                                            .Add(LastIndexOf_longArray_long_int_int, TypeID.LongArray, TypeID.Long, TypeID.Int, TypeID.Int)
                                            .Add(LastIndexOf_floatArray_float, TypeID.FloatArray, TypeID.Float)
                                            .Add(LastIndexOf_floatArray_float_int, TypeID.FloatArray, TypeID.Float, TypeID.Int)
                                            .Add(LastIndexOf_floatArray_float_int_int, TypeID.FloatArray, TypeID.Float, TypeID.Int, TypeID.Int)
                                            .Add(LastIndexOf_doubleArray_double, TypeID.DoubleArray, TypeID.Double)
                                            .Add(LastIndexOf_doubleArray_double_int, TypeID.DoubleArray, TypeID.Double, TypeID.Int)
                                            .Add(LastIndexOf_doubleArray_double_int_int, TypeID.DoubleArray, TypeID.Double, TypeID.Int, TypeID.Int)
                                            .Add(LastIndexOf_decimalArray_decimal, TypeID.DecimalArray, TypeID.Decimal)
                                            .Add(LastIndexOf_decimalArray_decimal_int, TypeID.DecimalArray, TypeID.Decimal, TypeID.Int)
                                            .Add(LastIndexOf_decimalArray_decimal_int_int, TypeID.DecimalArray, TypeID.Decimal, TypeID.Int, TypeID.Int)
                                            .Add(LastIndexOf_boolArray_bool, TypeID.BoolArray, TypeID.Bool)
                                            .Add(LastIndexOf_boolArray_bool_int, TypeID.BoolArray, TypeID.Bool, TypeID.Int)
                                            .Add(LastIndexOf_boolArray_bool_int_int, TypeID.BoolArray, TypeID.Bool, TypeID.Int, TypeID.Int)
                                            .Add(LastIndexOf_stringArray_string, TypeID.StringArray, TypeID.String)
                                            .Add(LastIndexOf_stringArray_string_int, TypeID.StringArray, TypeID.String, TypeID.Int)
                                            .Add(LastIndexOf_stringArray_string_int_int, TypeID.StringArray, TypeID.String, TypeID.Int, TypeID.Int)
                                            .Add(LastIndexOf_charArray_char, TypeID.CharArray, TypeID.Char)
                                            .Add(LastIndexOf_charArray_char_int, TypeID.CharArray, TypeID.Char, TypeID.Int)
                                            .Add(LastIndexOf_charArray_char_int_int, TypeID.CharArray, TypeID.Char, TypeID.Int, TypeID.Int)
                                            .Add(LastIndexOf_objectArray_object, TypeID.ObjectArray, TypeID.Object)
                                            .Add(LastIndexOf_objectArray_object_int, TypeID.ObjectArray, TypeID.Object, TypeID.Int)
                                            .Add(LastIndexOf_objectArray_object_int_int, TypeID.ObjectArray, TypeID.Object, TypeID.Int, TypeID.Int)
                                            .Add(LastIndexOf_shortArray_short, TypeID.ShortArray, TypeID.Short)
                                            .Add(LastIndexOf_shortArray_short_int, TypeID.ShortArray, TypeID.Short, TypeID.Int)
                                            .Add(LastIndexOf_shortArray_short_int_int, TypeID.ShortArray, TypeID.Short, TypeID.Int, TypeID.Int)
                                            .Add(LastIndexOf_byteArray_byte, TypeID.ByteArray, TypeID.Byte)
                                            .Add(LastIndexOf_byteArray_byte_int, TypeID.ByteArray, TypeID.Byte, TypeID.Int)
                                            .Add(LastIndexOf_byteArray_byte_int_int, TypeID.ByteArray, TypeID.Byte, TypeID.Int, TypeID.Int)
                                            .Add(LastIndexOf_dateArray_date, TypeID.DateArray, TypeID.Date)
                                            .Add(LastIndexOf_dateArray_date_int, TypeID.DateArray, TypeID.Date, TypeID.Int)
                                            .Add(LastIndexOf_dateArray_date_int_int, TypeID.DateArray, TypeID.Date, TypeID.Int, TypeID.Int)
                                            .Add(LastIndexOf_customArray_custom, TypeID.CustomArray, TypeID.Custom)
                                            .Add(LastIndexOf_customArray_custom_int, TypeID.CustomArray, TypeID.Custom, TypeID.Int)
                                            .Add(LastIndexOf_customArray_custom_int_int, TypeID.CustomArray, TypeID.Custom, TypeID.Int, TypeID.Int)
                                            );
            AddFunc("IndexOfAny", new BF().Add(IndexOfAny_string_charArray, TypeID.String, TypeID.CharArray)
                                                 .Add(IndexOfAny_string_charArray_int, TypeID.String, TypeID.CharArray, TypeID.Int)
                                                 .Add(IndexOfAny_string_charArray_int_int, TypeID.String, TypeID.CharArray, TypeID.Int, TypeID.Int));
            AddFunc("LastIndexOfAny", new BF().Add(LastIndexOfAny_string_charArray, TypeID.String, TypeID.CharArray)
                                                     .Add(LastIndexOfAny_string_charArray_int, TypeID.String, TypeID.CharArray, TypeID.Int)
                                                     .Add(LastIndexOfAny_string_charArray_int_int, TypeID.String, TypeID.CharArray, TypeID.Int, TypeID.Int));
            AddFunc("ElementAt", new BF().Add(ElementAt_object_int, TypeID.Object, TypeID.Int));
            AddFunc("Reverse", new BF().Add(Reverse_object, TypeID.Object).Add(Reverse_object_int_int, TypeID.Object, TypeID.Int, TypeID.Int));
            AddFunc("Sort", new BF().Add(Sort_object, TypeID.Object).Add(Sort_object_object, TypeID.Object, TypeID.Object).Add(Sort_object_int_int, TypeID.Object, TypeID.Int, TypeID.Int));
            AddFunc("IsNumeric", new BF().Add(IsNumeric_string, TypeID.String));
            AddFunc("Write", new BF().Add(Write_object, TypeID.Object).Add(Write_string_objectParams, TypeID.String).HasParams(TypeID.Object));
            AddFunc("WriteLine", new BF().Add(WriteLine).Add(WriteLine_object, TypeID.Object).Add(WriteLine_string_objectParams, TypeID.String).HasParams(TypeID.Object));
            AddFunc("CursorTop", new BF().Add(CursorTop));
            AddFunc("SetCursorTop", new BF().Add(SetCursorTop_int, TypeID.Int));
            AddFunc("CursorLeft", new BF().Add(CursorLeft));
            AddFunc("SetCursorLeft", new BF().Add(SetCursorLeft_int, TypeID.Int));
            AddFunc("CursorVisible", new BF().Add(CursorVisible));
            AddFunc("SetCursorVisible", new BF().Add(SetCursorVisible_bool, TypeID.Bool));
            AddFunc("TickCount", new BF().Add(TickCount));
            AddFunc("Create", new BF().Add(Create_object, TypeID.Object).HReturn(0, true)
                                            .Add(Create_string, TypeID.String)
                                            .Add(Create_string_objectParams, TypeID.String).HasParams(TypeID.Object)
                                            .Add(Create_object_objectParams, TypeID.Object).HasParams(TypeID.Object).HReturn(0, true)
                                            );
            AddFunc("CreateFrom", new BF().Add(CreateFrom_object_string, TypeID.Object, TypeID.String)
                                                .Add(CreateFrom_string_string, TypeID.String, TypeID.String)
                                                .Add(CreateFrom_object_string_objectParams, TypeID.Object, TypeID.String).HasParams(TypeID.Object)
                                                .Add(CreateFrom_string_string_objectParams, TypeID.String, TypeID.String).HasParams(TypeID.Object)
                                                );
            AddFunc("InvokeMember", new BF().Add(InvokeMember_object_string_string_object_object, TypeID.Object, TypeID.String, TypeID.String, TypeID.Object, TypeID.Object)
                                                .Add(InvokeMember_object_string_string_object_object_objectParams, TypeID.Object, TypeID.String, TypeID.String, TypeID.Object).HasParams(TypeID.Object)
                                                .Add(InvokeMember_object_string_object_object_object, TypeID.Object, TypeID.String, TypeID.Object, TypeID.Object, TypeID.Object)
                                                .Add(InvokeMember_object_string_object_object_object_objectParams, TypeID.Object, TypeID.String, TypeID.Object, TypeID.Object).HasParams(TypeID.Object)
                                                .Add(InvokeMember_object_string_int_object_object, TypeID.Object, TypeID.String, TypeID.Int, TypeID.Object, TypeID.Object)
                                                .Add(InvokeMember_object_string_int_object_object_objectParams, TypeID.Object, TypeID.String, TypeID.Int, TypeID.Object).HasParams(TypeID.Object)
                                                );
            AddFunc("Now", new BF().Add(Now).Add(Now_bool, TypeID.Bool).Add(Now_bool_bool, TypeID.Bool, TypeID.Bool));
            AddFunc("ToLocalTime", new BF().Add(ToLocalTime_date, TypeID.Date));
            AddFunc("ToUniversalTime", new BF().Add(ToUniversalTime_date, TypeID.Date));
            AddFunc("Join", new BF().Add(Join_string_intArray, TypeID.String, TypeID.IntArray)
                                           .Add(Join_string_longArray, TypeID.String, TypeID.LongArray)
                                           .Add(Join_string_floatArray, TypeID.String, TypeID.FloatArray)
                                           .Add(Join_string_doubleArray, TypeID.String, TypeID.DoubleArray)
                                           .Add(Join_string_decimalArray, TypeID.String, TypeID.DecimalArray)
                                           .Add(Join_string_boolArray, TypeID.String, TypeID.BoolArray)
                                           .Add(Join_string_stringArray, TypeID.String, TypeID.StringArray)
                                           .Add(Join_string_charArray, TypeID.String, TypeID.CharArray)
                                           .Add(Join_string_objectArray, TypeID.String, TypeID.ObjectArray)
                                           .Add(Join_string_shortArray, TypeID.String, TypeID.ShortArray)
                                           .Add(Join_string_byteArray, TypeID.String, TypeID.ByteArray)
                                           .Add(Join_string_dateArray, TypeID.String, TypeID.DateArray)
                                           .Add(Join_string_customArray, TypeID.String, TypeID.CustomArray)
                                           .Add(Join_string_object, TypeID.String, TypeID.Object)

                                           .Add(Join_char_intArray, TypeID.Char, TypeID.IntArray)
                                           .Add(Join_char_longArray, TypeID.Char, TypeID.LongArray)
                                           .Add(Join_char_floatArray, TypeID.Char, TypeID.FloatArray)
                                           .Add(Join_char_doubleArray, TypeID.Char, TypeID.DoubleArray)
                                           .Add(Join_char_decimalArray, TypeID.Char, TypeID.DecimalArray)
                                           .Add(Join_char_boolArray, TypeID.Char, TypeID.BoolArray)
                                           .Add(Join_char_stringArray, TypeID.Char, TypeID.StringArray)
                                           .Add(Join_char_charArray, TypeID.Char, TypeID.CharArray)
                                           .Add(Join_char_objectArray, TypeID.Char, TypeID.ObjectArray)
                                           .Add(Join_char_shortArray, TypeID.Char, TypeID.ShortArray)
                                           .Add(Join_char_byteArray, TypeID.Char, TypeID.ByteArray)
                                           .Add(Join_char_dateArray, TypeID.Char, TypeID.DateArray)
                                           .Add(Join_char_customArray, TypeID.Char, TypeID.CustomArray)
                                           .Add(Join_char_object, TypeID.Char, TypeID.Object)
                                        );
            AddFunc("Split", new BF().Add(Split_string_string, TypeID.String, TypeID.String)
                                           .Add(Split_string_char, TypeID.String, TypeID.Char)
                                           .Add(Split_string_string_int, TypeID.String, TypeID.String, TypeID.Int)
                                           .Add(Split_string_char_int, TypeID.String, TypeID.Char, TypeID.Int)
                                           .Add(Split_string_string_object, TypeID.String, TypeID.String, TypeID.Object)
                                           .Add(Split_string_char_object, TypeID.String, TypeID.Char, TypeID.Object)
                                           .Add(Split_string_string_int_int, TypeID.String, TypeID.String, TypeID.Int, TypeID.Int)
                                           .Add(Split_string_char_int_int, TypeID.String, TypeID.Char, TypeID.Int, TypeID.Int)
                                           .Add(Split_string_string_int_object, TypeID.String, TypeID.String, TypeID.Int, TypeID.Object)
                                           .Add(Split_string_char_int_object, TypeID.String, TypeID.Char, TypeID.Int, TypeID.Object)
                                            );
            AddFunc("Copy", new BF().Add(Copy_object_object_int, TypeID.Object, TypeID.Object, TypeID.Int)
                                          .Add(Copy_object_int_object_int_int, TypeID.Object, TypeID.Int, TypeID.Object, TypeID.Int, TypeID.Int));
            AddFunc("ReadAllText", new BF().Add(ReadAllText_string, TypeID.String)
                                                 .Add(ReadAllText_string_string, TypeID.String, TypeID.String)
                                                 .Add(ReadAllText_string_int, TypeID.String, TypeID.Int)
                                                 .Add(ReadAllText_string_object, TypeID.String, TypeID.Object));
            AddFunc("WriteAllText", new BF().Add(WriteAllText_string_string, TypeID.String, TypeID.String)
                                                  .Add(WriteAllText_string_string_string, TypeID.String, TypeID.String, TypeID.String)
                                                  .Add(WriteAllText_string_string_int, TypeID.String, TypeID.String, TypeID.Int)
                                                  .Add(WriteAllText_string_string_object, TypeID.String, TypeID.String, TypeID.Object));
            AddFunc("AppendAllText", new BF().Add(AppendAllText_string_string, TypeID.String, TypeID.String)
                                                  .Add(AppendAllText_string_string_string, TypeID.String, TypeID.String, TypeID.String)
                                                  .Add(AppendAllText_string_string_int, TypeID.String, TypeID.String, TypeID.Int)
                                                  .Add(AppendAllText_string_string_object, TypeID.String, TypeID.String, TypeID.Object));
            AddFunc("GetEncoding", new BF().Add(GetEncoding_int, TypeID.Int)
                                                  .Add(GetEncoding_string, TypeID.String));
            AddFunc("CopyFile", new BF().Add(CopyFile_string_string, TypeID.String, TypeID.String)
                                              .Add(CopyFile_string_string_bool, TypeID.String, TypeID.String, TypeID.Bool));
            AddFunc("MoveFile", new BF().Add(MoveFile_string_string, TypeID.String, TypeID.String)
                                              .Add(MoveFile_string_string_bool, TypeID.String, TypeID.String, TypeID.Bool));
            AddFunc("DeleteFile", new BF().Add(DeleteFile_string, TypeID.String));
            AddFunc("CreateDirectory", new BF().Add(CreateDir_string, TypeID.String));
            AddFunc("DeleteDirectory", new BF().Add(DeleteDir_string, TypeID.String)
                                                    .Add(DeleteDir_string_bool, TypeID.String, TypeID.Bool));
            AddFunc("MoveDirectory", new BF().Add(MoveDir_string_string, TypeID.String, TypeID.String));

            AddFunc("GetFiles", new BF().Add(GetFiles_string, TypeID.String)
                                                .Add(GetFiles_string_string, TypeID.String, TypeID.String)
                                                .Add(GetFiles_string_string_bool, TypeID.String, TypeID.String, TypeID.Bool));
            AddFunc("GetDirectories", new BF().Add(GetDirs_string, TypeID.String)
                                                    .Add(GetDirs_string_string, TypeID.String, TypeID.String)
                                                    .Add(GetDirs_string_string_bool, TypeID.String, TypeID.String, TypeID.Bool));
            AddFunc("GetFileName", new BF().Add(GetFileName_string, TypeID.String));
            AddFunc("FileSize", new BF().Add(FileSize_string, TypeID.String));
            AddFunc("FileExists", new BF().Add(FileExists_string, TypeID.String));
            AddFunc("DirectoryExists", new BF().Add(DirExists_string, TypeID.String));
            AddFunc("GetFileNameWithoutExtension", new BF().Add(GetFileNameWithoutExtension_string, TypeID.String));
            AddFunc("GetExtension", new BF().Add(GetExtension_string, TypeID.String));
            AddFunc("ToJson", new BF().Add(ToJson_object, TypeID.Object).Add(ToJson_object_bool, TypeID.Object, TypeID.Bool));
            AddFunc("FromJson", new BF().Add(FromJson_string_customType, TypeID.String, TypeID.CustomType).Return(1, true)
                                              .Add(FromJson_string_object, TypeID.String, TypeID.Object)
                                              .Add(FromJson_string_string, TypeID.String, TypeID.String)
                                              .Add(FromJson_string_customType_bool, TypeID.String, TypeID.CustomType, TypeID.Bool).Return(1, true)
                                              .Add(FromJson_string_object_bool, TypeID.String, TypeID.Object, TypeID.Bool)
                                              .Add(FromJson_string_string_bool, TypeID.String, TypeID.String, TypeID.Bool));
            AddFunc("StartThread", new BF().Add(StartThread_object, TypeID.Object)
                                                  .Add(StartThread_object_objectParams, TypeID.Object).HasParams(TypeID.Object));
            AddFunc("Thread", new BF().Add(CurrentThread)
                                      .Add(Thread_object, TypeID.Object)
                                      .Add(Thread_object_string, TypeID.Object, TypeID.String)
                                      .Add(Thread_object_bool, TypeID.Object, TypeID.Bool)
                                      .Add(Thread_object_string_bool, TypeID.Object, TypeID.String, TypeID.Bool));
            AddFunc("ThreadByFuncRef", new BF().Add(ThreadByFuncRef_object, TypeID.Object)
                                                  .Add(ThreadByFuncRef_object_string, TypeID.Object, TypeID.String)
                                                  .Add(ThreadByFuncRef_object_bool, TypeID.Object, TypeID.Bool)
                                                  .Add(ThreadByFuncRef_object_string_bool, TypeID.Object, TypeID.String, TypeID.Bool)
                                                  );

            AddFunc("Task", new BF().Add(Task_object_objectParams, TypeID.Object).HasParams(TypeID.Object));
            AddFunc("TaskByFuncRef", new BF().Add(TaskByFuncRef_object_objectParams, TypeID.Object).HasParams(TypeID.Object));
            AddFunc("StartTask", new BF().Add(StartTask_object, TypeID.Object));
            AddFunc("LocalThread", new BF().Add(LocalThread_object, TypeID.Object)
                                                   .Add(LocalThread_object_string, TypeID.Object, TypeID.String)
                                                   .Add(LocalThread_object_bool, TypeID.Object, TypeID.Bool)
                                                   .Add(LocalThread_object_string_bool, TypeID.Object, TypeID.String, TypeID.Bool));
            AddFunc("LocalThreadByExpr", new BF().Add(LocalThreadByExpr_object, TypeID.Object)
                                                  .Add(LocalThreadByExpr_object_string, TypeID.Object, TypeID.String)
                                                  .Add(LocalThreadByExpr_object_bool, TypeID.Object, TypeID.Bool)
                                                  .Add(LocalThreadByExpr_object_string_bool, TypeID.Object, TypeID.String, TypeID.Bool));
            AddFunc("RunLocalThread", new BF().Add(RunLocalThread_object, TypeID.Object)
                                                 .Add(RunLocalThread_object_string, TypeID.Object, TypeID.String)
                                                 .Add(RunLocalThread_object_bool, TypeID.Object, TypeID.Bool)
                                                 .Add(RunLocalThread_object_string_bool, TypeID.Object, TypeID.String, TypeID.Bool));
            AddFunc("RunThread", new BF().Add(RunThread_object, TypeID.Object)
                                                 .Add(RunThread_object_string, TypeID.Object, TypeID.String)
                                                 .Add(RunThread_object_bool, TypeID.Object, TypeID.Bool)
                                                 .Add(RunThread_object_string_bool, TypeID.Object, TypeID.String, TypeID.Bool));
            AddFunc("RunTimer", new BF().Add(RunTimer_object_int, TypeID.Object, TypeID.Int)
                                           .Add(RunTimer_object_int_bool, TypeID.Object, TypeID.Int, TypeID.Bool)
                                           .Add(RunTimer_object_int_bool_bool, TypeID.Object, TypeID.Int, TypeID.Bool, TypeID.Bool)
                                           );

            AddFunc("LocalTask", new BF().Add(LocalTask_object, TypeID.Object));
            AddFunc("LocalTaskByExpr", new BF().Add(LocalTaskByExpr_object, TypeID.Object));
            AddFunc("RunLocalTask", new BF().Add(RunLocalTask_object, TypeID.Object));
            AddFunc("RunTask", new BF().Add(RunTask_object, TypeID.Object));
            AddFunc("Wait", new BF().Add(Wait_object, TypeID.Object).Add(Wait_object_int, TypeID.Object, TypeID.Int));
            AddFunc("TaskResult", new BF().Add(TaskResult_object, TypeID.Object));
            AddFunc("TaskException", new BF().Add(TaskException_object, TypeID.Object));
            AddFunc("TaskInnerException", new BF().Add(TaskInnerException_object, TypeID.Object));
            AddFunc("TaskStatus", new BF().Add(TaskStatus_object, TypeID.Object));
            AddFunc("IsCanceled", new BF().Add(IsCanceled_object, TypeID.Object));
            AddFunc("IsCompleted", new BF().Add(IsCompleted_object, TypeID.Object));
            AddFunc("IsCompletedSuccessfully", new BF().Add(IsCompletedSuccessfully_object, TypeID.Object));
            AddFunc("IsFaulted", new BF().Add(IsFaulted_object, TypeID.Object));
            AddFunc("TaskId", new BF().Add(TaskId_object, TypeID.Object));
            AddFunc("ManagedThreadId", new BF().Add(ManagedThreadId).Add(ManagedThreadId_object, TypeID.Object));

            AddFunc("IsAlive", new BF().Add(ThreadIsAlive_object, TypeID.Object));
            AddFunc("ThreadState", new BF().Add(ThreadState_object, TypeID.Object));
            AddFunc("ThreadName", new BF().Add(ThreadName_object, TypeID.Object).Add(ThreadName));

            AddFunc("Interrupt", new BF().Add(InterruptThread_object, TypeID.Object));
            AddFunc("Sleep", new BF().Add(Sleep_int, TypeID.Int));

            AddFunc("WaitAll", new BF().Add(WaitAll_objectArray, TypeID.ObjectArray)
                                             .Add(WaitAll_objectArray_int, TypeID.ObjectArray, TypeID.Int));
            AddFunc("WaitAny", new BF().Add(WaitAny_objectArray, TypeID.ObjectArray)
                                             .Add(WaitAny_objectArray_int, TypeID.ObjectArray, TypeID.Int));
            AddFunc("SetEventWaitHandle", new BF().Add(SetEventWaitHandle_object, TypeID.Object));
            AddFunc("ResetEventWaitHandle", new BF().Add(ResetEventWaitHandle_object, TypeID.Object));

            AddFunc("CancellationTokenSource", new BF().Add(CancellationTokenSource).Add(CancellationTokenSource_int, TypeID.Int));
            AddFunc("CancellationToken", new BF().Add(CancellationToken_object, TypeID.Object));
            AddFunc("IsCancellationRequested", new BF().Add(IsCancellationRequested_object, TypeID.Object));
            AddFunc("Cancel", new BF().Add(Cancel_object, TypeID.Object).Add(Cancel_object_bool, TypeID.Object, TypeID.Bool));
            AddFunc("ThrowIfCancellationRequested", new BF().Add(ThrowIfCancellationRequested_object, TypeID.Object));

            AddFunc("CallChain", new BF().Add(CallChain).Add(CallChain_string, TypeID.String));
            AddFunc("GetTypeByName", new BF().Add(GetTypeByName_string, TypeID.String)
                                                   .Add(GetTypeByName_string_string, TypeID.String, TypeID.String)
                                                   .Add(GetTypeByName_object_string, TypeID.Object, TypeID.String));
            AddFunc("LoadAssembly", new BF().Add(LoadAssembly_string, TypeID.String));
            AddFunc("LoadAssemblyFrom", new BF().Add(LoadAssemblyFrom_string, TypeID.String));
            AddFunc("Fetch", new BF().Add(Fetch_string, TypeID.String)
                                            .Add(Fetch_string_string, TypeID.String, TypeID.String)
                                            .Add(Fetch_string_string_string, TypeID.String, TypeID.String, TypeID.String)
                                            .Add(Fetch_string_string_string_int, TypeID.String, TypeID.String, TypeID.String, TypeID.Int)
                                            .Add(Fetch_string_string_string_int_string, TypeID.String, TypeID.String, TypeID.String, TypeID.Int, TypeID.String)
                                            .Add(Fetch_string_string_string_int_object, TypeID.String, TypeID.String, TypeID.String, TypeID.Int, TypeID.Object)
                                            .Add(Fetch_string_string_string_int_object_string_string_string, TypeID.String, TypeID.String, TypeID.String, TypeID.Int, TypeID.Object, TypeID.String, TypeID.String, TypeID.String).FewerArgsAllowed()
                                            );
            AddFunc("UrlEncode", new BF().Add(UrlEncode_string, TypeID.String)
                                                .Add(UrlEncode_string_string, TypeID.String, TypeID.String)
                                                .Add(UrlEncode_string_object, TypeID.String, TypeID.Object));
            AddFunc("UrlDecode", new BF().Add(UrlDecode_string, TypeID.String)
                                                .Add(UrlDecode_string_string, TypeID.String, TypeID.String)
                                                .Add(UrlDecode_string_object, TypeID.String, TypeID.Object));
            AddFunc("Uri", new BF().Add(Uri_string, TypeID.String));

            AddFunc("Timer", new BF().Add(Timer_object_int, TypeID.Object, TypeID.Int)
                                                .Add(Timer_object_int_bool, TypeID.Object, TypeID.Int, TypeID.Bool)
                                                .Add(Timer_object_int_bool_bool, TypeID.Object, TypeID.Int, TypeID.Bool, TypeID.Bool)
                                                .Add(Timer_object_int_bool_bool_bool, TypeID.Object, TypeID.Int, TypeID.Bool, TypeID.Bool, TypeID.Bool)
                                                .Add(Timer_object_int_bool_bool_bool_objectParams, TypeID.Object, TypeID.Int, TypeID.Bool, TypeID.Bool, TypeID.Bool).HasParams(TypeID.Object)
                                                );
            AddFunc("TimerByFuncRef", new BF().Add(TimerByFuncRef_object_int, TypeID.Object, TypeID.Int)
                                               .Add(TimerByFuncRef_object_int_bool, TypeID.Object, TypeID.Int, TypeID.Bool)
                                               .Add(TimerByFuncRef_object_int_bool_bool, TypeID.Object, TypeID.Int, TypeID.Bool, TypeID.Bool)
                                               .Add(TimerByFuncRef_object_int_bool_bool_bool, TypeID.Object, TypeID.Int, TypeID.Bool, TypeID.Bool, TypeID.Bool)
                                               .Add(TimerByFuncRef_object_int_bool_bool_bool_objectParams, TypeID.Object, TypeID.Int, TypeID.Bool, TypeID.Bool, TypeID.Bool).HasParams(TypeID.Object)
                                               );
            AddFunc("LocalTimerByExpr", new BF().Add(LocalTimerByExpr_object_int, TypeID.Object, TypeID.Int)
                                              .Add(LocalTimerByExpr_object_int_bool, TypeID.Object, TypeID.Int, TypeID.Bool)
                                              .Add(LocalTimerByExpr_object_int_bool_bool, TypeID.Object, TypeID.Int, TypeID.Bool, TypeID.Bool)
                                              .Add(LocalTimerByExpr_object_int_bool_bool_bool, TypeID.Object, TypeID.Int, TypeID.Bool, TypeID.Bool, TypeID.Bool)
                                              );
            AddFunc("LocalTimer", new BF().Add(LocalTimer_object_int, TypeID.Object, TypeID.Int)
                                               .Add(LocalTimer_object_int_bool, TypeID.Object, TypeID.Int, TypeID.Bool)
                                               .Add(LocalTimer_object_int_bool_bool, TypeID.Object, TypeID.Int, TypeID.Bool, TypeID.Bool)
                                               .Add(LocalTimer_object_int_bool_bool_bool, TypeID.Object, TypeID.Int, TypeID.Bool, TypeID.Bool, TypeID.Bool)
                                               );


            AddFunc("StartTimer", new BF().Add(StartTimer_object, TypeID.Object));
            AddFunc("StopTimer", new BF().Add(StopTimer_object, TypeID.Object));
            AddFunc("Dispose", new BF().Add(DisposeObject_object, TypeID.Object));
            AddFunc("GetType", new BF().Add(GetObjectType_object, TypeID.Object));
            AddFunc("GetTypeName", new BF().Add(GetObjectTypeName_object, TypeID.Object)
                                                 .Add(GetObjectTypeName_object_bool, TypeID.Object, TypeID.Bool));
            AddFunc("GetHashCode", new BF().Add(GetObjectHashCode_object, TypeID.Object));
            AddFunc("Equals", new BF().Add(Equals_object_object, TypeID.Object, TypeID.Object));
            AddFunc("ChangeType", new BF().Add(ChangeType_object_string, TypeID.Object, TypeID.String)
                                                .Add(ChangeType_object_object, TypeID.Object, TypeID.Object));
            AddFunc("RegexMatches", new BF().Add(RegexMatches_string_string, TypeID.String, TypeID.String)
                                                    .Add(RegexMatches_string_string_int, TypeID.String, TypeID.String, TypeID.Int)
                                                    .Add(RegexMatches_string_string_object, TypeID.String, TypeID.String, TypeID.Object)
                                                    );
            AddFunc("RegexReplace", new BF().Add(RegexReplace_string_string_string, TypeID.String, TypeID.String, TypeID.String)
                                                  .Add(RegexReplace_string_string_string_int, TypeID.String, TypeID.String, TypeID.String, TypeID.Int)
                                                  .Add(RegexReplace_string_string_string_object, TypeID.String, TypeID.String, TypeID.String, TypeID.Object)
                                                  );
            AddFunc("CombinePath", new BF().Add(CombinePath_stringParams).HasParams(TypeID.String));
            AddFunc("ReadLine", new BF().Add(ReadLine).Add(ReadLine_string, TypeID.String));
            AddFunc("ReadKey", new BF().Add(ReadKey).Add(ReadKey_bool, TypeID.Bool).Add(ReadKey_string, TypeID.String).Add(ReadKey_string_bool, TypeID.String, TypeID.Bool)
                                       .Add(ReadKey_string_bool_bool, TypeID.String, TypeID.Bool, TypeID.Bool).Add(ReadKey_bool_bool, TypeID.Bool, TypeID.Bool));
            AddFunc("KeyModifiers", new BF().Add(KeyModifiers_object, TypeID.Object));

            AddFunc("PadRight", new BF().Add(PadRight_string_int, TypeID.String, TypeID.Int).Add(PadRight_string_int_char, TypeID.String, TypeID.Int, TypeID.Char));
            AddFunc("PadLeft", new BF().Add(PadLeft_string_int, TypeID.String, TypeID.Int).Add(PadLeft_string_int_char, TypeID.String, TypeID.Int, TypeID.Char));
            AddFunc("If", new BF().Add(If_bool_int_int, TypeID.Bool, TypeID.Int, TypeID.Int)
                                        .Add(If_bool_byte_byte, TypeID.Bool, TypeID.Byte, TypeID.Byte)
                                        .Add(If_bool_string_string, TypeID.Bool, TypeID.String, TypeID.String)
                                        .Add(If_bool_char_char, TypeID.Bool, TypeID.Char, TypeID.Char)
                                        .Add(If_bool_long_long, TypeID.Bool, TypeID.Long, TypeID.Long)
                                        .Add(If_bool_double_double, TypeID.Bool, TypeID.Double, TypeID.Double)
                                        .Add(If_bool_float_float, TypeID.Bool, TypeID.Float, TypeID.Float)
                                        .Add(If_bool_bool_bool, TypeID.Bool, TypeID.Bool, TypeID.Bool)
                                        .Add(If_bool_decimal_decimal, TypeID.Bool, TypeID.Decimal, TypeID.Decimal)
                                        .Add(If_bool_object_object, TypeID.Bool, TypeID.Object, TypeID.Object)
                                        .Add(If_bool_short_short, TypeID.Bool, TypeID.Short, TypeID.Short)
                                        .Add(If_bool_date_date, TypeID.Bool, TypeID.Date, TypeID.Date)
                                        .Add(If_bool_custom_custom, TypeID.Bool, TypeID.Custom, TypeID.Custom).Return2(1, 2)
                                        .Add(If_bool_intArray_intArray, TypeID.Bool, TypeID.IntArray, TypeID.IntArray)
                                        .Add(If_bool_byteArray_byteArray, TypeID.Bool, TypeID.ByteArray, TypeID.ByteArray)
                                        .Add(If_bool_stringArray_stringArray, TypeID.Bool, TypeID.StringArray, TypeID.StringArray)
                                        .Add(If_bool_charArray_charArray, TypeID.Bool, TypeID.CharArray, TypeID.CharArray)
                                        .Add(If_bool_longArray_longArray, TypeID.Bool, TypeID.LongArray, TypeID.LongArray)
                                        .Add(If_bool_doubleArray_doubleArray, TypeID.Bool, TypeID.DoubleArray, TypeID.DoubleArray)
                                        .Add(If_bool_floatArray_floatArray, TypeID.Bool, TypeID.FloatArray, TypeID.FloatArray)
                                        .Add(If_bool_boolArray_boolArray, TypeID.Bool, TypeID.BoolArray, TypeID.BoolArray)
                                        .Add(If_bool_decimalArray_decimalArray, TypeID.Bool, TypeID.DecimalArray, TypeID.DecimalArray)
                                        .Add(If_bool_objectArray_objectArray, TypeID.Bool, TypeID.ObjectArray, TypeID.ObjectArray)
                                        .Add(If_bool_shortArray_shortArray, TypeID.Bool, TypeID.ShortArray, TypeID.ShortArray)
                                        .Add(If_bool_dateArray_dateArray, TypeID.Bool, TypeID.DateArray, TypeID.DateArray)
                                        .Add(If_bool_customArray_customArray, TypeID.Bool, TypeID.CustomArray, TypeID.CustomArray).Return2(1, 2)
                                        );
            AddFunc("IfNotNull", new BF().Add(IfNotNull_object_string, TypeID.Object, TypeID.String)
                                     .Add(IfNotNull_object_object, TypeID.Object, TypeID.Object)
                                     .Add(IfNotNull_object_custom, TypeID.Object, TypeID.Custom).Return(1)
                                     .Add(IfNotNull_object_intArray, TypeID.Object, TypeID.IntArray)
                                     .Add(IfNotNull_object_byteArray, TypeID.Object, TypeID.ByteArray)
                                     .Add(IfNotNull_object_stringArray, TypeID.Object, TypeID.StringArray)
                                     .Add(IfNotNull_object_charArray, TypeID.Object, TypeID.CharArray)
                                     .Add(IfNotNull_object_longArray, TypeID.Object, TypeID.LongArray)
                                     .Add(IfNotNull_object_doubleArray, TypeID.Object, TypeID.DoubleArray)
                                     .Add(IfNotNull_object_floatArray, TypeID.Object, TypeID.FloatArray)
                                     .Add(IfNotNull_object_boolArray, TypeID.Object, TypeID.BoolArray)
                                     .Add(IfNotNull_object_decimalArray, TypeID.Object, TypeID.DecimalArray)
                                     .Add(IfNotNull_object_objectArray, TypeID.Object, TypeID.ObjectArray)
                                     .Add(IfNotNull_object_shortArray, TypeID.Object, TypeID.ShortArray)
                                     .Add(IfNotNull_object_dateArray, TypeID.Object, TypeID.DateArray)
                                     .Add(IfNotNull_object_customArray, TypeID.Object, TypeID.CustomArray).Return(1)
                                     );
            AddFunc("For", new BF().Add(For_int_int_int_int_objectParams, TypeID.Int, TypeID.Int, TypeID.Int, TypeID.Int).HasParams(TypeID.Object).Kind(3, EvalUnitKind.Variable).Strict(3)
                                        .Add(For_int_int_int_int_objectParams, TypeID.Int, TypeID.Int, TypeID.Int, TypeID.Empty).HasParams(TypeID.Object));
            AddFunc("ForEach", new BF().Add(ForEach_intArray_int_objectParams, TypeID.IntArray, TypeID.Int).HasParams(TypeID.Object)
                                            .Add(ForEach_byteArray_byte_objectParams, TypeID.ByteArray, TypeID.Byte).HasParams(TypeID.Object)
                                            .Add(ForEach_shortArray_short_objectParams, TypeID.ShortArray, TypeID.Short).HasParams(TypeID.Object)
                                            .Add(ForEach_longArray_long_objectParams, TypeID.LongArray, TypeID.Long).HasParams(TypeID.Object)
                                            .Add(ForEach_floatArray_float_objectParams, TypeID.FloatArray, TypeID.Float).HasParams(TypeID.Object)
                                            .Add(ForEach_doubleArray_double_objectParams, TypeID.DoubleArray, TypeID.Double).HasParams(TypeID.Object)
                                            .Add(ForEach_decimalArray_decimal_objectParams, TypeID.DecimalArray, TypeID.Decimal).HasParams(TypeID.Object)
                                            .Add(ForEach_charArray_char_objectParams, TypeID.CharArray, TypeID.Char).HasParams(TypeID.Object)
                                            .Add(ForEach_dateArray_date_objectParams, TypeID.DateArray, TypeID.Date).HasParams(TypeID.Object)
                                            .Add(ForEach_stringArray_string_objectParams, TypeID.StringArray, TypeID.String).HasParams(TypeID.Object)
                                            .Add(ForEach_boolArray_bool_objectParams, TypeID.BoolArray, TypeID.Bool).HasParams(TypeID.Object)
                                            .Add(ForEach_objectArray_object_objectParams, TypeID.ObjectArray, TypeID.Object).HasParams(TypeID.Object)
                                            .Add(ForEach_customArray_custom_objectParams, TypeID.CustomArray, TypeID.Custom).HasParams(TypeID.Object)

                                            .Add(ForEach_object_int_objectParams, TypeID.Object, TypeID.Int).HasParams(TypeID.Object)
                                            .Add(ForEach_object_byte_objectParams, TypeID.Object, TypeID.Byte).HasParams(TypeID.Object)
                                            .Add(ForEach_object_short_objectParams, TypeID.Object, TypeID.Short).HasParams(TypeID.Object)
                                            .Add(ForEach_object_long_objectParams, TypeID.Object, TypeID.Long).HasParams(TypeID.Object)
                                            .Add(ForEach_object_float_objectParams, TypeID.Object, TypeID.Float).HasParams(TypeID.Object)
                                            .Add(ForEach_object_double_objectParams, TypeID.Object, TypeID.Double).HasParams(TypeID.Object)
                                            .Add(ForEach_object_decimal_objectParams, TypeID.Object, TypeID.Decimal).HasParams(TypeID.Object)
                                            .Add(ForEach_object_char_objectParams, TypeID.Object, TypeID.Char).HasParams(TypeID.Object)
                                            .Add(ForEach_object_date_objectParams, TypeID.Object, TypeID.Date).HasParams(TypeID.Object)
                                            .Add(ForEach_object_string_objectParams, TypeID.Object, TypeID.String).HasParams(TypeID.Object)
                                            .Add(ForEach_object_bool_objectParams, TypeID.Object, TypeID.Bool).HasParams(TypeID.Object)
                                            .Add(ForEach_object_object_objectParams, TypeID.Object, TypeID.Object).HasParams(TypeID.Object)
                                            .Add(ForEach_object_custom_objectParams, TypeID.Object, TypeID.Custom).HasParams(TypeID.Object)

                                            .Add(ForEach_object_int_objectParams, TypeID.Custom, TypeID.Int).HasParams(TypeID.Object)
                                            .Add(ForEach_object_byte_objectParams, TypeID.Custom, TypeID.Byte).HasParams(TypeID.Object)
                                            .Add(ForEach_object_short_objectParams, TypeID.Custom, TypeID.Short).HasParams(TypeID.Object)
                                            .Add(ForEach_object_long_objectParams, TypeID.Custom, TypeID.Long).HasParams(TypeID.Object)
                                            .Add(ForEach_object_float_objectParams, TypeID.Custom, TypeID.Float).HasParams(TypeID.Object)
                                            .Add(ForEach_object_double_objectParams, TypeID.Custom, TypeID.Double).HasParams(TypeID.Object)
                                            .Add(ForEach_object_decimal_objectParams, TypeID.Custom, TypeID.Decimal).HasParams(TypeID.Object)
                                            .Add(ForEach_object_char_objectParams, TypeID.Custom, TypeID.Char).HasParams(TypeID.Object)
                                            .Add(ForEach_object_date_objectParams, TypeID.Custom, TypeID.Date).HasParams(TypeID.Object)
                                            .Add(ForEach_object_string_objectParams, TypeID.Custom, TypeID.String).HasParams(TypeID.Object)
                                            .Add(ForEach_object_bool_objectParams, TypeID.Custom, TypeID.Bool).HasParams(TypeID.Object)
                                            .Add(ForEach_object_object_objectParams, TypeID.Custom, TypeID.Object).HasParams(TypeID.Object)
                                            .Add(ForEach_object_custom_objectParams, TypeID.Custom, TypeID.Custom).HasParams(TypeID.Object)
                                            .ConvertAll(x => x.Strict(0, 1).Kind(1, EvalUnitKind.Variable))
                                            );
            AddFunc("DoWhile", new BF().Add(DoWhile_bool_objectParams, TypeID.Bool).HasParams(TypeID.Object));
            AddFunc("While", new BF().Add(While_bool_objectParams, TypeID.Bool).HasParams(TypeID.Object));
            AddFunc("ClearConsole", new BF().Add(ClearConsole));
            AddFunc("AppInfo", new BF().Add(GetAppInfo).Add(GetAppInfo_string, TypeID.String));
            AddFunc("Rand", new BF().Add(Rand_int_int, TypeID.Int, TypeID.Int));

            AddFunc("ToEnum", new BF().Add(ToEnum_string_string, TypeID.String, TypeID.String)
                                            .Add(ToEnum_string_object, TypeID.String, TypeID.Object)
                                            .Add(ToEnum_object_object, TypeID.Object, TypeID.Object)
                                            .Add(ToEnum_object_string, TypeID.Object, TypeID.String));
            AddFunc("CommandLine", new BF().Add(CommandLine));
            AddFunc("StartArgs", new BF().Add(StartArgs));
            AddFunc("StartProcess", new BF().Add(StartProcess_object, TypeID.Object)
                                                  .Add(StartProcess_object_bool, TypeID.Object, TypeID.Bool)
                                                  .Add(StartProcess_string, TypeID.String)
                                                  .Add(StartProcess_string_string, TypeID.String, TypeID.String)
                                                  .Add(StartProcess_string_string_int, TypeID.String, TypeID.String, TypeID.Int)
                                                  .Add(StartProcess_string_string_object, TypeID.String, TypeID.String, TypeID.Object)
                                                  .Add(StartProcess_string_string_int_bool, TypeID.String, TypeID.String, TypeID.Int, TypeID.Bool)
                                                  .Add(StartProcess_string_string_object_bool, TypeID.String, TypeID.String, TypeID.Object, TypeID.Bool)
                                                  );
            AddFunc("AppPath", new BF().Add(AppPath));
            AddFunc("CurrentDirectory", new BF().Add(CurrentDirectory));
            AddFunc("SetCurrentDirectory", new BF().Add(SetCurrentDirectory_string, TypeID.String));

            AddFunc("DateDiff", new BF().Add(DateDiff_date_date, TypeID.Date, TypeID.Date)
                                              .Add(DateDiff_date_date_char, TypeID.Date, TypeID.Date, TypeID.Char));
            AddFunc("DateAdd", new BF().Add(DateAdd_date_double, TypeID.Date, TypeID.Double)
                                            .Add(DateAdd_date_double_char, TypeID.Date, TypeID.Double, TypeID.Char));
            AddFunc("Select", new BF().Add(Select_intArray_int_byte_bool, TypeID.IntArray, TypeID.Int, TypeID.Byte, TypeID.Bool)
                                            .Add(Select_intArray_int_short_bool, TypeID.IntArray, TypeID.Int, TypeID.Short, TypeID.Bool)
                                            .Add(Select_intArray_int_int_bool, TypeID.IntArray, TypeID.Int, TypeID.Int, TypeID.Bool)
                                            .Add(Select_intArray_int_long_bool, TypeID.IntArray, TypeID.Int, TypeID.Long, TypeID.Bool)
                                            .Add(Select_intArray_int_float_bool, TypeID.IntArray, TypeID.Int, TypeID.Float, TypeID.Bool)
                                            .Add(Select_intArray_int_double_bool, TypeID.IntArray, TypeID.Int, TypeID.Double, TypeID.Bool)
                                            .Add(Select_intArray_int_decimal_bool, TypeID.IntArray, TypeID.Int, TypeID.Decimal, TypeID.Bool)
                                            .Add(Select_intArray_int_char_bool, TypeID.IntArray, TypeID.Int, TypeID.Char, TypeID.Bool)
                                            .Add(Select_intArray_int_string_bool, TypeID.IntArray, TypeID.Int, TypeID.String, TypeID.Bool)
                                            .Add(Select_intArray_int_object_bool, TypeID.IntArray, TypeID.Int, TypeID.Object, TypeID.Bool)
                                            .Add(Select_intArray_int_bool_bool, TypeID.IntArray, TypeID.Int, TypeID.Bool, TypeID.Bool)
                                            .Add(Select_intArray_int_date_bool, TypeID.IntArray, TypeID.Int, TypeID.Date, TypeID.Bool)
                                            .Add(Select_intArray_int_custom_bool, TypeID.IntArray, TypeID.Int, TypeID.Custom, TypeID.Bool).Return(2, false, true)
                                            .Add(Select_byteArray_byte_byte_bool, TypeID.ByteArray, TypeID.Byte, TypeID.Byte, TypeID.Bool)
                                            .Add(Select_byteArray_byte_short_bool, TypeID.ByteArray, TypeID.Byte, TypeID.Short, TypeID.Bool)
                                            .Add(Select_byteArray_byte_int_bool, TypeID.ByteArray, TypeID.Byte, TypeID.Int, TypeID.Bool)
                                            .Add(Select_byteArray_byte_long_bool, TypeID.ByteArray, TypeID.Byte, TypeID.Long, TypeID.Bool)
                                            .Add(Select_byteArray_byte_float_bool, TypeID.ByteArray, TypeID.Byte, TypeID.Float, TypeID.Bool)
                                            .Add(Select_byteArray_byte_double_bool, TypeID.ByteArray, TypeID.Byte, TypeID.Double, TypeID.Bool)
                                            .Add(Select_byteArray_byte_decimal_bool, TypeID.ByteArray, TypeID.Byte, TypeID.Decimal, TypeID.Bool)
                                            .Add(Select_byteArray_byte_char_bool, TypeID.ByteArray, TypeID.Byte, TypeID.Char, TypeID.Bool)
                                            .Add(Select_byteArray_byte_string_bool, TypeID.ByteArray, TypeID.Byte, TypeID.String, TypeID.Bool)
                                            .Add(Select_byteArray_byte_object_bool, TypeID.ByteArray, TypeID.Byte, TypeID.Object, TypeID.Bool)
                                            .Add(Select_byteArray_byte_bool_bool, TypeID.ByteArray, TypeID.Byte, TypeID.Bool, TypeID.Bool)
                                            .Add(Select_byteArray_byte_date_bool, TypeID.ByteArray, TypeID.Byte, TypeID.Date, TypeID.Bool)
                                            .Add(Select_byteArray_byte_custom_bool, TypeID.ByteArray, TypeID.Byte, TypeID.Custom, TypeID.Bool).Return(2, false, true)
                                            .Add(Select_shortArray_short_byte_bool, TypeID.ShortArray, TypeID.Short, TypeID.Byte, TypeID.Bool)
                                            .Add(Select_shortArray_short_short_bool, TypeID.ShortArray, TypeID.Short, TypeID.Short, TypeID.Bool)
                                            .Add(Select_shortArray_short_int_bool, TypeID.ShortArray, TypeID.Short, TypeID.Int, TypeID.Bool)
                                            .Add(Select_shortArray_short_long_bool, TypeID.ShortArray, TypeID.Short, TypeID.Long, TypeID.Bool)
                                            .Add(Select_shortArray_short_float_bool, TypeID.ShortArray, TypeID.Short, TypeID.Float, TypeID.Bool)
                                            .Add(Select_shortArray_short_double_bool, TypeID.ShortArray, TypeID.Short, TypeID.Double, TypeID.Bool)
                                            .Add(Select_shortArray_short_decimal_bool, TypeID.ShortArray, TypeID.Short, TypeID.Decimal, TypeID.Bool)
                                            .Add(Select_shortArray_short_char_bool, TypeID.ShortArray, TypeID.Short, TypeID.Char, TypeID.Bool)
                                            .Add(Select_shortArray_short_string_bool, TypeID.ShortArray, TypeID.Short, TypeID.String, TypeID.Bool)
                                            .Add(Select_shortArray_short_object_bool, TypeID.ShortArray, TypeID.Short, TypeID.Object, TypeID.Bool)
                                            .Add(Select_shortArray_short_bool_bool, TypeID.ShortArray, TypeID.Short, TypeID.Bool, TypeID.Bool)
                                            .Add(Select_shortArray_short_date_bool, TypeID.ShortArray, TypeID.Short, TypeID.Date, TypeID.Bool)
                                            .Add(Select_shortArray_short_custom_bool, TypeID.ShortArray, TypeID.Short, TypeID.Custom, TypeID.Bool).Return(2, false, true)
                                            .Add(Select_longArray_long_byte_bool, TypeID.LongArray, TypeID.Long, TypeID.Byte, TypeID.Bool)
                                            .Add(Select_longArray_long_short_bool, TypeID.LongArray, TypeID.Long, TypeID.Short, TypeID.Bool)
                                            .Add(Select_longArray_long_int_bool, TypeID.LongArray, TypeID.Long, TypeID.Int, TypeID.Bool)
                                            .Add(Select_longArray_long_long_bool, TypeID.LongArray, TypeID.Long, TypeID.Long, TypeID.Bool)
                                            .Add(Select_longArray_long_float_bool, TypeID.LongArray, TypeID.Long, TypeID.Float, TypeID.Bool)
                                            .Add(Select_longArray_long_double_bool, TypeID.LongArray, TypeID.Long, TypeID.Double, TypeID.Bool)
                                            .Add(Select_longArray_long_decimal_bool, TypeID.LongArray, TypeID.Long, TypeID.Decimal, TypeID.Bool)
                                            .Add(Select_longArray_long_char_bool, TypeID.LongArray, TypeID.Long, TypeID.Char, TypeID.Bool)
                                            .Add(Select_longArray_long_string_bool, TypeID.LongArray, TypeID.Long, TypeID.String, TypeID.Bool)
                                            .Add(Select_longArray_long_object_bool, TypeID.LongArray, TypeID.Long, TypeID.Object, TypeID.Bool)
                                            .Add(Select_longArray_long_bool_bool, TypeID.LongArray, TypeID.Long, TypeID.Bool, TypeID.Bool)
                                            .Add(Select_longArray_long_date_bool, TypeID.LongArray, TypeID.Long, TypeID.Date, TypeID.Bool)
                                            .Add(Select_longArray_long_custom_bool, TypeID.LongArray, TypeID.Long, TypeID.Custom, TypeID.Bool).Return(2, false, true)
                                            .Add(Select_floatArray_float_byte_bool, TypeID.FloatArray, TypeID.Float, TypeID.Byte, TypeID.Bool)
                                            .Add(Select_floatArray_float_short_bool, TypeID.FloatArray, TypeID.Float, TypeID.Short, TypeID.Bool)
                                            .Add(Select_floatArray_float_int_bool, TypeID.FloatArray, TypeID.Float, TypeID.Int, TypeID.Bool)
                                            .Add(Select_floatArray_float_long_bool, TypeID.FloatArray, TypeID.Float, TypeID.Long, TypeID.Bool)
                                            .Add(Select_floatArray_float_float_bool, TypeID.FloatArray, TypeID.Float, TypeID.Float, TypeID.Bool)
                                            .Add(Select_floatArray_float_double_bool, TypeID.FloatArray, TypeID.Float, TypeID.Double, TypeID.Bool)
                                            .Add(Select_floatArray_float_decimal_bool, TypeID.FloatArray, TypeID.Float, TypeID.Decimal, TypeID.Bool)
                                            .Add(Select_floatArray_float_char_bool, TypeID.FloatArray, TypeID.Float, TypeID.Char, TypeID.Bool)
                                            .Add(Select_floatArray_float_string_bool, TypeID.FloatArray, TypeID.Float, TypeID.String, TypeID.Bool)
                                            .Add(Select_floatArray_float_object_bool, TypeID.FloatArray, TypeID.Float, TypeID.Object, TypeID.Bool)
                                            .Add(Select_floatArray_float_bool_bool, TypeID.FloatArray, TypeID.Float, TypeID.Bool, TypeID.Bool)
                                            .Add(Select_floatArray_float_date_bool, TypeID.FloatArray, TypeID.Float, TypeID.Date, TypeID.Bool)
                                            .Add(Select_floatArray_float_custom_bool, TypeID.FloatArray, TypeID.Float, TypeID.Custom, TypeID.Bool).Return(2, false, true)
                                            .Add(Select_doubleArray_double_byte_bool, TypeID.DoubleArray, TypeID.Double, TypeID.Byte, TypeID.Bool)
                                            .Add(Select_doubleArray_double_short_bool, TypeID.DoubleArray, TypeID.Double, TypeID.Short, TypeID.Bool)
                                            .Add(Select_doubleArray_double_int_bool, TypeID.DoubleArray, TypeID.Double, TypeID.Int, TypeID.Bool)
                                            .Add(Select_doubleArray_double_long_bool, TypeID.DoubleArray, TypeID.Double, TypeID.Long, TypeID.Bool)
                                            .Add(Select_doubleArray_double_float_bool, TypeID.DoubleArray, TypeID.Double, TypeID.Float, TypeID.Bool)
                                            .Add(Select_doubleArray_double_double_bool, TypeID.DoubleArray, TypeID.Double, TypeID.Double, TypeID.Bool)
                                            .Add(Select_doubleArray_double_decimal_bool, TypeID.DoubleArray, TypeID.Double, TypeID.Decimal, TypeID.Bool)
                                            .Add(Select_doubleArray_double_char_bool, TypeID.DoubleArray, TypeID.Double, TypeID.Char, TypeID.Bool)
                                            .Add(Select_doubleArray_double_string_bool, TypeID.DoubleArray, TypeID.Double, TypeID.String, TypeID.Bool)
                                            .Add(Select_doubleArray_double_object_bool, TypeID.DoubleArray, TypeID.Double, TypeID.Object, TypeID.Bool)
                                            .Add(Select_doubleArray_double_bool_bool, TypeID.DoubleArray, TypeID.Double, TypeID.Bool, TypeID.Bool)
                                            .Add(Select_doubleArray_double_date_bool, TypeID.DoubleArray, TypeID.Double, TypeID.Date, TypeID.Bool)
                                            .Add(Select_doubleArray_double_custom_bool, TypeID.DoubleArray, TypeID.Double, TypeID.Custom, TypeID.Bool).Return(2, false, true)
                                            .Add(Select_decimalArray_decimal_byte_bool, TypeID.DecimalArray, TypeID.Decimal, TypeID.Byte, TypeID.Bool)
                                            .Add(Select_decimalArray_decimal_short_bool, TypeID.DecimalArray, TypeID.Decimal, TypeID.Short, TypeID.Bool)
                                            .Add(Select_decimalArray_decimal_int_bool, TypeID.DecimalArray, TypeID.Decimal, TypeID.Int, TypeID.Bool)
                                            .Add(Select_decimalArray_decimal_long_bool, TypeID.DecimalArray, TypeID.Decimal, TypeID.Long, TypeID.Bool)
                                            .Add(Select_decimalArray_decimal_float_bool, TypeID.DecimalArray, TypeID.Decimal, TypeID.Float, TypeID.Bool)
                                            .Add(Select_decimalArray_decimal_double_bool, TypeID.DecimalArray, TypeID.Decimal, TypeID.Double, TypeID.Bool)
                                            .Add(Select_decimalArray_decimal_decimal_bool, TypeID.DecimalArray, TypeID.Decimal, TypeID.Decimal, TypeID.Bool)
                                            .Add(Select_decimalArray_decimal_char_bool, TypeID.DecimalArray, TypeID.Decimal, TypeID.Char, TypeID.Bool)
                                            .Add(Select_decimalArray_decimal_string_bool, TypeID.DecimalArray, TypeID.Decimal, TypeID.String, TypeID.Bool)
                                            .Add(Select_decimalArray_decimal_object_bool, TypeID.DecimalArray, TypeID.Decimal, TypeID.Object, TypeID.Bool)
                                            .Add(Select_decimalArray_decimal_bool_bool, TypeID.DecimalArray, TypeID.Decimal, TypeID.Bool, TypeID.Bool)
                                            .Add(Select_decimalArray_decimal_date_bool, TypeID.DecimalArray, TypeID.Decimal, TypeID.Date, TypeID.Bool)
                                            .Add(Select_decimalArray_decimal_custom_bool, TypeID.DecimalArray, TypeID.Decimal, TypeID.Custom, TypeID.Bool).Return(2, false, true)
                                            .Add(Select_charArray_char_byte_bool, TypeID.CharArray, TypeID.Char, TypeID.Byte, TypeID.Bool)
                                            .Add(Select_charArray_char_short_bool, TypeID.CharArray, TypeID.Char, TypeID.Short, TypeID.Bool)
                                            .Add(Select_charArray_char_int_bool, TypeID.CharArray, TypeID.Char, TypeID.Int, TypeID.Bool)
                                            .Add(Select_charArray_char_long_bool, TypeID.CharArray, TypeID.Char, TypeID.Long, TypeID.Bool)
                                            .Add(Select_charArray_char_float_bool, TypeID.CharArray, TypeID.Char, TypeID.Float, TypeID.Bool)
                                            .Add(Select_charArray_char_double_bool, TypeID.CharArray, TypeID.Char, TypeID.Double, TypeID.Bool)
                                            .Add(Select_charArray_char_decimal_bool, TypeID.CharArray, TypeID.Char, TypeID.Decimal, TypeID.Bool)
                                            .Add(Select_charArray_char_char_bool, TypeID.CharArray, TypeID.Char, TypeID.Char, TypeID.Bool)
                                            .Add(Select_charArray_char_string_bool, TypeID.CharArray, TypeID.Char, TypeID.String, TypeID.Bool)
                                            .Add(Select_charArray_char_object_bool, TypeID.CharArray, TypeID.Char, TypeID.Object, TypeID.Bool)
                                            .Add(Select_charArray_char_bool_bool, TypeID.CharArray, TypeID.Char, TypeID.Bool, TypeID.Bool)
                                            .Add(Select_charArray_char_date_bool, TypeID.CharArray, TypeID.Char, TypeID.Date, TypeID.Bool)
                                            .Add(Select_charArray_char_custom_bool, TypeID.CharArray, TypeID.Char, TypeID.Custom, TypeID.Bool).Return(2, false, true)
                                            .Add(Select_dateArray_date_byte_bool, TypeID.DateArray, TypeID.Date, TypeID.Byte, TypeID.Bool)
                                            .Add(Select_dateArray_date_short_bool, TypeID.DateArray, TypeID.Date, TypeID.Short, TypeID.Bool)
                                            .Add(Select_dateArray_date_int_bool, TypeID.DateArray, TypeID.Date, TypeID.Int, TypeID.Bool)
                                            .Add(Select_dateArray_date_long_bool, TypeID.DateArray, TypeID.Date, TypeID.Long, TypeID.Bool)
                                            .Add(Select_dateArray_date_float_bool, TypeID.DateArray, TypeID.Date, TypeID.Float, TypeID.Bool)
                                            .Add(Select_dateArray_date_double_bool, TypeID.DateArray, TypeID.Date, TypeID.Double, TypeID.Bool)
                                            .Add(Select_dateArray_date_decimal_bool, TypeID.DateArray, TypeID.Date, TypeID.Decimal, TypeID.Bool)
                                            .Add(Select_dateArray_date_char_bool, TypeID.DateArray, TypeID.Date, TypeID.Char, TypeID.Bool)
                                            .Add(Select_dateArray_date_string_bool, TypeID.DateArray, TypeID.Date, TypeID.String, TypeID.Bool)
                                            .Add(Select_dateArray_date_object_bool, TypeID.DateArray, TypeID.Date, TypeID.Object, TypeID.Bool)
                                            .Add(Select_dateArray_date_bool_bool, TypeID.DateArray, TypeID.Date, TypeID.Bool, TypeID.Bool)
                                            .Add(Select_dateArray_date_date_bool, TypeID.DateArray, TypeID.Date, TypeID.Date, TypeID.Bool)
                                            .Add(Select_dateArray_date_custom_bool, TypeID.DateArray, TypeID.Date, TypeID.Custom, TypeID.Bool).Return(2, false, true)
                                            .Add(Select_stringArray_string_byte_bool, TypeID.StringArray, TypeID.String, TypeID.Byte, TypeID.Bool)
                                            .Add(Select_stringArray_string_short_bool, TypeID.StringArray, TypeID.String, TypeID.Short, TypeID.Bool)
                                            .Add(Select_stringArray_string_int_bool, TypeID.StringArray, TypeID.String, TypeID.Int, TypeID.Bool)
                                            .Add(Select_stringArray_string_long_bool, TypeID.StringArray, TypeID.String, TypeID.Long, TypeID.Bool)
                                            .Add(Select_stringArray_string_float_bool, TypeID.StringArray, TypeID.String, TypeID.Float, TypeID.Bool)
                                            .Add(Select_stringArray_string_double_bool, TypeID.StringArray, TypeID.String, TypeID.Double, TypeID.Bool)
                                            .Add(Select_stringArray_string_decimal_bool, TypeID.StringArray, TypeID.String, TypeID.Decimal, TypeID.Bool)
                                            .Add(Select_stringArray_string_char_bool, TypeID.StringArray, TypeID.String, TypeID.Char, TypeID.Bool)
                                            .Add(Select_stringArray_string_string_bool, TypeID.StringArray, TypeID.String, TypeID.String, TypeID.Bool)
                                            .Add(Select_stringArray_string_object_bool, TypeID.StringArray, TypeID.String, TypeID.Object, TypeID.Bool)
                                            .Add(Select_stringArray_string_bool_bool, TypeID.StringArray, TypeID.String, TypeID.Bool, TypeID.Bool)
                                            .Add(Select_stringArray_string_date_bool, TypeID.StringArray, TypeID.String, TypeID.Date, TypeID.Bool)
                                            .Add(Select_stringArray_string_custom_bool, TypeID.StringArray, TypeID.String, TypeID.Custom, TypeID.Bool).Return(2, false, true)
                                            .Add(Select_boolArray_bool_byte_bool, TypeID.BoolArray, TypeID.Bool, TypeID.Byte, TypeID.Bool)
                                            .Add(Select_boolArray_bool_short_bool, TypeID.BoolArray, TypeID.Bool, TypeID.Short, TypeID.Bool)
                                            .Add(Select_boolArray_bool_int_bool, TypeID.BoolArray, TypeID.Bool, TypeID.Int, TypeID.Bool)
                                            .Add(Select_boolArray_bool_long_bool, TypeID.BoolArray, TypeID.Bool, TypeID.Long, TypeID.Bool)
                                            .Add(Select_boolArray_bool_float_bool, TypeID.BoolArray, TypeID.Bool, TypeID.Float, TypeID.Bool)
                                            .Add(Select_boolArray_bool_double_bool, TypeID.BoolArray, TypeID.Bool, TypeID.Double, TypeID.Bool)
                                            .Add(Select_boolArray_bool_decimal_bool, TypeID.BoolArray, TypeID.Bool, TypeID.Decimal, TypeID.Bool)
                                            .Add(Select_boolArray_bool_char_bool, TypeID.BoolArray, TypeID.Bool, TypeID.Char, TypeID.Bool)
                                            .Add(Select_boolArray_bool_string_bool, TypeID.BoolArray, TypeID.Bool, TypeID.String, TypeID.Bool)
                                            .Add(Select_boolArray_bool_object_bool, TypeID.BoolArray, TypeID.Bool, TypeID.Object, TypeID.Bool)
                                            .Add(Select_boolArray_bool_bool_bool, TypeID.BoolArray, TypeID.Bool, TypeID.Bool, TypeID.Bool)
                                            .Add(Select_boolArray_bool_date_bool, TypeID.BoolArray, TypeID.Bool, TypeID.Date, TypeID.Bool)
                                            .Add(Select_boolArray_bool_custom_bool, TypeID.BoolArray, TypeID.Bool, TypeID.Custom, TypeID.Bool).Return(2, false, true)
                                            .Add(Select_objectArray_object_byte_bool, TypeID.ObjectArray, TypeID.Object, TypeID.Byte, TypeID.Bool)
                                            .Add(Select_objectArray_object_short_bool, TypeID.ObjectArray, TypeID.Object, TypeID.Short, TypeID.Bool)
                                            .Add(Select_objectArray_object_int_bool, TypeID.ObjectArray, TypeID.Object, TypeID.Int, TypeID.Bool)
                                            .Add(Select_objectArray_object_long_bool, TypeID.ObjectArray, TypeID.Object, TypeID.Long, TypeID.Bool)
                                            .Add(Select_objectArray_object_float_bool, TypeID.ObjectArray, TypeID.Object, TypeID.Float, TypeID.Bool)
                                            .Add(Select_objectArray_object_double_bool, TypeID.ObjectArray, TypeID.Object, TypeID.Double, TypeID.Bool)
                                            .Add(Select_objectArray_object_decimal_bool, TypeID.ObjectArray, TypeID.Object, TypeID.Decimal, TypeID.Bool)
                                            .Add(Select_objectArray_object_char_bool, TypeID.ObjectArray, TypeID.Object, TypeID.Char, TypeID.Bool)
                                            .Add(Select_objectArray_object_string_bool, TypeID.ObjectArray, TypeID.Object, TypeID.String, TypeID.Bool)
                                            .Add(Select_objectArray_object_object_bool, TypeID.ObjectArray, TypeID.Object, TypeID.Object, TypeID.Bool)
                                            .Add(Select_objectArray_object_bool_bool, TypeID.ObjectArray, TypeID.Object, TypeID.Bool, TypeID.Bool)
                                            .Add(Select_objectArray_object_date_bool, TypeID.ObjectArray, TypeID.Object, TypeID.Date, TypeID.Bool)
                                            .Add(Select_objectArray_object_custom_bool, TypeID.ObjectArray, TypeID.Object, TypeID.Custom, TypeID.Bool).Return(2, false, true)
                                            .Add(Select_customArray_custom_byte_bool, TypeID.CustomArray, TypeID.Custom, TypeID.Byte, TypeID.Bool)
                                            .Add(Select_customArray_custom_short_bool, TypeID.CustomArray, TypeID.Custom, TypeID.Short, TypeID.Bool)
                                            .Add(Select_customArray_custom_int_bool, TypeID.CustomArray, TypeID.Custom, TypeID.Int, TypeID.Bool)
                                            .Add(Select_customArray_custom_long_bool, TypeID.CustomArray, TypeID.Custom, TypeID.Long, TypeID.Bool)
                                            .Add(Select_customArray_custom_float_bool, TypeID.CustomArray, TypeID.Custom, TypeID.Float, TypeID.Bool)
                                            .Add(Select_customArray_custom_double_bool, TypeID.CustomArray, TypeID.Custom, TypeID.Double, TypeID.Bool)
                                            .Add(Select_customArray_custom_decimal_bool, TypeID.CustomArray, TypeID.Custom, TypeID.Decimal, TypeID.Bool)
                                            .Add(Select_customArray_custom_char_bool, TypeID.CustomArray, TypeID.Custom, TypeID.Char, TypeID.Bool)
                                            .Add(Select_customArray_custom_string_bool, TypeID.CustomArray, TypeID.Custom, TypeID.String, TypeID.Bool)
                                            .Add(Select_customArray_custom_object_bool, TypeID.CustomArray, TypeID.Custom, TypeID.Object, TypeID.Bool)
                                            .Add(Select_customArray_custom_bool_bool, TypeID.CustomArray, TypeID.Custom, TypeID.Bool, TypeID.Bool)
                                            .Add(Select_customArray_custom_date_bool, TypeID.CustomArray, TypeID.Custom, TypeID.Date, TypeID.Bool)
                                            .Add(Select_customArray_custom_custom_bool, TypeID.CustomArray, TypeID.Custom, TypeID.Custom, TypeID.Bool).Return(2, false, true)
                                            .Add(Select_object_int_byte_bool, TypeID.Object, TypeID.Int, TypeID.Byte, TypeID.Bool)
                                            .Add(Select_object_int_short_bool, TypeID.Object, TypeID.Int, TypeID.Short, TypeID.Bool)
                                            .Add(Select_object_int_int_bool, TypeID.Object, TypeID.Int, TypeID.Int, TypeID.Bool)
                                            .Add(Select_object_int_long_bool, TypeID.Object, TypeID.Int, TypeID.Long, TypeID.Bool)
                                            .Add(Select_object_int_float_bool, TypeID.Object, TypeID.Int, TypeID.Float, TypeID.Bool)
                                            .Add(Select_object_int_double_bool, TypeID.Object, TypeID.Int, TypeID.Double, TypeID.Bool)
                                            .Add(Select_object_int_decimal_bool, TypeID.Object, TypeID.Int, TypeID.Decimal, TypeID.Bool)
                                            .Add(Select_object_int_char_bool, TypeID.Object, TypeID.Int, TypeID.Char, TypeID.Bool)
                                            .Add(Select_object_int_string_bool, TypeID.Object, TypeID.Int, TypeID.String, TypeID.Bool)
                                            .Add(Select_object_int_object_bool, TypeID.Object, TypeID.Int, TypeID.Object, TypeID.Bool)
                                            .Add(Select_object_int_bool_bool, TypeID.Object, TypeID.Int, TypeID.Bool, TypeID.Bool)
                                            .Add(Select_object_int_date_bool, TypeID.Object, TypeID.Int, TypeID.Date, TypeID.Bool)
                                            .Add(Select_object_int_custom_bool, TypeID.Object, TypeID.Int, TypeID.Custom, TypeID.Bool).Return(2, false, true)
                                            .Add(Select_object_byte_byte_bool, TypeID.Object, TypeID.Byte, TypeID.Byte, TypeID.Bool)
                                            .Add(Select_object_byte_short_bool, TypeID.Object, TypeID.Byte, TypeID.Short, TypeID.Bool)
                                            .Add(Select_object_byte_int_bool, TypeID.Object, TypeID.Byte, TypeID.Int, TypeID.Bool)
                                            .Add(Select_object_byte_long_bool, TypeID.Object, TypeID.Byte, TypeID.Long, TypeID.Bool)
                                            .Add(Select_object_byte_float_bool, TypeID.Object, TypeID.Byte, TypeID.Float, TypeID.Bool)
                                            .Add(Select_object_byte_double_bool, TypeID.Object, TypeID.Byte, TypeID.Double, TypeID.Bool)
                                            .Add(Select_object_byte_decimal_bool, TypeID.Object, TypeID.Byte, TypeID.Decimal, TypeID.Bool)
                                            .Add(Select_object_byte_char_bool, TypeID.Object, TypeID.Byte, TypeID.Char, TypeID.Bool)
                                            .Add(Select_object_byte_string_bool, TypeID.Object, TypeID.Byte, TypeID.String, TypeID.Bool)
                                            .Add(Select_object_byte_object_bool, TypeID.Object, TypeID.Byte, TypeID.Object, TypeID.Bool)
                                            .Add(Select_object_byte_bool_bool, TypeID.Object, TypeID.Byte, TypeID.Bool, TypeID.Bool)
                                            .Add(Select_object_byte_date_bool, TypeID.Object, TypeID.Byte, TypeID.Date, TypeID.Bool)
                                            .Add(Select_object_byte_custom_bool, TypeID.Object, TypeID.Byte, TypeID.Custom, TypeID.Bool).Return(2, false, true)
                                            .Add(Select_object_short_byte_bool, TypeID.Object, TypeID.Short, TypeID.Byte, TypeID.Bool)
                                            .Add(Select_object_short_short_bool, TypeID.Object, TypeID.Short, TypeID.Short, TypeID.Bool)
                                            .Add(Select_object_short_int_bool, TypeID.Object, TypeID.Short, TypeID.Int, TypeID.Bool)
                                            .Add(Select_object_short_long_bool, TypeID.Object, TypeID.Short, TypeID.Long, TypeID.Bool)
                                            .Add(Select_object_short_float_bool, TypeID.Object, TypeID.Short, TypeID.Float, TypeID.Bool)
                                            .Add(Select_object_short_double_bool, TypeID.Object, TypeID.Short, TypeID.Double, TypeID.Bool)
                                            .Add(Select_object_short_decimal_bool, TypeID.Object, TypeID.Short, TypeID.Decimal, TypeID.Bool)
                                            .Add(Select_object_short_char_bool, TypeID.Object, TypeID.Short, TypeID.Char, TypeID.Bool)
                                            .Add(Select_object_short_string_bool, TypeID.Object, TypeID.Short, TypeID.String, TypeID.Bool)
                                            .Add(Select_object_short_object_bool, TypeID.Object, TypeID.Short, TypeID.Object, TypeID.Bool)
                                            .Add(Select_object_short_bool_bool, TypeID.Object, TypeID.Short, TypeID.Bool, TypeID.Bool)
                                            .Add(Select_object_short_date_bool, TypeID.Object, TypeID.Short, TypeID.Date, TypeID.Bool)
                                            .Add(Select_object_short_custom_bool, TypeID.Object, TypeID.Short, TypeID.Custom, TypeID.Bool).Return(2, false, true)
                                            .Add(Select_object_long_byte_bool, TypeID.Object, TypeID.Long, TypeID.Byte, TypeID.Bool)
                                            .Add(Select_object_long_short_bool, TypeID.Object, TypeID.Long, TypeID.Short, TypeID.Bool)
                                            .Add(Select_object_long_int_bool, TypeID.Object, TypeID.Long, TypeID.Int, TypeID.Bool)
                                            .Add(Select_object_long_long_bool, TypeID.Object, TypeID.Long, TypeID.Long, TypeID.Bool)
                                            .Add(Select_object_long_float_bool, TypeID.Object, TypeID.Long, TypeID.Float, TypeID.Bool)
                                            .Add(Select_object_long_double_bool, TypeID.Object, TypeID.Long, TypeID.Double, TypeID.Bool)
                                            .Add(Select_object_long_decimal_bool, TypeID.Object, TypeID.Long, TypeID.Decimal, TypeID.Bool)
                                            .Add(Select_object_long_char_bool, TypeID.Object, TypeID.Long, TypeID.Char, TypeID.Bool)
                                            .Add(Select_object_long_string_bool, TypeID.Object, TypeID.Long, TypeID.String, TypeID.Bool)
                                            .Add(Select_object_long_object_bool, TypeID.Object, TypeID.Long, TypeID.Object, TypeID.Bool)
                                            .Add(Select_object_long_bool_bool, TypeID.Object, TypeID.Long, TypeID.Bool, TypeID.Bool)
                                            .Add(Select_object_long_date_bool, TypeID.Object, TypeID.Long, TypeID.Date, TypeID.Bool)
                                            .Add(Select_object_long_custom_bool, TypeID.Object, TypeID.Long, TypeID.Custom, TypeID.Bool).Return(2, false, true)
                                            .Add(Select_object_float_byte_bool, TypeID.Object, TypeID.Float, TypeID.Byte, TypeID.Bool)
                                            .Add(Select_object_float_short_bool, TypeID.Object, TypeID.Float, TypeID.Short, TypeID.Bool)
                                            .Add(Select_object_float_int_bool, TypeID.Object, TypeID.Float, TypeID.Int, TypeID.Bool)
                                            .Add(Select_object_float_long_bool, TypeID.Object, TypeID.Float, TypeID.Long, TypeID.Bool)
                                            .Add(Select_object_float_float_bool, TypeID.Object, TypeID.Float, TypeID.Float, TypeID.Bool)
                                            .Add(Select_object_float_double_bool, TypeID.Object, TypeID.Float, TypeID.Double, TypeID.Bool)
                                            .Add(Select_object_float_decimal_bool, TypeID.Object, TypeID.Float, TypeID.Decimal, TypeID.Bool)
                                            .Add(Select_object_float_char_bool, TypeID.Object, TypeID.Float, TypeID.Char, TypeID.Bool)
                                            .Add(Select_object_float_string_bool, TypeID.Object, TypeID.Float, TypeID.String, TypeID.Bool)
                                            .Add(Select_object_float_object_bool, TypeID.Object, TypeID.Float, TypeID.Object, TypeID.Bool)
                                            .Add(Select_object_float_bool_bool, TypeID.Object, TypeID.Float, TypeID.Bool, TypeID.Bool)
                                            .Add(Select_object_float_date_bool, TypeID.Object, TypeID.Float, TypeID.Date, TypeID.Bool)
                                            .Add(Select_object_float_custom_bool, TypeID.Object, TypeID.Float, TypeID.Custom, TypeID.Bool).Return(2, false, true)
                                            .Add(Select_object_double_byte_bool, TypeID.Object, TypeID.Double, TypeID.Byte, TypeID.Bool)
                                            .Add(Select_object_double_short_bool, TypeID.Object, TypeID.Double, TypeID.Short, TypeID.Bool)
                                            .Add(Select_object_double_int_bool, TypeID.Object, TypeID.Double, TypeID.Int, TypeID.Bool)
                                            .Add(Select_object_double_long_bool, TypeID.Object, TypeID.Double, TypeID.Long, TypeID.Bool)
                                            .Add(Select_object_double_float_bool, TypeID.Object, TypeID.Double, TypeID.Float, TypeID.Bool)
                                            .Add(Select_object_double_double_bool, TypeID.Object, TypeID.Double, TypeID.Double, TypeID.Bool)
                                            .Add(Select_object_double_decimal_bool, TypeID.Object, TypeID.Double, TypeID.Decimal, TypeID.Bool)
                                            .Add(Select_object_double_char_bool, TypeID.Object, TypeID.Double, TypeID.Char, TypeID.Bool)
                                            .Add(Select_object_double_string_bool, TypeID.Object, TypeID.Double, TypeID.String, TypeID.Bool)
                                            .Add(Select_object_double_object_bool, TypeID.Object, TypeID.Double, TypeID.Object, TypeID.Bool)
                                            .Add(Select_object_double_bool_bool, TypeID.Object, TypeID.Double, TypeID.Bool, TypeID.Bool)
                                            .Add(Select_object_double_date_bool, TypeID.Object, TypeID.Double, TypeID.Date, TypeID.Bool)
                                            .Add(Select_object_double_custom_bool, TypeID.Object, TypeID.Double, TypeID.Custom, TypeID.Bool).Return(2, false, true)
                                            .Add(Select_object_decimal_byte_bool, TypeID.Object, TypeID.Decimal, TypeID.Byte, TypeID.Bool)
                                            .Add(Select_object_decimal_short_bool, TypeID.Object, TypeID.Decimal, TypeID.Short, TypeID.Bool)
                                            .Add(Select_object_decimal_int_bool, TypeID.Object, TypeID.Decimal, TypeID.Int, TypeID.Bool)
                                            .Add(Select_object_decimal_long_bool, TypeID.Object, TypeID.Decimal, TypeID.Long, TypeID.Bool)
                                            .Add(Select_object_decimal_float_bool, TypeID.Object, TypeID.Decimal, TypeID.Float, TypeID.Bool)
                                            .Add(Select_object_decimal_double_bool, TypeID.Object, TypeID.Decimal, TypeID.Double, TypeID.Bool)
                                            .Add(Select_object_decimal_decimal_bool, TypeID.Object, TypeID.Decimal, TypeID.Decimal, TypeID.Bool)
                                            .Add(Select_object_decimal_char_bool, TypeID.Object, TypeID.Decimal, TypeID.Char, TypeID.Bool)
                                            .Add(Select_object_decimal_string_bool, TypeID.Object, TypeID.Decimal, TypeID.String, TypeID.Bool)
                                            .Add(Select_object_decimal_object_bool, TypeID.Object, TypeID.Decimal, TypeID.Object, TypeID.Bool)
                                            .Add(Select_object_decimal_bool_bool, TypeID.Object, TypeID.Decimal, TypeID.Bool, TypeID.Bool)
                                            .Add(Select_object_decimal_date_bool, TypeID.Object, TypeID.Decimal, TypeID.Date, TypeID.Bool)
                                            .Add(Select_object_decimal_custom_bool, TypeID.Object, TypeID.Decimal, TypeID.Custom, TypeID.Bool).Return(2, false, true)
                                            .Add(Select_object_char_byte_bool, TypeID.Object, TypeID.Char, TypeID.Byte, TypeID.Bool)
                                            .Add(Select_object_char_short_bool, TypeID.Object, TypeID.Char, TypeID.Short, TypeID.Bool)
                                            .Add(Select_object_char_int_bool, TypeID.Object, TypeID.Char, TypeID.Int, TypeID.Bool)
                                            .Add(Select_object_char_long_bool, TypeID.Object, TypeID.Char, TypeID.Long, TypeID.Bool)
                                            .Add(Select_object_char_float_bool, TypeID.Object, TypeID.Char, TypeID.Float, TypeID.Bool)
                                            .Add(Select_object_char_double_bool, TypeID.Object, TypeID.Char, TypeID.Double, TypeID.Bool)
                                            .Add(Select_object_char_decimal_bool, TypeID.Object, TypeID.Char, TypeID.Decimal, TypeID.Bool)
                                            .Add(Select_object_char_char_bool, TypeID.Object, TypeID.Char, TypeID.Char, TypeID.Bool)
                                            .Add(Select_object_char_string_bool, TypeID.Object, TypeID.Char, TypeID.String, TypeID.Bool)
                                            .Add(Select_object_char_object_bool, TypeID.Object, TypeID.Char, TypeID.Object, TypeID.Bool)
                                            .Add(Select_object_char_bool_bool, TypeID.Object, TypeID.Char, TypeID.Bool, TypeID.Bool)
                                            .Add(Select_object_char_date_bool, TypeID.Object, TypeID.Char, TypeID.Date, TypeID.Bool)
                                            .Add(Select_object_char_custom_bool, TypeID.Object, TypeID.Char, TypeID.Custom, TypeID.Bool).Return(2, false, true)
                                            .Add(Select_object_date_byte_bool, TypeID.Object, TypeID.Date, TypeID.Byte, TypeID.Bool)
                                            .Add(Select_object_date_short_bool, TypeID.Object, TypeID.Date, TypeID.Short, TypeID.Bool)
                                            .Add(Select_object_date_int_bool, TypeID.Object, TypeID.Date, TypeID.Int, TypeID.Bool)
                                            .Add(Select_object_date_long_bool, TypeID.Object, TypeID.Date, TypeID.Long, TypeID.Bool)
                                            .Add(Select_object_date_float_bool, TypeID.Object, TypeID.Date, TypeID.Float, TypeID.Bool)
                                            .Add(Select_object_date_double_bool, TypeID.Object, TypeID.Date, TypeID.Double, TypeID.Bool)
                                            .Add(Select_object_date_decimal_bool, TypeID.Object, TypeID.Date, TypeID.Decimal, TypeID.Bool)
                                            .Add(Select_object_date_char_bool, TypeID.Object, TypeID.Date, TypeID.Char, TypeID.Bool)
                                            .Add(Select_object_date_string_bool, TypeID.Object, TypeID.Date, TypeID.String, TypeID.Bool)
                                            .Add(Select_object_date_object_bool, TypeID.Object, TypeID.Date, TypeID.Object, TypeID.Bool)
                                            .Add(Select_object_date_bool_bool, TypeID.Object, TypeID.Date, TypeID.Bool, TypeID.Bool)
                                            .Add(Select_object_date_date_bool, TypeID.Object, TypeID.Date, TypeID.Date, TypeID.Bool)
                                            .Add(Select_object_date_custom_bool, TypeID.Object, TypeID.Date, TypeID.Custom, TypeID.Bool).Return(2, false, true)
                                            .Add(Select_object_string_byte_bool, TypeID.Object, TypeID.String, TypeID.Byte, TypeID.Bool)
                                            .Add(Select_object_string_short_bool, TypeID.Object, TypeID.String, TypeID.Short, TypeID.Bool)
                                            .Add(Select_object_string_int_bool, TypeID.Object, TypeID.String, TypeID.Int, TypeID.Bool)
                                            .Add(Select_object_string_long_bool, TypeID.Object, TypeID.String, TypeID.Long, TypeID.Bool)
                                            .Add(Select_object_string_float_bool, TypeID.Object, TypeID.String, TypeID.Float, TypeID.Bool)
                                            .Add(Select_object_string_double_bool, TypeID.Object, TypeID.String, TypeID.Double, TypeID.Bool)
                                            .Add(Select_object_string_decimal_bool, TypeID.Object, TypeID.String, TypeID.Decimal, TypeID.Bool)
                                            .Add(Select_object_string_char_bool, TypeID.Object, TypeID.String, TypeID.Char, TypeID.Bool)
                                            .Add(Select_object_string_string_bool, TypeID.Object, TypeID.String, TypeID.String, TypeID.Bool)
                                            .Add(Select_object_string_object_bool, TypeID.Object, TypeID.String, TypeID.Object, TypeID.Bool)
                                            .Add(Select_object_string_bool_bool, TypeID.Object, TypeID.String, TypeID.Bool, TypeID.Bool)
                                            .Add(Select_object_string_date_bool, TypeID.Object, TypeID.String, TypeID.Date, TypeID.Bool)
                                            .Add(Select_object_string_custom_bool, TypeID.Object, TypeID.String, TypeID.Custom, TypeID.Bool).Return(2, false, true)
                                            .Add(Select_object_bool_byte_bool, TypeID.Object, TypeID.Bool, TypeID.Byte, TypeID.Bool)
                                            .Add(Select_object_bool_short_bool, TypeID.Object, TypeID.Bool, TypeID.Short, TypeID.Bool)
                                            .Add(Select_object_bool_int_bool, TypeID.Object, TypeID.Bool, TypeID.Int, TypeID.Bool)
                                            .Add(Select_object_bool_long_bool, TypeID.Object, TypeID.Bool, TypeID.Long, TypeID.Bool)
                                            .Add(Select_object_bool_float_bool, TypeID.Object, TypeID.Bool, TypeID.Float, TypeID.Bool)
                                            .Add(Select_object_bool_double_bool, TypeID.Object, TypeID.Bool, TypeID.Double, TypeID.Bool)
                                            .Add(Select_object_bool_decimal_bool, TypeID.Object, TypeID.Bool, TypeID.Decimal, TypeID.Bool)
                                            .Add(Select_object_bool_char_bool, TypeID.Object, TypeID.Bool, TypeID.Char, TypeID.Bool)
                                            .Add(Select_object_bool_string_bool, TypeID.Object, TypeID.Bool, TypeID.String, TypeID.Bool)
                                            .Add(Select_object_bool_object_bool, TypeID.Object, TypeID.Bool, TypeID.Object, TypeID.Bool)
                                            .Add(Select_object_bool_bool_bool, TypeID.Object, TypeID.Bool, TypeID.Bool, TypeID.Bool)
                                            .Add(Select_object_bool_date_bool, TypeID.Object, TypeID.Bool, TypeID.Date, TypeID.Bool)
                                            .Add(Select_object_bool_custom_bool, TypeID.Object, TypeID.Bool, TypeID.Custom, TypeID.Bool).Return(2, false, true)
                                            .Add(Select_object_object_byte_bool, TypeID.Object, TypeID.Object, TypeID.Byte, TypeID.Bool)
                                            .Add(Select_object_object_short_bool, TypeID.Object, TypeID.Object, TypeID.Short, TypeID.Bool)
                                            .Add(Select_object_object_int_bool, TypeID.Object, TypeID.Object, TypeID.Int, TypeID.Bool)
                                            .Add(Select_object_object_long_bool, TypeID.Object, TypeID.Object, TypeID.Long, TypeID.Bool)
                                            .Add(Select_object_object_float_bool, TypeID.Object, TypeID.Object, TypeID.Float, TypeID.Bool)
                                            .Add(Select_object_object_double_bool, TypeID.Object, TypeID.Object, TypeID.Double, TypeID.Bool)
                                            .Add(Select_object_object_decimal_bool, TypeID.Object, TypeID.Object, TypeID.Decimal, TypeID.Bool)
                                            .Add(Select_object_object_char_bool, TypeID.Object, TypeID.Object, TypeID.Char, TypeID.Bool)
                                            .Add(Select_object_object_string_bool, TypeID.Object, TypeID.Object, TypeID.String, TypeID.Bool)
                                            .Add(Select_object_object_object_bool, TypeID.Object, TypeID.Object, TypeID.Object, TypeID.Bool)
                                            .Add(Select_object_object_bool_bool, TypeID.Object, TypeID.Object, TypeID.Bool, TypeID.Bool)
                                            .Add(Select_object_object_date_bool, TypeID.Object, TypeID.Object, TypeID.Date, TypeID.Bool)
                                            .Add(Select_object_object_custom_bool, TypeID.Object, TypeID.Object, TypeID.Custom, TypeID.Bool).Return(2, false, true)
                                            .Add(Select_object_custom_byte_bool, TypeID.Object, TypeID.Custom, TypeID.Byte, TypeID.Bool)
                                            .Add(Select_object_custom_short_bool, TypeID.Object, TypeID.Custom, TypeID.Short, TypeID.Bool)
                                            .Add(Select_object_custom_int_bool, TypeID.Object, TypeID.Custom, TypeID.Int, TypeID.Bool)
                                            .Add(Select_object_custom_long_bool, TypeID.Object, TypeID.Custom, TypeID.Long, TypeID.Bool)
                                            .Add(Select_object_custom_float_bool, TypeID.Object, TypeID.Custom, TypeID.Float, TypeID.Bool)
                                            .Add(Select_object_custom_double_bool, TypeID.Object, TypeID.Custom, TypeID.Double, TypeID.Bool)
                                            .Add(Select_object_custom_decimal_bool, TypeID.Object, TypeID.Custom, TypeID.Decimal, TypeID.Bool)
                                            .Add(Select_object_custom_char_bool, TypeID.Object, TypeID.Custom, TypeID.Char, TypeID.Bool)
                                            .Add(Select_object_custom_string_bool, TypeID.Object, TypeID.Custom, TypeID.String, TypeID.Bool)
                                            .Add(Select_object_custom_object_bool, TypeID.Object, TypeID.Custom, TypeID.Object, TypeID.Bool)
                                            .Add(Select_object_custom_bool_bool, TypeID.Object, TypeID.Custom, TypeID.Bool, TypeID.Bool)
                                            .Add(Select_object_custom_date_bool, TypeID.Object, TypeID.Custom, TypeID.Date, TypeID.Bool)
                                            .Add(Select_object_custom_custom_bool, TypeID.Object, TypeID.Custom, TypeID.Custom, TypeID.Bool).Return(2, false, true)
                                            .ConvertAll(x => x.Strict().Kind(1, EvalUnitKind.Variable).FewerArgsAllowed(3))
                                            );


            AddFunc("Find", new BF().Add(Find_customArray_custom_bool, TypeID.CustomArray, TypeID.Custom, TypeID.Bool).Return(1)
                                             .Add(Find_intArray_int_bool, TypeID.IntArray, TypeID.Int, TypeID.Bool)
                                             .Add(Find_longArray_long_bool, TypeID.LongArray, TypeID.Long, TypeID.Bool)
                                             .Add(Find_floatArray_float_bool, TypeID.FloatArray, TypeID.Float, TypeID.Bool)
                                             .Add(Find_doubleArray_double_bool, TypeID.DoubleArray, TypeID.Double, TypeID.Bool)
                                             .Add(Find_decimalArray_decimal_bool, TypeID.DecimalArray, TypeID.Decimal, TypeID.Bool)
                                             .Add(Find_boolArray_bool_bool, TypeID.BoolArray, TypeID.Bool, TypeID.Bool)
                                             .Add(Find_charArray_char_bool, TypeID.CharArray, TypeID.Char, TypeID.Bool)
                                             .Add(Find_stringArray_string_bool, TypeID.StringArray, TypeID.String, TypeID.Bool)
                                             .Add(Find_objectArray_object_bool, TypeID.ObjectArray, TypeID.Object, TypeID.Bool).HReturn(1)
                                             .Add(Find_shortArray_short_bool, TypeID.ShortArray, TypeID.Short, TypeID.Bool)
                                             .Add(Find_byteArray_byte_bool, TypeID.ByteArray, TypeID.Byte, TypeID.Bool)
                                             .Add(Find_dateArray_date_bool, TypeID.DateArray, TypeID.Date, TypeID.Bool)

                                             .Add(Find_object_custom_bool, TypeID.Object, TypeID.Custom, TypeID.Bool).Return(1)
                                             .Add(Find_object_int_bool, TypeID.Object, TypeID.Int, TypeID.Bool)
                                             .Add(Find_object_long_bool, TypeID.Object, TypeID.Long, TypeID.Bool)
                                             .Add(Find_object_float_bool, TypeID.Object, TypeID.Float, TypeID.Bool)
                                             .Add(Find_object_double_bool, TypeID.Object, TypeID.Double, TypeID.Bool)
                                             .Add(Find_object_decimal_bool, TypeID.Object, TypeID.Decimal, TypeID.Bool)
                                             .Add(Find_object_bool_bool, TypeID.Object, TypeID.Bool, TypeID.Bool)
                                             .Add(Find_object_char_bool, TypeID.Object, TypeID.Char, TypeID.Bool)
                                             .Add(Find_object_string_bool, TypeID.Object, TypeID.String, TypeID.Bool)
                                             .Add(Find_object_object_bool, TypeID.Object, TypeID.Object, TypeID.Bool)
                                             .Add(Find_object_short_bool, TypeID.Object, TypeID.Short, TypeID.Bool)
                                             .Add(Find_object_byte_bool, TypeID.Object, TypeID.Byte, TypeID.Bool)
                                             .Add(Find_object_date_bool, TypeID.Object, TypeID.Date, TypeID.Bool)
                                             .ConvertAll(x => x.Strict().Kind(1, EvalUnitKind.Variable))
                                             );
            AddFunc("FindIndex", new BF().Add(FindIndex_customArray_custom_bool, TypeID.CustomArray, TypeID.Custom, TypeID.Bool)
                                            .Add(FindIndex_intArray_int_bool, TypeID.IntArray, TypeID.Int, TypeID.Bool)
                                            .Add(FindIndex_longArray_long_bool, TypeID.LongArray, TypeID.Long, TypeID.Bool)
                                            .Add(FindIndex_floatArray_float_bool, TypeID.FloatArray, TypeID.Float, TypeID.Bool)
                                            .Add(FindIndex_doubleArray_double_bool, TypeID.DoubleArray, TypeID.Double, TypeID.Bool)
                                            .Add(FindIndex_decimalArray_decimal_bool, TypeID.DecimalArray, TypeID.Decimal, TypeID.Bool)
                                            .Add(FindIndex_boolArray_bool_bool, TypeID.BoolArray, TypeID.Bool, TypeID.Bool)
                                            .Add(FindIndex_charArray_char_bool, TypeID.CharArray, TypeID.Char, TypeID.Bool)
                                            .Add(FindIndex_stringArray_string_bool, TypeID.StringArray, TypeID.String, TypeID.Bool)
                                            .Add(FindIndex_objectArray_object_bool, TypeID.ObjectArray, TypeID.Object, TypeID.Bool)
                                            .Add(FindIndex_shortArray_short_bool, TypeID.ShortArray, TypeID.Short, TypeID.Bool)
                                            .Add(FindIndex_byteArray_byte_bool, TypeID.ByteArray, TypeID.Byte, TypeID.Bool)
                                            .Add(FindIndex_dateArray_date_bool, TypeID.DateArray, TypeID.Date, TypeID.Bool)

                                            .Add(FindIndex_object_custom_bool, TypeID.Object, TypeID.Custom, TypeID.Bool)
                                            .Add(FindIndex_object_int_bool, TypeID.Object, TypeID.Int, TypeID.Bool)
                                            .Add(FindIndex_object_long_bool, TypeID.Object, TypeID.Long, TypeID.Bool)
                                            .Add(FindIndex_object_float_bool, TypeID.Object, TypeID.Float, TypeID.Bool)
                                            .Add(FindIndex_object_double_bool, TypeID.Object, TypeID.Double, TypeID.Bool)
                                            .Add(FindIndex_object_decimal_bool, TypeID.Object, TypeID.Decimal, TypeID.Bool)
                                            .Add(FindIndex_object_bool_bool, TypeID.Object, TypeID.Bool, TypeID.Bool)
                                            .Add(FindIndex_object_char_bool, TypeID.Object, TypeID.Char, TypeID.Bool)
                                            .Add(FindIndex_object_string_bool, TypeID.Object, TypeID.String, TypeID.Bool)
                                            .Add(FindIndex_object_object_bool, TypeID.Object, TypeID.Object, TypeID.Bool)
                                            .Add(FindIndex_object_short_bool, TypeID.Object, TypeID.Short, TypeID.Bool)
                                            .Add(FindIndex_object_byte_bool, TypeID.Object, TypeID.Byte, TypeID.Bool)
                                            .Add(FindIndex_object_date_bool, TypeID.Object, TypeID.Date, TypeID.Bool)
                                            .ConvertAll(x => x.Strict().Kind(1, EvalUnitKind.Variable))
                                            );
            AddFunc("FindAll", new BF().Add(FindAll_customArray_custom_bool, TypeID.CustomArray, TypeID.Custom, TypeID.Bool).Return(0)
                                            .Add(FindAll_intArray_int_bool, TypeID.IntArray, TypeID.Int, TypeID.Bool)
                                            .Add(FindAll_longArray_long_bool, TypeID.LongArray, TypeID.Long, TypeID.Bool)
                                            .Add(FindAll_floatArray_float_bool, TypeID.FloatArray, TypeID.Float, TypeID.Bool)
                                            .Add(FindAll_doubleArray_double_bool, TypeID.DoubleArray, TypeID.Double, TypeID.Bool)
                                            .Add(FindAll_decimalArray_decimal_bool, TypeID.DecimalArray, TypeID.Decimal, TypeID.Bool)
                                            .Add(FindAll_boolArray_bool_bool, TypeID.BoolArray, TypeID.Bool, TypeID.Bool)
                                            .Add(FindAll_charArray_char_bool, TypeID.CharArray, TypeID.Char, TypeID.Bool)
                                            .Add(FindAll_stringArray_string_bool, TypeID.StringArray, TypeID.String, TypeID.Bool)
                                            .Add(FindAll_objectArray_object_bool, TypeID.ObjectArray, TypeID.Object, TypeID.Bool)
                                            .Add(FindAll_shortArray_short_bool, TypeID.ShortArray, TypeID.Short, TypeID.Bool)
                                            .Add(FindAll_byteArray_byte_bool, TypeID.ByteArray, TypeID.Byte, TypeID.Bool)
                                            .Add(FindAll_dateArray_date_bool, TypeID.DateArray, TypeID.Date, TypeID.Bool)

                                            .Add(FindAll_object_custom_bool, TypeID.Object, TypeID.Custom, TypeID.Bool).Return(1, false, true)
                                            .Add(FindAll_object_int_bool, TypeID.Object, TypeID.Int, TypeID.Bool)
                                            .Add(FindAll_object_long_bool, TypeID.Object, TypeID.Long, TypeID.Bool)
                                            .Add(FindAll_object_float_bool, TypeID.Object, TypeID.Float, TypeID.Bool)
                                            .Add(FindAll_object_double_bool, TypeID.Object, TypeID.Double, TypeID.Bool)
                                            .Add(FindAll_object_decimal_bool, TypeID.Object, TypeID.Decimal, TypeID.Bool)
                                            .Add(FindAll_object_bool_bool, TypeID.Object, TypeID.Bool, TypeID.Bool)
                                            .Add(FindAll_object_char_bool, TypeID.Object, TypeID.Char, TypeID.Bool)
                                            .Add(FindAll_object_string_bool, TypeID.Object, TypeID.String, TypeID.Bool)
                                            .Add(FindAll_object_object_bool, TypeID.Object, TypeID.Object, TypeID.Bool)
                                            .Add(FindAll_object_short_bool, TypeID.Object, TypeID.Short, TypeID.Bool)
                                            .Add(FindAll_object_byte_bool, TypeID.Object, TypeID.Byte, TypeID.Bool)
                                            .Add(FindAll_object_date_bool, TypeID.Object, TypeID.Date, TypeID.Bool)
                                            .ConvertAll(x => x.Strict().Kind(1, EvalUnitKind.Variable))
                                            );

            AddFunc("Range", new BF().Add(GetLoopRange_int, TypeID.Int)
                                    .Add(GetLoopRange_int_int, TypeID.Int, TypeID.Int)
                                    .Add(GetLoopRange_int_int_int, TypeID.Int, TypeID.Int, TypeID.Int));
            AddFunc("FuncRef", new BF().Add(FuncRef_object, TypeID.Object)
                                      .Add(FuncRef_object_string_objectParams, TypeID.Object, TypeID.String).HasParams(TypeID.Object));
            AddFunc("FuncPureRef", new BF().Add(FuncPureRef_object, TypeID.Object)
                                           .Add(FuncPureRef_object_string_objectParams, TypeID.Object, TypeID.String).HasParams(TypeID.Object));

            AddFunc("Call", new BF().Add(Call_object_objectParams, TypeID.Object).HasParams(TypeID.Object));
            AddFunc("CallOn", new BF().Add(CallOn_object_object_objectParams, TypeID.Object, TypeID.Object).HasParams(TypeID.Object));
            AddFunc("DynCall", new BF().Add(DynCall_object_string_objectParams, TypeID.Object, TypeID.String).HasParams(TypeID.Object));
            AddFunc("Lock", new BF().Add(Lock_object, TypeID.OfRefType).Add(Lock_object_bool, TypeID.OfRefType, TypeID.Bool).Kind(1, EvalUnitKind.Variable));

            AddFunc("Unlock", new BF().Add(Unlock_object, TypeID.OfRefType));


            AddFunc("EventHandler", new BF().Add(EventHandler_object, TypeID.Object)
                                           .Add(EventHandler_object_object, TypeID.Object, TypeID.Object));
            AddFunc("EventHandlerByFuncRef", new BF().Add(EventHandlerByFuncRef_object, TypeID.Object).Strict()
                                          .Add(EventHandlerByFuncRef_object_object, TypeID.Object, TypeID.Object).Strict(0));

            AddFunc("Delegate", new BF().Add(Delegate_object_object, TypeID.Object, TypeID.Object));
            AddFunc("DelegateByFuncRef", new BF().Add(DelegateByFuncRef_object_object, TypeID.Object, TypeID.Object).Strict(0));

            AddFunc("Raise", new BF().Add(Raise_object_objectParams, TypeID.Object).HasParams(TypeID.Object));

            AddFunc("Enumerator", new BF().Add(Enumerator_object_object_object_object, TypeID.Object, TypeID.Object, TypeID.Object, TypeID.Object).FewerArgsAllowed(3)
                                           .Add(Enumerator_object, TypeID.Object));
            AddFunc("EnumeratorByFuncRefs", new BF().Add(EnumeratorByFuncRefs_object_object_object_object, TypeID.Object, TypeID.Object, TypeID.Object, TypeID.Object).FewerArgsAllowed(3));

            AddFunc("InvokeMethod", new BF().Add(InvokeMethod_object_string_objectParams, TypeID.Object, TypeID.String).HasParams(TypeID.Object));
            AddFunc("GetMemberValue", new BF().Add(GetMemberValue_object_string_objectParams, TypeID.Object, TypeID.String).HasParams(TypeID.Object));
            AddFunc("SetMemberValue", new BF().Add(SetMemberValue_object_string_object_objectParams, TypeID.Object, TypeID.String, TypeID.Object).HasParams(TypeID.Object));

            AddFunc("GetMember", new BF().Add(GetMember_object_string, TypeID.Object, TypeID.String));
            AddFunc("GetMembers", new BF().Add(GetMembers_object_string, TypeID.Object, TypeID.String));
            AddFunc("GetMethod", new BF().Add(GetMethod_object_string_objectParams, TypeID.Object, TypeID.String).HasParams(TypeID.Object));
            AddFunc("GetValue", new BF().Add(GetValue_object, TypeID.Object)
                                              .Add(GetValue_object_object, TypeID.Object, TypeID.Object)
                                              .Add(GetValue_object_object_objectParams, TypeID.Object, TypeID.Object).HasParams(TypeID.Object));
            AddFunc("SetValue", new BF().Add(SetValue_object_object_object, TypeID.Object, TypeID.Object, TypeID.Object)
                                       .Add(SetValue_object_object_object_objectParams, TypeID.Object, TypeID.Object, TypeID.Object).HasParams(TypeID.Object));
            AddFunc("Invoke", new BF().Add(Invoke_object, TypeID.Object)
                                     .Add(Invoke_object_object, TypeID.Object, TypeID.Object)
                                     .Add(Invoke_object_object_objectParams, TypeID.Object, TypeID.Object).HasParams(TypeID.Object));

            AddFunc("TInvoke", new BF().Add(TInvoke_object_intType, TypeID.Object, TypeID.IntType)
                                       .Add(TInvoke_object_intType_object, TypeID.Object, TypeID.IntType, TypeID.Object)
                                       .Add(TInvoke_object_intType_object_objectParams, TypeID.Object, TypeID.IntType, TypeID.Object).HasParams(TypeID.Object)
                                       .Add(TInvoke_object_longType, TypeID.Object, TypeID.LongType)
                                       .Add(TInvoke_object_longType_object, TypeID.Object, TypeID.LongType, TypeID.Object)
                                       .Add(TInvoke_object_longType_object_objectParams, TypeID.Object, TypeID.LongType, TypeID.Object).HasParams(TypeID.Object)
                                       .Add(TInvoke_object_floatType, TypeID.Object, TypeID.FloatType)
                                       .Add(TInvoke_object_floatType_object, TypeID.Object, TypeID.FloatType, TypeID.Object)
                                       .Add(TInvoke_object_floatType_object_objectParams, TypeID.Object, TypeID.FloatType, TypeID.Object).HasParams(TypeID.Object)
                                        .Add(TInvoke_object_doubleType, TypeID.Object, TypeID.DoubleType)
                                       .Add(TInvoke_object_doubleType_object, TypeID.Object, TypeID.DoubleType, TypeID.Object)
                                       .Add(TInvoke_object_doubleType_object_objectParams, TypeID.Object, TypeID.DoubleType, TypeID.Object).HasParams(TypeID.Object)
                                        .Add(TInvoke_object_decimalType, TypeID.Object, TypeID.DecimalType)
                                       .Add(TInvoke_object_decimalType_object, TypeID.Object, TypeID.DecimalType, TypeID.Object)
                                       .Add(TInvoke_object_decimalType_object_objectParams, TypeID.Object, TypeID.DecimalType, TypeID.Object).HasParams(TypeID.Object)
                                        .Add(TInvoke_object_boolType, TypeID.Object, TypeID.BoolType)
                                       .Add(TInvoke_object_boolType_object, TypeID.Object, TypeID.BoolType, TypeID.Object)
                                       .Add(TInvoke_object_boolType_object_objectParams, TypeID.Object, TypeID.BoolType, TypeID.Object).HasParams(TypeID.Object)
                                        .Add(TInvoke_object_charType, TypeID.Object, TypeID.CharType)
                                       .Add(TInvoke_object_charType_object, TypeID.Object, TypeID.CharType, TypeID.Object)
                                       .Add(TInvoke_object_charType_object_objectParams, TypeID.Object, TypeID.CharType, TypeID.Object).HasParams(TypeID.Object)
                                        .Add(TInvoke_object_stringType, TypeID.Object, TypeID.StringType)
                                       .Add(TInvoke_object_stringType_object, TypeID.Object, TypeID.StringType, TypeID.Object)
                                       .Add(TInvoke_object_stringType_object_objectParams, TypeID.Object, TypeID.StringType, TypeID.Object).HasParams(TypeID.Object)
                                        .Add(TInvoke_object_shortType, TypeID.Object, TypeID.ShortType)
                                       .Add(TInvoke_object_shortType_object, TypeID.Object, TypeID.ShortType, TypeID.Object)
                                       .Add(TInvoke_object_shortType_object_objectParams, TypeID.Object, TypeID.ShortType, TypeID.Object).HasParams(TypeID.Object)
                                         .Add(TInvoke_object_byteType, TypeID.Object, TypeID.ByteType)
                                       .Add(TInvoke_object_byteType_object, TypeID.Object, TypeID.ByteType, TypeID.Object)
                                       .Add(TInvoke_object_byteType_object_objectParams, TypeID.Object, TypeID.ByteType, TypeID.Object).HasParams(TypeID.Object)
                                         .Add(TInvoke_object_dateType, TypeID.Object, TypeID.DateType)
                                       .Add(TInvoke_object_dateType_object, TypeID.Object, TypeID.DateType, TypeID.Object)
                                       .Add(TInvoke_object_dateType_object_objectParams, TypeID.Object, TypeID.DateType, TypeID.Object).HasParams(TypeID.Object)

                                       .Add(TInvoke_object_objectArrayType, TypeID.Object, TypeID.ObjectArrayType)
                                       .Add(TInvoke_object_objectArrayType_object, TypeID.Object, TypeID.ObjectArrayType, TypeID.Object)
                                       .Add(TInvoke_object_objectArrayType_object_objectParams, TypeID.Object, TypeID.ObjectArrayType, TypeID.Object).HasParams(TypeID.Object)

                                       .Add(TInvoke_object_intArrayType, TypeID.Object, TypeID.IntArrayType)
                                       .Add(TInvoke_object_intArrayType_object, TypeID.Object, TypeID.IntArrayType, TypeID.Object)
                                       .Add(TInvoke_object_intArrayType_object_objectParams, TypeID.Object, TypeID.IntArrayType, TypeID.Object).HasParams(TypeID.Object)
                                       .Add(TInvoke_object_longArrayType, TypeID.Object, TypeID.LongArrayType)
                                       .Add(TInvoke_object_longArrayType_object, TypeID.Object, TypeID.LongArrayType, TypeID.Object)
                                       .Add(TInvoke_object_longArrayType_object_objectParams, TypeID.Object, TypeID.LongArrayType, TypeID.Object).HasParams(TypeID.Object)
                                       .Add(TInvoke_object_floatArrayType, TypeID.Object, TypeID.FloatArrayType)
                                       .Add(TInvoke_object_floatArrayType_object, TypeID.Object, TypeID.FloatArrayType, TypeID.Object)
                                       .Add(TInvoke_object_floatArrayType_object_objectParams, TypeID.Object, TypeID.FloatArrayType, TypeID.Object).HasParams(TypeID.Object)
                                        .Add(TInvoke_object_doubleArrayType, TypeID.Object, TypeID.DoubleArrayType)
                                       .Add(TInvoke_object_doubleArrayType_object, TypeID.Object, TypeID.DoubleArrayType, TypeID.Object)
                                       .Add(TInvoke_object_doubleArrayType_object_objectParams, TypeID.Object, TypeID.DoubleArrayType, TypeID.Object).HasParams(TypeID.Object)
                                        .Add(TInvoke_object_decimalArrayType, TypeID.Object, TypeID.DecimalArrayType)
                                       .Add(TInvoke_object_decimalArrayType_object, TypeID.Object, TypeID.DecimalArrayType, TypeID.Object)
                                       .Add(TInvoke_object_decimalArrayType_object_objectParams, TypeID.Object, TypeID.DecimalArrayType, TypeID.Object).HasParams(TypeID.Object)
                                        .Add(TInvoke_object_boolArrayType, TypeID.Object, TypeID.BoolArrayType)
                                       .Add(TInvoke_object_boolArrayType_object, TypeID.Object, TypeID.BoolArrayType, TypeID.Object)
                                       .Add(TInvoke_object_boolArrayType_object_objectParams, TypeID.Object, TypeID.BoolArrayType, TypeID.Object).HasParams(TypeID.Object)
                                        .Add(TInvoke_object_charArrayType, TypeID.Object, TypeID.CharArrayType)
                                       .Add(TInvoke_object_charArrayType_object, TypeID.Object, TypeID.CharArrayType, TypeID.Object)
                                       .Add(TInvoke_object_charArrayType_object_objectParams, TypeID.Object, TypeID.CharArrayType, TypeID.Object).HasParams(TypeID.Object)
                                        .Add(TInvoke_object_stringArrayType, TypeID.Object, TypeID.StringArrayType)
                                       .Add(TInvoke_object_stringArrayType_object, TypeID.Object, TypeID.StringArrayType, TypeID.Object)
                                       .Add(TInvoke_object_stringArrayType_object_objectParams, TypeID.Object, TypeID.StringArrayType, TypeID.Object).HasParams(TypeID.Object)
                                        .Add(TInvoke_object_shortArrayType, TypeID.Object, TypeID.ShortArrayType)
                                       .Add(TInvoke_object_shortArrayType_object, TypeID.Object, TypeID.ShortArrayType, TypeID.Object)
                                       .Add(TInvoke_object_shortArrayType_object_objectParams, TypeID.Object, TypeID.ShortArrayType, TypeID.Object).HasParams(TypeID.Object)
                                         .Add(TInvoke_object_byteArrayType, TypeID.Object, TypeID.ByteArrayType)
                                       .Add(TInvoke_object_byteArrayType_object, TypeID.Object, TypeID.ByteArrayType, TypeID.Object)
                                       .Add(TInvoke_object_byteArrayType_object_objectParams, TypeID.Object, TypeID.ByteArrayType, TypeID.Object).HasParams(TypeID.Object)
                                         .Add(TInvoke_object_dateArrayType, TypeID.Object, TypeID.DateArrayType)
                                       .Add(TInvoke_object_dateArrayType_object, TypeID.Object, TypeID.DateArrayType, TypeID.Object)
                                       .Add(TInvoke_object_dateArrayType_object_objectParams, TypeID.Object, TypeID.DateArrayType, TypeID.Object).HasParams(TypeID.Object)
                                       );
            AddFunc("TGetValue", new BF().Add(TGetValue_object_intType, TypeID.Object, TypeID.IntType)
                                      .Add(TGetValue_object_intType_object, TypeID.Object, TypeID.IntType, TypeID.Object)
                                      .Add(TGetValue_object_intType_object_objectParams, TypeID.Object, TypeID.IntType, TypeID.Object).HasParams(TypeID.Object)
                                      .Add(TGetValue_object_longType, TypeID.Object, TypeID.LongType)
                                      .Add(TGetValue_object_longType_object, TypeID.Object, TypeID.LongType, TypeID.Object)
                                      .Add(TGetValue_object_longType_object_objectParams, TypeID.Object, TypeID.LongType, TypeID.Object).HasParams(TypeID.Object)
                                      .Add(TGetValue_object_floatType, TypeID.Object, TypeID.FloatType)
                                      .Add(TGetValue_object_floatType_object, TypeID.Object, TypeID.FloatType, TypeID.Object)
                                      .Add(TGetValue_object_floatType_object_objectParams, TypeID.Object, TypeID.FloatType, TypeID.Object).HasParams(TypeID.Object)
                                       .Add(TGetValue_object_doubleType, TypeID.Object, TypeID.DoubleType)
                                      .Add(TGetValue_object_doubleType_object, TypeID.Object, TypeID.DoubleType, TypeID.Object)
                                      .Add(TGetValue_object_doubleType_object_objectParams, TypeID.Object, TypeID.DoubleType, TypeID.Object).HasParams(TypeID.Object)
                                       .Add(TGetValue_object_decimalType, TypeID.Object, TypeID.DecimalType)
                                      .Add(TGetValue_object_decimalType_object, TypeID.Object, TypeID.DecimalType, TypeID.Object)
                                      .Add(TGetValue_object_decimalType_object_objectParams, TypeID.Object, TypeID.DecimalType, TypeID.Object).HasParams(TypeID.Object)
                                       .Add(TGetValue_object_boolType, TypeID.Object, TypeID.BoolType)
                                      .Add(TGetValue_object_boolType_object, TypeID.Object, TypeID.BoolType, TypeID.Object)
                                      .Add(TGetValue_object_boolType_object_objectParams, TypeID.Object, TypeID.BoolType, TypeID.Object).HasParams(TypeID.Object)
                                       .Add(TGetValue_object_charType, TypeID.Object, TypeID.CharType)
                                      .Add(TGetValue_object_charType_object, TypeID.Object, TypeID.CharType, TypeID.Object)
                                      .Add(TGetValue_object_charType_object_objectParams, TypeID.Object, TypeID.CharType, TypeID.Object).HasParams(TypeID.Object)
                                       .Add(TGetValue_object_stringType, TypeID.Object, TypeID.StringType)
                                      .Add(TGetValue_object_stringType_object, TypeID.Object, TypeID.StringType, TypeID.Object)
                                      .Add(TGetValue_object_stringType_object_objectParams, TypeID.Object, TypeID.StringType, TypeID.Object).HasParams(TypeID.Object)
                                       .Add(TGetValue_object_shortType, TypeID.Object, TypeID.ShortType)
                                      .Add(TGetValue_object_shortType_object, TypeID.Object, TypeID.ShortType, TypeID.Object)
                                      .Add(TGetValue_object_shortType_object_objectParams, TypeID.Object, TypeID.ShortType, TypeID.Object).HasParams(TypeID.Object)
                                        .Add(TGetValue_object_byteType, TypeID.Object, TypeID.ByteType)
                                      .Add(TGetValue_object_byteType_object, TypeID.Object, TypeID.ByteType, TypeID.Object)
                                      .Add(TGetValue_object_byteType_object_objectParams, TypeID.Object, TypeID.ByteType, TypeID.Object).HasParams(TypeID.Object)
                                        .Add(TGetValue_object_dateType, TypeID.Object, TypeID.DateType)
                                      .Add(TGetValue_object_dateType_object, TypeID.Object, TypeID.DateType, TypeID.Object)
                                      .Add(TGetValue_object_dateType_object_objectParams, TypeID.Object, TypeID.DateType, TypeID.Object).HasParams(TypeID.Object)

                                      .Add(TGetValue_object_objectArrayType, TypeID.Object, TypeID.ObjectArrayType)
                                      .Add(TGetValue_object_objectArrayType_object, TypeID.Object, TypeID.ObjectArrayType, TypeID.Object)
                                      .Add(TGetValue_object_objectArrayType_object_objectParams, TypeID.Object, TypeID.ObjectArrayType, TypeID.Object).HasParams(TypeID.Object)

                                      .Add(TGetValue_object_intArrayType, TypeID.Object, TypeID.IntArrayType)
                                      .Add(TGetValue_object_intArrayType_object, TypeID.Object, TypeID.IntArrayType, TypeID.Object)
                                      .Add(TGetValue_object_intArrayType_object_objectParams, TypeID.Object, TypeID.IntArrayType, TypeID.Object).HasParams(TypeID.Object)
                                      .Add(TGetValue_object_longArrayType, TypeID.Object, TypeID.LongArrayType)
                                      .Add(TGetValue_object_longArrayType_object, TypeID.Object, TypeID.LongArrayType, TypeID.Object)
                                      .Add(TGetValue_object_longArrayType_object_objectParams, TypeID.Object, TypeID.LongArrayType, TypeID.Object).HasParams(TypeID.Object)
                                      .Add(TGetValue_object_floatArrayType, TypeID.Object, TypeID.FloatArrayType)
                                      .Add(TGetValue_object_floatArrayType_object, TypeID.Object, TypeID.FloatArrayType, TypeID.Object)
                                      .Add(TGetValue_object_floatArrayType_object_objectParams, TypeID.Object, TypeID.FloatArrayType, TypeID.Object).HasParams(TypeID.Object)
                                       .Add(TGetValue_object_doubleArrayType, TypeID.Object, TypeID.DoubleArrayType)
                                      .Add(TGetValue_object_doubleArrayType_object, TypeID.Object, TypeID.DoubleArrayType, TypeID.Object)
                                      .Add(TGetValue_object_doubleArrayType_object_objectParams, TypeID.Object, TypeID.DoubleArrayType, TypeID.Object).HasParams(TypeID.Object)
                                       .Add(TGetValue_object_decimalArrayType, TypeID.Object, TypeID.DecimalArrayType)
                                      .Add(TGetValue_object_decimalArrayType_object, TypeID.Object, TypeID.DecimalArrayType, TypeID.Object)
                                      .Add(TGetValue_object_decimalArrayType_object_objectParams, TypeID.Object, TypeID.DecimalArrayType, TypeID.Object).HasParams(TypeID.Object)
                                       .Add(TGetValue_object_boolArrayType, TypeID.Object, TypeID.BoolArrayType)
                                      .Add(TGetValue_object_boolArrayType_object, TypeID.Object, TypeID.BoolArrayType, TypeID.Object)
                                      .Add(TGetValue_object_boolArrayType_object_objectParams, TypeID.Object, TypeID.BoolArrayType, TypeID.Object).HasParams(TypeID.Object)
                                       .Add(TGetValue_object_charArrayType, TypeID.Object, TypeID.CharArrayType)
                                      .Add(TGetValue_object_charArrayType_object, TypeID.Object, TypeID.CharArrayType, TypeID.Object)
                                      .Add(TGetValue_object_charArrayType_object_objectParams, TypeID.Object, TypeID.CharArrayType, TypeID.Object).HasParams(TypeID.Object)
                                       .Add(TGetValue_object_stringArrayType, TypeID.Object, TypeID.StringArrayType)
                                      .Add(TGetValue_object_stringArrayType_object, TypeID.Object, TypeID.StringArrayType, TypeID.Object)
                                      .Add(TGetValue_object_stringArrayType_object_objectParams, TypeID.Object, TypeID.StringArrayType, TypeID.Object).HasParams(TypeID.Object)
                                       .Add(TGetValue_object_shortArrayType, TypeID.Object, TypeID.ShortArrayType)
                                      .Add(TGetValue_object_shortArrayType_object, TypeID.Object, TypeID.ShortArrayType, TypeID.Object)
                                      .Add(TGetValue_object_shortArrayType_object_objectParams, TypeID.Object, TypeID.ShortArrayType, TypeID.Object).HasParams(TypeID.Object)
                                        .Add(TGetValue_object_byteArrayType, TypeID.Object, TypeID.ByteArrayType)
                                      .Add(TGetValue_object_byteArrayType_object, TypeID.Object, TypeID.ByteArrayType, TypeID.Object)
                                      .Add(TGetValue_object_byteArrayType_object_objectParams, TypeID.Object, TypeID.ByteArrayType, TypeID.Object).HasParams(TypeID.Object)
                                        .Add(TGetValue_object_dateArrayType, TypeID.Object, TypeID.DateArrayType)
                                      .Add(TGetValue_object_dateArrayType_object, TypeID.Object, TypeID.DateArrayType, TypeID.Object)
                                      .Add(TGetValue_object_dateArrayType_object_objectParams, TypeID.Object, TypeID.DateArrayType, TypeID.Object).HasParams(TypeID.Object)
                                      );

            AddFunc("TGetElement", new BF()
                                     .Add(TGetElement_object_intType_object_intParams, TypeID.Object, TypeID.IntType, TypeID.Object).HasParams(TypeID.Int)
                                     .Add(TGetElement_object_longType_object_intParams, TypeID.Object, TypeID.LongType, TypeID.Object).HasParams(TypeID.Int)
                                     .Add(TGetElement_object_floatType_object_intParams, TypeID.Object, TypeID.FloatType, TypeID.Object).HasParams(TypeID.Int)
                                     .Add(TGetElement_object_doubleType_object_intParams, TypeID.Object, TypeID.DoubleType, TypeID.Object).HasParams(TypeID.Int)
                                     .Add(TGetElement_object_decimalType_object_intParams, TypeID.Object, TypeID.DecimalType, TypeID.Object).HasParams(TypeID.Int)
                                     .Add(TGetElement_object_boolType_object_intParams, TypeID.Object, TypeID.BoolType, TypeID.Object).HasParams(TypeID.Int)
                                     .Add(TGetElement_object_charType_object_intParams, TypeID.Object, TypeID.CharType, TypeID.Object).HasParams(TypeID.Int)
                                     .Add(TGetElement_object_stringType_object_intParams, TypeID.Object, TypeID.StringType, TypeID.Object).HasParams(TypeID.Int)
                                     .Add(TGetElement_object_shortType_object_intParams, TypeID.Object, TypeID.ShortType, TypeID.Object).HasParams(TypeID.Int)
                                     .Add(TGetElement_object_byteType_object_intParams, TypeID.Object, TypeID.ByteType, TypeID.Object).HasParams(TypeID.Int)
                                     .Add(TGetElement_object_dateType_object_intParams, TypeID.Object, TypeID.DateType, TypeID.Object).HasParams(TypeID.Int)

                                     );
            AddFunc("GetElement", new BF().Add(GetElement_object_object_intParams, TypeID.Object, TypeID.Object).HasParams(TypeID.Int));
            AddFunc("SetElement", new BF().Add(SetElement_object_object_object_intParams, TypeID.Object, TypeID.Object, TypeID.Object).HasParams(TypeID.Int));

            AddFunc("Array", new BF().Add(Array_string, TypeID.String).Add(Array_object, TypeID.Object)
                                                .Add(Array_string_objectParams, TypeID.String).HasParams(TypeID.Object)
                                                .Add(Array_object_objectParams, TypeID.Object).HasParams(TypeID.Object));
            AddFunc("Is", new BF().Add(Is_object_hintArrayType, TypeID.Object, TypeID.HintArrayType)
                                    .Add(Is_object_string, TypeID.Object, TypeID.String)
                                    .Add(Is_object_customType, TypeID.Object, TypeID.CustomType)
                                    .Add(Is_object_object, TypeID.Object, TypeID.Object)
                                    );
            AddFunc("GetAbsoluteUri", new BF().Add(GetAbsoluteUri_string_string, TypeID.String, TypeID.String)
                                                    .Add(GetAbsoluteUri_string_string_bool, TypeID.String, TypeID.String, TypeID.Bool));
         
            AddFunc("As", new BF().Add(As_object_customType, TypeID.Object, TypeID.CustomType).Return(1, true)
                                        .Add(As_object_hintArrayType, TypeID.Object, TypeID.HintArrayType).HAReturn(1, true)

                                        .Add(As_object_object, TypeID.Object, TypeID.Object).HReturn(1, true)
                                        .Add(As_object_string, TypeID.Object, TypeID.String));
            AddFunc("Clone", new BF().Add(Clone_object_string, TypeID.Object, TypeID.String)
                                           .Add(Clone_object_customType, TypeID.Object, TypeID.CustomType).Return(1, true)
                                           .Add(Clone_object_object, TypeID.Object, TypeID.Object));
            AddFunc("Slice", new BF().Add(Slice_string_empty_int, TypeID.String, TypeID.Empty, TypeID.Int)
                                               .Add(Slice_string_int_empty, TypeID.String, TypeID.Int, TypeID.Empty)
                                               .Add(Slice_string_int_empty, TypeID.String, TypeID.Int)
                                               .Add(Slice_string_int_int, TypeID.String, TypeID.Int, TypeID.Int)

                                               .Add(Slice_stringArray_empty_int, TypeID.StringArray, TypeID.Empty, TypeID.Int)
                                               .Add(Slice_stringArray_int_empty, TypeID.StringArray, TypeID.Int, TypeID.Empty)
                                               .Add(Slice_stringArray_int_empty, TypeID.StringArray, TypeID.Int)
                                               .Add(Slice_stringArray_int_int, TypeID.StringArray, TypeID.Int, TypeID.Int)

                                               .Add(Slice_intArray_empty_int, TypeID.IntArray, TypeID.Empty, TypeID.Int)
                                               .Add(Slice_intArray_int_empty, TypeID.IntArray, TypeID.Int, TypeID.Empty)
                                               .Add(Slice_intArray_int_empty, TypeID.IntArray, TypeID.Int)
                                               .Add(Slice_intArray_int_int, TypeID.IntArray, TypeID.Int, TypeID.Int)

                                               .Add(Slice_longArray_empty_int, TypeID.LongArray, TypeID.Empty, TypeID.Int)
                                               .Add(Slice_longArray_int_empty, TypeID.LongArray, TypeID.Int, TypeID.Empty)
                                               .Add(Slice_longArray_int_empty, TypeID.LongArray, TypeID.Int)
                                               .Add(Slice_longArray_int_int, TypeID.LongArray, TypeID.Int, TypeID.Int)

                                               .Add(Slice_floatArray_empty_int, TypeID.FloatArray, TypeID.Empty, TypeID.Int)
                                               .Add(Slice_floatArray_int_empty, TypeID.FloatArray, TypeID.Int, TypeID.Empty)
                                               .Add(Slice_floatArray_int_empty, TypeID.FloatArray, TypeID.Int)
                                               .Add(Slice_floatArray_int_int, TypeID.FloatArray, TypeID.Int, TypeID.Int)

                                               .Add(Slice_doubleArray_empty_int, TypeID.DoubleArray, TypeID.Empty, TypeID.Int)
                                               .Add(Slice_doubleArray_int_empty, TypeID.DoubleArray, TypeID.Int, TypeID.Empty)
                                               .Add(Slice_doubleArray_int_empty, TypeID.DoubleArray, TypeID.Int)
                                               .Add(Slice_doubleArray_int_int, TypeID.DoubleArray, TypeID.Int, TypeID.Int)

                                               .Add(Slice_decimalArray_empty_int, TypeID.DecimalArray, TypeID.Empty, TypeID.Int)
                                               .Add(Slice_decimalArray_int_empty, TypeID.DecimalArray, TypeID.Int, TypeID.Empty)
                                               .Add(Slice_decimalArray_int_empty, TypeID.DecimalArray, TypeID.Int)
                                               .Add(Slice_decimalArray_int_int, TypeID.DecimalArray, TypeID.Int, TypeID.Int)

                                               .Add(Slice_boolArray_empty_int, TypeID.BoolArray, TypeID.Empty, TypeID.Int)
                                               .Add(Slice_boolArray_int_empty, TypeID.BoolArray, TypeID.Int, TypeID.Empty)
                                               .Add(Slice_boolArray_int_empty, TypeID.BoolArray, TypeID.Int)
                                               .Add(Slice_boolArray_int_int, TypeID.BoolArray, TypeID.Int, TypeID.Int)

                                               .Add(Slice_charArray_empty_int, TypeID.CharArray, TypeID.Empty, TypeID.Int)
                                               .Add(Slice_charArray_int_empty, TypeID.CharArray, TypeID.Int, TypeID.Empty)
                                               .Add(Slice_charArray_int_empty, TypeID.CharArray, TypeID.Int)
                                               .Add(Slice_charArray_int_int, TypeID.CharArray, TypeID.Int, TypeID.Int)

                                               .Add(Slice_objectArray_empty_int, TypeID.ObjectArray, TypeID.Empty, TypeID.Int)
                                               .Add(Slice_objectArray_int_empty, TypeID.ObjectArray, TypeID.Int, TypeID.Empty)
                                               .Add(Slice_objectArray_int_empty, TypeID.ObjectArray, TypeID.Int)
                                               .Add(Slice_objectArray_int_int, TypeID.ObjectArray, TypeID.Int, TypeID.Int)

                                               .Add(Slice_byteArray_empty_int, TypeID.ByteArray, TypeID.Empty, TypeID.Int)
                                               .Add(Slice_byteArray_int_empty, TypeID.ByteArray, TypeID.Int, TypeID.Empty)
                                               .Add(Slice_byteArray_int_empty, TypeID.ByteArray, TypeID.Int)
                                               .Add(Slice_byteArray_int_int, TypeID.ByteArray, TypeID.Int, TypeID.Int)

                                               .Add(Slice_shortArray_empty_int, TypeID.ShortArray, TypeID.Empty, TypeID.Int)
                                               .Add(Slice_shortArray_int_empty, TypeID.ShortArray, TypeID.Int, TypeID.Empty)
                                               .Add(Slice_shortArray_int_empty, TypeID.ShortArray, TypeID.Int)
                                               .Add(Slice_shortArray_int_int, TypeID.ShortArray, TypeID.Int, TypeID.Int)

                                               .Add(Slice_dateArray_empty_int, TypeID.DateArray, TypeID.Empty, TypeID.Int)
                                               .Add(Slice_dateArray_int_empty, TypeID.DateArray, TypeID.Int, TypeID.Empty)
                                               .Add(Slice_dateArray_int_empty, TypeID.DateArray, TypeID.Int)
                                               .Add(Slice_dateArray_int_int, TypeID.DateArray, TypeID.Int, TypeID.Int)

                                               .Add(Slice_customArray_empty_int, TypeID.CustomArray, TypeID.Empty, TypeID.Int).Return(0)
                                               .Add(Slice_customArray_int_empty, TypeID.CustomArray, TypeID.Int, TypeID.Empty).Return(0)
                                               .Add(Slice_customArray_int_empty, TypeID.CustomArray, TypeID.Int).Return(0)
                                               .Add(Slice_customArray_int_int, TypeID.CustomArray, TypeID.Int, TypeID.Int).Return(0)
                                               );
            AddFunc("Splice", new BF().Add(Splice_intArray_int_int_objectParams, TypeID.IntArray, TypeID.Int, TypeID.Int).HasParams(TypeID.Object)
                                             .Add(Splice_intArray_int_int_objectParams, TypeID.IntArray, TypeID.Int)
                                              .Add(Splice_stringArray_int_int_objectParams, TypeID.StringArray, TypeID.Int, TypeID.Int).HasParams(TypeID.Object)
                                              .Add(Splice_stringArray_int_int_objectParams, TypeID.StringArray, TypeID.Int)
                                              .Add(Splice_longArray_int_int_objectParams, TypeID.LongArray, TypeID.Int, TypeID.Int).HasParams(TypeID.Object)
                                              .Add(Splice_longArray_int_int_objectParams, TypeID.LongArray, TypeID.Int)
                                              .Add(Splice_floatArray_int_int_objectParams, TypeID.FloatArray, TypeID.Int, TypeID.Int).HasParams(TypeID.Object)
                                              .Add(Splice_floatArray_int_int_objectParams, TypeID.FloatArray, TypeID.Int)
                                              .Add(Splice_doubleArray_int_int_objectParams, TypeID.DoubleArray, TypeID.Int, TypeID.Int).HasParams(TypeID.Object)
                                              .Add(Splice_doubleArray_int_int_objectParams, TypeID.DoubleArray, TypeID.Int)
                                              .Add(Splice_decimalArray_int_int_objectParams, TypeID.DecimalArray, TypeID.Int, TypeID.Int).HasParams(TypeID.Object)
                                              .Add(Splice_decimalArray_int_int_objectParams, TypeID.DecimalArray, TypeID.Int)
                                              .Add(Splice_boolArray_int_int_objectParams, TypeID.BoolArray, TypeID.Int, TypeID.Int).HasParams(TypeID.Object)
                                              .Add(Splice_boolArray_int_int_objectParams, TypeID.BoolArray, TypeID.Int)
                                              .Add(Splice_charArray_int_int_objectParams, TypeID.CharArray, TypeID.Int, TypeID.Int).HasParams(TypeID.Object)
                                              .Add(Splice_charArray_int_int_objectParams, TypeID.CharArray, TypeID.Int)
                                              .Add(Splice_objectArray_int_int_objectParams, TypeID.ObjectArray, TypeID.Int, TypeID.Int).HasParams(TypeID.Object)
                                              .Add(Splice_objectArray_int_int_objectParams, TypeID.ObjectArray, TypeID.Int)
                                              .Add(Splice_byteArray_int_int_objectParams, TypeID.ByteArray, TypeID.Int, TypeID.Int).HasParams(TypeID.Object)
                                              .Add(Splice_byteArray_int_int_objectParams, TypeID.ByteArray, TypeID.Int)
                                              .Add(Splice_shortArray_int_int_objectParams, TypeID.ShortArray, TypeID.Int, TypeID.Int).HasParams(TypeID.Object)
                                              .Add(Splice_shortArray_int_int_objectParams, TypeID.ShortArray, TypeID.Int)
                                              .Add(Splice_dateArray_int_int_objectParams, TypeID.DateArray, TypeID.Int, TypeID.Int).HasParams(TypeID.Object)
                                              .Add(Splice_dateArray_int_int_objectParams, TypeID.DateArray, TypeID.Int)
                                              .Add(Splice_customArray_int_int_objectParams, TypeID.CustomArray, TypeID.Int, TypeID.Int).HasParams(TypeID.Object).Return(0)
                                              .Add(Splice_customArray_int_int_objectParams, TypeID.CustomArray, TypeID.Int).Return(0)
                                              );

            AddFunc("IniGet", new BF().Add(IniGet_string_string, TypeID.String, TypeID.String)
                                              .Add(IniGet_string_string_string, TypeID.String, TypeID.String, TypeID.String));
            AddFunc("IniSet", new BF().Add(IniSet_string_string_object, TypeID.String, TypeID.String, TypeID.Object)
                                           .Add(IniSet_string_string_string_object, TypeID.String, TypeID.String, TypeID.String, TypeID.Object));

            AddFunc("Push", new BF().Add(Push_intArray_intParams, TypeID.IntArray).HasParams(TypeID.Int, true)
                                           .Add(Push_longArray_longParams, TypeID.LongArray).HasParams(TypeID.Long, true)
                                           .Add(Push_floatArray_floatParams, TypeID.FloatArray).HasParams(TypeID.Float, true)
                                           .Add(Push_doubleArray_doubleParams, TypeID.DoubleArray).HasParams(TypeID.Double, true)
                                           .Add(Push_decimalArray_decimalParams, TypeID.DecimalArray).HasParams(TypeID.Decimal, true)
                                           .Add(Push_boolArray_boolParams, TypeID.BoolArray).HasParams(TypeID.Bool, true)
                                           .Add(Push_stringArray_stringParams, TypeID.StringArray).HasParams(TypeID.String, true)
                                           .Add(Push_charArray_charParams, TypeID.CharArray).HasParams(TypeID.Char, true)
                                           .Add(Push_byteArray_byteParams, TypeID.ByteArray).HasParams(TypeID.Byte, true)
                                           .Add(Push_shortArray_shortParams, TypeID.ShortArray).HasParams(TypeID.Short, true)
                                           .Add(Push_dateArray_dateParams, TypeID.DateArray).HasParams(TypeID.Date, true)
                                           .Add(Push_customArray_customParams, TypeID.CustomArray).HasParams(TypeID.Custom)
                                           .Add(Push_objectArray_objectParams, TypeID.ObjectArray).HasParams(TypeID.Object)
                                           .Add(Push_object_objectParams, TypeID.Object).HasParams(TypeID.Object).Strict(0)
                                           );
            AddFunc("ConvPush", new BF().Add(Push_intArray_intParams, TypeID.IntArray).HasParams(TypeID.Int)
                                           .Add(Push_longArray_longParams, TypeID.LongArray).HasParams(TypeID.Long)
                                           .Add(Push_floatArray_floatParams, TypeID.FloatArray).HasParams(TypeID.Float)
                                           .Add(Push_doubleArray_doubleParams, TypeID.DoubleArray).HasParams(TypeID.Double)
                                           .Add(Push_decimalArray_decimalParams, TypeID.DecimalArray).HasParams(TypeID.Decimal)
                                           .Add(Push_boolArray_boolParams, TypeID.BoolArray).HasParams(TypeID.Bool)
                                           .Add(Push_stringArray_stringParams, TypeID.StringArray).HasParams(TypeID.String)
                                           .Add(Push_charArray_charParams, TypeID.CharArray).HasParams(TypeID.Char)
                                           .Add(Push_byteArray_byteParams, TypeID.ByteArray).HasParams(TypeID.Byte)
                                           .Add(Push_shortArray_shortParams, TypeID.ShortArray).HasParams(TypeID.Short)
                                           .Add(Push_dateArray_dateParams, TypeID.DateArray).HasParams(TypeID.Object)
                                           .Add(ConvPush_object_objectParams, TypeID.Object).HasParams(TypeID.Object).Strict(0)
                                         );


            AddFunc("Peek", new BF().Add(Peek_intArray, TypeID.IntArray)
                                      .Add(Peek_longArray, TypeID.LongArray)
                                      .Add(Peek_floatArray, TypeID.FloatArray)
                                      .Add(Peek_doubleArray, TypeID.DoubleArray)
                                      .Add(Peek_decimalArray, TypeID.DecimalArray)
                                      .Add(Peek_boolArray, TypeID.BoolArray)
                                      .Add(Peek_stringArray, TypeID.StringArray)
                                      .Add(Peek_charArray, TypeID.CharArray)
                                      .Add(Peek_objectArray, TypeID.ObjectArray)
                                      .Add(Peek_byteArray, TypeID.ByteArray)
                                      .Add(Peek_shortArray, TypeID.ShortArray)
                                      .Add(Peek_dateArray, TypeID.DateArray)
                                      .Add(Peek_customArray, TypeID.CustomArray).Return(0, false, true)
                                      .Add(Peek_object, TypeID.Object).Strict(0)
                                      );
            AddFunc("Pop", new BF().Add(Pop_intArray, TypeID.IntArray)
                                       .Add(Pop_longArray, TypeID.LongArray)
                                       .Add(Pop_floatArray, TypeID.FloatArray)
                                       .Add(Pop_doubleArray, TypeID.DoubleArray)
                                       .Add(Pop_decimalArray, TypeID.DecimalArray)
                                       .Add(Pop_boolArray, TypeID.BoolArray)
                                       .Add(Pop_stringArray, TypeID.StringArray)
                                       .Add(Pop_charArray, TypeID.CharArray)
                                       .Add(Pop_objectArray, TypeID.ObjectArray)
                                       .Add(Pop_byteArray, TypeID.ByteArray)
                                       .Add(Pop_shortArray, TypeID.ShortArray)
                                       .Add(Pop_dateArray, TypeID.DateArray)
                                       .Add(Pop_customArray, TypeID.CustomArray).Return(0, false, true)
                                       .Add(Pop_object, TypeID.Object).Strict(0));
            AddFunc("At", new BF().Add(At_intArray, TypeID.IntArray, TypeID.Int)
                                       .Add(At_longArray, TypeID.LongArray, TypeID.Int)
                                       .Add(At_floatArray, TypeID.FloatArray, TypeID.Int)
                                       .Add(At_doubleArray, TypeID.DoubleArray, TypeID.Int)
                                       .Add(At_decimalArray, TypeID.DecimalArray, TypeID.Int)
                                       .Add(At_boolArray, TypeID.BoolArray, TypeID.Int)
                                       .Add(At_stringArray, TypeID.StringArray, TypeID.Int)
                                       .Add(At_charArray, TypeID.CharArray, TypeID.Int)
                                       .Add(At_objectArray, TypeID.ObjectArray, TypeID.Int)
                                       .Add(At_byteArray, TypeID.ByteArray, TypeID.Int)
                                       .Add(At_shortArray, TypeID.ShortArray, TypeID.Int)
                                       .Add(At_dateArray, TypeID.DateArray, TypeID.Int)
                                       .Add(At_customArray, TypeID.CustomArray, TypeID.Int).Return(0, false, true)
                                       .Add(At_string, TypeID.String, TypeID.Int)
                                       .Add(At_object, TypeID.Object, TypeID.Int));

            AddFunc("Tuple", new BF().Add(Tuple_objectParams).HasParams(TypeID.Object));
            AddFunc("ValueTuple", new BF().Add(ValueTuple_objectParams).HasParams(TypeID.Object));


            AddFunc("RemoveAt", new BF().Add(RemoveRange_intArray_int_int, TypeID.IntArray, TypeID.Int)
                                                .Add(RemoveRange_longArray_int_int, TypeID.LongArray, TypeID.Int)
                                                .Add(RemoveRange_floatArray_int_int, TypeID.FloatArray, TypeID.Int)
                                                .Add(RemoveRange_doubleArray_int_int, TypeID.DoubleArray, TypeID.Int)
                                                .Add(RemoveRange_decimalArray_int_int, TypeID.DecimalArray, TypeID.Int)
                                                .Add(RemoveRange_boolArray_int_int, TypeID.BoolArray, TypeID.Int)
                                                .Add(RemoveRange_stringArray_int_int, TypeID.StringArray, TypeID.Int)
                                                .Add(RemoveRange_charArray_int_int, TypeID.CharArray, TypeID.Int)
                                                .Add(RemoveRange_objectArray_int_int, TypeID.ObjectArray, TypeID.Int)
                                                .Add(RemoveRange_byteArray_int_int, TypeID.ByteArray, TypeID.Int)
                                                .Add(RemoveRange_shortArray_int_int, TypeID.ShortArray, TypeID.Int)
                                                .Add(RemoveRange_dateArray_int_int, TypeID.DateArray, TypeID.Int)
                                                .Add(RemoveRange_customArray_int_int, TypeID.CustomArray, TypeID.Int)
                                                .Add(RemoveRange_object_int_int, TypeID.Object, TypeID.Int)
                                              );
            AddFunc("RemoveRange", new BF().Add(RemoveRange_intArray_int_int, TypeID.IntArray, TypeID.Int, TypeID.Int)
                                                  .Add(RemoveRange_longArray_int_int, TypeID.LongArray, TypeID.Int, TypeID.Int)
                                                  .Add(RemoveRange_floatArray_int_int, TypeID.FloatArray, TypeID.Int, TypeID.Int)
                                                  .Add(RemoveRange_doubleArray_int_int, TypeID.DoubleArray, TypeID.Int, TypeID.Int)
                                                  .Add(RemoveRange_decimalArray_int_int, TypeID.DecimalArray, TypeID.Int, TypeID.Int)
                                                  .Add(RemoveRange_boolArray_int_int, TypeID.BoolArray, TypeID.Int, TypeID.Int)
                                                  .Add(RemoveRange_stringArray_int_int, TypeID.StringArray, TypeID.Int, TypeID.Int)
                                                  .Add(RemoveRange_charArray_int_int, TypeID.CharArray, TypeID.Int, TypeID.Int)
                                                  .Add(RemoveRange_objectArray_int_int, TypeID.ObjectArray, TypeID.Int, TypeID.Int)
                                                  .Add(RemoveRange_byteArray_int_int, TypeID.ByteArray, TypeID.Int, TypeID.Int)
                                                  .Add(RemoveRange_shortArray_int_int, TypeID.ShortArray, TypeID.Int, TypeID.Int)
                                                  .Add(RemoveRange_dateArray_int_int, TypeID.DateArray, TypeID.Int, TypeID.Int)
                                                  .Add(RemoveRange_customArray_int_int, TypeID.CustomArray, TypeID.Int, TypeID.Int)
                                                  .Add(RemoveRange_object_int_int, TypeID.Object, TypeID.Int, TypeID.Int)
                                                );
            AddFunc("RemoveAll", new BF().Add(RemoveAll_customArray_custom_bool, TypeID.CustomArray, TypeID.Custom, TypeID.Bool)
                                                .Add(RemoveAll_intArray_int_bool, TypeID.IntArray, TypeID.Int, TypeID.Bool)
                                                .Add(RemoveAll_longArray_long_bool, TypeID.LongArray, TypeID.Long, TypeID.Bool)
                                                .Add(RemoveAll_floatArray_float_bool, TypeID.FloatArray, TypeID.Float, TypeID.Bool)
                                                .Add(RemoveAll_doubleArray_double_bool, TypeID.DoubleArray, TypeID.Double, TypeID.Bool)
                                                .Add(RemoveAll_decimalArray_decimal_bool, TypeID.DecimalArray, TypeID.Decimal, TypeID.Bool)
                                                .Add(RemoveAll_boolArray_bool_bool, TypeID.BoolArray, TypeID.Bool, TypeID.Bool)
                                                .Add(RemoveAll_charArray_char_bool, TypeID.CharArray, TypeID.Char, TypeID.Bool)
                                                .Add(RemoveAll_stringArray_string_bool, TypeID.StringArray, TypeID.String, TypeID.Bool)
                                                .Add(RemoveAll_objectArray_object_bool, TypeID.ObjectArray, TypeID.Object, TypeID.Bool)
                                                .Add(RemoveAll_shortArray_short_bool, TypeID.ShortArray, TypeID.Short, TypeID.Bool)
                                                .Add(RemoveAll_byteArray_byte_bool, TypeID.ByteArray, TypeID.Byte, TypeID.Bool)
                                                .Add(RemoveAll_dateArray_date_bool, TypeID.DateArray, TypeID.Date, TypeID.Bool)

                                                .Add(RemoveAll_object_custom_bool, TypeID.Object, TypeID.Custom, TypeID.Bool)
                                                .Add(RemoveAll_object_int_bool, TypeID.Object, TypeID.Int, TypeID.Bool)
                                                .Add(RemoveAll_object_long_bool, TypeID.Object, TypeID.Long, TypeID.Bool)
                                                .Add(RemoveAll_object_float_bool, TypeID.Object, TypeID.Float, TypeID.Bool)
                                                .Add(RemoveAll_object_double_bool, TypeID.Object, TypeID.Double, TypeID.Bool)
                                                .Add(RemoveAll_object_decimal_bool, TypeID.Object, TypeID.Decimal, TypeID.Bool)
                                                .Add(RemoveAll_object_bool_bool, TypeID.Object, TypeID.Bool, TypeID.Bool)
                                                .Add(RemoveAll_object_char_bool, TypeID.Object, TypeID.Char, TypeID.Bool)
                                                .Add(RemoveAll_object_string_bool, TypeID.Object, TypeID.String, TypeID.Bool)
                                                .Add(RemoveAll_object_object_bool, TypeID.Object, TypeID.Object, TypeID.Bool)
                                                .Add(RemoveAll_object_short_bool, TypeID.Object, TypeID.Short, TypeID.Bool)
                                                .Add(RemoveAll_object_byte_bool, TypeID.Object, TypeID.Byte, TypeID.Bool)
                                                .Add(RemoveAll_object_date_bool, TypeID.Object, TypeID.Date, TypeID.Bool)
                                                .ConvertAll(x => x.Strict().Kind(1, EvalUnitKind.Variable))
                                                );
            AddFunc("SetBasePath", new BF().Add(SetBasePath_string, TypeID.String));
            AddFunc("BasePath", new BF().Add(BasePath));
            AddFunc("CodeFile", new BF().Add(CodeFile));
            AddFunc("", new BF().Add(SequentialEval_object_objectParams, TypeID.Object).HasParams(TypeID.Object));
            AddFunc("_", new BF().Add(FirstValSequentialEval_int_objectParams, TypeID.Int).HasParams(TypeID.Object)
                                 .Add(FirstValSequentialEval_long_objectParams, TypeID.Long).HasParams(TypeID.Object)
                                 .Add(FirstValSequentialEval_float_objectParams, TypeID.Float).HasParams(TypeID.Object)
                                 .Add(FirstValSequentialEval_double_objectParams, TypeID.Double).HasParams(TypeID.Object)
                                 .Add(FirstValSequentialEval_decimal_objectParams, TypeID.Decimal).HasParams(TypeID.Object)
                                 .Add(FirstValSequentialEval_bool_objectParams, TypeID.Bool).HasParams(TypeID.Object)
                                 .Add(FirstValSequentialEval_char_objectParams, TypeID.Char).HasParams(TypeID.Object)
                                 .Add(FirstValSequentialEval_string_objectParams, TypeID.String).HasParams(TypeID.Object)
                                 .Add(FirstValSequentialEval_short_objectParams, TypeID.Short).HasParams(TypeID.Object)
                                 .Add(FirstValSequentialEval_byte_objectParams, TypeID.Byte).HasParams(TypeID.Object)
                                 .Add(FirstValSequentialEval_date_objectParams, TypeID.Date).HasParams(TypeID.Object)
                                 .Add(FirstValSequentialEval_custom_objectParams, TypeID.Custom).HasParams(TypeID.Object).Return(0)
                                 .Add(FirstValSequentialEval_custom_objectParams, TypeID.CustomArray).HasParams(TypeID.Object).Return(0)
                                 .Add(FirstValSequentialEval_object_objectParams, TypeID.Object).HasParams(TypeID.Object)

                                 .Add(FirstValSequentialEval_intArray_objectParams, TypeID.IntArray).HasParams(TypeID.Object)
                                 .Add(FirstValSequentialEval_longArray_objectParams, TypeID.LongArray).HasParams(TypeID.Object)
                                 .Add(FirstValSequentialEval_floatArray_objectParams, TypeID.FloatArray).HasParams(TypeID.Object)
                                 .Add(FirstValSequentialEval_doubleArray_objectParams, TypeID.DoubleArray).HasParams(TypeID.Object)
                                 .Add(FirstValSequentialEval_decimalArray_objectParams, TypeID.DecimalArray).HasParams(TypeID.Object)
                                 .Add(FirstValSequentialEval_boolArray_objectParams, TypeID.BoolArray).HasParams(TypeID.Object)
                                 .Add(FirstValSequentialEval_charArray_objectParams, TypeID.CharArray).HasParams(TypeID.Object)
                                 .Add(FirstValSequentialEval_stringArray_objectParams, TypeID.StringArray).HasParams(TypeID.Object)
                                 .Add(FirstValSequentialEval_shortArray_objectParams, TypeID.ShortArray).HasParams(TypeID.Object)
                                 .Add(FirstValSequentialEval_byteArray_objectParams, TypeID.ByteArray).HasParams(TypeID.Object)
                                 .Add(FirstValSequentialEval_dateArray_objectParams, TypeID.DateArray).HasParams(TypeID.Object)
                                 .Add(FirstValSequentialEval_objectArray_objectParams, TypeID.ObjectArray).HasParams(TypeID.Object)
                                 .ConvertAll(x => x.Strict(0))
                                 );
            AddFunc("__", new BF().Add(FirstValAfterSequentialEval_int_objectParams, TypeID.Int).HasParams(TypeID.Object)
                           .Add(FirstValAfterSequentialEval_long_objectParams, TypeID.Long).HasParams(TypeID.Object)
                           .Add(FirstValAfterSequentialEval_float_objectParams, TypeID.Float).HasParams(TypeID.Object)
                           .Add(FirstValAfterSequentialEval_double_objectParams, TypeID.Double).HasParams(TypeID.Object)
                           .Add(FirstValAfterSequentialEval_decimal_objectParams, TypeID.Decimal).HasParams(TypeID.Object)
                           .Add(FirstValAfterSequentialEval_bool_objectParams, TypeID.Bool).HasParams(TypeID.Object)
                           .Add(FirstValAfterSequentialEval_char_objectParams, TypeID.Char).HasParams(TypeID.Object)
                           .Add(FirstValAfterSequentialEval_string_objectParams, TypeID.String).HasParams(TypeID.Object)
                           .Add(FirstValAfterSequentialEval_short_objectParams, TypeID.Short).HasParams(TypeID.Object)
                           .Add(FirstValAfterSequentialEval_byte_objectParams, TypeID.Byte).HasParams(TypeID.Object)
                           .Add(FirstValAfterSequentialEval_date_objectParams, TypeID.Date).HasParams(TypeID.Object)
                           .Add(FirstValAfterSequentialEval_custom_objectParams, TypeID.Custom).HasParams(TypeID.Object).Return(0)
                           .Add(FirstValAfterSequentialEval_custom_objectParams, TypeID.CustomArray).HasParams(TypeID.Object).Return(0)
                           .Add(FirstValAfterSequentialEval_object_objectParams, TypeID.Object).HasParams(TypeID.Object)

                           .Add(FirstValAfterSequentialEval_intArray_objectParams, TypeID.IntArray).HasParams(TypeID.Object)
                           .Add(FirstValAfterSequentialEval_longArray_objectParams, TypeID.LongArray).HasParams(TypeID.Object)
                           .Add(FirstValAfterSequentialEval_floatArray_objectParams, TypeID.FloatArray).HasParams(TypeID.Object)
                           .Add(FirstValAfterSequentialEval_doubleArray_objectParams, TypeID.DoubleArray).HasParams(TypeID.Object)
                           .Add(FirstValAfterSequentialEval_decimalArray_objectParams, TypeID.DecimalArray).HasParams(TypeID.Object)
                           .Add(FirstValAfterSequentialEval_boolArray_objectParams, TypeID.BoolArray).HasParams(TypeID.Object)
                           .Add(FirstValAfterSequentialEval_charArray_objectParams, TypeID.CharArray).HasParams(TypeID.Object)
                           .Add(FirstValAfterSequentialEval_stringArray_objectParams, TypeID.StringArray).HasParams(TypeID.Object)
                           .Add(FirstValAfterSequentialEval_shortArray_objectParams, TypeID.ShortArray).HasParams(TypeID.Object)
                           .Add(FirstValAfterSequentialEval_byteArray_objectParams, TypeID.ByteArray).HasParams(TypeID.Object)
                           .Add(FirstValAfterSequentialEval_dateArray_objectParams, TypeID.DateArray).HasParams(TypeID.Object)
                           .Add(FirstValAfterSequentialEval_objectArray_objectParams, TypeID.ObjectArray).HasParams(TypeID.Object)
                           .ConvertAll(x => x.Strict(0))
                           );
            AddFunc("Expr", new BF().Add(Expr_object, TypeID.Object).Add(Expr_object_objectParams, TypeID.Object).HasParams(TypeID.Object));
            AddFunc("SetVarScopeKind", new BF().Add(SetVarScopeKind_object_string_objectParams, TypeID.Object, TypeID.String).HasParams(TypeID.Object));
            AddFunc("Box", new BF().Add(BoxValues_object_objectParams, TypeID.Object).HasParams(TypeID.Object));
            AddFunc("BoxAll", new BF().Add(BoxAll_object_bool, TypeID.Object, TypeID.Bool).FewerArgsAllowed(1));
            AddFunc("FixOrigExpr", new BF().Add(FixOrigExpr_object, TypeID.Object));
            AddFunc("OrigExpr", new BF().Add(OrigExpr_object, TypeID.Object).Add(OrigExpr_object_objectParams, TypeID.Object).HasParams(TypeID.Object));
            AddFunc("Involve", new BF().Add(Involve_object_objectParams, TypeID.Object).HasParams(TypeID.Object));
            AddFunc("CopyExpr", new BF().Add(CopyExpr_object, TypeID.Object));
            AddFunc("Renew", new BF().Add(RenewExpr_object_object, TypeID.Object, TypeID.Object)
                                     .Add(RenewExpr_object_object_bool, TypeID.Object, TypeID.Object, TypeID.Bool));

            AddFunc("Eval", new BF().Add(Eval_object, TypeID.Object)
                                    .Add(Eval_object_objectType, TypeID.Object, TypeID.ObjectType)
                                    .Add(Eval_object_intType, TypeID.Object, TypeID.IntType)
                                    .Add(Eval_object_longType, TypeID.Object, TypeID.LongType)
                                    .Add(Eval_object_floatType, TypeID.Object, TypeID.FloatType)
                                    .Add(Eval_object_doubleType, TypeID.Object, TypeID.DoubleType)
                                    .Add(Eval_object_decimalType, TypeID.Object, TypeID.DecimalType)
                                    .Add(Eval_object_boolType, TypeID.Object, TypeID.BoolType)
                                    .Add(Eval_object_charType, TypeID.Object, TypeID.CharType)
                                    .Add(Eval_object_stringType, TypeID.Object, TypeID.StringType)
                                    .Add(Eval_object_shortType, TypeID.Object, TypeID.ShortType)
                                    .Add(Eval_object_byteType, TypeID.Object, TypeID.ByteType)
                                    .Add(Eval_object_dateType, TypeID.Object, TypeID.DateType)
                                    .Add(Eval_object_customType, TypeID.Object, TypeID.CustomType).Return(1, true)


                                    .Add(Eval_object_objectArrayType, TypeID.Object, TypeID.ObjectArrayType)
                                    .Add(Eval_object_intArrayType, TypeID.Object, TypeID.IntArrayType)
                                    .Add(Eval_object_longArrayType, TypeID.Object, TypeID.LongArrayType)
                                    .Add(Eval_object_floatArrayType, TypeID.Object, TypeID.FloatArrayType)
                                    .Add(Eval_object_doubleArrayType, TypeID.Object, TypeID.DoubleArrayType)
                                    .Add(Eval_object_decimalArrayType, TypeID.Object, TypeID.DecimalArrayType)
                                    .Add(Eval_object_boolArrayType, TypeID.Object, TypeID.BoolArrayType)
                                    .Add(Eval_object_charArrayType, TypeID.Object, TypeID.CharArrayType)
                                    .Add(Eval_object_stringArrayType, TypeID.Object, TypeID.StringArrayType)
                                    .Add(Eval_object_shortArrayType, TypeID.Object, TypeID.ShortArrayType)
                                    .Add(Eval_object_byteArrayType, TypeID.Object, TypeID.ByteArrayType)
                                    .Add(Eval_object_dateArrayType, TypeID.Object, TypeID.DateArrayType)
                                    .ConvertAll(x => x.Strict(0))
                                  );
            AddFunc("ConvertArray", new BF().Add(ConvertArray_object_string, TypeID.Object, TypeID.String).Add(ConvertArray_object_object, TypeID.Object, TypeID.Object));
            AddFunc("ToObjectArray", new BF().Add(ToObjectArray_object, TypeID.Object).Add(ToObjectArray_object_string, TypeID.Object, TypeID.String).Add(ToObjectArray_object_object, TypeID.Object, TypeID.Object));
            AddFunc("ToArray", new BF().Add(ToArray_object_string, TypeID.Object, TypeID.String).Add(ToArray_object_string_int, TypeID.Object, TypeID.String, TypeID.Int)
                                       .Add(ToArray_object_object, TypeID.Object, TypeID.Object).Add(ToArray_object_object_int, TypeID.Object, TypeID.Object, TypeID.Int));

            AddFunc("GetGlobal", new BF().Add(GetGlobal_string, TypeID.String));
            AddFunc("SetGlobal", new BF().Add(SetGlobal_string_object, TypeID.String, TypeID.Object));
            AddFunc("Executor", new BF().Add(Executor));

            AddFunc("AddEventHandler", new BF().Add(AddEventHandler_object_object_object, TypeID.Object, TypeID.Object, TypeID.Object));
            AddFunc("RemoveEventHandler", new BF().Add(RemoveEventHandler_object_object_object, TypeID.Object, TypeID.Object, TypeID.Object));

            AddFunc("Throw", new BF().Add(Throw).Add(Throw_object_string, TypeID.Object, TypeID.String).FewerArgsAllowed(1));

            //==========OPERATORS============

            AddFunc("op_Addition", new BF()
                         .Add(AdditionOp_int_int, TypeID.Int, TypeID.Int)
                         .Add(AdditionOp_byte_byte, TypeID.Byte, TypeID.Byte)
                         .Add(AdditionOp_short_short, TypeID.Short, TypeID.Short)
                         .Add(AdditionOp_long_long, TypeID.Long, TypeID.Long)
                         .Add(AdditionOp_float_float, TypeID.Float, TypeID.Float)
                         .Add(AdditionOp_double_double, TypeID.Double, TypeID.Double)
                         .Add(AdditionOp_decimal_decimal, TypeID.Decimal, TypeID.Decimal)
                         .Add(AdditionOp_char_char, TypeID.Char, TypeID.Char)
                         .Add(AdditionOp_string_string, TypeID.String, TypeID.String)
                         .Add(AdditionOp_string_string, TypeID.String, TypeID.Object)
                         .Add(AdditionOp_string_string, TypeID.Object, TypeID.String)

                 );
            AddFunc("op_Subtraction", new BF()
                    .Add(SubtractionOp_int_int, TypeID.Int, TypeID.Int)
                    .Add(SubtractionOp_byte_byte, TypeID.Byte, TypeID.Byte)
                    .Add(SubtractionOp_short_short, TypeID.Short, TypeID.Short)
                    .Add(SubtractionOp_long_long, TypeID.Long, TypeID.Long)
                    .Add(SubtractionOp_float_float, TypeID.Float, TypeID.Float)
                    .Add(SubtractionOp_double_double, TypeID.Double, TypeID.Double)
                    .Add(SubtractionOp_decimal_decimal, TypeID.Decimal, TypeID.Decimal)
                    .Add(SubtractionOp_char_char, TypeID.Char, TypeID.Char)



            );
            AddFunc("op_Multiply", new BF()
                    .Add(MultiplyOp_int_int, TypeID.Int, TypeID.Int)
                    .Add(MultiplyOp_byte_byte, TypeID.Byte, TypeID.Byte)
                    .Add(MultiplyOp_short_short, TypeID.Short, TypeID.Short)
                    .Add(MultiplyOp_long_long, TypeID.Long, TypeID.Long)
                    .Add(MultiplyOp_float_float, TypeID.Float, TypeID.Float)
                    .Add(MultiplyOp_double_double, TypeID.Double, TypeID.Double)
                    .Add(MultiplyOp_decimal_decimal, TypeID.Decimal, TypeID.Decimal)
                    .Add(MultiplyOp_char_char, TypeID.Char, TypeID.Char)



            );
            AddFunc("op_Division", new BF()
                    .Add(DivisionOp_int_int, TypeID.Int, TypeID.Int)
                    .Add(DivisionOp_byte_byte, TypeID.Byte, TypeID.Byte)
                    .Add(DivisionOp_short_short, TypeID.Short, TypeID.Short)
                    .Add(DivisionOp_long_long, TypeID.Long, TypeID.Long)
                    .Add(DivisionOp_float_float, TypeID.Float, TypeID.Float)
                    .Add(DivisionOp_double_double, TypeID.Double, TypeID.Double)
                    .Add(DivisionOp_decimal_decimal, TypeID.Decimal, TypeID.Decimal)
                    .Add(DivisionOp_char_char, TypeID.Char, TypeID.Char)



            );
            AddFunc("op_Modulus", new BF()
                    .Add(ModulusOp_int_int, TypeID.Int, TypeID.Int)
                    .Add(ModulusOp_byte_byte, TypeID.Byte, TypeID.Byte)
                    .Add(ModulusOp_short_short, TypeID.Short, TypeID.Short)
                    .Add(ModulusOp_long_long, TypeID.Long, TypeID.Long)
                    .Add(ModulusOp_float_float, TypeID.Float, TypeID.Float)
                    .Add(ModulusOp_double_double, TypeID.Double, TypeID.Double)
                    .Add(ModulusOp_decimal_decimal, TypeID.Decimal, TypeID.Decimal)
                    .Add(ModulusOp_char_char, TypeID.Char, TypeID.Char)



            );
            AddFunc("op_BitwiseAnd", new BF()
                    .Add(BitwiseAndOp_int_int, TypeID.Int, TypeID.Int)
                    .Add(BitwiseAndOp_byte_byte, TypeID.Byte, TypeID.Byte)
                    .Add(BitwiseAndOp_short_short, TypeID.Short, TypeID.Short)
                    .Add(BitwiseAndOp_long_long, TypeID.Long, TypeID.Long)
                    .Add(BitwiseAndOp_char_char, TypeID.Char, TypeID.Char)
                    .Add(BitwiseAndOp_bool_bool, TypeID.Bool, TypeID.Bool)



            );
            AddFunc("op_BitwiseOr", new BF()
                    .Add(BitwiseOrOp_int_int, TypeID.Int, TypeID.Int)
                    .Add(BitwiseOrOp_byte_byte, TypeID.Byte, TypeID.Byte)
                    .Add(BitwiseOrOp_short_short, TypeID.Short, TypeID.Short)
                    .Add(BitwiseOrOp_long_long, TypeID.Long, TypeID.Long)
                    .Add(BitwiseOrOp_char_char, TypeID.Char, TypeID.Char)
                    .Add(BitwiseOrOp_bool_bool, TypeID.Bool, TypeID.Bool)



            );
            AddFunc("op_ExclusiveOr", new BF()
                    .Add(ExclusiveOrOp_int_int, TypeID.Int, TypeID.Int)
                    .Add(ExclusiveOrOp_byte_byte, TypeID.Byte, TypeID.Byte)
                    .Add(ExclusiveOrOp_short_short, TypeID.Short, TypeID.Short)
                    .Add(ExclusiveOrOp_long_long, TypeID.Long, TypeID.Long)
                    .Add(ExclusiveOrOp_char_char, TypeID.Char, TypeID.Char)
                    .Add(ExclusiveOrOp_bool_bool, TypeID.Bool, TypeID.Bool)



            );
            AddFunc("op_LeftShift", new BF()
                    .Add(LeftShiftOp_int_int, TypeID.Int, TypeID.Int)
                    .Add(LeftShiftOp_byte_byte, TypeID.Byte, TypeID.Byte)
                    .Add(LeftShiftOp_short_short, TypeID.Short, TypeID.Short)
                    .Add(LeftShiftOp_char_char, TypeID.Char, TypeID.Char)



            );
            AddFunc("op_RightShift", new BF()
                    .Add(RightShiftOp_int_int, TypeID.Int, TypeID.Int)
                    .Add(RightShiftOp_byte_byte, TypeID.Byte, TypeID.Byte)
                    .Add(RightShiftOp_short_short, TypeID.Short, TypeID.Short)
                    .Add(RightShiftOp_char_char, TypeID.Char, TypeID.Char)



            );
            AddFunc("op_OnesComplement", new BF()
                    .Add(OnesComplementOp_int, TypeID.Empty, TypeID.Int)
                    .Add(OnesComplementOp_byte, TypeID.Empty, TypeID.Byte)
                    .Add(OnesComplementOp_short, TypeID.Empty, TypeID.Short)
                    .Add(OnesComplementOp_long, TypeID.Empty, TypeID.Long)
                    .Add(OnesComplementOp_char, TypeID.Empty, TypeID.Char)

            );
            AddFunc("op_Equality", new BF()
                    .Add(EqualityOp_int_int, TypeID.Int, TypeID.Int)
                    .Add(EqualityOp_byte_byte, TypeID.Byte, TypeID.Byte)
                    .Add(EqualityOp_short_short, TypeID.Short, TypeID.Short)
                    .Add(EqualityOp_long_long, TypeID.Long, TypeID.Long)
                    .Add(EqualityOp_float_float, TypeID.Float, TypeID.Float)
                    .Add(EqualityOp_double_double, TypeID.Double, TypeID.Double)
                    .Add(EqualityOp_decimal_decimal, TypeID.Decimal, TypeID.Decimal)
                    .Add(EqualityOp_char_char, TypeID.Char, TypeID.Char)
                    .Add(EqualityOp_date_date, TypeID.Date, TypeID.Date)
                    .Add(EqualityOp_string_string, TypeID.String, TypeID.String)
                    .Add(EqualityOp_bool_bool, TypeID.Bool, TypeID.Bool)



                    .Add(EqualityOp_object_object, TypeID.Object, TypeID.Array).Strict(0)
                    .Add(EqualityOp_object_object, TypeID.Array, TypeID.Object).Strict(1)

                    .Add(EqualityOp_object_object, TypeID.Object, TypeID.Object).Strict()

                    .Add(EqualityOp_custom_custom, TypeID.Custom, TypeID.Custom).Strict()
                    .Add(EqualityOp_custom_custom, TypeID.Custom, TypeID.Object).Strict()
                    .Add(EqualityOp_custom_custom, TypeID.Object, TypeID.Custom).Strict()
                    );
            AddFunc("op_Inequality", new BF()
                    .Add(InequalityOp_int_int, TypeID.Int, TypeID.Int)
                    .Add(InequalityOp_byte_byte, TypeID.Byte, TypeID.Byte)
                    .Add(InequalityOp_short_short, TypeID.Short, TypeID.Short)
                    .Add(InequalityOp_long_long, TypeID.Long, TypeID.Long)
                    .Add(InequalityOp_float_float, TypeID.Float, TypeID.Float)
                    .Add(InequalityOp_double_double, TypeID.Double, TypeID.Double)
                    .Add(InequalityOp_decimal_decimal, TypeID.Decimal, TypeID.Decimal)
                    .Add(InequalityOp_char_char, TypeID.Char, TypeID.Char)
                    .Add(InequalityOp_date_date, TypeID.Date, TypeID.Date)
                    .Add(InequalityOp_string_string, TypeID.String, TypeID.String)
                    .Add(InequalityOp_bool_bool, TypeID.Bool, TypeID.Bool)



                    .Add(InequalityOp_object_object, TypeID.Object, TypeID.Array).Strict(0)
                    .Add(InequalityOp_object_object, TypeID.Array, TypeID.Object).Strict(1)

                    .Add(InequalityOp_object_object, TypeID.Object, TypeID.Object).Strict()

                    .Add(InequalityOp_custom_custom, TypeID.Custom, TypeID.Custom).Strict()
                    .Add(InequalityOp_custom_custom, TypeID.Custom, TypeID.Object).Strict()
                    .Add(InequalityOp_custom_custom, TypeID.Object, TypeID.Custom).Strict()
                    );
            AddFunc("op_LogicalNot", new BF()
                    .Add(LogicalNotOp_int, TypeID.Empty, TypeID.Int)
                    .Add(LogicalNotOp_byte, TypeID.Empty, TypeID.Byte)
                    .Add(LogicalNotOp_short, TypeID.Empty, TypeID.Short)
                    .Add(LogicalNotOp_long, TypeID.Empty, TypeID.Long)
                    .Add(LogicalNotOp_float, TypeID.Empty, TypeID.Float)
                    .Add(LogicalNotOp_double, TypeID.Empty, TypeID.Double)
                    .Add(LogicalNotOp_decimal, TypeID.Empty, TypeID.Decimal)
                    .Add(LogicalNotOp_char, TypeID.Empty, TypeID.Char)
                    .Add(LogicalNotOp_date, TypeID.Empty, TypeID.Date)
                    .Add(LogicalNotOp_string, TypeID.Empty, TypeID.String)
                    .Add(LogicalNotOp_bool, TypeID.Empty, TypeID.Bool)

            );
            AddFunc("op_GreaterThan", new BF()
                    .Add(GreaterThanOp_int_int, TypeID.Int, TypeID.Int)
                    .Add(GreaterThanOp_byte_byte, TypeID.Byte, TypeID.Byte)
                    .Add(GreaterThanOp_short_short, TypeID.Short, TypeID.Short)
                    .Add(GreaterThanOp_long_long, TypeID.Long, TypeID.Long)
                    .Add(GreaterThanOp_float_float, TypeID.Float, TypeID.Float)
                    .Add(GreaterThanOp_double_double, TypeID.Double, TypeID.Double)
                    .Add(GreaterThanOp_decimal_decimal, TypeID.Decimal, TypeID.Decimal)
                    .Add(GreaterThanOp_char_char, TypeID.Char, TypeID.Char)
                    .Add(GreaterThanOp_date_date, TypeID.Date, TypeID.Date)
                    .Add(GreaterThanOp_string_string, TypeID.String, TypeID.String)
                    .Add(GreaterThanOp_bool_bool, TypeID.Bool, TypeID.Bool)



            );
            AddFunc("op_GreaterThanOrEqual", new BF()
                    .Add(GreaterThanOrEqualOp_int_int, TypeID.Int, TypeID.Int)
                    .Add(GreaterThanOrEqualOp_byte_byte, TypeID.Byte, TypeID.Byte)
                    .Add(GreaterThanOrEqualOp_short_short, TypeID.Short, TypeID.Short)
                    .Add(GreaterThanOrEqualOp_long_long, TypeID.Long, TypeID.Long)
                    .Add(GreaterThanOrEqualOp_float_float, TypeID.Float, TypeID.Float)
                    .Add(GreaterThanOrEqualOp_double_double, TypeID.Double, TypeID.Double)
                    .Add(GreaterThanOrEqualOp_decimal_decimal, TypeID.Decimal, TypeID.Decimal)
                    .Add(GreaterThanOrEqualOp_char_char, TypeID.Char, TypeID.Char)
                    .Add(GreaterThanOrEqualOp_date_date, TypeID.Date, TypeID.Date)
                    .Add(GreaterThanOrEqualOp_string_string, TypeID.String, TypeID.String)
                    .Add(GreaterThanOrEqualOp_bool_bool, TypeID.Bool, TypeID.Bool)



            );
            AddFunc("op_LessThan", new BF()
                    .Add(LessThanOp_int_int, TypeID.Int, TypeID.Int)
                    .Add(LessThanOp_byte_byte, TypeID.Byte, TypeID.Byte)
                    .Add(LessThanOp_short_short, TypeID.Short, TypeID.Short)
                    .Add(LessThanOp_long_long, TypeID.Long, TypeID.Long)
                    .Add(LessThanOp_float_float, TypeID.Float, TypeID.Float)
                    .Add(LessThanOp_double_double, TypeID.Double, TypeID.Double)
                    .Add(LessThanOp_decimal_decimal, TypeID.Decimal, TypeID.Decimal)
                    .Add(LessThanOp_char_char, TypeID.Char, TypeID.Char)
                    .Add(LessThanOp_date_date, TypeID.Date, TypeID.Date)
                    .Add(LessThanOp_string_string, TypeID.String, TypeID.String)
                    .Add(LessThanOp_bool_bool, TypeID.Bool, TypeID.Bool)



            );
            AddFunc("op_LessThanOrEqual", new BF()
                    .Add(LessThanOrEqualOp_int_int, TypeID.Int, TypeID.Int)
                    .Add(LessThanOrEqualOp_byte_byte, TypeID.Byte, TypeID.Byte)
                    .Add(LessThanOrEqualOp_short_short, TypeID.Short, TypeID.Short)
                    .Add(LessThanOrEqualOp_long_long, TypeID.Long, TypeID.Long)
                    .Add(LessThanOrEqualOp_float_float, TypeID.Float, TypeID.Float)
                    .Add(LessThanOrEqualOp_double_double, TypeID.Double, TypeID.Double)
                    .Add(LessThanOrEqualOp_decimal_decimal, TypeID.Decimal, TypeID.Decimal)
                    .Add(LessThanOrEqualOp_char_char, TypeID.Char, TypeID.Char)
                    .Add(LessThanOrEqualOp_date_date, TypeID.Date, TypeID.Date)
                    .Add(LessThanOrEqualOp_string_string, TypeID.String, TypeID.String)
                    .Add(LessThanOrEqualOp_bool_bool, TypeID.Bool, TypeID.Bool)



            );
            AddFunc("op_LogicalAnd", new BF()
                    .Add(LogicalAndOp_int_int, TypeID.Int, TypeID.Int)
                    .Add(LogicalAndOp_byte_byte, TypeID.Byte, TypeID.Byte)
                    .Add(LogicalAndOp_short_short, TypeID.Short, TypeID.Short)
                    .Add(LogicalAndOp_long_long, TypeID.Long, TypeID.Long)
                    .Add(LogicalAndOp_float_float, TypeID.Float, TypeID.Float)
                    .Add(LogicalAndOp_double_double, TypeID.Double, TypeID.Double)
                    .Add(LogicalAndOp_decimal_decimal, TypeID.Decimal, TypeID.Decimal)
                    .Add(LogicalAndOp_char_char, TypeID.Char, TypeID.Char)
                    .Add(LogicalAndOp_date_date, TypeID.Date, TypeID.Date)
                    .Add(LogicalAndOp_string_string, TypeID.String, TypeID.String)
                    .Add(LogicalAndOp_bool_bool, TypeID.Bool, TypeID.Bool)
            );
            AddFunc("op_LogicalOr", new BF()
                    .Add(LogicalOrOp_int_int, TypeID.Int, TypeID.Int)
                    .Add(LogicalOrOp_byte_byte, TypeID.Byte, TypeID.Byte)
                    .Add(LogicalOrOp_short_short, TypeID.Short, TypeID.Short)
                    .Add(LogicalOrOp_long_long, TypeID.Long, TypeID.Long)
                    .Add(LogicalOrOp_float_float, TypeID.Float, TypeID.Float)
                    .Add(LogicalOrOp_double_double, TypeID.Double, TypeID.Double)
                    .Add(LogicalOrOp_decimal_decimal, TypeID.Decimal, TypeID.Decimal)
                    .Add(LogicalOrOp_char_char, TypeID.Char, TypeID.Char)
                    .Add(LogicalOrOp_date_date, TypeID.Date, TypeID.Date)
                    .Add(LogicalOrOp_string_string, TypeID.String, TypeID.String)
                    .Add(LogicalOrOp_bool_bool, TypeID.Bool, TypeID.Bool)
            );
            AddFunc("op_IntCasting", new BF().Add(IntCastingOp_object, TypeID.Object).Strict(0)
                                            .Add(IntCastingOp_char, TypeID.Char).Strict(0)
                                            .Add(IntCastingOp_byte, TypeID.Byte).Strict(0)
                                            .Add(IntCastingOp_short, TypeID.Short).Strict(0)
                                            .Add(IntCastingOp_long, TypeID.Long).Strict(0)
                                            .Add(IntCastingOp_float, TypeID.Float).Strict(0)
                                            .Add(IntCastingOp_double, TypeID.Double).Strict(0)
                                            .Add(IntCastingOp_decimal, TypeID.Decimal).Strict(0)
                                            );
            AddFunc("op_BoolCasting", new BF().Add(BoolCastingOp_object, TypeID.Object).Strict(0));
            AddFunc("op_StringCasting", new BF().Add(StringCastingOp_object, TypeID.Object).Strict(0));
            AddFunc("op_CharCasting", new BF().Add(CharCastingOp_object, TypeID.Object).Strict(0)
                                            .Add(CharCastingOp_decimal, TypeID.Decimal).Strict(0)
                                            .Add(CharCastingOp_byte, TypeID.Byte).Strict(0)
                                            .Add(CharCastingOp_short, TypeID.Short).Strict(0)
                                            .Add(CharCastingOp_int, TypeID.Int).Strict(0)
                                            .Add(CharCastingOp_long, TypeID.Long).Strict(0)
                                            .Add(CharCastingOp_float, TypeID.Float).Strict(0)
                                            .Add(CharCastingOp_double, TypeID.Double).Strict(0)
                                            );
            AddFunc("op_DecimalCasting", new BF().Add(DecimalCastingOp_object, TypeID.Object).Strict(0)
                                            .Add(DecimalCastingOp_char, TypeID.Char).Strict(0)
                                            .Add(DecimalCastingOp_byte, TypeID.Byte).Strict(0)
                                            .Add(DecimalCastingOp_short, TypeID.Short).Strict(0)
                                            .Add(DecimalCastingOp_int, TypeID.Int).Strict(0)
                                            .Add(DecimalCastingOp_long, TypeID.Long).Strict(0)
                                            .Add(DecimalCastingOp_float, TypeID.Float).Strict(0)
                                            .Add(DecimalCastingOp_double, TypeID.Double).Strict(0)
                                            );
            AddFunc("op_LongCasting", new BF().Add(LongCastingOp_object, TypeID.Object).Strict(0)
                                            .Add(LongCastingOp_char, TypeID.Char).Strict(0)
                                            .Add(LongCastingOp_byte, TypeID.Byte).Strict(0)
                                            .Add(LongCastingOp_short, TypeID.Short).Strict(0)
                                            .Add(LongCastingOp_int, TypeID.Int).Strict(0)
                                            .Add(LongCastingOp_float, TypeID.Float).Strict(0)
                                            .Add(LongCastingOp_double, TypeID.Double).Strict(0)
                                            .Add(LongCastingOp_decimal, TypeID.Decimal).Strict(0)
                                            );


            AddFunc("op_ObjectCasting", new BF().Add(BoxingOp_object, TypeID.Object));
            AddFunc("op_DoubleCasting", new BF().Add(DoubleCastingOp_object, TypeID.Object).Strict(0)
                                            .Add(DoubleCastingOp_char, TypeID.Char).Strict(0)
                                            .Add(DoubleCastingOp_byte, TypeID.Byte).Strict(0)
                                            .Add(DoubleCastingOp_short, TypeID.Short).Strict(0)
                                            .Add(DoubleCastingOp_int, TypeID.Int).Strict(0)
                                            .Add(DoubleCastingOp_long, TypeID.Long).Strict(0)
                                            .Add(DoubleCastingOp_float, TypeID.Float).Strict(0)
                                            .Add(DoubleCastingOp_decimal, TypeID.Decimal).Strict(0)
                                            );
            AddFunc("op_FloatCasting", new BF().Add(FloatCastingOp_object, TypeID.Object).Strict(0)
                                            .Add(FloatCastingOp_char, TypeID.Char).Strict(0)
                                            .Add(FloatCastingOp_byte, TypeID.Byte).Strict(0)
                                            .Add(FloatCastingOp_short, TypeID.Short).Strict(0)
                                            .Add(FloatCastingOp_int, TypeID.Int).Strict(0)
                                            .Add(FloatCastingOp_long, TypeID.Long).Strict(0)
                                            .Add(FloatCastingOp_double, TypeID.Double).Strict(0)
                                            .Add(FloatCastingOp_decimal, TypeID.Decimal).Strict(0)
                                            );
            AddFunc("op_ByteCasting", new BF().Add(ByteCastingOp_object, TypeID.Object).Strict(0)
                                            .Add(ByteCastingOp_char, TypeID.Char).Strict(0)
                                            .Add(ByteCastingOp_short, TypeID.Short).Strict(0)
                                            .Add(ByteCastingOp_int, TypeID.Int).Strict(0)
                                            .Add(ByteCastingOp_long, TypeID.Long).Strict(0)
                                            .Add(ByteCastingOp_float, TypeID.Float).Strict(0)
                                            .Add(ByteCastingOp_double, TypeID.Double).Strict(0)
                                            .Add(ByteCastingOp_decimal, TypeID.Decimal).Strict(0)
                                            );
            AddFunc("op_ShortCasting", new BF().Add(ShortCastingOp_object, TypeID.Object).Strict(0)
                                            .Add(ShortCastingOp_char, TypeID.Char).Strict(0)
                                            .Add(ShortCastingOp_byte, TypeID.Byte).Strict(0)
                                            .Add(ShortCastingOp_int, TypeID.Int).Strict(0)
                                            .Add(ShortCastingOp_long, TypeID.Long).Strict(0)
                                            .Add(ShortCastingOp_float, TypeID.Float).Strict(0)
                                            .Add(ShortCastingOp_double, TypeID.Double).Strict(0)
                                            .Add(ShortCastingOp_decimal, TypeID.Decimal).Strict(0)
                                            );
            AddFunc("op_DateCasting", new BF().Add(DateCastingOp_object, TypeID.Object).Strict(0));

            AddFunc("op_Casting", new BF().Add(CustomCastingOp_object_object, TypeID.Object, TypeID.Object).Return(0, true));

            AddFunc("op_ByHintCasting", new BF().Add(ByHintCastingOp_object_object, TypeID.Object, TypeID.Object).Strict(0)
                                                .Add(ByHintConvCastingOp_object_object, TypeID.Object, TypeID.Object));
            AddFunc("op_ByHintArrayCasting", new BF().Add(ByHintArrayCastingOp_object_object, TypeID.Object, TypeID.Object));


            AddFunc("op_IntArrayCasting", new BF().Add(IntArrayCastingOp_object, TypeID.Object).Strict(0));
            AddFunc("op_BoolArrayCasting", new BF().Add(BoolArrayCastingOp_object, TypeID.Object).Strict(0));
            AddFunc("op_StringArrayCasting", new BF().Add(StringArrayCastingOp_object, TypeID.Object).Strict(0));
            AddFunc("op_CharArrayCasting", new BF().Add(CharArrayCastingOp_object, TypeID.Object).Strict(0));
            AddFunc("op_DecimalArrayCasting", new BF().Add(DecimalArrayCastingOp_object, TypeID.Object).Strict(0));
            AddFunc("op_LongArrayCasting", new BF().Add(LongArrayCastingOp_object, TypeID.Object).Strict(0));
            AddFunc("op_ObjectArrayCasting", new BF().Add(ObjectArrayCastingOp_object, TypeID.Object).Strict(0));
            AddFunc("op_DoubleArrayCasting", new BF().Add(DoubleArrayCastingOp_object, TypeID.Object).Strict(0));
            AddFunc("op_FloatArrayCasting", new BF().Add(FloatArrayCastingOp_object, TypeID.Object).Strict(0));
            AddFunc("op_ByteArrayCasting", new BF().Add(ByteArrayCastingOp_object, TypeID.Object).Strict(0));
            AddFunc("op_ShortArrayCasting", new BF().Add(ShortArrayCastingOp_object, TypeID.Object).Strict(0));
            AddFunc("op_DateArrayCasting", new BF().Add(DateArrayCastingOp_object, TypeID.Object).Strict(0));


            AddFunc("op_ArrayCasting", new BF().Add(CustomCastingOp_object_object, TypeID.Object, TypeID.Object).Return(0, true));

            AddFunc("op_Increment", new BF()
                    .Add(IncrementOp_byte, TypeID.Byte)
                    .Add(IncrementOp_int, TypeID.Int)
                    .Add(IncrementOp_short, TypeID.Short)
                    .Add(IncrementOp_long, TypeID.Long)
                    .Add(IncrementOp_float, TypeID.Float)
                    .Add(IncrementOp_double, TypeID.Double)
                    .Add(IncrementOp_decimal, TypeID.Decimal)
                    .Add(IncrementOp_char, TypeID.Char)
            );
            AddFunc("op_Decrement", new BF()
                    .Add(DecrementOp_byte, TypeID.Byte)
                    .Add(DecrementOp_int, TypeID.Int)
                    .Add(DecrementOp_short, TypeID.Short)
                    .Add(DecrementOp_long, TypeID.Long)
                    .Add(DecrementOp_float, TypeID.Float)
                    .Add(DecrementOp_double, TypeID.Double)
                    .Add(DecrementOp_decimal, TypeID.Decimal)
                    .Add(DecrementOp_char, TypeID.Char)
            );

            AddFunc("op_Coalescing", new BF()
                .Add(CoalescingOp_string_string, TypeID.String, TypeID.String)
                .Add(CoalescingOp_custom_custom, TypeID.Custom, TypeID.Custom).Return2(0, 1)
                .Add(CoalescingOp_customArray_customArray, TypeID.CustomArray, TypeID.CustomArray).Return2(0, 1)
                .Add(CoalescingOp_intArray_intArray, TypeID.IntArray, TypeID.IntArray)
                .Add(CoalescingOp_longArray_longArray, TypeID.LongArray, TypeID.LongArray)
                .Add(CoalescingOp_floatArray_floatArray, TypeID.FloatArray, TypeID.FloatArray)
                .Add(CoalescingOp_doubleArray_doubleArray, TypeID.DoubleArray, TypeID.DoubleArray)
                .Add(CoalescingOp_decimalArray_decimalArray, TypeID.DecimalArray, TypeID.DecimalArray)
                .Add(CoalescingOp_boolArray_boolArray, TypeID.BoolArray, TypeID.BoolArray)
                .Add(CoalescingOp_stringArray_stringArray, TypeID.StringArray, TypeID.StringArray)
                .Add(CoalescingOp_charArray_charArray, TypeID.CharArray, TypeID.CharArray)
                .Add(CoalescingOp_byteArray_byteArray, TypeID.ByteArray, TypeID.ByteArray)
                .Add(CoalescingOp_shortArray_shortArray, TypeID.ShortArray, TypeID.ShortArray)
                .Add(CoalescingOp_dateArray_dateArray, TypeID.DateArray, TypeID.DateArray)
                .Add(CoalescingOp_objectArray_objectArray, TypeID.ObjectArray, TypeID.ObjectArray)
                .Add(CoalescingOp_object_object, TypeID.Object, TypeID.Object)
                );





        }






        public void Import(PPDirective ppd)
        {
            var opt = new FuncImportOptions(ppd);
            AddFuncsFromLib(opt);
        }

        class FuncImportOptions
        {
            public string Directive = null;
            public string DirectiveFile = null;
            public string File = null;
            public string[] Call = null;

            public string From = null;
            public bool Override = false;
            public string As = null;
            public string[] Funcs = null;
            public FuncImportOptions(PPDirective ppd)
            {
                try
                {

                    Directive = ppd.Code;
                    DirectiveFile = ppd.CodeFile;

                    File = ppd.Path;



                    if (ppd.Data != null && ppd.Data.StartsWith("as "))
                        As = ppd.Data.Substring(3);

                    if (ppd.Params != null)
                    {
                        foreach (var p in ppd.Params)

                        {


                            switch (p.Key)
                            {
                                case "override":
                                    Override = Convert.ToBoolean(p.Value);
                                    break;



                                case "from":
                                    From = p.Value;
                                    break;
                                case "call":
                                    Call = p.Value.Split('|');
                                    break;
                                case "funcs":
                                    Funcs = p.Value.Split('|');
                                    break;
                                default:
                                    throw new ScriptLoadingException($"Unknown parameter '{p.Key}'.");

                            }
                        }


                    }
                }
                catch (Exception ex)
                {
                    throw new ScriptLoadingException($"{ErrorStart()} {ex.Message}");
                }

                string ErrorStart() => $"Invalid import directive '{ppd.Code}' in '{ppd.CodeFile}'.";
            }


        }

        public void AddFunc(string name, BF bf, string funcNamePrefix = null, bool overrideMode = false, string[] importFuncNames = null, string importFrom = null)
        {
            if (importFuncNames != null && !importFuncNames.Contains(name)) return;

            string fnName = funcNamePrefix + name;
            try
            {

                if (!overrideMode)
                    BasicFuncs.Add(fnName, bf);
                else
                    BasicFuncs[fnName] = bf;

            }
            catch (Exception ex) { throw new ScriptLoadingException($"Failed to add basic function '{fnName}' from '{importFrom}'. {ex.Message}"); }
        }

        void AddFuncsFromLib(FuncImportOptions opt)
        {

            if (!File.Exists(opt.File)) throw new ScriptLoadingException($"{ErrorStart()} Could not find file '{opt.File}'.");
            Assembly assemby = Assembly.LoadFrom(opt.File);
            var types = assemby.GetExportedTypes();
            Type type = opt.From == null ? (types.Where(x => x.IsDefined(typeof(ImportAttribute))).FirstOrDefault()) : types.Where(x => x.Name == opt.From).FirstOrDefault();
            if (type == null) throw new ScriptLoadingException($"{ErrorStart()} Could not find import type {(opt.From != null ? $"'{opt.From}' " : "")}in '{opt.File}'.");

            string funcNamePrefix = opt.As == null ? "" : (opt.As.EndsWith('*') ? opt.As.TrimEnd('*') : opt.As + BasicFunctionPrefix);
            bool overrideMode = opt.Override;
            string importFrom = opt.File;
            string[] importFuncNames = opt.Funcs;


            Action<string, BF> addFunc = (name, bf) => AddFunc(name, bf, funcNamePrefix, overrideMode, importFuncNames, importFrom);
            object[] arg = { addFunc };
            if (opt.Call != null)
            {


                foreach (var method in opt.Call)
                {


                    var methodInfo = type.GetMethod(method);
                    if (methodInfo == null) throw new ScriptLoadingException($"{ErrorStart()} Could not find import method '{method}' in '{opt.File}'.");

                    CallImport(methodInfo, arg);
                }
            }
            else
            {
                var methodInfo = type.GetMethods().Where(x => x.IsDefined(typeof(ImportAttribute))).FirstOrDefault();
                if (methodInfo == null) throw new ScriptLoadingException($"{ErrorStart()} Could not find import method'.");
                CallImport(methodInfo, arg);
            }






            string ErrorStart() => $"Invalid import directive '{opt.Directive}' in '{opt.DirectiveFile}'.";
            void CallImport(MethodInfo methodInfo, object[] arg)
            {
                try
                {
                    methodInfo.Invoke(null, arg);
                }
                catch (System.Reflection.TargetInvocationException ex)
                {


                    throw new ScriptLoadingException($"{ErrorStart()} " + ex.InnerException.Message);
                }
                catch (Exception ex)
                {
                    throw new ScriptLoadingException($"{ErrorStart()} Method '{methodInfo.Name}' call error. " + ex.Message);
                }
            }
        }
    }


}

