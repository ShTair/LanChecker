using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace LanChecker.ViewModels
{
    class DeviceViewModel : INotifyPropertyChanged, IDisposable
    {
        private HashSet<TargetViewModel> _targets;

        #region properties

        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsRunning { get; private set; }

        public string MacAddress { get; private set; }

        public string Name { get; private set; }

        public string Category { get; private set; }

        public DateTime LastReach
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
        private DateTime _LastReach;
        private PropertyChangedEventArgs _LastReachChangedEventArgs = new PropertyChangedEventArgs(nameof(LastReach));

        public TimeSpan Elapsed
        {
            get { return _Elapsed; }
            set
            {
                if (_Elapsed == value) return;
                _Elapsed = value;

                if (Elapsed == TimeSpan.Zero) Status = 0;
                else if (Elapsed < TimeSpan.FromHours(1)) Status = 1;
                else if (Elapsed < TimeSpan.FromDays(3)) Status = 2;
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
                _Status = value;
                PropertyChanged?.Invoke(this, _StatusChangedEventArgs);
                PropertyChanged?.Invoke(this, _OrderTimeChangedEventArgs);
            }
        }
        private int _Status;
        private PropertyChangedEventArgs _StatusChangedEventArgs = new PropertyChangedEventArgs(nameof(Status));

        public DateTime OrderTime
        {
            get { return Status <= 1 ? DateTime.MaxValue : LastReach; }
        }
        private PropertyChangedEventArgs _OrderTimeChangedEventArgs = new PropertyChangedEventArgs(nameof(OrderTime));

        #endregion

        public DeviceViewModel(string mac, string name, string category)
        {
            _targets = new HashSet<TargetViewModel>();

            MacAddress = mac;
            Name = name;
            Category = category;

            IsRunning = true;
            Task.Run((Action)Run);
        }

        private async void Run()
        {
            while (IsRunning)
            {
                await Task.Delay(5000);
                if (LastReach >= DateTime.Now.AddSeconds(-30)) continue;

                Elapsed = DateTime.Now - LastReach;
            }
        }

        public void Reach()
        {
            LastReach = DateTime.Now;
            Elapsed = TimeSpan.Zero;
        }

        public void Dispose()
        {
            IsRunning = false;
        }
    }
}
