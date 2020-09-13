using System;
using System.Linq;
using System.IO;
using Newtonsoft.Json;

namespace MhwModManager
{
    public class ModInfo
    {
        #region Public Fields

        [NonSerialized] public string path;

        #endregion Public Fields

        #region Public Properties

        public bool activated { get; set; }
        public string category { get; set; }
        public string name { get; set; }
        public int order { get; set; }
        public bool root { get; set; }

        #endregion Public Properties

        #region Public Methods

        public void GenInfo(string folderName, int? index = null, bool rootMod = false)
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
                    root = rootMod;

                    App.logStream.Warning($"Mod {name} info not found");

                    ParseSettingsJSON();
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
                    activated = sets.activated;
                    order = sets.order;
                    category = sets.category;
                    root = sets.root;
                    if (sets.category == null)
                    {
                        // If old version of info mod, add new infos
                        category = "None";
                        ParseSettingsJSON();
                    }

                    App.logStream.Log($"Mod {name} info found");
                }
            }
            catch (Exception e) { App.logStream.Error(e.ToString()); }
        }

        public void ParseSettingsJSON()
        {
            try
            {
                App.logStream.Log("Mod info updated");
                using (StreamWriter file = new StreamWriter(Path.Combine(App.ModsPath, path, "mod.info")))
                {
                    file.Write(JsonConvert.SerializeObject(this, Formatting.Indented));
                    file.Close();
                }
            }
            catch (Exception e) { App.logStream.Error(e.ToString()); }
        }

        #endregion Public Methods
    }
}