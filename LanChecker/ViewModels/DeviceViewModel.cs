using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace LanChecker.ViewModels
{
    class DeviceViewModel : INotifyPropertyChanged, IDisposable
    {
        private HashSet<int> _targets;

        public event Action Expired;
        public event Action<bool, DateTimeOffset> IsInChanged;

        #region properties

        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsRunning { get; private set; }

        public string MacAddress { get; private set; }

        public string Category { get; private set; }

        public string Name { get; private set; }

        public DateTimeOffset LastReach
        {
            get { return _LastReach; }
            set
            {
                if (_LastReach == value) return;
                _LastReach = value;
                PropertyChanged?.Invoke(this, _LastReachChangedEventArgs);
                PropertyChanged?.Invoke(this, _OrderTimeChangedEventArgs);
            }
        }
        private DateTimeOffset _LastReach;
        private PropertyChangedEventArgs _LastReachChangedEventArgs = new PropertyChangedEventArgs(nameof(LastReach));

        public TimeSpan Elapsed
        {
            get { return _Elapsed; }
            set
            {
                if (_Elapsed == value) return;
                _Elapsed = value;

                if (Elapsed == TimeSpan.Zero)
                {
                    Status = 0;
                    IsIn = true;

                }
                else if (Elapsed < TimeSpan.FromHours(1))
                {
                    Status = 1;
                    IsIn = true;
                }
                else if (Elapsed < TimeSpan.FromDays(3))
                {
                    Status = 2;
                    IsIn = false;
                }
                else
                {
                    Status = 3;
                    IsIn = false;
                    IsRunning = false;
                    Expired?.Invoke();
                }

                PropertyChanged?.Invoke(this, _ElapsedChangedEventArgs);
            }
        }
        private TimeSpan _Elapsed = TimeSpan.MinValue;
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
        private string _ElapsedString = "0.00:00";
        private PropertyChangedEventArgs _ElapsedStringChangedEventArgs = new PropertyChangedEventArgs(nameof(ElapsedString));

        public int Status
        {
            get { return _Status; }
            set
            {
                if (_Status == value) return;
                _Status = value;
                ColorFlag = value;
                PropertyChanged?.Invoke(this, _StatusChangedEventArgs);
                PropertyChanged?.Invoke(this, _OrderTimeChangedEventArgs);
            }
        }
        private int _Status = -1;
        private PropertyChangedEventArgs _StatusChangedEventArgs = new PropertyChangedEventArgs(nameof(Status));

        public bool IsIn
        {
            get { return _IsIn; }
            set
            {
                if (_IsIn == value) return;
                _IsIn = value;
                if (value) LastIn = LastReach;

                IsInChanged?.Invoke(value, LastReach);
                PropertyChanged?.Invoke(this, _IsInChangedEventArgs);
            }
        }
        private bool _IsIn;
        private PropertyChangedEventArgs _IsInChangedEventArgs = new PropertyChangedEventArgs(nameof(IsIn));

        public DateTimeOffset OrderTime
        {
            get { return Status <= 1 ? DateTimeOffset.MaxValue : LastReach; }
        }
        private PropertyChangedEventArgs _OrderTimeChangedEventArgs = new PropertyChangedEventArgs(nameof(OrderTime));

        public int LastIP
        {
            get { return _LastIP; }
            set
            {
                if (_LastIP == value) return;
                _LastIP = value;
                PropertyChanged?.Invoke(this, _LastIPChangedEventArgs);
            }
        }
        private int _LastIP;
        private PropertyChangedEventArgs _LastIPChangedEventArgs = new PropertyChangedEventArgs(nameof(LastIP));

        public DateTimeOffset LastIn
        {
            get { return _LastIn; }
            set
            {
                if (_LastIn == value) return;
                _LastIn = value;
                PropertyChanged?.Invoke(this, _LastInChangedEventArgs);
            }
        }
        private DateTimeOffset _LastIn;
        private PropertyChangedEventArgs _LastInChangedEventArgs = new PropertyChangedEventArgs(nameof(LastIn));

        public int ColorFlag
        {
            get { return _ColorFlag; }
            set
            {
                if (_ColorFlag == value) return;
                _ColorFlag = value;
                PropertyChanged?.Invoke(this, _ColorFlagChangedEventArgs);
            }
        }
        private int _ColorFlag;
        private PropertyChangedEventArgs _ColorFlagChangedEventArgs = new PropertyChangedEventArgs(nameof(ColorFlag));

        #endregion

        public DeviceViewModel(string mac, string category, string name)
        {
            _targets = new HashSet<int>();

            MacAddress = mac;
            Category = category;
            Name = name;
        }

        public void Start(DateTimeOffset lastReach, DateTimeOffset lastIn)
        {
            LastReach = lastReach;
            Elapsed = DateTimeOffset.Now - LastReach;
            LastIn = lastIn;

            Update();

            IsRunning = true;
            Task.Run((Action)Run);
        }

        private async void Run()
        {
            while (IsRunning)
            {
                await Task.Delay(5000);
                if (_targets.Count == 0)
                {
                    Elapsed = DateTimeOffset.Now - LastReach;
                }

                Update();
            }
        }

        private void Update()
        {
            if (Status == 0)
            {
                if (LastIn < DateTimeOffset.Now.AddDays(-3))
                {
                    LastIn = DateTimeOffset.MinValue;
                    ColorFlag = -1;
                    ElapsedString = TimeSpan.FromDays(3).ToString(@"d\.hh\:mm");
                }
                else
                {
                    ElapsedString = (DateTimeOffset.Now - LastIn).ToString(@"d\.hh\:mm");
                }
            }
            else ElapsedString = Elapsed.ToString(@"d\.hh\:mm");
        }

        public void Reach(int ip)
        {
            _targets.Add(ip);
            LastIP = ip;

            LastReach = DateTimeOffset.Now;
            Elapsed = TimeSpan.Zero;

            Update();
        }

        public void Unreach(int ip)
        {
            _targets.Remove(ip);
            if (_targets.Count != 0) return;

            Elapsed = DateTimeOffset.Now - LastReach;

            Update();
        }

        public void Dispose()
        {
            IsRunning = false;
        }
    }
}
