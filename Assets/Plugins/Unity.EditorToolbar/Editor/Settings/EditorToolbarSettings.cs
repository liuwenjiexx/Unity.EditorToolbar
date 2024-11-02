using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;

namespace EditorToolbars
{
    /// <summary>
    /// 工具条设置
    /// </summary>
    [Serializable]
    public class EditorToolbarSettings
    {

        private static SettingsProvider provider;

        static SettingsProvider Provider
        {
            get
            {
                if (provider == null)
                {
                    provider = new SettingsProvider(typeof(EditorToolbarSettings), EditorToolbarUtility.UnityPackageName, false, true) { FileName = "Settings.json" };
                    provider.OnLoadAfter = OnLoad;
                }
                return provider;
            }
        }

        public static EditorToolbarSettings Settings
        {
            get
            {
                var settings = (EditorToolbarSettings)Provider.Settings;
                EditorToolbar.Initalize();
                return settings;
            }
        }

        [SerializeField]
        List<ToolbarGroup> groups = new List<ToolbarGroup>();
        public static List<ToolbarGroup> Groups { get => Settings.groups; set => Provider.Set(nameof(Groups), ref Settings.groups, value); }

        public static IEnumerable<ToolbarGroup> AllGroups => Groups.Concat(EditorToolbarUserSettings.Groups);

        public static List<ToolbarGroup> CurrentGroups => EditorToolbarUtility.IsProjectWorkspace ? Groups : EditorToolbarUserSettings.Groups;

        public static ToolbarGroup DefaultGroup
        {
            get
            {
                var g = Groups.FirstOrDefault(o => o.Name == EditorToolbar.GROUP_DEFAULT);
                if (g == null)
                {
                    g = new ToolbarGroup();
                    g.Name = EditorToolbar.GROUP_DEFAULT;
                    g.Position = ToolbarPosition.LeftToolbar;
                    Groups.Add(g);
                }
                return g;
            }
        }



        public void Load()
        {
            var oldGroups = Groups;

            Provider.Load();

            foreach (var group in Groups)
            {
                var oldGroup = oldGroups.FirstOrDefault(o => o.Name == group.Name);
                if (oldGroup != group)
                {
                    group.Icon = oldGroup.Icon;
                    group.Text = oldGroup.Text;
                    group.Tooltip = oldGroup.Tooltip;
                    group.Description = oldGroup.Description;

                }
            }

            Refresh();
        }

        private void Refresh()
        {
            var tools = EditorToolbar.tools;

            //Debug.Log("load before " + EditorToolbarSettings.Groups.SelectMany(o => o.items).Count());

            foreach (var tool in tools)
            {
                //修正配置
                ToolbarConfig newToolConfig = null;
                if (!string.IsNullOrEmpty(tool.Id))
                {
                    foreach (var group in AllGroups)
                    {
                        foreach (var item in group.items)
                        {
                            if (item.Id == tool.Id /*&& item.Initialized*/)
                            {
                                newToolConfig = item;
                                newToolConfig.Target = tool;
                                tool.Group = group;
                                break;
                            }
                        }
                        if (newToolConfig != null)
                            break;
                    }
                }

                if (tool.Group == null)
                    tool.Group = DefaultGroup;


                if (newToolConfig != null)
                {
                    //清除重复的
                    foreach (var group in AllGroups)
                    {
                        for (int i = 0; i < group.items.Count; i++)
                        {
                            var item = group.items[i];
                            if (newToolConfig != item)
                            {
                                if (item.Target == tool || item.Id == tool.Id)
                                {
                                    group.items.RemoveAt(i);
                                    i--;
                                    item.Target = null;
                                    continue;
                                }
                            }
                        }
                    }
                }
                else
                {
                    newToolConfig = EditorToolbarUtility.FindToolConfig(tool, out var g);
                    if (newToolConfig != null && !newToolConfig.Initialized)
                    {
                        newToolConfig.Initalize();
                    }
                }
            }


            foreach (var group in AllGroups)
            {
                for (int i = group.items.Count - 1; i >= 0; i--)
                {
                    var toolConfig = group.items[i];

                    //清理手动创建的
                    if (toolConfig.ManualCreated)
                    {
                        toolConfig.Target = null;
                        toolConfig.InstanceCreated = false;
                    }
                    else
                    {

                        //if (toolConfig.Target != null)
                        //{
                        //    if (!string.IsNullOrEmpty(toolConfig.Target.Id))
                        //    {
                        //        var newConfig = FindToolConfig(toolConfig.Target.Id, out var newGroup);
                        //        if (newConfig != null && toolConfig != newConfig)
                        //        {
                        //            newConfig.Target = toolConfig.Target;
                        //            group.items.RemoveAt(i);
                        //            i--;
                        //        }
                        //    }
                        //}
                    }
                }
            }




            foreach (var tool in tools)
            {
                if (tool.manualCreated)
                {
                    continue;
                }
                var toolConfig = EditorToolbarUtility.FindToolConfig(tool.Id, out var group);

                if (toolConfig == null)
                {
                    if (tool.Group != null)
                    {
                        tool.Group = tool.Group.Name;
                    }
                    else
                    {
                        tool.Group = DefaultGroup;
                    }
                }
                else
                {
                    if (toolConfig != null && toolConfig.Initialized)
                    {
                        EditorJsonUtility.FromJsonOverwrite(toolConfig.Data, tool);
                        tool.Group = group;
                        toolConfig.Target = tool;
                        toolConfig.InstanceCreated = true;
                        toolConfig.UpdateTarget();
                    }
                }
            }

            //生成手动创建的工具
            foreach (var group in AllGroups)
            {
                for (int j = 0; j < group.items.Count; j++)
                {
                    var toolConfig = group.items[j];

                    if (toolConfig.ManualCreated && toolConfig.Initialized)
                    {
                        EditorToolbar tool = toolConfig.GetOrCreateTarget();

                        if (tool != null)
                        {
                            tool.Group = group;
                        }

                    }
                }
            }

            for (int i = groups.Count - 1; i >= 0; i--)
            {
                var g = groups[i];

                for (int j = g.items.Count - 1; j >= 0; j--)
                {
                    var toolConfig = g.items[j];
                    if (!toolConfig.ManualCreated)
                    {
                        //移除自动生成的 null 工具
                        if (toolConfig.target == null)
                        {
                            g.items.RemoveAt(j);
                            continue;
                        }
                    }
                }


                //移除空自动组
                if (!g.manualCreated)
                {
                    if (g.items.Count == 0)
                    {
                        groups.RemoveAt(i);
                        continue;
                    }
                }

                //排序  
                //for (int j = 0; j < g.items.Count && j < g.Items.Count; j++)
                //{
                //    int index = g.Items.FindIndex(o => o.Id == g.items[j].id);
                //    if (index < 0)
                //    {
                //        continue;
                //    }
                //    if (index != j)
                //    {
                //        var tmp = g.Items[j];
                //        g.Items[j] = g.Items[index];
                //        g.Items[index] = tmp;
                //    }
                //}
                //g.items.Sort((a, b) => (a.target == null ? 0 : a.target.Order) - (b.target == null ? 0 : b.target.Order));
            }

            EditorToolbarUserSettings.Settings.Load();


            //Debug.Log("load after " + EditorToolbarSettings.Groups.SelectMany(o => o.items).Count());
        }



        public static void Save()
        {
            //Debug.Log("save before " + Groups.SelectMany(o => o.items).Count());

            foreach (var toolConfig in AllGroups.SelectMany(o => o.items))
            {
                var target = toolConfig.Target;
                if (target != null)
                {
                    if (!toolConfig.Initialized)
                    {
                        toolConfig.Initalize();
                    }
                    toolConfig.Type = target.GetType().AssemblyQualifiedName;
                    toolConfig.Data = EditorJsonUtility.ToJson(target);
                }
            }

            foreach (var group in AllGroups)
            {
                for (int i = group.items.Count - 1; i >= 0; i--)
                {
                    var item = group.items[i];
                    if (string.IsNullOrEmpty(item.Id))
                    {
                        group.items.RemoveAt(i);
                        continue;
                    }
                }
            }

            for (int i = 0; i < Groups.Count; i++)
            {
                var group = Groups[i];
                //移除自动空组
                if (!group.manualCreated && group.items.Count == 0)
                {
                    Groups.RemoveAt(i);
                    i--;
                }
            }

            //List<ToolbarConfig> newConfigs = new List<ToolbarConfig>();
            //foreach (var tool in tools)
            //{
            //    ToolbarConfig itemConfig = new ToolbarConfig()
            //    {
            //        id = tool.Id,
            //        type = tool.GetType().AssemblyQualifiedName,
            //        data = EditorJsonUtility.ToJson(tool),
            //        manualCreated = tool.manualCreated,
            //    };

            //    newConfigs.Add(itemConfig);
            //}

            //for (int i = 0; i < groups.Count; i++)
            //{
            //    var group = groups[i];
            //    for (int j = 0; j < group.items.Count; j++)
            //    {
            //        var item = group.items[j];
            //        var newConfig = newConfigs.FirstOrDefault(o => o.id == item.id);
            //        if (newConfig != null)
            //        {
            //            group.items[j] = newConfig;
            //            //var tool = tools.Find(o => o.Id == newConfig.id);
            //            //tool.Group = group;
            //            //newConfigs.Remove(newConfig);
            //        }
            //        else
            //        {
            //            if (!item.manualCreated)
            //            {
            //                group.items.RemoveAt(j);
            //                j--;
            //            }
            //        }
            //    }
            //}

            //if (newConfigs.Count > 0)
            //{
            //    foreach (var newConfig in newConfigs)
            //    {
            //        var tool = tools.Find(o => o.Id == newConfig.id);
            //        var group = FindGroup(tool.Group.Name);
            //        if (group == null)
            //        {
            //            group = tool.Group;
            //            group.items.Clear();
            //            groups.Add(group);
            //        }
            //        group.items.Add(newConfig);
            //    }
            //    newConfigs.Clear();
            //}

            //Debug.Log("save after " + Groups.SelectMany(o => o.items).Count());
            Provider.Save();


            EditorToolbarUserSettings.Settings.Save();
        }




        static void OnLoad(object obj)
        {
            foreach (var group in Groups)
            {
                foreach (var item in group.items)
                {
                    item.Initialized = true;
                }
            }

            if (EditorToolbar.initalized)
            {
                Settings.Refresh();
                EditorToolbarSettingsProvider.updateList = true;
            }
        }


    }


}
