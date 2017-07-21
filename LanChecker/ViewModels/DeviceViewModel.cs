using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace LanChecker.ViewModels
{
    class DeviceViewModel : INotifyPropertyChanged
    {
        private HashSet<TargetViewModel> _targets;

        public event PropertyChangedEventHandler PropertyChanged;

        public string MacAddress { get; private set; }

        public string Name { get; private set; }

        public TimeSpan Elapsed
        {
            get { return _Elapsed; }
            set
            {
                if (_Elapsed == value) return;
                _Elapsed = value;
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
            }
        }
        private int _Status;
        private PropertyChangedEventArgs _StatusChangedEventArgs = new PropertyChangedEventArgs(nameof(Status));

        public DeviceViewModel(string mac)
        {
            _targets = new HashSet<TargetViewModel>();

            MacAddress = mac;
        }

        public void Find(TargetViewModel target)
        {
            _targets.Add(target);
        }
    }
}
