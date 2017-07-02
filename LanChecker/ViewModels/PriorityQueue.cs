using System;
using System.Collections.Specialized;

namespace LanChecker.ViewModels
{
    class PriorityQueue<TScore, TValue> : INotifyCollectionChanged
    {
        private _Container _first;
        private _Container _end;

        private Func<TScore, TScore, int> _comparison;

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public PriorityQueue(Func<TScore, TScore, int> comparison)
        {
            _comparison = comparison;
        }

        public void Queue(TScore score, TValue value)
        {
            var ct = _first;
            var con = new _Container { Score = score, Value = value };
            int index = 0;

            while (ct.Next != _end && _comparison(ct.Next.Score, score) < 0)
            {
                ct = ct.Next;
                index++;
            }

            con.Prev = ct;
            con.Next = ct.Next;
            con.Prev.Next = con;
            con.Next.Prev = con;

            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, value, index));
        }

        public TValue Dequeue()
        {
            if (_first.Next == _end) return default(TValue);

            var con = _first.Next;
            con.Next.Prev = con.Prev;
            con.Prev.Next = con.Next;

            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, con.Value, 0));

            return con.Value;
        }

        public TValue Peek()
        {
            if (_first.Next == _end) return default(TValue);
            return _first.Next.Value;
        }

        public void ChangeScore(TScore score)
        { }

        private class _Container
        {
            public _Container Prev;
            public _Container Next;

            public TScore Score;
            public TValue Value;
        }
    }
}
