using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;

namespace Data
{
	[Serializable]
    public class ListSet<T>:Set<T>
    {
        List<T> set;

        public ListSet()
        {
            set = new List<T>();
        }

        public ListSet(Set<T> copy)
            : this()
        {
            foreach (T item in copy)
                Add(item);
        }

        public override object Clone()
        {
            return new ListSet<T>(this);
        }

        public override void Add(T item)
        {
			if (!set.Contains(item))
				set.Add(item);
        }

        public override void Remove(T item)
        {
            set.Remove(item);
        }

        public override bool Contains(T item)
        {
            return set.Contains(item);
        }

        public override void Union(Set<T> other)
        {
            foreach (T item in other)
                Add(item);
        }

        public override void Intersection(Set<T> other)
        {
            List<T> toRemove = new List<T>();

            foreach (T item in this)
            {
                if (!other.Contains(item)) toRemove.Add(item);
            }
            foreach (T item in toRemove)
                Remove(item);

        }

        public override IEnumerator<T> GetEnumerator()
        {
            foreach (T item in set)
                yield return item;
        }

		public override int Count
		{
			get
			{
				return set.Count;
			}
		}

        public override List<T> AsList()
        {
            return new List<T>(set);
        }
    }
}
