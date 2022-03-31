

namespace OverScript
{
    public struct NullableObject<T>
    {

        public bool IsNull;
        public T Item;

        private NullableObject(T item, bool isNull) : this()
        {
            IsNull = isNull;
            Item = item;
        }

        public NullableObject(T item) : this(item, item == null)
        {
        }


        public static implicit operator T(NullableObject<T> nobj)
        {
            return nobj.Item;
        }

        public static implicit operator NullableObject<T>(T item)
        {
            return new NullableObject<T>(item);
        }

        public override string ToString() => Item?.ToString();

        public override bool Equals(object obj)
        {
            if (obj == null)
                return IsNull;

            if (!(obj is NullableObject<T> nobj))
                return false;
            else
            {


                if (IsNull)
                    return nobj.IsNull;

                if (nobj.IsNull)
                    return false;

                return Item.Equals(nobj.Item);
            }
        }

        public override int GetHashCode()
        {
            if (IsNull)
                return 0;

            var result = Item.GetHashCode();

            if (result >= 0)
                result++;

            return result;
        }
    }


}
