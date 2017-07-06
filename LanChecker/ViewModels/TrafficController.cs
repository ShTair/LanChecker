using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LanChecker.ViewModels
{
    class TrafficController
    {
        private Queue<TaskCompletionSource<IDisposable>>[] _qs;
        private int _counter;

        private bool _isRunning;

        public int Count { get; private set; }

        public TrafficController()
        {
            _qs = Enumerable.Range(0, 3).Select(t => new Queue<TaskCompletionSource<IDisposable>>()).ToArray();
        }

        public Task<IDisposable> WaitAsync(int p)
        {
            lock (_qs)
            {
                Count += 1;

                if (_isRunning)
                {
                    var tcs = new TaskCompletionSource<IDisposable>();
                    _qs[p].Enqueue(tcs);
                    return tcs.Task;
                }
                else
                {
                    _isRunning = true;
                    return Task.FromResult((IDisposable)new _Releaser(this));
                }
            }
        }

        public void Release()
        {
            lock (_qs)
            {
                Count -= 1;

                _counter++;
                if (_counter >= 3) _counter = 0;

                Queue<TaskCompletionSource<IDisposable>> q = null;

                q = _qs[_counter];
                if (q.Count == 0)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        var temp = _qs[i];
                        if (temp.Count != 0)
                        {
                            q = temp;
                            break;
                        }
                    }
                }

                if (q.Count == 0)
                {
                    _isRunning = false;
                    return;
                }

                var tcs = q.Dequeue();
                Task.Run(() => tcs.TrySetResult(new _Releaser(this)));
            }
        }

        private class _Releaser : IDisposable
        {
            private TrafficController _p;

            public _Releaser(TrafficController p)
            {
                _p = p;
            }

            public void Dispose()
            {
                _p.Release();
            }
        }
    }
}
