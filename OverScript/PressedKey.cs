using System;

namespace OverScript
{
    struct PressedKey : IConvertible
    {
        public ConsoleKeyInfo KeyInfo { set; get; }
        public override string ToString() => KeyInfo.Key.ToString();
        public ConsoleKey Key { get { return KeyInfo.Key; } }
        public char KeyChar { get { return KeyInfo.KeyChar; } }
        public PressedKey(ConsoleKeyInfo k)
        {
            KeyInfo = k;
        }
        string IConvertible.ToString(IFormatProvider provider) => ToString();
        TypeCode IConvertible.GetTypeCode() { throw new NotImplementedException(); }
        bool IConvertible.ToBoolean(IFormatProvider provider) { throw new NotImplementedException(); }
        byte IConvertible.ToByte(IFormatProvider provider) => (Byte)KeyInfo.Key;
        DateTime IConvertible.ToDateTime(IFormatProvider provider) { throw new NotImplementedException(); }
        decimal IConvertible.ToDecimal(IFormatProvider provider) => (Decimal)KeyInfo.Key;
        double IConvertible.ToDouble(IFormatProvider provider) => (Double)KeyInfo.Key;
        short IConvertible.ToInt16(IFormatProvider provider) => (Int16)KeyInfo.Key;
        int IConvertible.ToInt32(IFormatProvider provider) => (Int32)KeyInfo.Key;
        long IConvertible.ToInt64(IFormatProvider provider) => (Int64)KeyInfo.Key;
        sbyte IConvertible.ToSByte(IFormatProvider provider) => (SByte)KeyInfo.Key;
        float IConvertible.ToSingle(IFormatProvider provider) => (Single)KeyInfo.Key;
        object IConvertible.ToType(Type conversionType, IFormatProvider provider) { throw new NotImplementedException(); }
        ushort IConvertible.ToUInt16(IFormatProvider provider) => (UInt16)KeyInfo.Key;
        uint IConvertible.ToUInt32(IFormatProvider provider) => (UInt32)KeyInfo.Key;
        ulong IConvertible.ToUInt64(IFormatProvider provider) => (UInt64)KeyInfo.Key;
        char IConvertible.ToChar(IFormatProvider provider) => KeyInfo.KeyChar;

    }
}
