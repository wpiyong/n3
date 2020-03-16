using SettingsLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace N3Imager.Model
{
    public class GlobalSettings : Settings
    {
        public GlobalSettings()
            : base("globalSettings.config")
        {
        }

        public int Version { get; set; }
        public string SaveFolderPath { get; set; }
        public string SaveFolderName { get; set; }
    }
}
