﻿using System;
using System.ComponentModel;
using System.Linq;
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

        public double Elapsed
        {
            get { return _Elapsed; }
            set
            {
                if (_Elapsed == value) return;
                _Elapsed = value;
                PropertyChanged?.Invoke(this, _ElapsedChangedEventArgs);
            }
        }
        private double _Elapsed = 3;
        private PropertyChangedEventArgs _ElapsedChangedEventArgs = new PropertyChangedEventArgs(nameof(Elapsed));

        public double Score
        {
            get { return _Score; }
            set
            {
                if (_Score == value) return;
                _Score = value;
                if (_Score < 0.1) _Score = 0;
                PropertyChanged?.Invoke(this, _ScoreChangedEventArgs);
            }
        }
        private double _Score;
        private PropertyChangedEventArgs _ScoreChangedEventArgs = new PropertyChangedEventArgs(nameof(Score));

        public string MacAddress
        {
            get { return _MacAddress; }
            set
            {
                if (_MacAddress == value) return;
                _MacAddress = value;
                PropertyChanged?.Invoke(this, _MacAddressChangedEventArgs);
            }
        }
        private string _MacAddress;
        private PropertyChangedEventArgs _MacAddressChangedEventArgs = new PropertyChangedEventArgs(nameof(MacAddress));

        public int IPAddress { get; }

        public TargetViewModel(uint host)
        {
            _mac = new byte[6];
            _lastReach = DateTime.Now.AddDays(-3);

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
                        MacAddress = string.Join(":", _mac.Select(t => t.ToString("X2")));
                        _lastReach = DateTime.Now;
                    }

                    Elapsed = Math.Min(3, (DateTime.Now - _lastReach).TotalDays);
                    Score = Math.Min(100, (Score * 29 + Elapsed * 24 * 60) / 30);
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
