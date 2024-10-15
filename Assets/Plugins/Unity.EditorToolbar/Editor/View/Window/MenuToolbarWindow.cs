using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.Reflection;
using System;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEditor.UIElements;
using System.IO;

namespace Unity.Toolbars.Editor
{
    public class MenuToolbarWindow : EditorWindow
    {
        private VisualElement root;
        private ListView listView;
        public string filterText;
        public bool reloadList;
        public string selectedGroup;
        Label groupField;
        private static List<MenuData> allList;


        private void OnEnable()
        {
            if (EditorToolbarSettingsProvider.instance == null)
            {
                Close();
                return;
            }

            EditorToolbarUtility.WorkspaceChanged += WorkspaceChanged;
            EditorToolbarUtility.ToolAdded += EditorToolbar_Added;
            EditorToolbarUtility.ToolRemoved += EditorToolbar_Removed;
            EditorToolbarUtility.GroupChanged += EditorToolbarUtility_GroupAdded;


            titleContent = new GUIContent("Menu Toolbar");

            if (string.IsNullOrEmpty(selectedGroup))
            {
                if (EditorToolbarUtility.IsProjectWorkspace)
                    selectedGroup = EditorToolbar.GROUP_DEFAULT;
                else
                    selectedGroup = EditorToolbar.USER_GROUP_DEFAULT;
            }
            CreateUI();
        }


        private void OnDisable()
        {
            EditorToolbarUtility.ToolAdded -= EditorToolbar_Added;
            EditorToolbarUtility.ToolRemoved -= EditorToolbar_Removed;
            EditorToolbarUtility.GroupChanged -= EditorToolbarUtility_GroupAdded;
        }

        public void CreateUI()
        {
            root = EditorToolbarUtility.LoadUXML(typeof(MenuToolbarWindow));
            root.style.flexGrow = 1f;
            EditorToolbarUtility.AddStyle(root, typeof(MenuToolbarWindow));

            var searchField = root.Q<ToolbarSearchField>();
            searchField.SetValueWithoutNotify(filterText);
            searchField.RegisterValueChangedCallback(e =>
            {
                filterText = e.newValue;
                reloadList = true;
            });

            groupField = root.Q<Label>("groupField");



            listView = root.Q<ListView>();
            listView.makeItem = () =>
            {
                VisualElement container = new VisualElement();
                container.AddToClassList("menu-item");

                Toggle checkField = new Toggle();
                checkField.AddToClassList("menu-item-check");
                checkField.RegisterValueChangedCallback(e =>
                {
                    var menuData = container.userData as MenuData;
                    if (e.newValue != menuData.isChecked)
                    {
                        menuData.isChecked = e.newValue;
                        if (menuData.isChecked)
                        {
                            var tool = MenuTool.Create(menuData.menuInfo.menuName, selectedGroup);
                            if (tool != null)
                            {
                                tool.Enabled = true;
                                EditorToolbarUtility.AddTool(tool);
                            }
                            //ToolbarConfig toolConfig = new ToolbarConfig();
                            //toolConfig.Id = menuData.id;
                            //toolConfig.Enabled = true;
                            //var group = (ToolbarGroup)selectedGroup;
                            //group.items.Add(toolConfig);
                            //EditorToolbarSettings.Save();
                            //EditorToolbar.Rebuild();
                            //EditorToolbarSettingsProvider.updateList = true;
                        }
                        else
                        {

                            var toolConfig = EditorToolbarUtility.FindToolConfig(menuData.menuInfo.id, out var group);

                            if (group != null)
                            {
                                group.Remove(toolConfig.Id);
                                EditorToolbarSettings.Save();
                                EditorToolbar.Rebuild();
                                EditorToolbarSettingsProvider.updateList = true;
                            }
                            //MenuTool menuTool = null;
                            //foreach (var tool in EditorToolbarSettings.Groups.Where(o => o.Name == selectedGroup).SelectMany(o => o.Items))
                            //{
                            //    var menu = tool as MenuTool;
                            //    if (menu != null && menu.menuName == menuData.menuName)
                            //    {
                            //        menuTool = menu;
                            //        break;
                            //    }
                            //}
                            //if (menuTool != null)
                            //{
                            //    EditorToolbarUtility.RemoveTool(menuTool);
                            //}
                        }
                    }
                });
                container.Add(checkField);

                Label nameLabel = new Label();
                nameLabel.AddToClassList("menu-item-name");
                container.Add(nameLabel);
                return container;
            };

            listView.bindItem = (view, index) =>
            {
                var menuData = listView.itemsSource[index] as MenuData;
                view.userData = menuData;

                var checkField = view.Q<Toggle>(className: "menu-item-check");
                var nameLabel = view.Q<Label>(className: "menu-item-name");
                nameLabel.text = menuData.menuInfo.name;
                nameLabel.tooltip = menuData.menuInfo.menuName;
                if (menuData.isFolder)
                {
                    nameLabel.text = menuData.menuInfo.displayMenuName;
                    checkField.style.display = DisplayStyle.None;
                    view.style.paddingLeft = 8;
                }
                else
                {
                    checkField.style.display = DisplayStyle.Flex;
                    checkField.SetValueWithoutNotify(menuData.isChecked);
                    view.style.paddingLeft = menuData.depth * 20;
                    view.style.paddingLeft = 10;
                }

            };

            Refresh();
            LoadList();

        }

        public void CreateGUI()
        {
            rootVisualElement.Add(root);
        }
        void WorkspaceChanged()
        {
            Refresh();
            reloadList = true;
        }


        public void Refresh()
        {
            //groupField.menu.MenuItems().Clear();
            ToolbarGroup selected = null;
            //foreach (var group in EditorToolbarSettings.CurrentGroups)
            //{
            //    if (selected == null && selectedGroup == group.Name)
            //    {
            //        selected = group;
            //    }

            //    groupField.menu.AppendAction(group.GetShortName(), act =>
            //    {
            //        selectedGroup = group.Name;
            //        groupField.text = group.GetShortName();
            //        reloadList = true;
            //    }, act =>
            //    {
            //        DropdownMenuAction.Status status = DropdownMenuAction.Status.Normal;
            //        if (selected == group)
            //        {
            //            status |= DropdownMenuAction.Status.Checked;
            //        }
            //        return status;
            //    }, group);
            //}

            groupField.text = ToolbarGroup.GetShortName(selectedGroup);
        }

        private void EditorToolbarUtility_GroupRemoved(ToolbarGroup obj)
        {
            Refresh();
        }

        private void EditorToolbarUtility_GroupAdded(ToolbarGroup obj)
        {
            Refresh();
        }
        private void EditorToolbar_Added(EditorToolbar obj)
        {
            if (obj is MenuTool)
            {
                reloadList = true;
            }
        }

        private void EditorToolbar_Removed(EditorToolbar obj)
        {
            if (obj is MenuTool)
            {
                reloadList = true;
            }
        }

        void LoadList()
        {
            reloadList = false;

            if (allList == null)
            {
                allList = new List<MenuData>();
                HashSet<string> folders = new HashSet<string>();

                foreach (var menuInfo in MenuTool.AllMenus.Values)
                {
                    MenuData menuData = new MenuData();
                    menuData.menuInfo = menuInfo;
                    string menuName2 = menuInfo.displayMenuName.Replace('\\', '/');

                    var parts = menuName2.Split('/');
                    menuData.depth = parts.Length - 1;
                    if (parts.Length > 1)
                    {
                        string _dir = null;
                        for (int i = 0; i < parts.Length - 1; i++)
                        {
                            if (i == 0)
                            {
                                _dir = parts[i];
                            }
                            else
                            {
                                _dir += "/" + parts[i];
                            }
                            if (!folders.Contains(_dir))
                            {
                                allList.Add(new MenuData()
                                {
                                    menuInfo = new MenuTool.MenuInfo()
                                    {
                                        id = _dir,
                                        name = parts[i],
                                        displayMenuName = _dir,
                                    },
                                    depth = i,
                                    isFolder = true,
                                });
                                folders.Add(_dir);
                            }
                        }

                    }

                    allList.Add(menuData);
                }

                allList.Sort((a, b) => StringComparer.Ordinal.Compare(a.menuInfo.displayMenuName, b.menuInfo.displayMenuName));

                for (int i = 0; i < allList.Count; i++)
                {
                    var item = allList[i];
                    if (item.isFolder)
                    {
                        if (i + 1 == allList.Count || allList[i + 1].isFolder)
                        {
                            allList.RemoveAt(i);
                            i--;
                        }
                    }
                }

            }

            RefreshCheck();

            IEnumerable<MenuData> items = allList;

            if (!string.IsNullOrEmpty(filterText))
            {
                //Regex regex = new Regex(filterText, RegexOptions.IgnoreCase);
                var parts = filterText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                items = from o in items
                            //where regex.IsMatch(o.menuName)
                        where parts.All(o2 => !o.isFolder && (o.menuInfo.displayMenuName.Contains(o2, StringComparison.InvariantCultureIgnoreCase)))
                        select o;
            }



            var list = listView.itemsSource as List<MenuData>;
            if (list == null) list = new List<MenuData>();
            list.Clear();
            list.AddRange(items);

            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                if (item.isFolder)
                {
                    if (i + 1 == list.Count || list[i + 1].isFolder)
                    {
                        list.RemoveAt(i);
                        i--;
                    }
                }
            }

            listView.itemsSource = list;

            listView.RefreshItems();
        }

        void RefreshCheck()
        {
            Dictionary<string, ToolbarConfig> menuTools = new Dictionary<string, ToolbarConfig>();
            foreach (var toolConfig in EditorToolbarSettings.CurrentGroups.SelectMany(o => o.items))
            {
                if (!string.IsNullOrEmpty(toolConfig.Id))
                {
                    menuTools[toolConfig.Id] = toolConfig;
                }
            }

            foreach (var menuData in allList)
            {
                if (menuTools.TryGetValue(menuData.menuInfo.id, out var toolConfig))
                {
                    menuData.isChecked = true;
                }
                else
                {
                    menuData.isChecked = false;
                }

            }
        }

        void Update()
        {
            if (reloadList)
            {
                LoadList();
            }
        }
        internal class MenuData
        {
            public MenuTool.MenuInfo menuInfo;
            public bool isChecked;
            public int depth;
            public bool isFolder;
        }

    }

}