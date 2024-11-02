using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace EditorToolbars
{
    [CustomInspector(typeof(CommandLineTool))]
    public class CommandLineToolInspector : CustomInspector
    {
        ListView listView;

        CommandLineTool Tool => (CommandLineTool)target;


        public override void CreateInspector(VisualElement root)
        {
            base.CreateInspector(root);

            VisualElement container = new VisualElement();
            EditorToolbarUtility.AddStyle(container, typeof(CommandLineToolInspector));
            container.AddToClassList("list-container");
            root.Add(container);

            Toolbar toolbar = new Toolbar();

            ToolbarMenu addMenu = new ToolbarMenu();
            addMenu.name = "addMenu";
            addMenu.text = "+";

            addMenu.menu.AppendAction("Command", act =>
            {
                var item = new CommandLineTool.Item();
                item.Name = "New Command";
                if (Tool.Items.Count == 0)
                {
                    item.IsDefault = true;
                }
                Tool.Items.Add(item);
                EditorToolbarSettings.Save();
                LoadList();
            });

            addMenu.menu.AppendSeparator();
            addMenu.menu.AppendAction("Separator", act =>
            {
                Tool.AddSeparator();
                EditorToolbarSettings.Save();
                LoadList();
            });
            toolbar.Add(addMenu);

            container.Add(toolbar);

            listView = new ListView();
            listView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            listView.horizontalScrollingEnabled = false;

            container.Add(listView);

            listView.makeItem = () =>
            {
                VisualElement container = new VisualElement();
                container.AddToClassList("list-item");

                container.AddManipulator(new MenuManipulator(e =>
                {
                    e.menu.AppendAction("Up", act =>
                    { 
                        var index = Tool.Items.FindIndex(o => o == container.userData);
                        if (index > 0)
                        {
                            var tmp = Tool.Items[index];
                            Tool.Items[index] = Tool.Items[index - 1];
                            Tool.Items[index - 1] = tmp;
                            EditorToolbarSettings.Save();
                            LoadList();
                        }
                    }, act =>
                    {
                        var index = Tool.Items.FindIndex(o => o == container.userData);
                        if (index <= 0)
                            return DropdownMenuAction.Status.Disabled;
                        return DropdownMenuAction.Status.Normal;
                    });


                    e.menu.AppendAction("Down", act =>
                    { 
                        var index = Tool.Items.FindIndex(o => o == container.userData);
                        if (index < Tool.Items.Count - 1)
                        {
                            var tmp = Tool.Items[index];
                            Tool.Items[index] = Tool.Items[index + 1];
                            Tool.Items[index + 1] = tmp;
                            EditorToolbarSettings.Save();
                            LoadList();
                        }
                    }, act =>
                    {
                        var index = Tool.Items.FindIndex(o => o == container.userData);
                        if (index >= Tool.Items.Count - 1)
                            return DropdownMenuAction.Status.Disabled;
                        return DropdownMenuAction.Status.Normal;
                    });

                    e.menu.AppendSeparator();

                    e.menu.AppendAction("Delete", act =>
                    {
                        var item = container.userData as CommandLineTool.Item;
                        var index = Tool.Items.IndexOf(item);
                        if (index >= 0)
                        {
                            Tool.Items.RemoveAt(index);
                            EditorToolbarSettings.Save();
                            LoadList();
                        }
                    });
                }));

                {
                    VisualElement separatorContainer = new VisualElement();
                    separatorContainer.AddToClassList("item-separator");

                    container.Add(separatorContainer);
                }

                {
                    VisualElement itemContainer = new VisualElement();
                    itemContainer.AddToClassList("item-cmd");

                    Toggle defaultField = new Toggle();
                    defaultField.label = "Default";
                    defaultField.AddToClassList("item-default");
                    defaultField.RegisterValueChangedCallback(e =>
                    {
                        var fileItem = (CommandLineTool.Item)container.userData;
                        fileItem.IsDefault = e.newValue;
                        if (fileItem.IsDefault)
                        {
                            foreach (var item in Tool.Items)
                            {
                                if (item != fileItem)
                                {
                                    item.IsDefault = false;
                                }
                            }
                        }
                        EditorToolbarSettings.Save();
                        listView.RefreshItems();
                    });
                    itemContainer.Add(defaultField);

                    TextField nameField = new TextField();
                    nameField.label = "Name";
                    nameField.AddToClassList("item-name");
                    nameField.RegisterValueChangedCallback(e =>
                    {
                        var fileItem = (CommandLineTool.Item)container.userData;
                        fileItem.Name = e.newValue;
                        EditorToolbarSettings.Save();
                    });
                    itemContainer.Add(nameField);


                    TextField pathField = new TextField();
                    pathField.label = "Command Name";
                    pathField.AddToClassList("item-file");
                    pathField.RegisterValueChangedCallback(e =>
                    {
                        var fileItem = (CommandLineTool.Item)container.userData;
                        fileItem.CommandName = e.newValue;
                        EditorToolbarSettings.Save();
                    });
                    itemContainer.Add(pathField);

                    TextField argumentField = new TextField();
                    argumentField.label = "Arguments";
                    argumentField.AddToClassList("item-argument");
                    argumentField.RegisterValueChangedCallback(e =>
                    {
                        var fileItem = (CommandLineTool.Item)container.userData;
                        fileItem.Arguments = e.newValue;
                        EditorToolbarSettings.Save();
                    });
                    itemContainer.Add(argumentField);


                    TextField descField = new TextField();
                    descField.label = "Description";
                    descField.AddToClassList("item-desc");
                    descField.multiline = true;
                    descField.RegisterValueChangedCallback(e =>
                    {
                        var fileItem = (CommandLineTool.Item)container.userData;
                        fileItem.Description = e.newValue;
                        EditorToolbarSettings.Save();
                    });
                    itemContainer.Add(descField);


                    container.Add(itemContainer);
                }

                return container;
            };

            listView.bindItem = (view, index) =>
            {
                var item = listView.itemsSource[index];
                var cmdItem = item as CommandLineTool.Item;
                var separatorContainer = view.Q(className: "item-separator");
                var itemContainer = view.Q(className: "item-cmd");

                view.userData = item;

                separatorContainer.style.display = DisplayStyle.None;
                itemContainer.style.display = DisplayStyle.None;

                if (cmdItem.IsSeparator)
                {
                    separatorContainer.style.display = DisplayStyle.Flex;
                }
                else
                {
                    itemContainer.style.display = DisplayStyle.Flex;

                    var nameField = itemContainer.Q<TextField>(className: "item-name");
                    nameField.SetValueWithoutNotify(cmdItem.Name);
                    itemContainer.Q<TextField>(className: "item-file").SetValueWithoutNotify(cmdItem.CommandName);
                    itemContainer.Q<TextField>(className: "item-argument").SetValueWithoutNotify(cmdItem.Arguments);
                    itemContainer.Q<Toggle>(className: "item-default").SetValueWithoutNotify(cmdItem.IsDefault);
                    itemContainer.Q<TextField>(className: "item-desc").SetValueWithoutNotify(cmdItem.Description);
                }

            };

            LoadList();
        }

        void LoadList()
        {
            listView.itemsSource = Tool.Items;
            listView.RefreshItems();
        }

    }
}