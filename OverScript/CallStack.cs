using System;
using System.Collections.Generic;
using System.Linq;
using static OverScript.ScriptClass;

namespace OverScript
{
    public class CallStack
    {



        public List<Item> Items = new List<Item>();
        public void Push(Item item) { Items.Add(item); }
        public Item Peek() => Items.Last();
        public Item Pop() { var item = Items.Last(); Items.RemoveAt(Items.Count - 1); return item; }
        public struct Item
        {
            public ScriptFunction Fn;
            public ScriptClass FnClass;

            public List<RefParam> Refs;
            public RefParam Ref;
            public string BasePath;

            public Exception ThrownException;

            public struct RefParam
            {
                public VarInfo VI;
                public int ParamVarID;
                public RefParam(int paramVarID, VarInfo vi)
                {
                    VI = vi;
                    ParamVarID = paramVarID;

                }

            }

            public Item(ScriptFunction f, ScriptClass c)
            {
                Refs = null;
                Ref = default(RefParam);

                Fn = f;
                FnClass = c;


                BasePath = f.BasePath;
                ThrownException = null;
            }

        }
        public Exception GetThrownException()
        {
            int n = Items.Count - 1;
            return Items[n].ThrownException;
        }
        public bool SetThrownException(Exception ex)
        {
            int n = Items.Count - 1;
            var newItem = Items[n];
            bool upd = newItem.ThrownException != ex;
            if (upd)
            {
                newItem.ThrownException = ex;
                Items[n] = newItem;
            }
            return upd;
        }
        public void SetBasePath(string path)
        {
            int n = Items.Count - 1;
            var newItem = Items[n];
            newItem.BasePath = path;
            Items[n] = newItem;
        }
        public string GetBasePath() => Items[Items.Count - 1].BasePath;

        public void CreateRefs(int c)
        {
            int n = Items.Count - 1;
            var newItem = Items[n];
            newItem.Refs = new System.Collections.Generic.List<Item.RefParam>(c); ;
            Items[n] = newItem;



        }
        public void CreateRef(Item.RefParam rf)
        {
            int n = Items.Count - 1;
            var newItem = Items[n];
            newItem.Ref = rf;
            Items[n] = newItem;



        }
        public void AddRef(Item.RefParam rf)
        {
            int n = Items.Count - 1;
            Items[n].Refs.Add(rf);
        }

    }
}
