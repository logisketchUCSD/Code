using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;

namespace Data
{
	[Serializable]
    public class TreeSet<T>:Set<T>
    {
        SortedDictionary<T, bool> set;
        IComparer<T> comp;

        public TreeSet()
        {
            set = new SortedDictionary<T, bool>();
        }

        public TreeSet(Set<T> copy) :
            this()
        {
            if (copy.GetType() == this.GetType())
            {
                if (((TreeSet<T>)copy).comp != null)
                {
                    set = new SortedDictionary<T, bool>(((TreeSet<T>)copy).comp);
                }
            }
            foreach (T item in copy)
                Add(item);
        }

        public TreeSet(IComparer<T> comp)
        {
            set = new SortedDictionary<T, bool>(comp);
            this.comp = comp;
        }

        public TreeSet(Set<T> copy, IComparer<T> comp) :
            this(comp)
        {
            this.comp = comp;
            foreach (T item in copy)
                Add(item);
        }

        public override object Clone()
        {
            return new TreeSet<T>(this);
        }

        public override void Add(T item)
        {
            if (!set.ContainsKey(item)) set.Add(item, true);
        }

        public override void Remove(T item)
        {
            if (!set.ContainsKey(item)) set.Remove(item);
        }

        public override bool Contains(T item)
        {
            return set.ContainsKey(item);
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
                if (!other.Contains(item)) toRemove.Add(item);
            foreach (T item in toRemove)
                Remove(item);
        }

        public override IEnumerator<T> GetEnumerator()
        {
            foreach (T item in set.Keys)
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
            return new List<T>(set.Keys);
        }
    }
}
