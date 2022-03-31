using System;
using System.Collections;
using static OverScript.ScriptClass;

namespace OverScript
{
    public class CustomObject : System.Collections.IEnumerable, ICollection, IComparable, IDisposable
    {

        public object Object;
        public CustomType Type;
        public Executor Exec;
        public bool ExecutorIsValid => Exec.IsValid;
        public CustomObject(Executor exec, object obj, ScriptClass type, bool isArray = false)
        {
            Object = obj;
            Type = CustomType.Get(type, isArray);
            Exec = exec;
        }

        public IEnumerator GetEnumerator()
        {
            try
            {
                return Type.IsArray ? ((CustomObject[])Object).GetEnumerator() : (IEnumerator)Type.Class.GetEnumeratorFunc(null, -1, null, (ClassInstance)Object, null, null);
            }
            catch (Exception ex)
            {
                throw new ScriptExecutionException("Failed to get enumerator. " + ex.Message);
            }
        }

        public int Count => ((CustomObject[])Object).Length;

        public bool IsSynchronized => throw new NotImplementedException();

        public object SyncRoot => throw new NotImplementedException();

        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        public static explicit operator ClassInstance(CustomObject obj) => (ClassInstance)obj.Object;

        public static explicit operator CustomObject[](CustomObject obj) => (CustomObject[])obj.Object;
        public static explicit operator Array(CustomObject obj) => (CustomObject[])obj.Object;



        public override string ToString()
        {
            if (Type.IsArray)
            {
                if (Type.Class.ArrayToStringFunc != null)
                {
                    EvalUnit arg = new EvalUnit();
                    arg.Type = new VarType(TypeID.CustomArray, Type);
                    arg.SpecificValue = this;
                    arg.Kind = EvalUnitKind.SpecificValue;

                    EvalUnit[] args = { arg };
                    return Type.Class.ArrayToStringFunc(args, -1, null, Exec.GetStaticInstance(Type.Class), null, null);
                }

                return $"Array of {Type.Name.Replace("[]", "")}";
            }
            if (Type.Class.ToStringFunc != null)
                return Type.Class.ToStringFunc(null, -1, null, (ClassInstance)Object, null, null);

            return $"Instance of {Type.Name}";
        }
        public override int GetHashCode()
        {
            if (Type.Class.GetHashCodeFunc != null)
                return Type.Class.GetHashCodeFunc(null, -1, null, (ClassInstance)Object, null, null);
            else
                return Object.GetHashCode();
        }
        public void Dispose()
        {
            if (Object is CustomObject[] arr)
            {
                for (int i = 0; i < arr.Length; i++) arr[i]?.Dispose();
            }
            else
            {

                if (Type.Class.DisposeFunc != null && Object != null)
                    Type.Class.DisposeFunc(null, -1, null, (ClassInstance)Object, null, null);

            }
        }

        public override bool Equals(object obj)
        {
            if (Type.Class.EqualsFunc != null)
            {
                EvalUnit arg = new EvalUnit();
                arg.Type = GetVarTypeByID(TypeID.Object);
                arg.SpecificValue = obj;
                arg.Kind = EvalUnitKind.SpecificValue;

                EvalUnit[] args = { arg };
                return Type.Class.EqualsFunc(args, -1, null, (ClassInstance)Object, null, null);
            }
            else if (obj is CustomObject co)
                return Object == co.Object;
            else
                return base.Equals(obj);
        }
        public int CompareTo(object obj)
        {
            if (Type.Class.CompareToFunc != null)
            {
                EvalUnit arg = new EvalUnit();
                arg.Type = GetVarTypeByID(TypeID.Object);
                arg.SpecificValue = obj;
                arg.Kind = EvalUnitKind.SpecificValue;

                EvalUnit[] args = { arg };
                return Type.Class.CompareToFunc(args, -1, null, (ClassInstance)Object, null, null);
            }
            else
                throw new ScriptExecutionException($"'{Type.Class.ClassName}' does not implement CompareTo method.");
        }
        public object Call(string fnName, params object[] args)
        {
            if (Object is ClassInstance inst)
                return DynFuncCall(inst, fnName, args);
            else
                throw new InvalidOperationException("Cannot call a function on this object.");

        }
    }
}
