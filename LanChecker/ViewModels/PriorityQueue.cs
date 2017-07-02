﻿using System;
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
        { }

        public TValue Dequeue()
        {
            if (_first.Next == _end) return default(TValue);

            var con = _first.Next;
            con.Next.Prev = con.Prev;
            con.Prev.Next = con.Next;

            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, con.Value, 0));

            return con.Value;
        }

        private class _Container
        {
            public _Container Prev;
            public _Container Next;

            public TScore Score;
            public TValue Value;
        }
    }
}
