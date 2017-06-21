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

        public MainViewModel()
        {
            Targets = new ObservableCollection<TargetViewModel>(GetTargetHosts().Select(t => new TargetViewModel(t)));
            foreach (var target in Targets) target.Start();
        }

        public void Dispose()
        {
            Task.WhenAll(Targets.Select(t => t.Stop())).Wait(30000);
        }

        private IEnumerable<uint> GetTargetHosts()
        {
            uint v = 192 + (168 << 8) + (10 << 16);
            for (uint i = 0; i < 32; i++)
            {
                yield return v + ((11 + i) << 24);
            }
        }
    }
}
