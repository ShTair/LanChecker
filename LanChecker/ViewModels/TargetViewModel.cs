using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace LanChecker.ViewModels
{
    class TargetViewModel : INotifyPropertyChanged
    {
        private static SemaphoreSlim _sem = new SemaphoreSlim(1);

        private byte[] _mac;
        private DateTime _lastReach;

        private Task _run;
        private CancellationTokenSource _cts;

        private uint _host;

        public event Action Reached;

        public event PropertyChangedEventHandler PropertyChanged;

        public int ElapsedMinutes
        {
            get { return _ElapsedMinutes; }
            set
            {
                if (_ElapsedMinutes == value) return;
                _ElapsedMinutes = value;
                PropertyChanged?.Invoke(this, _ElapsedMinutesChangedEventArgs);
            }
        }
        private int _ElapsedMinutes;
        private PropertyChangedEventArgs _ElapsedMinutesChangedEventArgs = new PropertyChangedEventArgs(nameof(ElapsedMinutes));

        public int IPAddress { get; }

        public TargetViewModel(uint host)
        {
            _mac = new byte[6];
            _lastReach = DateTime.Now;

            _host = host;
            IPAddress = (int)(host >> 24);
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
                    ElapsedMinutes = (int)(DateTime.Now - _lastReach).TotalMinutes;

                    if (result != old)
                    {
                        Console.WriteLine($"{_host >> 24} {result}");
                        old = result;
                    }

                    if (result)
                    {
                        _lastReach = DateTime.Now;
                        Reached?.Invoke();
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
