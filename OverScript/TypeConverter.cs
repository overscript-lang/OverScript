using System;

namespace OverScript
{
    public partial class ScriptClass
    {

        public static class TypeConverter
        {

            struct CType<T, T2>
            {
                T Value;
                public CType(T v) => Value = v;

                public static explicit operator CType<T, T2>(T x) => new CType<T, T2>(x);

                public static implicit operator T2(CType<T, T2> x)
                {
                    try
                    {
                        switch (x)
                        {
                            case CType<T2, T> y: return y.Value;
                            case CType<T, string> y: return y.Value == null ? (T2)(object)null : (T2)(object)y.Value.ToString();
                            case CType<T, int> y: return (T2)(object)Convert.ToInt32(y.Value);
                            case CType<T, short> y: return (T2)(object)Convert.ToInt16(y.Value);
                            case CType<T, byte> y: return (T2)(object)Convert.ToByte(y.Value);
                            case CType<T, long> y: return (T2)(object)Convert.ToInt64(y.Value);
                            case CType<T, decimal> y: return (T2)(object)Convert.ToDecimal(y.Value);
                            case CType<T, char> y: return (T2)(object)Convert.ToChar(y.Value);
                            case CType<T, bool> y: return (T2)(object)Convert.ToBoolean(y.Value);
                            case CType<T, DateTime> y: return (T2)(object)Convert.ToDateTime(y.Value);
                            case CType<T, float> y: return (T2)(object)Convert.ToSingle(y.Value);
                            case CType<T, double> y: return (T2)(object)Convert.ToDouble(y.Value);

                            case CType<CustomObject, Array> y: return (T2)(object)(Array)y.Value;
                            default:

                                return (T2)(object)x.Value;

                        }
                    }
                    catch (InvalidCastException)
                    {
                        var t = x.Value.GetType();
                        string n;
                        try { n = x.Value is CustomObject co ? co.Type.FullName : GetTypeName(TypeIds[t]); }
                        catch { n = t.FullName; }

                        string toTypeName;
                        TypeID toType;
                        if (TypeIds.TryGetValue(typeof(T2), out toType)) toTypeName = GetTypeName(toType); else toTypeName = typeof(T2).FullName;

                        throw new ScriptExecutionException($"Unable to cast to type '{toTypeName}'. Value is of type '{n}'.");
                    }

                }
            }

 
            public static T2 ConvertValue<T1, T2>(T1 x) => (CType<T1, T2>)x;
            public static bool ConvertAbility(TypeID id, TypeID id2)
            {
                if (id == TypeID.Object || id2 == TypeID.Object || id2 == TypeID.String) return true;

                switch (id2)
                {
                    case TypeID.Int: return ConvertAbility<int>(id);
                    case TypeID.String: return ConvertAbility<string>(id);
                    case TypeID.Char: return ConvertAbility<char>(id);
                    case TypeID.Double: return ConvertAbility<double>(id);
                    case TypeID.Float: return ConvertAbility<float>(id);
                    case TypeID.Long: return ConvertAbility<long>(id);
                    case TypeID.Decimal: return ConvertAbility<decimal>(id);
                    case TypeID.Bool: return ConvertAbility<bool>(id);
                    case TypeID.Short: return ConvertAbility<short>(id);
                    case TypeID.Byte: return ConvertAbility<byte>(id);
                    case TypeID.Date: return ConvertAbility<DateTime>(id);

                    default:
                        return id == id2;

                }

            }
            public static bool ConvertAbility<T>(TypeID id)
            {
                try
                {
                    switch (id)
                    {
                        case TypeID.Int: ConvertValue<int, T>(default(int)); break;
                        case TypeID.String: ConvertValue<string, T>(default(string)); break;
                        case TypeID.Char: ConvertValue<char, T>(default(char)); break;
                        case TypeID.Double: ConvertValue<double, T>(default(double)); break;
                        case TypeID.Float: ConvertValue<float, T>(default(float)); break;
                        case TypeID.Long: ConvertValue<long, T>(default(long)); break;
                        case TypeID.Decimal: ConvertValue<decimal, T>(default(decimal)); break;
                        case TypeID.Bool: ConvertValue<bool, T>(default(bool)); break;
                        case TypeID.Short: ConvertValue<short, T>(default(short)); break;
                        case TypeID.Byte: ConvertValue<byte, T>(default(byte)); break;
                        case TypeID.Date: ConvertValue<DateTime, T>(default(DateTime)); break;
                        default:
                            return id == TypeIds[typeof(T)];

                    }
                    return true;
                }
                catch
                {
                    return false;
                }

            }

        }

    }

}
