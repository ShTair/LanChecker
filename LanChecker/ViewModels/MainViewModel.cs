using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace LanChecker.ViewModels
{
    class MainViewModel : IDisposable
    {
        public ObservableCollection<TargetViewModel> Targets { get; }

        public MainViewModel(uint sub, uint start, int count)
        {
            Targets = new ObservableCollection<TargetViewModel>(GetTargetHosts(sub, start, count).Select(t => new TargetViewModel(t)));
            foreach (var target in Targets) target.Start();
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
