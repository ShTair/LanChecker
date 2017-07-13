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

        public event Action CountChanged;

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
                    Task.Run(() => CountChanged?.Invoke());
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
                    Task.Run(() => CountChanged?.Invoke());

                    if (_qs[0].Count != 0)
                    {
                        return Task.FromResult(_qs[0].Dequeue());
                    }

                    var ci = _nextIndex++ % _qs.Length - 1;

                    if (_qs[ci + 1].Count != 0)
                    {
                        return Task.FromResult(_qs[ci + 1].Dequeue());
                    }

                    for (int i = 1; i < _qs.Length + 1; i++)
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
