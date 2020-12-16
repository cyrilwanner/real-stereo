using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace RealStereo.Config
{
    class Configuration
    {
        public Dictionary<string, List<PointConfiguration>> Rooms { get; set; } = new Dictionary<string, List<PointConfiguration>>();
        public string SelectedRoom;

        [JsonIgnore]
        private static Configuration config = null;

        /// <summary>
        /// Get the configuration instance.
        /// </summary>
        /// <returns>Configuration instance.</returns>
        public static Configuration GetInstance()
        {
            if (config != null)
            {
                return config;
            }

            // load the configuration from the appdata folder if it exists
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

            // deserialize the configuration into objects
            config = JsonConvert.DeserializeObject<Configuration>(serialized);

            return config;
        }

        /// <summary>
        /// Save the current configuration.
        /// </summary>
        public void Save()
        {
            string basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string serialized = JsonConvert.SerializeObject(this);
            File.WriteAllText(basePath + "\\realstereo.json", serialized);
        }
    }
}
