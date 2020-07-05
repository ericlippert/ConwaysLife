using System.Collections;
using System.Collections.Generic;

namespace ConwaysLife
{
    interface IDoubleLink<T> where T : class, IDoubleLink<T>
    {
        T Prev { get; set; }
        T Next { get; set; }
    }

    sealed class DoubleLinkList<T> : IEnumerable<T> where T : class, IDoubleLink<T>
    {
        private T head = null;
        public int Count { get; private set; }

        public void Add(T q)
        {
            q.Prev = null;
            q.Next = head;
            if (head != null)
                head.Prev = q;
            head = q;
            Count += 1;
        }

        public void Remove(T q)
        {
            if (q.Prev == null)
                head = q.Next;
            else
                q.Prev.Next = q.Next;
            if (q.Next != null)
                q.Next.Prev = q.Prev;
            Count -= 1;
        }

        public void Clear()
        {
            head = null;
            Count = 0;
        }

        public IEnumerator<T> GetEnumerator()
        {
            // The item might be removed from the current list during the enumeration,
            // so cache the next link so we always know where to pick up when we execute
            // the continuation of the yield.
            T next;
            for (T q = head; q != null; q = next)
            {
                next = q.Next;
                yield return q;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() =>
            this.GetEnumerator();
    }
}