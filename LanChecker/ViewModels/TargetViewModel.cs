﻿using LanChecker.Models;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;

namespace LanChecker.ViewModels
{
    class TargetViewModel : INotifyPropertyChanged
    {
        private static readonly TimeSpan _ts1 = TimeSpan.Zero;
        private static readonly TimeSpan _ts2 = TimeSpan.FromHours(1);
        private static readonly TimeSpan _ts3 = TimeSpan.FromDays(3);

        private uint _host;
        public int IPAddress { get; }

        private byte[] _mac;
        private DateTimeOffset _lastReach;

        public event Action<string> Reached;
        public event Action<string> Unreached;

        #region properties

        public event PropertyChangedEventHandler PropertyChanged;

        public TimeSpan Elapsed
        {
            get { return _Elapsed; }
            set
            {
                if (value > _ts3) value = _ts3;

                if (_Elapsed == value) return;
                _Elapsed = value;

                if (value == _ts1) Status = 0;
                else if (value < _ts2) Status = 1;
                else if (value < _ts3) Status = 2;
                else Status = 3;

                PropertyChanged?.Invoke(this, _ElapsedChangedEventArgs);
            }
        }
        private TimeSpan _Elapsed;
        private PropertyChangedEventArgs _ElapsedChangedEventArgs = new PropertyChangedEventArgs(nameof(Elapsed));

        public int Status
        {
            get { return _Status; }
            set
            {
                if (_Status == value) return;
                var old = _Status;
                _Status = value;
                PropertyChanged?.Invoke(this, _StatusChangedEventArgs);
            }
        }
        private int _Status;
        private PropertyChangedEventArgs _StatusChangedEventArgs = new PropertyChangedEventArgs(nameof(Status));

        public string MacAddress
        {
            get { return _MacAddress; }
            set
            {
                if (_MacAddress == value) return;
                if (_MacAddress != null) Unreached?.Invoke(_MacAddress);
                _MacAddress = value;
                PropertyChanged?.Invoke(this, _MacAddressChangedEventArgs);
            }
        }
        private string _MacAddress;
        private PropertyChangedEventArgs _MacAddressChangedEventArgs = new PropertyChangedEventArgs(nameof(MacAddress));

        #endregion

        public TargetViewModel(uint host)
        {
            _mac = new byte[6];
            _lastReach = DateTimeOffset.Now.AddDays(-3).AddSeconds(1);
            Elapsed = TimeSpan.FromDays(3) - TimeSpan.FromSeconds(1);

            _host = host;
            IPAddress = (int)(host >> 24);
        }

        public void Check()
        {
            var result = SendArp();

            var now = DateTimeOffset.Now;

            if (result)
            {
                MacAddress = string.Join(":", _mac.Select(t => t.ToString("X2")));
                Reached?.Invoke(MacAddress);
                _lastReach = now;
            }
            else
            {
                if (MacAddress != null) Unreached?.Invoke(MacAddress);
            }

            Elapsed = now - _lastReach;
        }

        public void Find(string mac)
        {
            MacAddress = mac;
            Reached?.Invoke(MacAddress);
            _lastReach = DateTimeOffset.Now;
            Elapsed = TimeSpan.Zero;
        }

        private bool SendArp()
        {
            var macLen = _mac.Length;
            var result = SendARP(_host, 0, _mac, ref macLen);
            return result == 0;
        }

        public void Update(TargetInfo ti)
        {
            if (ti.IPAddress != IPAddress) return;

            MacAddress = ti.MacAddress;
            _lastReach = ti.LastReach;
            Status = ti.Status;
        }

        [DllImport("iphlpapi.dll", ExactSpelling = true)]
        private static extern int SendARP(uint dstIp, uint srcIp, byte[] mac, ref int macLen);
    }
}
