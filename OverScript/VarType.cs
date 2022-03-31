using System;
using static OverScript.ScriptClass;

namespace OverScript
{
    public struct VarType
    {

        public string Name { get { return GetName(); } }
        public TypeID ID;
        public readonly CustomType CType;
        public Type T;
        public TypeID SubTypeID;
        public Type TypeHint;
        public Type SubType;
        public bool IsEmpty;
        public bool IsNull;
        public bool IsHintArray;
        public bool IsHintType;
        public bool IsByRef;
        public Type GetAbsType()
        {
            Type type = null;
            if (ID != TypeID.Object)
                type = T;
            else if (SubTypeID != TypeID.None && SubTypeID != TypeID.CustomType)
                type = GetTypeByID(SubTypeIdToTypeId(SubTypeID));
            else if (SubType != null)
                type = SubType;

            else if (TypeHint != null)
                type = TypeHint;
            else
                type = T;

            return type;
        }
        string GetName()
        {
            string name;
            if (ID == TypeID.Object)
            {
                if (SubTypeID != TypeID.None)
                {
                    name = TypeStr.Object + "/type:";
                    if (CType == null)
                        name = name + GetTypeName(SubTypeIdToTypeId(SubTypeID));
                    else
                        name = name + CType.FullName;

                }
                else if (TypeHint != null)
                    name = TypeStr.Object + "/hint:" + TypeHint.ToString();
                else if (IsEmpty)
                    name = TypeStr.Object + "/empty";


                else
                    name = TypeStr.Object;

            }
            else if (ID == TypeID.ObjectArray && TypeHint != null)
            {
                name = TypeStr.ObjectArray + "/hint:" + TypeHint.ToString();
            }
            else
            {
                if (CType == null)
                    name = GetTypeName(ID);
                else
                    name = CType.FullName;

            }
            return IsByRef ? "ref*" + name : name;
        }
        public VarType(TypeID type, CustomType ct = null)
        {

            ID = type;
            T = Types[(int)ID];
            CType = ct;
            SubTypeID = TypeID.None;
            TypeHint = null;
            IsEmpty = IsNull = false;
            SubType = null;
            IsHintArray = IsHintType = false;
            IsByRef = false;
        }
        public VarType SetType(TypeID type)
        {
            ID = type;
            T = Types[(int)ID];
            return this;
        }
        public VarType MakeArrayTypeHint(bool withTypeHint)
        {
            if (SubType != null) SubType = SubType.MakeArrayType();
            if (withTypeHint && TypeHint != null) TypeHint = TypeHint.MakeArrayType();
            IsHintArray = true;
            return this;
        }

        public VarType AddHint(Type type)
        {
            TypeHint = type;
            return this;
        }

        public VarType AddSubType(Type type)
        {
            SubType = type;
            return this;
        }
        public VarType MakeEmpty()
        {
            IsEmpty = true;
            return this;
        }
        public VarType MakeNull()
        {
            IsNull = true;
            return this;
        }
        public VarType SetHintBySubType()
        {

            if (SubType != null || (SubTypeID != TypeID.None && SubTypeID != TypeID.CustomType))
            {
                TypeHint = SubType ?? GetTypeByID(SubTypeIdToTypeId(SubTypeID));
                SubType = null;
                SubTypeID = TypeID.None;
            }
            return this;
        }
        public static bool operator ==(VarType a, VarType b)
        {
            return a.ID == b.ID && a.CType == b.CType && (a.ID != TypeID.Object || a.TypeHint == b.TypeHint);
        }
        public static bool operator !=(VarType a, VarType b)
        {
            return !(a == b);
        }

        public override string ToString()
        {
            return Name;
        }

    }
}
