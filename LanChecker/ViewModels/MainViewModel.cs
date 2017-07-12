﻿using LanChecker.Models;
using LanChecker.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace LanChecker.ViewModels
{
    class MainViewModel : INotifyPropertyChanged
    {
        private Dispatcher _d;

        private object _counterLock = new object();

        private MultiLaneQueue<Action> _mlq;
        private Dictionary<int, TargetViewModel> _allTargets;
        private Dictionary<int, TargetViewModel> _inTargets;

        public bool IsStoped { get; private set; }
        private Task _running;

        #region properties

        public event PropertyChangedEventHandler PropertyChanged;

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

        #endregion

        public MainViewModel(uint sub, Dictionary<string, DeviceInfo> names)
        {
            _d = Dispatcher.CurrentDispatcher;

            _mlq = new MultiLaneQueue<Action>(4);
            _mlq.CountChanged += () => QueueCount = _mlq.Count;
            _inTargets = new Dictionary<int, TargetViewModel>();

            Targets = new ObservableCollection<TargetViewModel>();


            if (names == null) names = new Dictionary<string, DeviceInfo>();

            Directory.CreateDirectory("log");

            _allTargets = Enumerable.Range(1, 254).Select(t =>
            {
                var target = new TargetViewModel(ConvertToUint(sub, (uint)t), names);
                target.IsInChanged += (isin, time) =>
                {
                    lock (_counterLock)
                    {
                        ReachCount += isin ? 1 : -1;
                        File.AppendAllLines($"log\\log_{target.FileName}.txt", new[] { $"{DateTime.Now:yyyy/MM/dd_HH:mm:ss}\t{time:yyyy/MM/dd_HH:mm:ss}\t{target.IPAddress}\t{isin}\t{target.MacAddress}\t{target.Name}\t{target.FileName}" });
                    }
                };
                target.StatusChanged += (old, status) =>
                {
                    IEnumerable<TargetViewModel> getot()
                    {
                        return _inTargets.Values.Where(t2 => t2 != target && t2.MacAddress == target.MacAddress);
                    };

                    if (status == 0)
                    {
                        lock (_inTargets)
                        {
                            foreach (var t2 in getot().Where(t2 => t2.Status != 0))
                            {
                                t2.Out();
                            }
                        }
                    }
                    else if (status == 2)
                    {
                        lock (_inTargets)
                        {
                            if (getot().Where(t2 => t2.Status <= 1).Any())
                            {
                                target.Out();
                            }
                        }
                    }
                };

                _mlq.Enqueue(() => CheckAllProcess(target, 3), 3);
                return target;
            }).ToDictionary(t => t.IPAddress);

            foreach (var da in from line in Settings.Default.Last.Split('\n')
                               let sp = line.Split('\t')
                               where sp.Length == 4
                               let ip = int.Parse(sp[0])
                               select new { IP = ip, Line = line })
            {
                TargetViewModel target;
                if (!_allTargets.TryGetValue(da.IP, out target)) continue;
                target.Deserialize(da.Line);

                if (target.Status != 3)
                {
                    lock (_inTargets)
                    {
                        if (!_inTargets.ContainsKey(target.IPAddress))
                        {
                            _inTargets.Add(target.IPAddress, target);
                            _d.Invoke(() => Targets.Add(target));
                            _mlq.Enqueue(() => CheckInProcess(target, 1), 1);
                        }
                    }
                }
            }

            _running = Task.Run(RunChecking);
            _ = ReceivingDhcp();
        }

        private async Task RunChecking()
        {
            try
            {
                while (!IsStoped)
                {
                    var p = await _mlq.Dequeue();
                    p();
                }
            }
            catch (Exception exp)
            {
                MessageBox.Show("ARPループが終了しました\r\n" + exp);
            }
        }

        private void CheckInProcess(TargetViewModel target, int p)
        {
            Console.WriteLine($"Check IN {p} {target.IPAddress}");
            target.Check();

            if (target.Status == 3)
            {
                lock (_inTargets)
                {
                    _inTargets.Remove(target.IPAddress);
                    _d.Invoke(() => Targets.Remove(target));
                }
            }
            else
            {
                Task.Delay(TimeSpan.FromSeconds(target.Status == 2 ? 60 : 20)).ContinueWith(_ =>
                {
                    _mlq.Enqueue(() => CheckInProcess(target, target.Status == 0 ? 1 : 2), target.Status == 0 ? 1 : 2);
                });
            }
        }

        private void CheckAllProcess(TargetViewModel target, int p)
        {
            Console.WriteLine($"Check OT {p} {target.IPAddress}");
            target.Check();

            if (target.Status != 3)
            {
                lock (_inTargets)
                {
                    if (!_inTargets.ContainsKey(target.IPAddress))
                    {
                        _inTargets.Add(target.IPAddress, target);
                        _d.Invoke(() => Targets.Add(target));

                        Task.Delay(TimeSpan.FromSeconds(20)).ContinueWith(_ =>
                        {
                            _mlq.Enqueue(() => CheckInProcess(target, 1), 1);
                        });
                    }
                }
            }

            Task.Delay(TimeSpan.FromHours(1)).ContinueWith(_ =>
            {
                _mlq.Enqueue(() => CheckAllProcess(target, 3), 3);
            });
        }

        private async Task ReceivingDhcp()
        {
            try
            {
                var udp = new UdpClient(68);
                while (true)
                {
                    var result = await udp.ReceiveAsync();
                    var ip = result.Buffer[19];
                    var mac = string.Join(":", result.Buffer.Skip(28).Take(6).Select(t => t.ToString("X2")));
                    Console.WriteLine($"DHCP {mac} {ip}");

                    if (ip != 0)
                    {
                        lock (_inTargets)
                        {
                            TargetViewModel target;
                            if (!_inTargets.TryGetValue(ip, out target))
                            {
                                if (_allTargets.TryGetValue(ip, out target))
                                {
                                    _inTargets.Add(target.IPAddress, target);
                                    _d.Invoke(() => Targets.Add(target));
                                    Task.Delay(TimeSpan.FromSeconds(5)).ContinueWith(_ =>
                                    {
                                        _mlq.Enqueue(() => CheckInProcess(target, 0), 0);
                                    });
                                }
                            }

                            if (target != null)
                            {
                                target.Find(mac);
                            }
                        }
                    }
                }
            }
            catch (Exception exp)
            {
                MessageBox.Show("UDPが終了しました\r\n" + exp);
            }
        }

        public void Stop()
        {
            IsStoped = true;
            Settings.Default.Last = string.Join("\n", _allTargets.Values.Select(t => t.Serialize()));
            Settings.Default.Save();
            _running.Wait();
        }

        private uint ConvertToUint(uint c, uint d) => 192 + (168 << 8) + (c << 16) + (d << 24);
    }
}
