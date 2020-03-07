using System;
using System.Linq;
using System.IO;
using Newtonsoft.Json;

namespace MhwModManager
{
    public class ModInfo
    {
        public bool activated { get; set; }
        public string name { get; set; }
        public int order { get; set; }

        public string category { get; set; }

        [NonSerialized] public string path;

        public void GenInfo(string folderName, int? index = null)
        {
            try
            {
                path = folderName;

                if (!File.Exists(Path.Combine(App.ModsPath, path, "mod.info")))
                {
                    activated = false;
                    if (index != null)
                        order = index.Value;
                    else
                        order = App.Mods.Count();

                    name = folderName;
                    category = "None";

                    App.logStream.Warning($"Mod {name} info not found");

                    ParseSettingsJSON(folderName);
                }
                else
                {
                    ModInfo sets;
                    using (StreamReader file = new StreamReader(Path.Combine(App.ModsPath, path, "mod.info")))
                    {
                        sets = JsonConvert.DeserializeObject<ModInfo>(file.ReadToEnd());
                        file.Close();
                    }

                    name = sets.name;
                    category = sets.category;
                    activated = sets.activated;
                    order = sets.order;

                    App.logStream.Log($"Mod {name} info found");
                }
            }
            catch (Exception e) { App.logStream.Error(e.ToString()); }
        }

        public void ParseSettingsJSON(string path)
        {
            try
            {
                App.logStream.Log("Mod info updated");
                using (StreamWriter file = new StreamWriter(Path.Combine(path, "mod.info")))
                {
                    file.Write(JsonConvert.SerializeObject(this, Formatting.Indented));
                    file.Close();
                }
            }
            catch (Exception e) { App.logStream.Error(e.ToString()); }
        }
    }
}