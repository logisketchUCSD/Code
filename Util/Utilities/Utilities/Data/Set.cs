using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Data
{
	[Serializable]
    public abstract class Set<T> : IEnumerable<T>, ICloneable
    {
        public Set() { }
        public Set(Set<T> copy) { }

        public abstract object Clone(); 

        public abstract void Add(T item);
        public abstract void Remove(T item);
        public abstract bool Contains(T item);
        public abstract void Union(Set<T> other);
        public abstract void Intersection(Set<T> other);

        public static Set<T> Union(Set<T> a, Set<T> b)
        {
            Set<T> clone = (Set<T>)a.Clone();
            clone.Union(b);
            return clone;
        }

        public static Set<T> Intersection(Set<T> a, Set<T> b)
        {
            Set<T> clone = (Set<T>)a.Clone();
            clone.Intersection(b);
            return clone;
        }

        public abstract IEnumerator<T> GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Contains(Set<T> b)
        {
            foreach (T item in b)
                if (!Contains(item)) return false;
            return true;
        }

		public abstract int Count { get; }

        public override bool Equals(object obj)
        {
            Set<T> ob = (Set<T>)obj;
            foreach (T key in ob)
            {
                if (!Contains(key)) return false;
            }
            foreach (T key in this)
            {
                if (!ob.Contains(key)) return false;
            }
            return true;
        }

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

        public abstract List<T> AsList();
    }
}
