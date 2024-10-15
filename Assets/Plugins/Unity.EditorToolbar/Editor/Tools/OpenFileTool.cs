using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using UnityEditor;
using System.Collections;


namespace Unity.Toolbars.Editor
{
    [Serializable]
    public class OpenFileTool : EditorToolbarButton
    {
        [SerializeField]
        private List<Item> items = new List<Item>();
        public List<Item> Items => items;

        public override Texture2D Icon
        {
            get => base.icon == ConfigValueKeyword.Null ? Icons.OpenedFolder : base.Icon;
            set => base.Icon = value;
        }
        public override List<EditorToolbar.Item> GetItems()
        {
            return items.ToList<EditorToolbar.Item>();
        }


        protected override void OnCreateMenu(DropdownMenu menu)
        {
            foreach (Item item in Items)
            {
                if (item.IsSeparator)
                {
                    menu.AppendSeparator();
                    continue;
                }

                if (item.fileType == FileType.Folder || item.fileType == FileType.Files)
                {
                    if (item.fileType == FileType.Files)
                    {
                        Regex include = null, exclude = null;
                        if (!string.IsNullOrEmpty(item.includePattern))
                            include = new Regex(item.includePattern, RegexOptions.IgnoreCase);
                        if (!string.IsNullOrEmpty(item.excludePattern))
                            exclude = new Regex(item.excludePattern, RegexOptions.IgnoreCase);
                        string rootPath = item.GetPath();
                        if (Directory.Exists(rootPath))
                        {
                            int index = rootPath.Length;
                            if (!(rootPath.EndsWith("/") || rootPath.EndsWith("\\")))
                                index++;
                            SearchOption option = SearchOption.TopDirectoryOnly;
                            if (item.recurve)
                                option = SearchOption.AllDirectories;
                            foreach (var file in Directory.GetFiles(rootPath, "*", option))
                            {
                                if (file.EndsWith(".meta"))
                                    continue;

                                if (include != null && !include.IsMatch(file))
                                    continue;
                                if (exclude != null && exclude.IsMatch(file))
                                    continue;
                                string name = file;
                                name = name.Substring(index);
                                if (item.hideExtension)
                                    name = Path.Combine(Path.GetDirectoryName(name), Path.GetFileNameWithoutExtension(name));
                                name = name.Replace('\\', '/');
                                if (!string.IsNullOrEmpty(item.Name))
                                    name = item.Name + "/" + name;
                                menu.AppendAction(name, (menuItem) =>
                                {
                                    Open(item, file);
                                });
                            }
                        }
                    }
                    else
                    {
                        menu.AppendAction(GetDisplayName(item), (menuItem) =>
                        {
                            Open(item);
                        });
                    }

                }
                else
                {
                    menu.AppendAction(GetDisplayName(item), (menuItem) =>
                    {
                        Open(item);
                    });
                }
            }


        }

        string GetDisplayName(Item item)
        {
            if (!string.IsNullOrEmpty(item.Name))
                return item.Name;
            if (!string.IsNullOrEmpty(item.path))
            {
                if (item.hideExtension)
                    return Path.GetFileNameWithoutExtension(item.path);
                return Path.GetFileName(item.path);
            }
            return string.Empty;
        }

        bool IsScene(string assetPath)
        {
            return assetPath.EndsWith(".unity");
        }

        void Open(Item item)
        {
            string path = item.GetPath();
            Open(item, path);
        }
        void Open(Item item, string path)
        {
            if (string.IsNullOrEmpty(path))
                return;
            path = EditorToolbarUtility.ReplaceProjectScheme(path);
            if (item.openMethod == OpenMethod.OpenAsset || item.openMethod == OpenMethod.Ping)
            {
                Object obj = AssetDatabase.LoadAssetAtPath<Object>(path);
                if (obj)
                {
                    if (item.openMethod == OpenMethod.Ping)
                    {
                        EditorGUIUtility.PingObject(obj);
                    }
                    else
                    {
                        AssetDatabase.OpenAsset(obj);
                    }
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(path))
                {
                    OpenFileOrFolder(Path.GetFullPath(path), async: true);
                }
            }
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



        public OpenFileTool AddFile(string path, string name, bool isDefault = false)
        {
            items.Add(new Item()
            {
                Name = name,
                fileType = FileType.File,
                path = path,
                IsDefault = isDefault
            });
            return this;
        }
        public OpenFileTool AddFolder(string path, string name, bool isDefault = false)
        {
            items.Add(new Item()
            {
                Name = name,
                fileType = FileType.Folder,
                path = path,
                IsDefault = isDefault
            });
            return this;
        }
        public OpenFileTool AddFileFromFolder(string path, string include = null, string exclude = null, bool isDefault = false, bool hideExtension = true, bool recurve = true)
        {
            items.Add(new Item()
            {
                fileType = FileType.Files,
                path = path,
                includePattern = include,
                excludePattern = exclude,
                IsDefault = isDefault,
                hideExtension = hideExtension,
                recurve = recurve,
                openMethod = OpenMethod.OpenFile,
            });
            return this;
        }
        public OpenFileTool AddAssetPath(string assetPath, string name = null, bool isDefault = false, OpenMethod openMethod = OpenMethod.OpenAsset)
        {
            items.Add(new Item()
            {
                Name = name,
                fileType = FileType.File,
                path = assetPath,
                IsDefault = isDefault,
                openMethod = openMethod
            });
            return this;
        }
        public OpenFileTool AddAssetFolder(string assetPath, string name = null, bool isDefault = false, OpenMethod openMethod = OpenMethod.OpenAsset)
        {
            items.Add(new Item()
            {
                Name = name,
                fileType = FileType.Folder,
                path = assetPath,
                IsDefault = isDefault,
                openMethod = openMethod
            });
            return this;
        }
        public OpenFileTool AddAssetFileFromFolder(string assetPath, string include = null, string exclude = null, bool isDefault = false, OpenMethod openMethod = OpenMethod.OpenAsset, bool hideExtension = true, bool recurve = true)
        {
            items.Add(new Item()
            {
                fileType = FileType.Files,
                path = assetPath,
                includePattern = include,
                excludePattern = exclude,
                IsDefault = isDefault,
                openMethod = openMethod,
                hideExtension = hideExtension,
                recurve = recurve
            });
            return this;
        }
        public OpenFileTool AddAssetGuid(string guid, string name = null, bool isDefault = false, OpenMethod openMethod = OpenMethod.OpenAsset)
        {
            items.Add(new Item()
            {
                fileType = FileType.File,
                Name = name,
                guid = guid,
                IsDefault = isDefault,
                openMethod = openMethod
            });
            return this;
        }

        public OpenFileTool AddSeparator()
        {
            items.Add(new Item() { IsSeparator = true });
            return this;
        }

        public enum FileType
        {
            File,
            Folder,
            Files,
        }

        public enum OpenMethod
        {
            OpenAsset,
            OpenFile,
            Ping,
        }

        [Serializable]
        public new class Item : EditorToolbar.Item
        {
            [SerializeField]
            public FileType fileType;
            [SerializeField]
            public OpenMethod openMethod;
            [SerializeField]
            public string path;
            [SerializeField]
            public string guid;
            [SerializeField]
            public bool recurve;
            [SerializeField]
            public bool hideExtension;
            [SerializeField]
            public string includePattern;
            [SerializeField]
            public string excludePattern;

            public string GetPath()
            {
                string path = null;
                if (!string.IsNullOrEmpty(guid))
                {
                    path = AssetDatabase.GUIDToAssetPath(guid);
                }
                if (string.IsNullOrEmpty(path))
                    path = this.path;
                return path;
            }
        }


    }
}
