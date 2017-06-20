using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace LanChecker.ViewModels
{
    class TargetViewModel
    {
        private static SemaphoreSlim _sem = new SemaphoreSlim(1);

        private byte[] _mac;

        private Task _run;

        private uint _host;

        private CancellationTokenSource _cts;

        public TargetViewModel(uint host)
        {
            _mac = new byte[6];

            _host = host;
        }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            _run = Task.Run(Run);
        }

        public Task Stop()
        {
            _cts.Cancel();
            return _run;
        }

        private async Task Run()
        {
            bool old = false;

            while (true)
            {
                try
                {
                    await _sem.WaitAsync();
                    if (_cts.IsCancellationRequested) break;

                    var result = SendArp();
                    if (result != old)
                    {
                        Console.WriteLine($"{_host >> 24} {result}");
                        old = result;
                    }
                }
                finally { _sem.Release(); }

                try { await Task.Delay(20000, _cts.Token); }
                catch { break; }
            }
        }

        private bool SendArp()
        {
            var macLen = _mac.Length;
            var result = SendARP(_host, 0, _mac, ref macLen);
            return result == 0;
        }

        [DllImport("iphlpapi.dll", ExactSpelling = true)]
        private static extern int SendARP(uint dstIp, uint srcIp, byte[] mac, ref int macLen);
    }
}
