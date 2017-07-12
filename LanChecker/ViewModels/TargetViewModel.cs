using LanChecker.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace LanChecker.ViewModels
{
    class TargetViewModel : INotifyPropertyChanged
    {
        private static readonly Regex _fileNameRegex = new Regex(@"[\/:,;*?""<>|]", RegexOptions.Compiled);
        private static TrafficController _tc = new TrafficController();

        private static readonly TimeSpan _ts1 = TimeSpan.Zero;
        private static readonly TimeSpan _ts2 = TimeSpan.FromHours(1);
        private static readonly TimeSpan _ts3 = TimeSpan.FromDays(3);

        private Dictionary<string, DeviceInfo> _names;

        private uint _host;
        public int IPAddress { get; }

        private byte[] _mac;

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

                ElapsedString = value.ToString(@"d\.hh\:mm");

                PropertyChanged?.Invoke(this, _ElapsedChangedEventArgs);
            }
        }
        private TimeSpan _Elapsed;
        private PropertyChangedEventArgs _ElapsedChangedEventArgs = new PropertyChangedEventArgs(nameof(Elapsed));

        public DateTime InLastReach
        {
            get { return _InLastReach; }
            set
            {
                if (_InLastReach == value) return;
                _InLastReach = value;
                if (Status != 0) LastReachDateTime = value;
                PropertyChanged?.Invoke(this, _InLastReachChangedEventArgs);
            }
        }
        private DateTime _InLastReach;
        private PropertyChangedEventArgs _InLastReachChangedEventArgs = new PropertyChangedEventArgs(nameof(InLastReach));

        public event Action<int, int> StatusChanged;
        public int Status
        {
            get { return _Status; }
            set
            {
                if (_Status == value) return;
                var old = _Status;
                _Status = value;
                IsIn = value <= 1;
                LastReachDateTime = value == 0 ? DateTime.MaxValue : InLastReach;
                StatusChanged?.Invoke(old, value);
                PropertyChanged?.Invoke(this, _StatusChangedEventArgs);
            }
        }
        private int _Status;
        private PropertyChangedEventArgs _StatusChangedEventArgs = new PropertyChangedEventArgs(nameof(Status));

        public DateTime LastReachDateTime
        {
            get { return _LastReachDateTime; }
            set
            {
                if (_LastReachDateTime == value) return;
                _LastReachDateTime = value;
                PropertyChanged?.Invoke(this, _LastReachDateTimeChangedEventArgs);
            }
        }
        private DateTime _LastReachDateTime;
        private PropertyChangedEventArgs _LastReachDateTimeChangedEventArgs = new PropertyChangedEventArgs(nameof(LastReachDateTime));

        public string ElapsedString
        {
            get { return _ElapsedString; }
            set
            {
                if (_ElapsedString == value) return;
                _ElapsedString = value;
                PropertyChanged?.Invoke(this, _ElapsedStringChangedEventArgs);
            }
        }
        private string _ElapsedString;
        private PropertyChangedEventArgs _ElapsedStringChangedEventArgs = new PropertyChangedEventArgs(nameof(ElapsedString));

        public string MacAddress
        {
            get { return _MacAddress; }
            set
            {
                if (_MacAddress == value) return;
                _MacAddress = value;

                DeviceInfo name;
                if (_names.TryGetValue(value, out name))
                {
                    Name = name.Name;
                    FileName = name.FileName;
                }
                else
                {
                    Name = null;
                    FileName = "Unknown";
                }

                PropertyChanged?.Invoke(this, _MacAddressChangedEventArgs);
            }
        }
        private string _MacAddress;
        private PropertyChangedEventArgs _MacAddressChangedEventArgs = new PropertyChangedEventArgs(nameof(MacAddress));

        public string Name
        {
            get { return _Name; }
            set
            {
                if (_Name == value) return;
                _Name = value;
                PropertyChanged?.Invoke(this, _NameChangedEventArgs);
            }
        }
        private string _Name;
        private PropertyChangedEventArgs _NameChangedEventArgs = new PropertyChangedEventArgs(nameof(Name));

        public string FileName
        {
            get { return _FileName; }
            set
            {
                if (_FileName == value) return;
                _FileName = value;
                PropertyChanged?.Invoke(this, _FileNameChangedEventArgs);
            }
        }
        private string _FileName;
        private PropertyChangedEventArgs _FileNameChangedEventArgs = new PropertyChangedEventArgs(nameof(FileName));

        public event Action<bool, DateTime> IsInChanged;
        public bool IsIn
        {
            get { return _IsIn; }
            set
            {
                if (_IsIn == value) return;
                _IsIn = value;
                IsInChanged?.Invoke(value, InLastReach);
                PropertyChanged?.Invoke(this, _IsInChangedEventArgs);
            }
        }
        private bool _IsIn;
        private PropertyChangedEventArgs _IsInChangedEventArgs = new PropertyChangedEventArgs(nameof(IsIn));

        #endregion

        public TargetViewModel(uint host, Dictionary<string, DeviceInfo> names)
        {
            _mac = new byte[6];
            InLastReach = DateTime.Now.AddDays(-3);
            Elapsed = TimeSpan.FromDays(3);

            _host = host;
            IPAddress = (int)(host >> 24);

            _names = names;
        }

        public void Check()
        {
            var result = SendArp();

            var now = DateTime.Now;

            if (result)
            {
                MacAddress = string.Join(":", _mac.Select(t => t.ToString("X2")));
                InLastReach = now;
            }

            Elapsed = now - InLastReach;
        }

        public void Find(string mac)
        {
            MacAddress = mac;
            InLastReach = DateTime.Now;
            Elapsed = TimeSpan.Zero;
        }

        public void Out()
        {
            InLastReach = DateTime.Now.AddDays(-3);
        }

        private bool SendArp()
        {
            var macLen = _mac.Length;
            var result = SendARP(_host, 0, _mac, ref macLen);
            return result == 0;
        }

        public string Serialize()
        {
            return $"{IPAddress}\t{MacAddress}\t{InLastReach.ToBinary()}\t{Status}";
        }

        public void Deserialize(string data)
        {
            try
            {
                var sp = data.Split('\t');
                if (sp.Length != 4) return;

                if (sp[0] != IPAddress.ToString()) return;

                MacAddress = sp[1];
                InLastReach = DateTime.FromBinary(long.Parse(sp[2]));
                Status = int.Parse(sp[3]);
            }
            catch { }
        }

        [DllImport("iphlpapi.dll", ExactSpelling = true)]
        private static extern int SendARP(uint dstIp, uint srcIp, byte[] mac, ref int macLen);
    }
}
