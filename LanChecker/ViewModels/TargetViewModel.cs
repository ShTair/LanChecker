using LanChecker.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace LanChecker.ViewModels
{
    class TargetViewModel : INotifyPropertyChanged
    {
        private static Regex _fileNameRegex = new Regex(@"[\/:,;*?""<>|]", RegexOptions.Compiled);
        private static TrafficController _tc = new TrafficController();

        public static event Action<int> QueueCountChanged;

        private Dictionary<string, DeviceInfo> _names;

        private byte[] _mac;
        private DateTime _lastReach;

        private Task _run;
        private CancellationTokenSource _cts;

        private uint _host;

        public event Action<bool, DateTime> StatusChanged;

        public event Action<bool> IsEnabledChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        public TimeSpan Elapsed
        {
            get { return _Elapsed; }
            set
            {
                if (_Elapsed == value) return;
                _Elapsed = value;

                if (value == TimeSpan.Zero) Status = 0;
                else if (value < TimeSpan.FromHours(1)) Status = 1;
                else Status = 2;

                ElapsedString = value.ToString(@"d\.hh\:mm");

                PropertyChanged?.Invoke(this, _ElapsedChangedEventArgs);
            }
        }
        private TimeSpan _Elapsed;
        private PropertyChangedEventArgs _ElapsedChangedEventArgs = new PropertyChangedEventArgs(nameof(Elapsed));

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
                    FileName = null;
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

        public int IPAddress { get; }

        public int Status
        {
            get { return _Status; }
            set
            {
                if (_Status == value) return;
                _Status = value;

                if (value == 0 || value == 1) IsIn = true;
                else IsIn = false;

                PropertyChanged?.Invoke(this, _StatusChangedEventArgs);
            }
        }
        private int _Status;
        private PropertyChangedEventArgs _StatusChangedEventArgs = new PropertyChangedEventArgs(nameof(Status));

        public bool IsIn
        {
            get { return _IsIn; }
            set
            {
                if (_IsIn == value) return;
                _IsIn = value;
                StatusChanged.Invoke(value, _lastReach);
                PropertyChanged?.Invoke(this, _IsInChangedEventArgs);
            }
        }
        private bool _IsIn;
        private PropertyChangedEventArgs _IsInChangedEventArgs = new PropertyChangedEventArgs(nameof(IsIn));

        public bool IsInDhcp
        {
            get { return _IsInDhcp; }
            set
            {
                if (_IsInDhcp == value) return;
                _IsInDhcp = value;
                PropertyChanged?.Invoke(this, _IsInDhcpChangedEventArgs);
            }
        }
        private bool _IsInDhcp;
        private PropertyChangedEventArgs _IsInDhcpChangedEventArgs = new PropertyChangedEventArgs(nameof(IsInDhcp));

        public bool IsEnabled
        {
            get { return _IsEnabled; }
            set
            {
                if (_IsEnabled == value) return;
                _IsEnabled = value;
                IsEnabledChanged?.Invoke(value);
                PropertyChanged?.Invoke(this, _IsEnabledChangedEventArgs);
            }
        }
        private bool _IsEnabled;
        private PropertyChangedEventArgs _IsEnabledChangedEventArgs = new PropertyChangedEventArgs(nameof(IsEnabled));

        public TargetViewModel(uint host, bool isInDhcp, Dictionary<string, DeviceInfo> names)
        {
            _mac = new byte[6];
            _lastReach = DateTime.Now.AddDays(-3);
            Elapsed = TimeSpan.FromDays(3);

            _host = host;
            IPAddress = (int)(host >> 24);

            IsInDhcp = isInDhcp;

            _names = names;
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
            int priority = IsInDhcp ? 1 : 2;

            while (true)
            {
                using (await _tc.WaitAsync(priority))
                {
                    QueueCountChanged?.Invoke(_tc.Count);

                    Console.WriteLine($"Start {priority} {_host >> 24} {old}");

                    if (_cts.IsCancellationRequested) break;

                    var result = SendArp();

                    if (old != result)
                    {
                        old = result;
                        Console.WriteLine($"{_host >> 24} {result}");
                    }

                    var now = DateTime.Now;

                    if (result)
                    {
                        MacAddress = string.Join(":", _mac.Select(t => t.ToString("X2")));
                        _lastReach = now;
                    }

                    var e = now - _lastReach;
                    Elapsed = e.TotalDays < 3 ? e : TimeSpan.FromDays(3);
                }
                QueueCountChanged?.Invoke(_tc.Count);

                priority = old ? 0 : (IsInDhcp || Elapsed < TimeSpan.FromDays(3)) ? 1 : 2;
                IsEnabled = priority != 2;

                try { await Task.Delay(old ? 20000 : IsInDhcp ? 60000 : 60000 * 60, _cts.Token); }
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
