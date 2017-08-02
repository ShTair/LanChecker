using LanChecker.Models;
using LanChecker.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
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

        private Dictionary<string, DeviceViewModel> _devices;

        public bool IsStoped { get; private set; }
        private Task _running;

        #region properties

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<DeviceViewModel> Devices { get; }

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

        public MainViewModel(Dictionary<string, DeviceInfo> names)
        {
            _d = Dispatcher.CurrentDispatcher;

            _mlq = new MultiLaneQueue<Action>(4);
            _mlq.CountChanged += () => QueueCount = _mlq.Count;

            if (names == null) names = new Dictionary<string, DeviceInfo>();
            Directory.CreateDirectory("log");

            _devices = new Dictionary<string, DeviceViewModel>();
            Devices = new ObservableCollection<DeviceViewModel>();

            foreach (var item in from line in Settings.Default.LastDevices.Split('\n')
                                 let sp = line.Split('\t')
                                 where sp.Length == 3
                                 select sp)
            {
                var lastReach = DateTime.FromBinary(long.Parse(item[1]));
                var lastIn = DateTime.FromBinary(long.Parse(item[2]));
                var device = CreateDeviceViewModel(names, item[0], lastReach, lastIn);
            }

            ReachCount = Devices.Count(t => t.IsIn);

            _allTargets = GenerateIps().Distinct().Select(t => new TargetViewModel(t)).ToDictionary(t => t.IPAddress);
            _inTargets = new Dictionary<int, TargetViewModel>();

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
                            if (target.Status <= 1)
                            {
                                _mlq.Enqueue(() => CheckInProcess(target, 0), 0);
                            }
                            else
                            {
                                _mlq.Enqueue(() => CheckInProcess(target, 2), 2);
                            }
                        }
                    }
                }
            }

            foreach (var target in _allTargets.Values)
            {
                target.Reached += mac =>
                {
                    lock (_devices)
                    {
                        var device = CreateDeviceViewModel(names, mac, DateTime.MinValue, DateTime.Now);
                        device.Reach(target.IPAddress);
                    }
                };
                target.Unreached += mac =>
                {
                    lock (_devices)
                    {
                        DeviceViewModel device;
                        if (_devices.TryGetValue(mac, out device))
                        {
                            device.Unreach(target.IPAddress);
                        }
                    }
                };

                _mlq.Enqueue(() => CheckAllProcess(target, 3), 3);
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

        private DeviceViewModel CreateDeviceViewModel(Dictionary<string, DeviceInfo> names, string mac, DateTime lastReach, DateTime lastIn)
        {
            DeviceViewModel device;
            if (!_devices.TryGetValue(mac, out device))
            {
                DeviceInfo di;
                if (!names.TryGetValue(mac, out di))
                {
                    di = new DeviceInfo(mac, null, "Unknown");
                }
                device = new DeviceViewModel(mac, di.Category, di.Name);
                device.Expired += () =>
                {
                    lock (_devices)
                    {
                        _devices.Remove(mac);
                        _d.Invoke(() => Devices.Remove(device));
                    }
                };

                if (lastReach != DateTime.MinValue) device.Start(lastReach, lastIn);

                device.IsInChanged += (isin, time) =>
                {
                    lock (_counterLock)
                    {
                        ReachCount += isin ? 1 : -1;
                        File.AppendAllLines($"log\\log_{device.Category}.txt", new[] { $"{DateTime.Now:yyyy/MM/dd_HH:mm:ss}\t{time:yyyy/MM/dd_HH:mm:ss}\t{isin}\t{device.MacAddress}\t{device.Name}\t{device.Category}" });
                    }
                };

                if (lastReach == DateTime.MinValue) device.Start(DateTime.Now, DateTime.Now);

                _devices.Add(mac, device);
                _d.Invoke(() => Devices.Add(device));
            }

            return device;
        }

        public void Stop()
        {
            IsStoped = true;
            Settings.Default.Last = string.Join("\n", _allTargets.Values.Select(t => t.Serialize()));
            Settings.Default.LastDevices = string.Join("\n", _devices.Values.Select(t => t.MacAddress + "\t" + t.LastReach.ToBinary() + "\t" + t.LastIn.ToBinary()));
            Settings.Default.Save();
            _running.Wait();
        }

        private uint ConvertToUint(uint c, uint d) => 192 + (168 << 8) + (c << 16) + (d << 24);

        private IEnumerable<uint> GenerateIps()
        {
            var ips = Dns.GetHostAddresses(Dns.GetHostName());
            foreach (var ip in ips)
            {
                var ipb = ip.GetAddressBytes();
                if (ipb.Length != 4) continue;
                if (ipb[0] != 192 || ipb[1] != 168) continue;

                foreach (var sub in Enumerable.Range(1, 254))
                {
                    yield return ConvertToUint(ipb[2], (uint)sub);
                }
            }
        }
    }
}
