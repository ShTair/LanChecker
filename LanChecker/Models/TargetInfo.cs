using Realms;
using System;

namespace LanChecker.Models
{
    class TargetInfo : RealmObject
    {
        [PrimaryKey]
        public int IPAddress { get; set; }

        public string MacAddress { get; set; }

        public DateTimeOffset LastReach { get; set; }

        public int Status { get; set; }
    }
}
