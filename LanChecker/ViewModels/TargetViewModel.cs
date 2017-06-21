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

        public event PropertyChangedEventHandler PropertyChanged;

        public double ElapsedMinutes
        {
            get { return _ElapsedMinutes; }
            set
            {
                if (_ElapsedMinutes == value) return;
                _ElapsedMinutes = value;
                PropertyChanged?.Invoke(this, _ElapsedMinutesChangedEventArgs);
            }
        }
        private double _ElapsedMinutes;
        private PropertyChangedEventArgs _ElapsedMinutesChangedEventArgs = new PropertyChangedEventArgs(nameof(ElapsedMinutes));

        public double Score
        {
            get { return _Score; }
            set
            {
                if (_Score == value) return;
                _Score = value;
                PropertyChanged?.Invoke(this, _ScoreChangedEventArgs);
            }
        }
        private double _Score = 1440;
        private PropertyChangedEventArgs _ScoreChangedEventArgs = new PropertyChangedEventArgs(nameof(Score));

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

                    if (result != old)
                    {
                        Console.WriteLine($"{_host >> 24} {result}");
                        old = result;
                    }

                    if (result)
                    {
                        _lastReach = DateTime.Now;
                    }

                    ElapsedMinutes = (DateTime.Now - _lastReach).TotalMinutes;
                    Score = (Score * 29 + Math.Min(ElapsedMinutes, 1440)) / 30;
                }
                finally { _sem.Release(); }

                try { await Task.Delay(old ? 20000 : 60000, _cts.Token); }
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
