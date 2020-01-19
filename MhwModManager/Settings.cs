using System.IO;
using Newtonsoft.Json;

namespace MhwModManager
{
    internal class Settings
    {
        public Settings settings { get; set; }
        public bool dark_mode { get; set; }
        public string mhw_path { get; set; }

        public Settings()
        {
            settings.dark_mode = false;
            settings.mhw_path = @"C:\Program Files (x86)\Steam\steamapps\common\Monster Hunter World\";

            if (!File.Exists("config.json"))
            {
                File.Create("config.json").Close();
                ParseSettingsJSON();
            }
            else
            {
                using (StreamReader file = new StreamReader("config.json"))
                {
                    settings = JsonConvert.DeserializeObject<Settings>(file.ReadToEnd());
                    file.Close();
                }
            }
        }

        public void ParseSettingsJSON()
        {
            using (StreamWriter file = new StreamWriter("config.json"))
            {
                file.Write(JsonConvert.SerializeObject(settings));
                file.Close();
            }
        }
    }
}