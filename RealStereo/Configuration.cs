using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace RealStereo
{
    class Configuration
    {
        public Dictionary<string, List<PointConfiguration>> Rooms { get; set; } = new Dictionary<string, List<PointConfiguration>>();
        public string SelectedRoom;

        [JsonIgnore]
        private static Configuration Config = null;

        public static Configuration GetInstance()
        {
            if (Config != null)
            {
                return Config;
            }

            string basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string serialized;

            try
            {
                serialized = File.ReadAllText(basePath + "\\realstereo.json");
            }
            catch
            {
                serialized = "{}";
            }

            Config = JsonConvert.DeserializeObject<Configuration>(serialized);

            return Config;
        }

        public void Save()
        {
            string basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string serialized = JsonConvert.SerializeObject(this);
            File.WriteAllText(basePath + "\\realstereo.json", serialized);
        }
    }
}
