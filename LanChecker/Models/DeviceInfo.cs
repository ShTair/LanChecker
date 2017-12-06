using Realms;
using System;

namespace LanChecker.Models
{
    class DeviceInfo : RealmObject
    {
        [PrimaryKey]
        public string MacAddress { get; set; }

        public string Category { get; set; }

        public string Name { get; set; }

        public DateTimeOffset LastReach { get; set; }

        public DateTimeOffset LastIn { get; set; }
    }
}
