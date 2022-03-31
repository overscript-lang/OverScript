using System;
using static OverScript.ScriptClass;

namespace OverScript
{

    public class Var
    {
        public string Name;
        public object Value;
        public Var(string name, object value)
        {
            Name = name;
            Value = value;
        }
    }

    public struct VarKey
    {
        public int id;
        public int scope;

        public VarKey(int id, int scope)
        {
            this.id = id;
            this.scope = scope;
        }
    }
    public class VarName
    {
        public string Name;
        public int Id;
        public VarType Type;
        public bool IsPublic;
        public VarName(Script script, string name, VarType type, bool pub = false)
        {
            Name = name;
            Type = type;
            Id = script.GetVarID(name, type.ID);
            IsPublic = pub;
        }

    }


    public partial class Executor
    {


        internal void SetVarStorage()
        {
            ScriptVars<bool>.BuildVarStorage(this);
            ScriptVars<byte>.BuildVarStorage(this);
            ScriptVars<short>.BuildVarStorage(this);
            ScriptVars<int>.BuildVarStorage(this);
            ScriptVars<long>.BuildVarStorage(this);
            ScriptVars<float>.BuildVarStorage(this);
            ScriptVars<double>.BuildVarStorage(this);
            ScriptVars<decimal>.BuildVarStorage(this);
            ScriptVars<string>.BuildVarStorage(this);
            ScriptVars<char>.BuildVarStorage(this);
            ScriptVars<object>.BuildVarStorage(this);
            ScriptVars<DateTime>.BuildVarStorage(this);

            ScriptVars<bool[]>.BuildVarStorage(this);
            ScriptVars<byte[]>.BuildVarStorage(this);
            ScriptVars<short[]>.BuildVarStorage(this);
            ScriptVars<int[]>.BuildVarStorage(this);
            ScriptVars<long[]>.BuildVarStorage(this);
            ScriptVars<float[]>.BuildVarStorage(this);
            ScriptVars<double[]>.BuildVarStorage(this);
            ScriptVars<decimal[]>.BuildVarStorage(this);
            ScriptVars<string[]>.BuildVarStorage(this);
            ScriptVars<char[]>.BuildVarStorage(this);
            ScriptVars<object[]>.BuildVarStorage(this);
            ScriptVars<DateTime[]>.BuildVarStorage(this);
            ScriptVars<CustomObject>.BuildVarStorage(this);

        }
        internal void DropVarStorage()
        {
            ScriptVars<bool>.ResetVarStorage(this);
            ScriptVars<byte>.ResetVarStorage(this);
            ScriptVars<short>.ResetVarStorage(this);
            ScriptVars<int>.ResetVarStorage(this);
            ScriptVars<long>.ResetVarStorage(this);
            ScriptVars<float>.ResetVarStorage(this);
            ScriptVars<double>.ResetVarStorage(this);
            ScriptVars<decimal>.ResetVarStorage(this);
            ScriptVars<string>.ResetVarStorage(this);
            ScriptVars<char>.ResetVarStorage(this);
            ScriptVars<object>.ResetVarStorage(this);
            ScriptVars<DateTime>.ResetVarStorage(this);

            ScriptVars<bool[]>.ResetVarStorage(this);
            ScriptVars<byte[]>.ResetVarStorage(this);
            ScriptVars<short[]>.ResetVarStorage(this);
            ScriptVars<int[]>.ResetVarStorage(this);
            ScriptVars<long[]>.ResetVarStorage(this);
            ScriptVars<float[]>.ResetVarStorage(this);
            ScriptVars<double[]>.ResetVarStorage(this);
            ScriptVars<decimal[]>.ResetVarStorage(this);
            ScriptVars<string[]>.ResetVarStorage(this);
            ScriptVars<char[]>.ResetVarStorage(this);
            ScriptVars<object[]>.ResetVarStorage(this);
            ScriptVars<DateTime[]>.ResetVarStorage(this);
            ScriptVars<CustomObject>.ResetVarStorage(this);

        }
        internal void ClearVarsInScope(int scope)
        {
            ScriptVars<bool>.ClearScope(ExecID, scope);
            ScriptVars<byte>.ClearScope(ExecID, scope);
            ScriptVars<short>.ClearScope(ExecID, scope);
            ScriptVars<int>.ClearScope(ExecID, scope);
            ScriptVars<long>.ClearScope(ExecID, scope);
            ScriptVars<float>.ClearScope(ExecID, scope);
            ScriptVars<double>.ClearScope(ExecID, scope);
            ScriptVars<decimal>.ClearScope(ExecID, scope);
            ScriptVars<string>.ClearScope(ExecID, scope);
            ScriptVars<char>.ClearScope(ExecID, scope);
            ScriptVars<object>.ClearScope(ExecID, scope);
            ScriptVars<DateTime>.ClearScope(ExecID, scope);

            ScriptVars<bool[]>.ClearScope(ExecID, scope);
            ScriptVars<byte[]>.ClearScope(ExecID, scope);
            ScriptVars<short[]>.ClearScope(ExecID, scope);
            ScriptVars<int[]>.ClearScope(ExecID, scope);
            ScriptVars<long[]>.ClearScope(ExecID, scope);
            ScriptVars<float[]>.ClearScope(ExecID, scope);
            ScriptVars<double[]>.ClearScope(ExecID, scope);
            ScriptVars<decimal[]>.ClearScope(ExecID, scope);
            ScriptVars<string[]>.ClearScope(ExecID, scope);
            ScriptVars<char[]>.ClearScope(ExecID, scope);
            ScriptVars<object[]>.ClearScope(ExecID, scope);
            ScriptVars<DateTime[]>.ClearScope(ExecID, scope);
            ScriptVars<CustomObject>.ClearScope(ExecID, scope);

        }


        internal int NewScope()
        {
            lock (FreeScopesLocker)
            {

                return FreeScopes.Count > 0 ? FreeScopes.Pop() : ++LastScope;
            }
        }




        internal static object GetVarValue(Executor exec, TypeID type, int id, int scope)
        {
            switch (type)
            {
                case TypeID.IntArray: return ScriptVars<int[]>.Get(exec, id, scope);
                case TypeID.ObjectArray: return ScriptVars<object[]>.Get(exec, id, scope);
                case TypeID.LongArray: return ScriptVars<long[]>.Get(exec, id, scope);
                case TypeID.FloatArray: return ScriptVars<float[]>.Get(exec, id, scope);
                case TypeID.DoubleArray: return ScriptVars<double[]>.Get(exec, id, scope);
                case TypeID.DecimalArray: return ScriptVars<decimal[]>.Get(exec, id, scope);
                case TypeID.StringArray: return ScriptVars<string[]>.Get(exec, id, scope);
                case TypeID.CharArray: return ScriptVars<char[]>.Get(exec, id, scope);
                case TypeID.BoolArray: return ScriptVars<bool[]>.Get(exec, id, scope);
                case TypeID.ShortArray: return ScriptVars<short[]>.Get(exec, id, scope);
                case TypeID.ByteArray: return ScriptVars<byte[]>.Get(exec, id, scope);
                case TypeID.DateArray: return ScriptVars<DateTime[]>.Get(exec, id, scope);

                case TypeID.Int: return ScriptVars<int>.Get(exec, id, scope);
                case TypeID.String: return ScriptVars<string>.Get(exec, id, scope);
                case TypeID.Char: return ScriptVars<char>.Get(exec, id, scope);
                case TypeID.Double: return ScriptVars<double>.Get(exec, id, scope);
                case TypeID.Float: return ScriptVars<float>.Get(exec, id, scope);
                case TypeID.Long: return ScriptVars<long>.Get(exec, id, scope);
                case TypeID.Decimal: return ScriptVars<decimal>.Get(exec, id, scope);
                case TypeID.Bool: return ScriptVars<bool>.Get(exec, id, scope);
                case TypeID.Object: return ScriptVars<object>.Get(exec, id, scope);
                case TypeID.Short: return ScriptVars<short>.Get(exec, id, scope);
                case TypeID.Byte: return ScriptVars<byte>.Get(exec, id, scope);
                case TypeID.Date: return ScriptVars<DateTime>.Get(exec, id, scope);
                case TypeID.CustomArray:
                case TypeID.Custom: return ScriptVars<CustomObject>.Get(exec, id, scope);
                default: throw new ScriptExecutionException($"Type '{type}' not supported.");
            }

        }

        internal static class ScriptVars<T>
        {
            public static T[][][] VarStorage = new T[ExecPool.Capacity][][];
            static object[][] VarStorageResizeLocker = new object[ExecPool.Capacity][];
            public static void BuildVarStorage(Executor exec)
            {
                int execId = exec.ExecID;
                int c = exec.ExecutedScript.VarNameCount<T>();
                VarStorage[execId] = new T[c][];
                VarStorageResizeLocker[execId] = new object[c];

                for (int i = 0; i < VarStorage[execId].Length; i++)
                {
                    Array.Resize(ref VarStorage[execId][i], 16);
                    VarStorageResizeLocker[execId][i] = new object();
                }

            }
            public static void ResetVarStorage(Executor exec)
            {
                VarStorage[exec.ExecID] = null;
            }
            public static T SetAndReturnPrev(Executor exec, int id, int scope, ref T value)
            {
                try
                {
                    int execId = exec.ExecID;
                    T prev = VarStorage[execId][id][scope];
                    VarStorage[execId][id][scope] = value;
                    return prev;
                }
                catch (NullReferenceException)
                {
                    throw new ScriptExecutionException(exec.Disposed ? "Executor is disposed." : $"Failed to set variable '{exec.ExecutedScript.VarIdToName<T>(id)}' due to corruption of variable store.");
                }
                catch (IndexOutOfRangeException)
                {
                    throw new ScriptExecutionException(exec.Disposed ? "Executor is disposed." : $"Variable '{exec.ExecutedScript.VarIdToName<T>(id)}' not found.");
                }
            }
            public static void Set(Executor exec, int id, int scope, ref T value)
            {
                try
                {
                    int execId = exec.ExecID;
                    lock (VarStorageResizeLocker[execId][id])
                    {
                        VarStorage[execId][id][scope] = value;
                    }

                }
                catch (NullReferenceException)
                {
                    throw new ScriptExecutionException(exec.Disposed ? "Executor is disposed." : $"Failed to set variable '{exec.ExecutedScript.VarIdToName<T>(id)}' due to corruption of variable store.");
                }
                catch (IndexOutOfRangeException)
                {
                    throw new ScriptExecutionException(exec.Disposed ? "Executor is disposed." : $"Variable '{exec.ExecutedScript.VarIdToName<T>(id)}' not found.");
                }
            }





            public static void AddSet(Executor exec, int id, int scope, ref T value)
            {

                if (scope >= VarStorage[exec.ExecID][id].Length) Add(exec, id, scope, value);
                else Set(exec, id, scope, ref value);

            }
            public static void Add(Executor exec, int id, int scope, T defVal = default)
            {
                int execId = exec.ExecID;
                var vs = VarStorage[execId];
                var v = vs[id];
                if (scope >= v.Length)
                {
                    lock (VarStorageResizeLocker[execId][id])
                    {
                        if (scope >= v.Length)
                        {

                            Array.Resize(ref vs[id], scope * 2);

                        }
                    }
                    v = vs[id];
                }

                ref T var = ref v[scope];
                var = defVal;

            }
            public static T Get(Executor exec, int id, int scope)
            {
                try
                {


                    var v = VarStorage[exec.ExecID][id][scope];

                    return v;

                }
                catch (NullReferenceException)
                {
                    throw new ScriptExecutionException(exec.Disposed ? "Executor is disposed." : $"Failed to get variable '{exec.ExecutedScript.VarIdToName<T>(id)}' due to corruption of variable store.");
                }
                catch (IndexOutOfRangeException)
                {
                    throw new ScriptExecutionException(exec.Disposed ? "Executor is disposed." : $"Variable '{exec.ExecutedScript.VarIdToName<T>(id)}' not found.");
                }

            }
  

            public static void RemVarsByIds(int execId, int[] ids, int scope)
            {
                try
                {

                    var vs = VarStorage[execId];
                    for (int j = 0; j < ids.Length; j++)
                    {
                        int i = ids[j];

                        if (vs[i].Length <= scope) continue;
                        vs[i][scope] = default(T);

                    }
                }
                catch (Exception ex)
                {
                    if (ex is IndexOutOfRangeException || ex is NullReferenceException)
                        throw new ScriptExecutionException("Failed to clear variables due to corruption of variable store.");

                    throw;
                }

            }
            public static void ClearScope(int execId, int scope)
            {
                var vs = VarStorage[execId];
                for (int i = 0; i < vs.Length; i++)
                {
                    if (vs[i].Length <= scope) continue;
                    vs[i][scope] = default(T);

                }

            }


        }

       

        public int DisposeStored(bool allowExecuting = false)
        {
            int c = 0;
            bool canceled = Canceled, forciblyCanceled = ForciblyCanceled;
            if (allowExecuting) Canceled = ForciblyCanceled = false;
            c += DisposeAll<CustomObject>();
            c += DisposeAll<object>();
            c += DisposeAll<object[]>();
            if (allowExecuting) { Canceled = canceled; ForciblyCanceled = forciblyCanceled; }

            return c;
        }
        internal int DisposeAll<T>()
        {
            int c = 0;
            var vs = ScriptVars<T>.VarStorage[ExecID];
            foreach (var vars in vs)
            {
                foreach (var obj in vars)
                {
                    if (obj is IDisposable d)
                    {
                        try { d.Dispose(); c++; } catch { }
                        
                    }
                    else if (obj is Array arr) c += DisposeArray(arr);

                }

            }
            return c;
        }
        internal int DisposeArray(Array arr)
        {
            int c = 0;
            for (int i = 0; i < arr.Length; i++)
            {
                object v = arr.GetValue(i);
                if (v is IDisposable d)
                {
                    try { d.Dispose(); c++; } catch { }
                    
                }
                else if (v is Array) c += DisposeArray(arr);

            }
            return c;
        }
        internal void RemVars(int scope, VarsToClear forCleaning)
        {
            if (ForciblyCanceled) return;




            int flagCount = forCleaning.UsedTypeCount;

            if (forCleaning.StringUsed) { ScriptVars<string>.RemVarsByIds(ExecID, forCleaning.VarIDs[(int)TypeID.String], scope); if (--flagCount == 0) goto remEnd; }
            if (forCleaning.ObjectUsed) { ScriptVars<object>.RemVarsByIds(ExecID, forCleaning.VarIDs[(int)TypeID.Object], scope); if (--flagCount == 0) goto remEnd; }

            if (forCleaning.CustomUsed) { ScriptVars<CustomObject>.RemVarsByIds(ExecID, forCleaning.VarIDs[(int)TypeID.Custom], scope); if (--flagCount == 0) goto remEnd; }

            if (forCleaning.IntArrayUsed) { ScriptVars<int[]>.RemVarsByIds(ExecID, forCleaning.VarIDs[(int)TypeID.IntArray], scope); if (--flagCount == 0) goto remEnd; }
            if (forCleaning.StringArrayUsed) { ScriptVars<string[]>.RemVarsByIds(ExecID, forCleaning.VarIDs[(int)TypeID.StringArray], scope); if (--flagCount == 0) goto remEnd; }
            if (forCleaning.ByteArrayUsed) { ScriptVars<byte[]>.RemVarsByIds(ExecID, forCleaning.VarIDs[(int)TypeID.ByteArray], scope); if (--flagCount == 0) goto remEnd; }
            if (forCleaning.ObjectArrayUsed) { ScriptVars<object[]>.RemVarsByIds(ExecID, forCleaning.VarIDs[(int)TypeID.ObjectArray], scope); if (--flagCount == 0) goto remEnd; }

            if (forCleaning.LongArrayUsed) { ScriptVars<long[]>.RemVarsByIds(ExecID, forCleaning.VarIDs[(int)TypeID.LongArray], scope); if (--flagCount == 0) goto remEnd; }
            if (forCleaning.DoubleArrayUsed) { ScriptVars<double[]>.RemVarsByIds(ExecID, forCleaning.VarIDs[(int)TypeID.DoubleArray], scope); if (--flagCount == 0) goto remEnd; }
            if (forCleaning.FloatArrayUsed) { ScriptVars<float[]>.RemVarsByIds(ExecID, forCleaning.VarIDs[(int)TypeID.FloatArray], scope); if (--flagCount == 0) goto remEnd; }
            if (forCleaning.BoolArrayUsed) { ScriptVars<bool[]>.RemVarsByIds(ExecID, forCleaning.VarIDs[(int)TypeID.BoolArray], scope); if (--flagCount == 0) goto remEnd; }
            if (forCleaning.DecimalArrayUsed) { ScriptVars<decimal[]>.RemVarsByIds(ExecID, forCleaning.VarIDs[(int)TypeID.DecimalArray], scope); if (--flagCount == 0) goto remEnd; }
            if (forCleaning.CharArrayUsed) { ScriptVars<char[]>.RemVarsByIds(ExecID, forCleaning.VarIDs[(int)TypeID.CharArray], scope); if (--flagCount == 0) goto remEnd; }
            if (forCleaning.ShortArrayUsed) { ScriptVars<short[]>.RemVarsByIds(ExecID, forCleaning.VarIDs[(int)TypeID.ShortArray], scope); }
            if (forCleaning.DateArrayUsed) { ScriptVars<DateTime[]>.RemVarsByIds(ExecID, forCleaning.VarIDs[(int)TypeID.DateArray], scope); }



        remEnd:
            return;


        }
        internal void FreeScope(int scope)
        {
            lock (FreeScopesLocker) FreeScopes.Push(scope);
        }


    }
}
