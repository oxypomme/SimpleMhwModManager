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

        public void GenInfo(string path, int? index = null)
        {
            try
            {
                if (!File.Exists(Path.Combine(path, "mod.info")))
                {
                    activated = false;
                    if (index != null)
                        order = index.Value;
                    else
                        order = App.Mods.Count();

                    // Get the name of the extracted folder (without the .zip at the end), not the
                    // full path
                    var foldName = path.Split('\\');
                    name = foldName[foldName.GetLength(0) - 1].Split('.')[0];

                    App.logStream.Warning($"Mod {name} info not found");

                    ParseSettingsJSON(path);
                }
                else
                {
                    ModInfo sets;
                    using (StreamReader file = new StreamReader(Path.Combine(path, "mod.info")))
                    {
                        sets = JsonConvert.DeserializeObject<ModInfo>(file.ReadToEnd());
                        file.Close();
                    }

                    activated = sets.activated;
                    order = sets.order;
                    name = sets.name;

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