using System;
using System.Collections.Generic;
using System.Linq;
using Unity;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace EditorToolbars
{
    class EditorToolbarSettingsProvider : UnityEditor.SettingsProvider
    {
        private VisualElement contentRoot;
        private List<CustomInspector> inspectors = new List<CustomInspector>();
        ListView listView;
        ToolbarMenu workspaceField;
        MenuToolbarWindow menuWin;
        public static EditorToolbarSettingsProvider instance;
        internal static bool updateList;

        const string MenuPath = "Tool/Toolbar";

        public EditorToolbarSettingsProvider()
          : base(MenuPath, SettingsScope.Project)
        {
        }




        [SettingsProvider]
        public static UnityEditor.SettingsProvider CreateSettingsProvider()
        {
            var provider = new EditorToolbarSettingsProvider();
            provider.keywords = new string[] { "toolbar" };
            return provider;
        }
        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            EditorToolbarUtility.WorkspaceChanged += EditorToolbar_WorkspaceChanged;
            EditorToolbarUtility.ToolAdded += EditorToolbar_Added;
            EditorToolbarUtility.ToolRemoved += EditorToolbar_Removed;

            contentRoot = EditorToolbarUtility.LoadUXML(typeof(EditorToolbarSettingsProvider), rootElement);
            contentRoot.style.flexGrow = 1f;
            EditorToolbarUtility.AddStyle(rootElement, typeof(EditorToolbarSettingsProvider));

            contentRoot.Add(new IMGUIContainer(() => { if (Event.current.type == EventType.Repaint) Update(); }));

            var toolbar = contentRoot.Q<Toolbar>();

            workspaceField = toolbar.Q<ToolbarMenu>("workspace");

            workspaceField.menu.AppendAction("Project", act =>
           {
               if (!EditorToolbarUtility.IsProjectWorkspace)
               {
                   EditorToolbarUtility.IsProjectWorkspace = true;
               }
           }, act =>
           {
               return EditorToolbarUtility.IsProjectWorkspace ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;
           });
            workspaceField.menu.AppendAction("User", act =>
            {
                if (EditorToolbarUtility.IsProjectWorkspace)
                {
                    EditorToolbarUtility.IsProjectWorkspace = false;
                }
            }, act =>
            {
                return !EditorToolbarUtility.IsProjectWorkspace ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;
            });

            var addMenu = toolbar.Q<ToolbarMenu>("addMenu");
            addMenu.menu.AppendAction("Group",
                act =>
                {
                    NewGroup();
                });

            addMenu.menu.AppendSeparator();

            addMenu.menu.AppendAction("Menu",
                act =>
                {
                    OpenMenuToolbar();
                },
                act =>
                {
                    if (string.IsNullOrEmpty(GetSelectedGroup()))
                        return DropdownMenuAction.Status.Disabled;
                    return DropdownMenuAction.Status.Normal;
                });
            addMenu.menu.AppendSeparator();

            foreach (var type in TypeCache.GetTypesDerivedFrom<EditorToolbar>().OrderBy(o => o.Name))
            {
                if (type.IsAbstract)
                    continue;
                if (type.IsDefined(typeof(HideInInspector), false))
                    continue;
                var c = type.GetConstructor(Type.EmptyTypes);
                if (c == null || !c.IsPublic)
                    continue;
                string name = type.Name;
                if (name.EndsWith("Tool"))
                    name = name.Substring(0, name.Length - 4);
                name = ObjectNames.NicifyVariableName(name);
                addMenu.menu.AppendAction(name, act =>
                {
                    var tool = (EditorToolbar)Activator.CreateInstance(type);
                    tool.Group = GetSelectedGroup();
                    if (string.IsNullOrEmpty(tool.Name))
                        tool.Name = name;
                    tool.Enabled = true;
                    EditorToolbarUtility.AddTool(tool);
                    UpdateList();
                    var toolConfig = tool.Group.GetToolConfig(tool);
                    Select(toolConfig);
                });
            }

            toolbar.Q<ToolbarButton>("refresh").clicked += () =>
            {
                updateList = true;
                EditorToolbar.Rebuild();
            };
            toolbar.Q<ToolbarButton>("newGroup").clicked += NewGroup;
            toolbar.Q<ToolbarButton>("menuToolbar").clicked += OpenMenuToolbar;

            var container = contentRoot.Q("toolbar-list-container");
            listView = container.Q<ListView>();
            InitalizeContainer(container);
            InitalizeListView(listView);

            UpdateList();
            instance = this;
        }

        public override void OnDeactivate()
        {
            EditorToolbarUtility.WorkspaceChanged -= EditorToolbar_WorkspaceChanged;
            EditorToolbarUtility.ToolAdded -= EditorToolbar_Added;
            EditorToolbarUtility.ToolRemoved -= EditorToolbar_Removed;

            ClearInspector();
            if (menuWin)
            {
                menuWin.Close();
                menuWin = null;
            }
            instance = null;
            base.OnDeactivate();
        }

        void InitalizeContainer(VisualElement container)
        {

            container.AddManipulator(new DragAndDropManipulator((objs, paths, accept) =>
            {
                bool isOk = false;

                foreach (var item in CreateFromAttribute.MethodInfos)
                {
                    foreach (var obj in objs)
                    {
                        var method = CreateFromAttribute.GetCreateFromMethod(obj.GetType(), typeof(EditorToolbar));
                        if (method != null)
                        {
                            if (accept)
                            {
                                var tool = method.Invoke(null, new object[] { obj }) as EditorToolbar;
                                if (tool != null)
                                {
                                    tool.Enabled = true;
                                    EditorToolbarUtility.AddTool(tool);
                                    isOk = true;
                                    break;
                                }
                            }
                            else
                            {
                                isOk = true;
                                break;
                            }
                        }
                    }

                    if (isOk)
                        break;
                }

                return isOk;
            }));
        }


        void InitalizeListView(ListView listView)
        {
            listView.onSelectionChange += (items) =>
            {
                var target = items.FirstOrDefault();
                if (target != null)
                {
                    var toolConfig = target as ToolbarConfig;
                    if (toolConfig != null)
                    {
                        var tool = toolConfig.GetOrCreateTarget();
                        if (tool != null)
                        {
                            EditorToolbarUtility.FindToolConfig(toolConfig.Id, out var group);
                            if (tool.Group != group)
                            {
                                tool.Group = group;
                            }
                            EditorToolbarUtility.Select(tool);
                        }
                    }
                    else
                    {
                        var group = target as ToolbarGroup;
                        if (group != null)
                        {
                            EditorToolbarUtility.Select(group);
                        }
                    }
                }
                if (menuWin)
                {
                    string newGroup = GetSelectedGroup();
                    if (menuWin.selectedGroup != newGroup)
                    {
                        menuWin.selectedGroup = newGroup;
                        menuWin.Refresh();
                        menuWin.reloadList = true;
                    }
                }
            };

            listView.makeItem = () =>
            {
                VisualElement itemContainer = new VisualElement();
                itemContainer.AddToClassList("toolbar-item");


                itemContainer.AddManipulator(new MenuManipulator((e) =>
                {
                    var group = itemContainer.userData as ToolbarGroup;
                    var toolConfig = itemContainer.userData as ToolbarConfig;

                    if (group != null)
                    {
                        e.menu.AppendAction("Up", act =>
                        {
                            var groups = EditorToolbarSettings.CurrentGroups;

                            int index = groups.IndexOf(group);
                            if (index > 0)
                            {
                                var tmp = groups[index];
                                groups[index] = groups[index - 1];
                                groups[index - 1] = tmp;
                            }

                            EditorToolbarSettings.Save();
                            updateList = true;
                            EditorToolbar.Rebuild();
                        }, act =>
                        {
                            DropdownMenuAction.Status status = DropdownMenuAction.Status.Normal;

                            var groups = EditorToolbarSettings.CurrentGroups;
                            int index = groups.IndexOf(group);
                            if (index <= 0)
                            {
                                status = DropdownMenuAction.Status.Disabled;
                            }
                            return status;
                        });

                        e.menu.AppendAction("Down", act =>
                        {
                            var groups = EditorToolbarSettings.CurrentGroups;
                            int index = groups.IndexOf(group);
                            if (index < groups.Count - 1)
                            {
                                var tmp = groups[index];
                                groups[index] = groups[index + 1];
                                groups[index + 1] = tmp;
                            }

                            EditorToolbarSettings.Save();
                            updateList = true;
                            EditorToolbar.Rebuild();
                        }, act =>
                        {
                            DropdownMenuAction.Status status = DropdownMenuAction.Status.Normal;

                            var groups = EditorToolbarSettings.CurrentGroups;
                            int index = groups.IndexOf(group);
                            if (index >= groups.Count - 1)
                            {
                                status = DropdownMenuAction.Status.Disabled;
                            }
                            return status;
                        });
                        e.menu.AppendSeparator();

                        e.menu.AppendAction("Delete", act =>
                        {
                            //if (!group.manualCreated || group.items.Count > 0)
                            //    return;
                            var groups = EditorToolbarSettings.CurrentGroups;
                            var index = groups.IndexOf(group);
                            if (index >= 0)
                            {
                                groups.RemoveAt(index);
                                EditorToolbarSettings.Save();
                                updateList = true;
                                EditorToolbarUtility.OnGroupChanged(group);
                            }
                        }, act => true || group.manualCreated && group.items.Count == 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                    }

                    if (toolConfig != null)
                    {
                        EditorToolbarUtility.FindToolConfig(toolConfig.Id, out group);

                        e.menu.AppendAction("Up", act =>
                        {
                            int index = group.items.IndexOf(toolConfig);
                            if (index > 0)
                            {
                                index = group.items.IndexOf(toolConfig);
                                if (index > 0)
                                {
                                    var tmp = group.items[index];
                                    group.items[index] = group.items[index - 1];
                                    group.items[index - 1] = tmp;
                                }
                            }
                            else
                            {
                                ToolbarGroup findGroup = FindPreviousGroup(group);
                                if (findGroup != null)
                                {
                                    group.Remove(toolConfig.Id);
                                    findGroup.items.Add(toolConfig);
                                }
                            }
                            EditorToolbarSettings.Save();
                            updateList = true;
                            EditorToolbar.Rebuild();
                        }, act =>
                        {
                            DropdownMenuAction.Status status = DropdownMenuAction.Status.Normal;

                            int index = group.items.IndexOf(toolConfig);
                            if (index <= 0)
                            {
                                ToolbarGroup findGroup = FindPreviousGroup(group);
                                if (findGroup == null)
                                {
                                    status = DropdownMenuAction.Status.Disabled;
                                }
                            }
                            return status;
                        });
                        e.menu.AppendAction("Down", act =>
                        {
                            int index = group.items.IndexOf(toolConfig);
                            if (index < group.items.Count - 1)
                            {
                                var tmp = group.items[index];
                                group.items[index] = group.items[index + 1];
                                group.items[index + 1] = tmp;
                            }
                            else
                            {
                                var findGroup = FindNextGroup(group);
                                if (findGroup != null)
                                {
                                    group.Remove(toolConfig.Id);
                                    findGroup.items.Insert(0, toolConfig);
                                }
                            }
                            EditorToolbarSettings.Save();
                            updateList = true;
                            EditorToolbar.Rebuild();
                        }, act =>
                        {
                            DropdownMenuAction.Status status = DropdownMenuAction.Status.Normal;
                            int index = group.items.IndexOf(toolConfig);
                            if (index >= group.items.Count - 1)
                            {
                                var findGroup = FindNextGroup(group);
                                if (findGroup == null)
                                {
                                    status = DropdownMenuAction.Status.Disabled;
                                }
                            }
                            return status;
                        });

                        e.menu.AppendSeparator();
                        e.menu.AppendAction("Delete", act =>
                        {
                            if (!toolConfig.ManualCreated)
                                return;
                            group.items.Remove(toolConfig);
                            EditorToolbarSettings.Save();
                            EditorToolbar.Rebuild();
                            updateList = true;
                        }, act => toolConfig.ManualCreated ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                    }

                }));

                {
                    VisualElement positionContainer = new VisualElement();
                    positionContainer.AddToClassList("toolbar-item-position");

                    Label positionLabel = new Label();
                    positionLabel.AddToClassList("toolbar-item-position-name");
                    positionContainer.Add(positionLabel);

                    itemContainer.Add(positionContainer);
                }

                {
                    VisualElement groupContainer = new VisualElement();
                    groupContainer.AddToClassList("toolbar-item-group");

                    var groupEnabled = new Toggle();
                    groupEnabled.AddToClassList("toolbar-item-group-check");
                    groupEnabled.RegisterValueChangedCallback(e =>
                    {
                        var group = (ToolbarGroup)itemContainer.userData;
                        group.Enabled = e.newValue;

                        EditorToolbarSettings.Save();
                        updateList = true;
                        EditorToolbar.Rebuild();
                    });
                    groupContainer.Add(groupEnabled);

                    var groupNameLabel = new Label();
                    groupNameLabel.AddToClassList("toolbar-item-group-name");
                    groupContainer.Add(groupNameLabel);

                    itemContainer.Add(groupContainer);
                }

                {
                    VisualElement toolContainer = new VisualElement();
                    toolContainer.AddToClassList("toolbar-item-tool");

                    var tglEnabled = new Toggle();
                    tglEnabled.AddToClassList("toolbar-item-tool-check");
                    tglEnabled.RegisterValueChangedCallback(e =>
                    {
                        var tool = (ToolbarConfig)itemContainer.userData;
                        tool.Enabled = e.newValue;
                        EditorToolbarSettings.Save();
                        updateList = true;
                        EditorToolbar.Rebuild();
                    });
                    toolContainer.Add(tglEnabled);

                    var toolNameLabel = new Label();
                    toolNameLabel.AddToClassList("toolbar-item-tool-name");
                    toolContainer.Add(toolNameLabel);

                    itemContainer.Add(toolContainer);
                }
                return itemContainer;
            };

            listView.bindItem = (view, index) =>
            {
                var item = listView.itemsSource[index];
                view.userData = item;

                var positionContainer = view.Q(className: "toolbar-item-position");
                var groupContainer = view.Q(className: "toolbar-item-group");
                var toolContainer = view.Q(className: "toolbar-item-tool");

                positionContainer.style.display = DisplayStyle.None;
                groupContainer.style.display = DisplayStyle.None;
                toolContainer.style.display = DisplayStyle.None;

                if (item is ToolbarPosition)
                {
                    ToolbarPosition position = (ToolbarPosition)item;
                    positionContainer.style.display = DisplayStyle.Flex;
                    Label nameLabel = positionContainer.Q<Label>(className: "toolbar-item-position-name");
                    nameLabel.text = ObjectNames.NicifyVariableName(position.ToString());
                }
                else if (item is ToolbarGroup)
                {
                    ToolbarGroup group = (ToolbarGroup)item;
                    groupContainer.style.display = DisplayStyle.Flex;
                    var groupEnabled = groupContainer.Q<Toggle>(className: "toolbar-item-group-check");
                    groupEnabled.SetValueWithoutNotify(group.Enabled);

                    var groupNameLabel = groupContainer.Q<Label>(className: "toolbar-item-group-name");
                    groupNameLabel.text = group.GetShortName();
                }
                else
                {
                    var toolConfig = item as ToolbarConfig;
                    toolContainer.style.display = DisplayStyle.Flex;

                    var tglEnabled = toolContainer.Q<Toggle>(className: "toolbar-item-tool-check");
                    tglEnabled.SetValueWithoutNotify(toolConfig.Enabled);

                    EditorToolbarUtility.FindToolConfig(toolConfig.Id, out var group);
                    var tool = EditorToolbarUtility.FindTool(toolConfig.Id);
                    if (tool == null)
                    {
                        tool = toolConfig.GetOrCreateTarget();
                    }
                    //   tglEnabled.SetEnabled(group == null ? false : group.Enabled);

                    var nameLabel = toolContainer.Q<Label>(className: "toolbar-item-tool-name");
                    string name = null;
                    if (toolConfig.Name.Keyword == ConfigValueKeyword.Undefined)
                    {
                        name = toolConfig.Name;
                    }
                    if (tool != null)
                    {
                        if (string.IsNullOrEmpty(name))
                            name = tool.Name;
                        if (string.IsNullOrEmpty(name))
                        {
                            name = $"{tool.GetType().Name}";
                        }
                    }
                    else
                    {
                        tool = EditorToolbarUtility.FindTool(toolConfig.Id);
                    }
                    nameLabel.text = name;
                }

            };

        }

        ToolbarGroup FindPreviousGroup(ToolbarGroup group)
        {
            ToolbarGroup findGroup = null;
            ToolbarGroup lastGroup = null;
            foreach (var item in listView.itemsSource)
            {
                var g = item as ToolbarGroup;
                if (g != null)
                {
                    if (g == group)
                    {
                        findGroup = lastGroup;
                        break;
                    }
                    lastGroup = g;
                }
            }
            return findGroup;
        }
        ToolbarGroup FindNextGroup(ToolbarGroup group)
        {
            ToolbarGroup findGroup = null;
            ToolbarGroup lastGroup = null;
            foreach (var item in listView.itemsSource)
            {
                var g = item as ToolbarGroup;
                if (g != null)
                {
                    if (lastGroup == group)
                    {
                        findGroup = g;
                        break;
                    }
                    lastGroup = g;
                }
            }
            return findGroup;
        }

        void UpdateList()
        {
            updateList = false;
            ClearInspector();

            workspaceField.text = EditorToolbarUtility.IsProjectWorkspace ? "Project" : "User";

            List<object> list = new List<object>();

            //HashSet<string> toolIds = new HashSet<string>();
            //foreach (var tool in EditorToolbarSettings.Groups.SelectMany(o => o.Items))
            //{
            //    toolIds.Add(tool.Id);
            //}

            //foreach (var toolConfig in EditorToolbarSettings.Groups.SelectMany(o => o.items))
            //{
            //    if (!toolIds.Contains(toolConfig.Id))
            //    {
            //        EditorToolbarSettings.Save();
            //        break;
            //    }
            //} 


            foreach (var position in EditorToolbarSettings.CurrentGroups.GroupBy(o => o.Position.Keyword == ConfigValueKeyword.Null ? ToolbarPosition.LeftToolbar : o.Position.Value).Select(o => o.Key))
            {
                list.Add(position);

                foreach (var group in EditorToolbarSettings.CurrentGroups.Where(o => o.Position == position))
                {
                    list.Add(group);
                    foreach (var item in group.items)
                    {
                        list.Add(item);
                    }
                }
            }

            listView.itemsSource = list;
            listView.RefreshItems();
        }

        private void EditorToolbar_WorkspaceChanged()
        {
            listView.ClearSelection();
            updateList = true;
        }

        private void EditorToolbar_Added(EditorToolbar obj)
        {
            updateList = true;
        }

        private void EditorToolbar_Removed(EditorToolbar obj)
        {
            updateList = true;
        }

        void NewGroup()
        {
            string newGroupName = "New Group";
            int n = 1;
            do
            {
                if (!EditorToolbarSettings.CurrentGroups.Any(g => g.Name == newGroupName))
                {
                    break;
                }
                newGroupName = $"New Group {n++}";
            } while (true);

            ToolbarGroup group = new ToolbarGroup();

            group.Position = ToolbarPosition.LeftToolbar;
            group.manualCreated = true;
            group.Enabled = true;

            if (EditorToolbarUtility.IsProjectWorkspace)
            {
                group.Name = EditorToolbar.ProjectScopePrefix + newGroupName;
                EditorToolbarSettings.Groups.Add(group);
            }
            else
            {
                group.Name = EditorToolbar.UserScopePrefix + newGroupName;
                EditorToolbarUserSettings.Groups.Add(group);
            }
            EditorToolbarSettings.Save();

            UpdateList();
            Select(group);

            EditorToolbarUtility.OnGroupChanged(group);
        }

        string GetSelectedGroup()
        {
            ToolbarGroup group = null;

            if (listView.selectedItem != null)
            {
                group = listView.selectedItem as ToolbarGroup;
                if (group == null)
                {
                    var tool = listView.selectedItem as EditorToolbar;
                    if (tool != null)
                    {
                        group = tool.Group;
                    }
                    if (group == null)
                    {
                        var toolConfig = listView.selectedItem as ToolbarConfig;
                        if (toolConfig != null)
                        {
                            EditorToolbarUtility.FindToolConfig(toolConfig.Id, out group);
                        }
                    }
                }
            }
            if (group == null)
            {
                return EditorToolbarUtility.GetDefaultGroupName();
            }
            return group.Name;
        }

        void Select(object item)
        {
            listView.Focus();
            var index = listView.itemsSource.IndexOf(item);
            listView.selectedIndex = index;
            if (index >= 0)
            {
                listView.ScrollToItem(listView.selectedIndex);
            }
        }

        void OpenMenuToolbar()
        {
            menuWin = EditorWindow.GetWindow<MenuToolbarWindow>();
            menuWin.selectedGroup = GetSelectedGroup();
            menuWin.Show();
            menuWin.Refresh();
        }

        public override void OnInspectorUpdate()
        {
            foreach (var inspector in inspectors)
            {
                inspector.OnUpdate();
            }
        }


        void Update()
        {
            if (updateList)
            {
                UpdateList();
            }
        }

        void ClearInspector()
        {
            foreach (var inspector in inspectors)
            {
                inspector.OnDisable();
            }
            inspectors.Clear();
        }

    }
}
