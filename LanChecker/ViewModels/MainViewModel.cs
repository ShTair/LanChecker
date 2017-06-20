using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LanChecker.ViewModels
{
    class MainViewModel : IDisposable
    {
        private List<TargetViewModel> _targets;

        public MainViewModel()
        {
            _targets = GetTargetHosts().Select(t =>
            {
                var target = new TargetViewModel(t);
                target.Start();
                return target;
            }).ToList();
        }

        public void Dispose()
        {
            Task.WhenAll(_targets.Select(t => t.Stop())).Wait();
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
