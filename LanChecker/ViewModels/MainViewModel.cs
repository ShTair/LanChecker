using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace LanChecker.ViewModels
{
    class MainViewModel : IDisposable, INotifyPropertyChanged
    {
        private int _counter;
        private object _counterLock = new object();

        public ObservableCollection<TargetViewModel> Targets { get; }

        public string Status
        {
            get { return _Status; }
            set
            {
                if (_Status == value) return;
                _Status = value;
                PropertyChanged?.Invoke(this, _StatusChangedEventArgs);
            }
        }
        private string _Status;
        private PropertyChangedEventArgs _StatusChangedEventArgs = new PropertyChangedEventArgs(nameof(Status));

        public event PropertyChangedEventHandler PropertyChanged;

        public MainViewModel(uint sub, uint start, int count)
        {
            Status = "Ready...";

            Targets = new ObservableCollection<TargetViewModel>(GetTargetHosts(sub, start, count).Select(t => new TargetViewModel(t)));
            foreach (var target in Targets)
            {
                target.StatusChanged += status =>
                {
                    lock (_counterLock)
                    {
                        _counter += status ? 1 : -1;
                    }

                    Status = $"Reach: {_counter}";
                };
                target.Start();
            }
        }

        public void Dispose()
        {
            Task.WhenAll(Targets.Select(t => t.Stop())).Wait(30000);
        }

        private IEnumerable<uint> GetTargetHosts(uint sub, uint start, int count)
        {
            uint v = 192 + (168 << 8) + (sub << 16);
            for (uint i = 0; i < count; i++)
            {
                yield return v + ((start + i) << 24);
            }
        }
    }
}
