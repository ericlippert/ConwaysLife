using System.Collections;
using System.Collections.Generic;

namespace ConwaysLife.Hensel
{
    sealed class Quad4List : IEnumerable<Quad4>
    {
        private Quad4 head = null;
        public int Count { get; private set; }

        public void Add(Quad4 q)
        {
            q.Prev = null;
            q.Next = head;
            if (head != null)
                head.Prev = q;
            head = q;
            Count += 1;
        }

        public void Remove(Quad4 q)
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

        public IEnumerator<Quad4> GetEnumerator()
        {
            // The quad might be removed from the current list during the enumeration,
            // so cache the next link so we always know where to pick up when we execute
            // the continuation of the yield.
            Quad4 next;
            for (Quad4 q = head; q != null; q = next)
            {
                next = q.Next;
                yield return q;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() =>
            this.GetEnumerator();
    }
}