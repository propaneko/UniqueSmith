using System;
using System.Collections.Generic;

namespace UniqueSmith.Config
{
    public class SmithClassItemsObject {
        public string ClassName { get; set; }
        public string[] Itemlist { get; set; }
    }
    public class ModConfig
    {
        public string Mode = "Block";

        public List<SmithClassItemsObject> Blacklist;

        public List<SmithClassItemsObject> Allowlist;
    }
}
