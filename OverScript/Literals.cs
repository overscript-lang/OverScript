using System;
using System.Collections.Generic;
using System.Linq;
using static OverScript.ScriptClass;

namespace OverScript
{
    public static class Literals
    {

        static int litCount = 0;

        internal static int SetLiteral<T>(T lit)
        {

            int n = Lit<T>.Find(lit);

            if (n >= 0) return n;
            n = Lit<T>.Add(++litCount, lit);
            LiteralTypes[n] = GetTypeID(typeof(T));

            return n;
        }
        internal static int SetLiteral(object lit)
        {
            switch (lit)
            {
                case string v: return SetLiteral<string>(v);
                case int v: return SetLiteral<int>(v);
                case long v: return SetLiteral<long>(v);
                case float v: return SetLiteral<float>(v);
                case double v: return SetLiteral<double>(v);
                case decimal v: return SetLiteral<decimal>(v);
                case char v: return SetLiteral<char>(v);

                default: return SetLiteral<object>(lit);
            }
        }

        internal static void TrimLit()
        {
            Lit<string>.TrimExcess();
            Lit<int>.TrimExcess();
            Lit<long>.TrimExcess();
            Lit<float>.TrimExcess();
            Lit<double>.TrimExcess();
            Lit<decimal>.TrimExcess();
            Lit<char>.TrimExcess();
            Lit<object>.TrimExcess();

        }
        public static void Reset()
        {
            Lit<string>.Reset();
            Lit<int>.Reset();
            Lit<long>.Reset();
            Lit<float>.Reset();
            Lit<double>.Reset();
            Lit<decimal>.Reset();
            Lit<char>.Reset();
            Lit<object>.Reset();

        }

        internal const char LiteralMark = (char)2;
        internal static Dictionary<int, TypeID> LiteralTypes = new Dictionary<int, TypeID>();
        internal static Dictionary<Type, int> LitTypeID = new Dictionary<Type, int>(){
                {typeof(string),1 },{typeof(int),2 } ,{typeof(long),3},
                {typeof(float),4 },{typeof(double),5 },{typeof(decimal),6},
                {typeof(char),7},{typeof(object),8}
            };


        static class Lit<T>
        {
            const int StartCapacity = 16;
            public static T[] Literals = new T[StartCapacity];
            static int Count = 0;
            public static void Reset()
            {
                Literals = new T[StartCapacity];
                Count = 0;
            }
            public static int Find(T v)
            {
                int n = Array.IndexOf(Literals, v, 0, Count);
                if (n < 0) return -1;
                return GetID(typeof(T), n);
            }

            public static int Add(int id, T value)
            {
                Count++;
                if (Count > Literals.Length) Array.Resize(ref Literals, Count * 2);
                int n = Count - 1;
                Literals[n] = value;

                return GetID(typeof(T), n);
            }
            static int GetID(Type t, int n) => int.Parse(LitTypeID[t].ToString() + n.ToString());
            public static void TrimExcess()
            {
                Array.Resize(ref Literals, Count);
            }

        }

        internal static object GetLitByStr(string id)
        {
            id = id.Trim(LiteralMark);
            TypeID typeId = GetLitTypeIdByStr(id, true);
            int litIndex = int.Parse(id.Substring(1));
            return GetLiteral(litIndex, typeId);
        }
        internal static TypeID GetLitTypeIdByStr(string id, bool skipTrim = false)
        {
            if (!skipTrim) id = id.TrimStart(LiteralMark);
            int typeNum = int.Parse(id.Substring(0, 1));
            var type = LitTypeID.Where(x => x.Value == typeNum).Select(x => x.Key).FirstOrDefault();
            TypeID typeId = GetTypeID(type);
            return typeId;
        }

        internal static object GetLiteral(int id, TypeID type)
        {
            try
            {
                switch (type)
                {
                    case TypeID.Int: return Lit<int>.Literals[id];
                    case TypeID.Double: return Lit<double>.Literals[id];
                    case TypeID.String: return Lit<string>.Literals[id];
                    case TypeID.Float: return Lit<float>.Literals[id];
                    case TypeID.Decimal: return Lit<decimal>.Literals[id];
                    case TypeID.Long: return Lit<long>.Literals[id];
                    case TypeID.Char: return Lit<char>.Literals[id];
                    case TypeID.Object: return Lit<object>.Literals[id];
                    default: throw new ScriptExecutionException($"Literal of type '{type}' not found.");
                }
            }
            catch (IndexOutOfRangeException)
            {
                throw new ScriptExecutionException($"Failed to get literal.");
            }
        }
        internal static T GetLiteral<T>(int id)
        {
            try
            {
                return Lit<T>.Literals[id];
            }
            catch (IndexOutOfRangeException)
            {
                throw new ScriptExecutionException($"Failed to get literal.");
            }
        }

    }
}
