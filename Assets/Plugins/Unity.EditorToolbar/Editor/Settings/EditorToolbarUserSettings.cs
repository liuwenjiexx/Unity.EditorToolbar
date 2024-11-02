using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;
using Unity;
using Unity.Serialization;

namespace EditorToolbars
{
    /// <summary>
    /// 工具条用户设置
    /// </summary>
    [Serializable]
    public class EditorToolbarUserSettings
    {
        private static SettingsProvider provider;

        internal static SettingsProvider Provider
        {
            get
            {
                if (provider == null)
                    provider = new SettingsProvider(typeof(EditorToolbarUserSettings), EditorToolbarUtility.UnityPackageName, false, false) { FileName = "Settings.json" };
                return provider;
            }
        }

        public static EditorToolbarUserSettings Settings => (EditorToolbarUserSettings)Provider.Settings;

        [SerializeField]
        private bool isProjectWorkspace = true;
        internal static bool IsProjectWorkspace { get => Settings.isProjectWorkspace; set => Provider.Set(nameof(IsProjectWorkspace), ref Settings.isProjectWorkspace, value); }

        [SerializeField]
        private List<ToolbarGroup> groups = new List<ToolbarGroup>();
        public static List<ToolbarGroup> Groups { get => Settings.groups; set => Provider.Set(nameof(Groups), ref Settings.groups, value); }

        public void Load()
        {
            Provider.Load();

            for (int i = 0; i < this.userConfigs.Count; i++)
            {
                var itemConfig = this.userConfigs[i];

                var toolConfig = EditorToolbarUtility.FindToolConfig(itemConfig.id, out var group);
                if (toolConfig == null)
                {
                    continue;
                }

                EditorToolbar tool = toolConfig.Target;
                if (tool == null)
                    continue;
                tool.UserSettings = itemConfig.userSettings.Target;
            }

        }


        public void Save()
        {
            for (int i = 0; i < userConfigs.Count; i++)
            {
                var toolUserConfig = userConfigs[i];
                var toolConfig = EditorToolbarUtility.FindToolConfig(toolUserConfig.id, out var group);
                if (toolConfig == null || (toolConfig.Target != null && toolConfig.Target.UserSettings == null))
                {
                    userConfigs.RemoveAt(i);
                    i--;
                    continue;
                }
            }

            foreach (var tool in EditorToolbarSettings.AllGroups.SelectMany(o => o.items).Select(o => o.target))
            {
                if (tool == null)
                    continue;
                var userSettings = tool.UserSettings;
                if (userSettings == null)
                    continue;

                ToolbarUserConfig toolUserConfig;

                toolUserConfig = userConfigs.FirstOrDefault(o => o.id == tool.Id);

                if (toolUserConfig == null)
                {
                    toolUserConfig = new ToolbarUserConfig()
                    {
                        id = tool.Id,
                        manualCreated = tool.manualCreated,
                    };
                    userConfigs.Add(toolUserConfig);
                }

                toolUserConfig.userSettings = new SerializableObject<object>(userSettings);
            }
            Provider.Save();
        }

        [SerializeField]
        List<ToolbarUserConfig> userConfigs = new List<ToolbarUserConfig>();


        public static object FindUserConfig(Type type)
        {
            var config = Settings.userConfigs.Find(o => o.userSettings.Target?.GetType() == type);
            return config.userSettings.Target;
        }



        [Serializable]
        class ToolbarUserConfig
        {
            public string id;
            public bool manualCreated;
            public SerializableObject<object> userSettings;
        }




    }


}
