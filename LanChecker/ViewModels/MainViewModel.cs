using LanChecker.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace LanChecker.ViewModels
{
    class MainViewModel : IDisposable, INotifyPropertyChanged
    {
        private object _counterLock = new object();

        private List<TargetViewModel> _allTargets;
        public ObservableCollection<TargetViewModel> Targets { get; }

        public int ReachCount
        {
            get { return _ReachCount; }
            set
            {
                if (_ReachCount == value) return;
                _ReachCount = value;
                PropertyChanged?.Invoke(this, _ReachCountChangedEventArgs);
            }
        }
        private int _ReachCount;
        private PropertyChangedEventArgs _ReachCountChangedEventArgs = new PropertyChangedEventArgs(nameof(ReachCount));

        public int QueueCount
        {
            get { return _QueueCount; }
            set
            {
                if (_QueueCount == value) return;
                _QueueCount = value;
                PropertyChanged?.Invoke(this, _QueueCountChangedEventArgs);
            }
        }
        private int _QueueCount;
        private PropertyChangedEventArgs _QueueCountChangedEventArgs = new PropertyChangedEventArgs(nameof(QueueCount));

        public event PropertyChangedEventHandler PropertyChanged;

        public MainViewModel(uint sub, uint start, int count, Dictionary<string, DeviceInfo> names)
        {
            Targets = new ObservableCollection<TargetViewModel>();

            var d = Dispatcher.CurrentDispatcher;

            if (names == null) names = new Dictionary<string, DeviceInfo>();

            Directory.CreateDirectory("log");

            _allTargets = Enumerable.Range(1, 254).Select(t =>
            {
                var isInDhcp = t >= start && t < (start + count);
                return new TargetViewModel(ConvertToUint(sub, (uint)t), isInDhcp, names);
            }).ToList();

            TargetViewModel.QueueCountChanged += qc => QueueCount = qc;

            foreach (var target in _allTargets)
            {
                target.StatusChanged += (status, time) =>
                {
                    lock (_counterLock)
                    {
                        ReachCount += status ? 1 : -1;
                        File.AppendAllLines($"log\\log_{target.FileName}.txt", new[] { $"{DateTime.Now:yyyy/MM/dd_HH:mm:ss}\t{time:yyyy/MM/dd_HH:mm:ss}\t{status}\t{target.MacAddress}\t{target.Name}\t{target.FileName}" });
                    }
                };

                target.IsEnabledChanged += isEnabled =>
                {
                    d.Invoke(() =>
                    {
                        if (isEnabled) Targets.Add(target);
                        else Targets.Remove(target);
                    });
                };

                target.Start();
            }
        }

        public void Dispose()
        {
            Task.WhenAll(_allTargets.Select(t => t.Stop())).Wait(30000);
        }

        private uint ConvertToUint(uint c, uint d) => 192 + (168 << 8) + (c << 16) + (d << 24);
    }
}
