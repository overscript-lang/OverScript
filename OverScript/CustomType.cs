using System;
using System.Collections.Generic;
using static OverScript.ScriptClass;

namespace OverScript
{
    public class CustomType
    {
        public string Name, FullName;
        public ScriptClass Class;
        public bool IsArray;
        public Script CurScript;
        public CustomType(string name, ScriptClass c)
        {
            FullName = name;
            Class = c;
            Name = name.Substring(name.LastIndexOf('.') + 1);
            IsArray = name.EndsWith("[]");

        }
        public bool IsOfType(object obj)
        {
            if (!(obj is CustomObject co)) return false;
            var ct = this;

            var t = co.Type;
            if (t == ct) return true;
            if (ct.IsArray != co.Type.IsArray) return false;

            return t.Class.Is(ct.Class);
        }
        public override string ToString() => FullName;
        public override bool Equals(object obj)
        {

            if (obj is CustomType) return obj == this;
            return base.Equals(obj);
        }

        static Dictionary<string, CustomType> Types = new Dictionary<string, CustomType>();
        public static CustomType Get(ScriptClass c, bool isArray = false)
        {

            string name = c.ClassFullName;
            if (isArray) name += "[]";
            CustomType t;
            if (Types.TryGetValue(name, out t)) return t;
            t = Types[name] = new CustomType(name, c);
            return t;
        }

        public override int GetHashCode()
        {
            return FullName.GetHashCode();
        }

        public object Call(Executor exec, string fnName, params object[] args)
        {
            if (!IsArray)
                return DynFuncCall(exec.GetStaticInstance(Class), fnName, args);
            else
                throw new InvalidOperationException("Cannot call a function on this type.");

        }

    }
}
