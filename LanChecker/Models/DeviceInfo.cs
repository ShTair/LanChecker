using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LanChecker.Models
{
    class DeviceInfo
    {
        public string MacAddress { get; }

        public string Name { get; }

        public string FileName { get; }

        public DeviceInfo(string mac, string name, string fileName)
        {
            MacAddress = mac;
            Name = name;
            FileName = string.IsNullOrWhiteSpace(fileName) ? "Unknown" : fileName;
        }

        private static Regex _r = new Regex(@"^(.+?)\t(.*?)\t(.*?)$", RegexOptions.Compiled);

        public static IEnumerable<DeviceInfo> Load(string path)
        {
            return File.ReadLines(path, Encoding.Default).Select(line => _r.Match(line)).Where(m => m.Success).Select(m => new DeviceInfo(m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value));
        }
    }
}
