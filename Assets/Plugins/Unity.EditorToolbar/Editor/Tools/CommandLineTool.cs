using System;
using System.Diagnostics;
using System.Extensions;
using System.Linq;
using UnityEditor.GUIExtensions;
using UnityEngine;

namespace UnityEditor.Toolbars
{

    public class CommandLineTool : EditorToolbarMenuButton
    {

        private GUIContent icon;

        static CommandLineSettings Settings
        {
            get => CommandLineSettings.Settings;
        }

        public override GUIContent Icon
        {
            get
            {
                if (icon == null)
                {
                    var group = GetDefaultGroup();

                    icon = GetGUIConent(group.icon, group.name, group.tooltip);
                }
                return icon;
            }
        }


        public override void OnGUI()
        {
            base.OnGUI();
            Settings.Groups.Where(group =>
            {
                if (!group.isDefault)
                {
                    GUIMenuButton(GetGUIConent(group.icon, group.name, group.tooltip),
                        () =>
                        {
                            var defaultItem = group.items.Where(o => o.isDeffault).FirstOrDefault();
                            if (defaultItem != null)
                            {
                                Execute(defaultItem);
                            }
                            else
                            {
                                ShowGroupMenu(group);
                            }
                        },
                        () =>
                        {
                            ShowGroupMenu(group);
                        });
                }
                return false;
            }).ToArray();

        }

        public override void OnClick()
        {
            var group = GetDefaultGroup();
            var defaultItem = group.items.Where(o => o.isDeffault).FirstOrDefault();
            if (defaultItem != null)
            {
                Execute(defaultItem);
            }
            else
            {
                ShowGroupMenu(group);
            }
        }

        CommandLineSettings.CommandLineGroup GetDefaultGroup()
        {
            var group = Settings.Groups.Where(o => o.isDefault).FirstOrDefault();
            if (group == null)
            {
                if (Settings.Groups.Length > 0)
                    Settings.Groups[0].isDefault = true;
            }
            return group;
        }

        public override void OnMenu()
        {
            GenericMenu menu = new GenericMenu();

            var defaultGroup = GetDefaultGroup();
            CreateMenu(menu, defaultGroup);

            menu.AddSeparator(string.Empty);

            menu.AddItem(new GUIContent("Settings"), false, () =>
            {
                EditorWindow.GetWindow<CommandLineSettingsWindow>().Show();
            });
            menu.ShowAsContext();
        }


        void ShowGroupMenu(CommandLineSettings.CommandLineGroup group)
        {
            if (group == null)
                return;
            GenericMenu menu = new GenericMenu();

            CreateMenu(menu, group);

            menu.ShowAsContext();
        }

        void CreateMenu(GenericMenu menu, CommandLineSettings.CommandLineGroup group)
        {
            if (group.items == null)
                group.items = new CommandLineSettings.CommandLineItem[0];
            foreach (var folder in group.items)
            {
                if (folder.separator)
                {
                    menu.AddSeparator(folder.name);
                }
                else
                {
                    menu.AddItem(new GUIContent(folder.name), false, (state) =>
                    {
                        Execute((CommandLineSettings.CommandLineItem)state);
                    }, folder);
                }
            }
        }

        static void Execute(CommandLineSettings.CommandLineItem item)
        {
            Process.Start(item.file, item.argument);
        }

        [Serializable]
        class CommandLineSettings
        {

            public CommandLineGroup[] groups;


            private static Internal.SettingsProvider provider;
            internal static Internal.SettingsProvider Provider
            {
                get
                {
                    if (provider == null)
                    {
                        provider = new Internal.SettingsProvider(typeof(CommandLineSettings), EditorToolbar.PackageName, false, true) { FileName = "CommandLineSettings.json" };
                        provider.OnLoadAfter = (o) =>
                        {
                            CommandLineSettings settings = (CommandLineSettings)o;
                            if (settings.groups == null || settings.groups.Length == 0)
                            {
                                settings.groups = new CommandLineGroup[] { new CommandLineGroup() { isDefault = true, name = "Command" } };
                            }

                        };
                    }
                    return provider;
                }
            }

            public static CommandLineSettings Settings
            {
                get => (CommandLineSettings)Provider.Settings;
            }

            public CommandLineGroup[] Groups
            {
                get => Settings.groups ?? new CommandLineGroup[0];
                set => Settings.groups = value;
            }

            [Serializable]
            public class CommandLineGroup
            {
                public CommandLineItem[] items;
                public bool isDefault;
                public string icon;
                public string name;
                public string tooltip;
            }

            [Serializable]
            public class CommandLineItem
            {
                public string name;
                public string file;
                public string argument;
                public bool separator;
                public bool isDeffault;
            }
        }


        class CommandLineSettingsWindow : EditorWindow
        {
            private void OnEnable()
            {
                titleContent = new GUIContent("Command Line Settings");
            }

            private void OnGUI()
            {
                using (var checker = new EditorGUI.ChangeCheckScope())
                {

                    foreach (var group in Settings.Groups)
                    {
                        var items = group.items;
                        if (items == null)
                            items = new CommandLineSettings.CommandLineItem[0];

                        using (new GUILayout.HorizontalScope())
                        {
                            using (new EditorGUI.DisabledGroupScope(group.isDefault))
                            {
                                bool isDefault = group.isDefault;
                                group.isDefault = EditorGUILayout.Toggle(group.isDefault, GUILayout.ExpandWidth(false), GUILayout.Width(16));
                                if (isDefault != group.isDefault && group.isDefault)
                                {
                                    Settings.groups.Where(o =>
                                    {
                                        if (o != group)
                                            o.isDefault = false;
                                        return false;
                                    }).ToArray();
                                }
                                GUILayout.Label("Default");

                                if (GUILayout.Button("-", GUILayout.ExpandWidth(false)))
                                {
                                    if (EditorUtility.DisplayDialog("delete", "Delete group", "ok", "cancel"))
                                    {
                                        ArrayUtility.Remove(ref Settings.groups, group);
                                    }
                                }
                            }
                        }

                        group.name = EditorGUILayout.DelayedTextField(group.name);
                        group.tooltip = EditorGUILayout.DelayedTextField("Tooltip", group.tooltip);
                        group.icon = EditorGUILayout.DelayedTextField("Icon", group.icon);

                        using (new EditorGUILayoutx.Scopes.IndentLevelVerticalScope())
                        {
                            for (int i = 0; i < items.Length; i++)
                            {
                                var item = items[i];

                                using (new GUILayout.HorizontalScope())
                                {
                                    if (item.separator)
                                    {
                                        GUILayout.Label("Separator");
                                    }
                                    else
                                    {
                                        item.name = EditorGUILayout.TextField("Name", item.name);
                                    }

                                    //GUILayout.FlexibleSpace();
                                    var enabled = GUI.enabled;
                                    GUI.enabled = i > 0;
                                    if (GUILayout.Button("↑", "label", GUILayout.ExpandWidth(false)))
                                    {
                                        var tmp = items[i];
                                        items[i] = items[i - 1];
                                        items[i - 1] = tmp;
                                    }

                                    GUI.enabled = i < items.Length - 1;
                                    if (GUILayout.Button("↓", "label", GUILayout.ExpandWidth(false)))
                                    {
                                        var tmp = items[i];
                                        items[i] = items[i + 1];
                                        items[i + 1] = tmp;
                                    }
                                    GUI.enabled = enabled;

                                    if (GUILayout.Button("-", "label", GUILayout.ExpandWidth(false)))
                                    {
                                        ArrayUtility.RemoveAt(ref items, i);
                                        i--;
                                        group.items = items;
                                    }
                                }


                                using (new EditorGUILayoutx.Scopes.IndentLevelVerticalScope())
                                {
                                    if (!item.separator)
                                    {

                                        if (item.isDeffault != EditorGUILayout.Toggle("Default", item.isDeffault))
                                        {
                                            item.isDeffault = !item.isDeffault;
                                            Array.ForEach(items, o =>
                                             {
                                                 if (o != item)
                                                 {
                                                     o.isDeffault = false;
                                                 }
                                             });
                                        }


                                        item.file = EditorGUILayout.TextField("File", item.file);
                                        item.argument = EditorGUILayout.TextField("Argument", item.argument);
                                    }
                                }

                            }

                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.FlexibleSpace();
                                if (GUILayout.Button("+", GUILayout.ExpandWidth(false)))
                                {
                                    GenericMenu menu = new GenericMenu();
                                    menu.AddItem(new GUIContent("Item"), false, () =>
                                    {
                                        var folder = new CommandLineSettings.CommandLineItem();
                                        ArrayUtility.Add(ref items, folder);
                                        group.items = items;
                                        CommandLineSettings.Provider.Save();
                                    });

                                    menu.AddItem(new GUIContent("Separator"), false, () =>
                                    {
                                        var folder = new CommandLineSettings.CommandLineItem();
                                        folder.separator = true;
                                        ArrayUtility.Add(ref items, folder);
                                        group.items = items;
                                        CommandLineSettings.Provider.Save();
                                    });

                                    menu.ShowAsContext();
                                }
                            }


                        }
                    }

                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Add Group", GUILayout.MinWidth(100)))
                        {
                            ArrayUtility.Add(ref Settings.groups, new CommandLineSettings.CommandLineGroup() { name = "Group" });
                            GUILayout.FlexibleSpace();
                        }
                        GUILayout.FlexibleSpace();
                    }

                    if (checker.changed)
                    {
                        CommandLineSettings.Provider.Save();
                    }
                }
            }
        }

    }

}