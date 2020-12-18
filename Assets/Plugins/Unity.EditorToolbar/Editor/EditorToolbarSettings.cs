using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace UnityEditor.Toolbars
{
    /// <summary>
    /// 工具条配置
    /// </summary>
    [Serializable]
    public class EditorToolbarSettings
    {


        private static Internal.SettingsProvider provider;
        static Internal.SettingsProvider Provider
        {
            get
            {
                if (provider == null)
                    provider = new Internal.SettingsProvider(typeof(EditorToolbarSettings), EditorToolbar.PackageName, false, true) { FileName = "Settings.json" };
                return provider;
            }
        }

        public static EditorToolbarSettings Settings
        {
            get
            {
                EditorToolbarSettings settings = (EditorToolbarSettings)Provider.Settings;

                return settings;
            }
        }

        public void Load(IEnumerable<EditorToolbar> items)
        {
            Provider.Load();
            foreach (var item in items)
            {
                var itemConfig = this.items.Where(o => o.type == item.GetType().AssemblyQualifiedName)
                    .FirstOrDefault();
                if (itemConfig == null)
                    continue;
                JsonUtility.FromJsonOverwrite(itemConfig.json, item);
            }
        }

        public void Save(IEnumerable<EditorToolbar> items)
        {
            this.items.Clear();
            foreach (var toolbar in items)
            {
                ToolbarItemConfig itemConfig = new ToolbarItemConfig()
                {
                    type = toolbar.GetType().AssemblyQualifiedName,
                    json = JsonUtility.ToJson(toolbar)
                }; 
                this.items.Add(itemConfig);
            }
            Provider.Save();
        }

        [SerializeField]
        List<ToolbarItemConfig> items = new List<ToolbarItemConfig>();


        [Serializable]
        class ToolbarItemConfig
        {
            public string type;
            public string json;
        }

    }
     

}
