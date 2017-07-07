using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LanChecker.ViewModels
{
    class MultiLaneQueue<T>
    {
        private Queue<T>[] _qs;
        private int _nextIndex;

        private Queue<TaskCompletionSource<T>> _tcsq;

        public int Count { get; private set; }

        public MultiLaneQueue(int count)
        {
            _qs = new Queue<T>[count];
            for (int i = 0; i < count; i++) _qs[i] = new Queue<T>();

            _tcsq = new Queue<TaskCompletionSource<T>>();
        }

        public void Enqueue(T item, int priority)
        {
            lock (_tcsq)
            {
                if (_tcsq.Count > 0)
                {
                    var tcs = _tcsq.Dequeue();
                    Task.Run(() => tcs.SetResult(item));
                }
                else
                {
                    _qs[priority].Enqueue(item);
                    Count++;
                }
            }
        }

        public Task<T> Dequeue()
        {
            lock (_tcsq)
            {
                if (Count > 0)
                {
                    Count--;
                    var ci = _nextIndex++ % _qs.Length;

                    if (_qs[ci].Count != 0)
                    {
                        return Task.FromResult(_qs[ci].Dequeue());
                    }

                    for (int i = 0; i < _qs.Length; i++)
                    {
                        if (_qs[i].Count != 0)
                        {
                            return Task.FromResult(_qs[i].Dequeue());
                        }
                    }

                    throw new Exception();
                }

                var tcs = new TaskCompletionSource<T>();
                _tcsq.Enqueue(tcs);
                return tcs.Task;
            }
        }
    }
}
