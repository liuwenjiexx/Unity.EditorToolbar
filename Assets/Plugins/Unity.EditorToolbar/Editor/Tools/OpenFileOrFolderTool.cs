using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace UnityEditor.Toolbars
{
    class OpenFileOrFolderTool : EditorToolbarMenuButton
    {
        private GUIContent icon;


        public override GUIContent Icon
        {
            get
            {
                if (icon == null)
                {
#if UNITY_2020_1_OR_NEWER
                    var tmp = EditorGUIUtility.IconContent("Folder Icon");
#else
                     var tmp = EditorGUIUtility.IconContent("OpenedFolder On Icon");
#endif
                    icon = new GUIContent(tmp.image, "Open Folder");
                }
                return icon;
            }
        }

        
        public override void OnGUI()
        {
            base.OnGUI();
            OpenFileOrFolderSettings.Settings.Groups.Where(group =>
            {
                if (!group.isDefault)
                {
                    GUIButton(new GUIContent(group.name), () =>
                     {
                         ShowGroupMenu(group);
                     });
                }
                return false;
            }).ToArray();
        }

        public override void OnClick()
        {
            var group = OpenFileOrFolderSettings.Settings.Groups.Where(o => o.isDefault).FirstOrDefault();
            ShowGroupMenu(group);
        }

        public override void OnMenu()
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Settings"), false, () =>
            {
                EditorWindow.GetWindow<OpenFolderSettingsWindow>().Show();
            });
            menu.ShowAsContext();
        }

        void ShowGroupMenu(OpenFileOrFolderSettings.FolderGroup group)
        {
            if (group == null)
                return;
            GenericMenu menu = new GenericMenu();
            if (group.items == null)
                group.items = new OpenFileOrFolderSettings.FolderItem[0];
            foreach (var folder in group.items)
            {
                if (folder.separator)
                {
                    menu.AddSeparator(folder.name);
                }
                else if (folder.isFile)
                {
                    Regex includeRegex = new Regex(folder.includeFilePattern, RegexOptions.IgnoreCase);
                    Directory.GetFiles(folder.GetPath())
                        .Where(o =>
                        {
                            if (includeRegex.IsMatch(o))
                            {
                                menu.AddItem(new GUIContent(Path.GetFileName(o)), false, () =>
                                {
                                    OpenFileOrFolder(Path.GetFullPath(Path.GetFullPath(o)), async: true);
                                });
                            }
                            return false;
                        }).ToArray();
                }
                else
                {
                    menu.AddItem(new GUIContent(folder.name), false, (state) =>
                    {
                        OpenFileOrFolderSettings.FolderItem f = (OpenFileOrFolderSettings.FolderItem)state;
                        string path = f.GetPath();
                        if (!string.IsNullOrEmpty(f.guid))
                        {
                            if (!string.IsNullOrEmpty(path))
                            {
                                Object obj = AssetDatabase.LoadAssetAtPath<Object>(path);
                                if (obj)
                                    EditorGUIUtility.PingObject(obj);
                            }
                        }
                        else if (!string.IsNullOrEmpty(path))
                        {
                            OpenFileOrFolder(Path.GetFullPath(path), async: true);
                        }
                    }, folder);
                }
            }

            menu.ShowAsContext();
        }


        static void OpenFileOrFolder(string path, bool async = false)
        {
            if (Directory.Exists(path))
            {
                string firstFile = Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly).FirstOrDefault();

                if (string.IsNullOrEmpty(firstFile))
                {
                    firstFile = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly).FirstOrDefault();
                }

                if (string.IsNullOrEmpty(firstFile))
                {
                    EditorUtility.RevealInFinder(path);
                }
                else
                {
                    EditorUtility.RevealInFinder(firstFile);
                }

                //OpenFile(path, "explorer.exe", false);
            }
            else if (File.Exists(path))
            {
                OpenFile(path, null, false, async);
            }
            else
            {
                Debug.LogError("not exists file or folder " + path);
            }
        }


        public static bool OpenFile(string filePath, string openFile = null, bool useShell = false, bool async = false)
        {
            try
            {
                Process p = null;
                if (string.IsNullOrEmpty(openFile))
                {
                    p = Process.Start(filePath);
                }
                else
                {
                    if (useShell)
                    {
                        ProcessStartInfo startInfo = new ProcessStartInfo();
                        startInfo.FileName = openFile;
                        startInfo.Arguments = "\"" + filePath + "\"";
                        startInfo.CreateNoWindow = true;
                        startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        startInfo.UseShellExecute = true;

                        p = Process.Start(startInfo);
                    }
                    else
                    {
                        p = Process.Start(openFile, filePath);

                    }
                }

                if (!async)
                {
                    if (p != null)
                    {
                        p.WaitForExit();
                        if (!p.HasExited)
                            p.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError($"File: {openFile}");
                return false;
            }
            return true;
        }


        [Serializable]
        class OpenFileOrFolderSettings
        {
            public FolderGroup[] groups;

            private static Internal.SettingsProvider provider;
            internal static Internal.SettingsProvider Provider
            {
                get
                {
                    if (provider == null)
                    {
                        provider = new Internal.SettingsProvider(typeof(OpenFileOrFolderSettings), EditorToolbar.PackageName, false, true) { FileName = "OpenFileOrFolderSettings.json" };
                        provider.OnLoadAfter = (o) =>
                        {
                            OpenFileOrFolderSettings settings = (OpenFileOrFolderSettings)o;
                            if (settings.groups == null || settings.groups.Length == 0)
                            {
                                settings.groups = new FolderGroup[] { new FolderGroup() { isDefault = true, name = "Default" } };
                            }

                        };
                    }
                    return provider;
                }
            }

            public static OpenFileOrFolderSettings Settings { get => (OpenFileOrFolderSettings)Provider.Settings; }

            public FolderGroup[] Groups
            {
                get => Settings.groups ?? new FolderGroup[0];
                set => Settings.groups = value;
            }

            [Serializable]
            public class FolderGroup
            {
                public FolderItem[] items;
                public bool isDefault;
                public string name;
            }

            [Serializable]
            public class FolderItem
            {
                public string path;
                public string guid;
                public string name;
                public bool separator;
                public bool isFile;
                public string includeFilePattern;

                public string GetPath()
                {
                    if (!string.IsNullOrEmpty(guid))
                        return AssetDatabase.GUIDToAssetPath(guid);
                    return path;
                }
            }
        }


        class OpenFolderSettingsWindow : EditorWindow
        {
            private void OnEnable()
            {
                titleContent = new GUIContent("Open Folder Settings");
            }

            private void OnGUI()
            {
                using (var checker = new EditorGUI.ChangeCheckScope())
                {
                    foreach (var group in OpenFileOrFolderSettings.Settings.Groups)
                    {
                        var folders = group.items;
                        if (folders == null)
                            folders = new OpenFileOrFolderSettings.FolderItem[0];
                        string path;

                        using (new GUILayout.HorizontalScope())
                        {
                            using (new EditorGUI.DisabledGroupScope(group.isDefault))
                            {
                                bool isDefault = group.isDefault;
                                group.isDefault = EditorGUILayout.Toggle(group.isDefault, GUILayout.ExpandWidth(false), GUILayout.Width(16));
                                if (isDefault != group.isDefault && group.isDefault)
                                {
                                    OpenFileOrFolderSettings.Settings.groups.Where(o =>
                                    {
                                        if (o != group)
                                            o.isDefault = false;
                                        return false;
                                    }).ToArray();
                                }

                                group.name = EditorGUILayout.DelayedTextField(group.name);
                                if (GUILayout.Button("-", GUILayout.ExpandWidth(false)))
                                {
                                    if (EditorUtility.DisplayDialog("delete", "Delete group", "ok", "cancel"))
                                    {
                                        ArrayUtility.Remove(ref OpenFileOrFolderSettings.Settings.groups, group);
                                    }
                                }
                            }
                        }
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Space(16);
                            using (new GUILayout.VerticalScope())
                            {
                                for (int i = 0; i < folders.Length; i++)
                                {
                                    var folder = folders[i];
                                    path = null;

                                    if (!folder.separator)
                                    {
                                        if (!string.IsNullOrEmpty(folder.guid))
                                        {
                                            path = AssetDatabase.GUIDToAssetPath(folder.guid);
                                        }
                                        else
                                        {
                                            path = folder.path;
                                        }
                                    }

                                    using (new GUILayout.HorizontalScope())
                                    {
                                        if (folder.separator)
                                        {
                                            GUILayout.Label("Separator");
                                        }
                                        else
                                        {
                                            if (!folder.separator)
                                            {
                                                folder.name = EditorGUILayout.TextField(folder.name);
                                            }

                                            GUILayout.Label("File", GUILayout.ExpandWidth(false));
                                            folder.isFile = EditorGUILayout.Toggle(folder.isFile, GUILayout.Width(16));

                                            if (GUILayout.Button(path, "label"))
                                            {
                                                if (!string.IsNullOrEmpty(folder.guid))
                                                {
                                                    string assetPath = AssetDatabase.GUIDToAssetPath(folder.guid);
                                                    if (!string.IsNullOrEmpty(assetPath))
                                                    {
                                                        Object obj = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                                                        if (obj)
                                                            EditorGUIUtility.PingObject(obj);
                                                    }
                                                }
                                                else
                                                {
                                                    OpenFileOrFolder(path);
                                                }
                                            }

                                        }

                                        GUILayout.FlexibleSpace();
                                        var enabled = GUI.enabled;
                                        GUI.enabled = i > 0;
                                        if (GUILayout.Button("↑", "label", GUILayout.ExpandWidth(false)))
                                        {
                                            var tmp = folders[i];
                                            folders[i] = folders[i - 1];
                                            folders[i - 1] = tmp;
                                        }

                                        GUI.enabled = i < folders.Length - 1;
                                        if (GUILayout.Button("↓", "label", GUILayout.ExpandWidth(false)))
                                        {
                                            var tmp = folders[i];
                                            folders[i] = folders[i + 1];
                                            folders[i + 1] = tmp;
                                        }
                                        GUI.enabled = enabled;

                                        if (GUILayout.Button("-", "label", GUILayout.ExpandWidth(false)))
                                        {
                                            ArrayUtility.RemoveAt(ref folders, i);
                                            i--;
                                            group.items = folders;
                                        }
                                    }

                                    using (new GUILayout.HorizontalScope())
                                    {
                                        GUILayout.Space(16);
                                        using (new GUILayout.VerticalScope())
                                        {
                                            if (folder.isFile)
                                            {
                                                folder.includeFilePattern = EditorGUILayout.TextField("Include File", folder.includeFilePattern);
                                            }
                                        }
                                    }

                                    //if (!folder.separator)
                                    //{
                                    //    using (new GUILayout.HorizontalScope())
                                    //    {
                                    //        GUILayout.Space(16);
                                    //        using (new GUILayout.VerticalScope())
                                    //        {
                                    //            folder.name = EditorGUILayout.TextField("Name", folder.name);
                                    //        }
                                    //    }
                                    //}

                                }

                                using (new GUILayout.HorizontalScope())
                                {
                                    GUILayout.FlexibleSpace();
                                    if (GUILayout.Button("Add Folder", GUILayout.MinWidth(100)))
                                    {
                                        GenericMenu menu = new GenericMenu();
                                        menu.AddItem(new GUIContent("Folder"), false, () =>
                                        {

                                            path = EditorUtility.OpenFolderPanel("Select Open Folder", "Assets", "");
                                            if (!string.IsNullOrEmpty(path))
                                            {
                                                path = Path.GetFullPath(path);
                                                path.ToRelativePath(Path.GetFullPath("."), out path);
                                                path = path.Replace('\\', '/');


                                                if (folders.Where(o => string.Equals(o.GetPath(), path, StringComparison.InvariantCultureIgnoreCase)).Count() == 0)
                                                {
                                                    var folder = new OpenFileOrFolderSettings.FolderItem();
                                                    if (!Path.IsPathRooted(path) && path.StartsWith("Assets/"))
                                                    {
                                                        string guid = AssetDatabase.AssetPathToGUID(path);
                                                        if (!string.IsNullOrEmpty(guid))
                                                        {
                                                            folder.guid = guid;
                                                        }
                                                    }
                                                    if (string.IsNullOrEmpty(folder.guid))
                                                    {
                                                        folder.path = path;
                                                    }
                                                    folder.name = Path.GetFileName(path);
                                                    ArrayUtility.Add(ref folders, folder);
                                                    group.items = folders;
                                                    OpenFileOrFolderSettings.Provider.Save();
                                                }
                                            }
                                        });

                                        menu.AddItem(new GUIContent("Separator"), false, () =>
                                        {
                                            var folder = new OpenFileOrFolderSettings.FolderItem();
                                            folder.separator = true;
                                            ArrayUtility.Add(ref folders, folder);
                                            group.items = folders;
                                            OpenFileOrFolderSettings.Provider.Save();
                                        });

                                        menu.ShowAsContext();
                                    }


                                    GUILayout.FlexibleSpace();
                                }

                            }
                        }
                    }

                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Add Group", GUILayout.MinWidth(100)))
                        {
                            ArrayUtility.Add(ref OpenFileOrFolderSettings.Settings.groups, new OpenFileOrFolderSettings.FolderGroup() { name = "Group" });
                            GUILayout.FlexibleSpace();
                        }
                        GUILayout.FlexibleSpace();
                    }

                    if (checker.changed)
                    {
                        OpenFileOrFolderSettings.Provider.Save();
                    }
                }
            }
        }

    }
}
