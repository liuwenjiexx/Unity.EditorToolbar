using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bindings;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace EditorToolbars
{
    [CustomInspector(typeof(OpenFileTool))]
    public class OpenFileToolInspector : CustomInspector
    {
        ListView listView;
        BindingSet<OpenFileTool> bindingSet;

        OpenFileTool Tool => (OpenFileTool)target;


        public override void OnEnable()
        {
            base.OnEnable();
            bindingSet = new BindingSet<OpenFileTool>(Tool);
        }

        public override void OnDisable()
        {
            if(bindingSet != null)
            {
                bindingSet.Unbind();
                bindingSet = null;
            }
            base.OnDisable();
        }

        public override void CreateInspector(VisualElement root)
        {
            base.CreateInspector(root);

            VisualElement container = new VisualElement();
            EditorToolbarUtility.AddStyle(container, typeof(OpenFileToolInspector));
            container.AddToClassList("list-container");
            root.Add(container);

            Toolbar toolbar = new Toolbar();

            ToolbarMenu addMenu = new ToolbarMenu();
            addMenu.name = "addMenu";
            addMenu.text = "+";


            foreach (OpenFileTool.FileType fileType in Enum.GetValues(typeof(OpenFileTool.FileType)))
            {
                addMenu.menu.AppendAction(fileType.ToString(), act =>
                {
                    var item = new OpenFileTool.Item()
                    {
                        fileType = fileType,
                    };
                    Tool.Items.Add(item);
                    EditorToolbarSettings.Save();
                    LoadList();
                });
            }

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
                        var item = container.userData as OpenFileTool.Item;
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
                        var item = container.userData as OpenFileTool.Item;
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
                        var item = container.userData as OpenFileTool.Item;
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

                    VisualElement separator = new VisualElement();
                    separator.AddToClassList("item-separator");
                    container.Add(separator);
                }

                {
                    VisualElement itemContainer = new VisualElement();
                    itemContainer.AddToClassList("item-file");


                    Label typeLabel = new Label();
                    typeLabel.AddToClassList("item-file-type-name");
                    itemContainer.Add(typeLabel);


                    Toggle defaultField = new Toggle();
                    defaultField.label = "Default";
                    defaultField.AddToClassList("item-file-default");
                    defaultField.RegisterValueChangedCallback(e =>
                    {
                        var fileItem = (OpenFileTool.Item)container.userData;
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
                    nameField.AddToClassList("item-file-name");
                    nameField.RegisterValueChangedCallback(e =>
                    {
                        var fileItem = (OpenFileTool.Item)container.userData;
                        fileItem.Name = e.newValue;
                        EditorToolbarSettings.Save();
                    });
                    itemContainer.Add(nameField);



                    EnumField methodField = new EnumField(OpenFileTool.OpenMethod.OpenAsset);
                    methodField.label = "Open";
                    methodField.AddToClassList("item-file-open-method");
                    methodField.RegisterValueChangedCallback(e =>
                    {
                        var fileItem = (OpenFileTool.Item)container.userData;
                        fileItem.openMethod = (OpenFileTool.OpenMethod)e.newValue;
                        EditorToolbarSettings.Save();
                    });
                    itemContainer.Add(methodField);



                    TextField pathField = new TextField();
                    pathField.label = "Path";
                    pathField.AddToClassList("item-file-path");
                    pathField.RegisterValueChangedCallback(e =>
                    {
                        var fileItem = (OpenFileTool.Item)container.userData;
                        fileItem.path = e.newValue;
                        EditorToolbarSettings.Save();
                    });
                    itemContainer.Add(pathField);

                    VisualElement filesContainer = new VisualElement();
                    filesContainer.AddToClassList("item-file-files");

                    {
                        TextField includeField = new TextField();
                        includeField.label = "Include";
                        includeField.AddToClassList("item-file-include");
                        includeField.RegisterValueChangedCallback(e =>
                        {
                            var fileItem = (OpenFileTool.Item)container.userData;
                            fileItem.includePattern = e.newValue;
                            EditorToolbarSettings.Save();
                        });
                        filesContainer.Add(includeField);

                        TextField excludeField = new TextField();
                        excludeField.label = "Exclude";
                        excludeField.AddToClassList("item-file-exclude");
                        excludeField.RegisterValueChangedCallback(e =>
                        {
                            var fileItem = (OpenFileTool.Item)container.userData;
                            fileItem.excludePattern = e.newValue;
                            EditorToolbarSettings.Save();
                        });
                        filesContainer.Add(excludeField);


                        Toggle recurveField = new Toggle();
                        recurveField.label = "Recurve";
                        recurveField.AddToClassList("item-file-recurve");
                        recurveField.RegisterValueChangedCallback(e =>
                        {
                            var fileItem = (OpenFileTool.Item)container.userData;
                            fileItem.recurve = e.newValue;
                            EditorToolbarSettings.Save();
                        });
                        filesContainer.Add(recurveField);

                        Toggle hideExtensionField = new Toggle();
                        hideExtensionField.label = "Hide Extension";
                        hideExtensionField.AddToClassList("item-file-hide-extension");
                        hideExtensionField.RegisterValueChangedCallback(e =>
                        {
                            var fileItem = (OpenFileTool.Item)container.userData;
                            fileItem.hideExtension = e.newValue;
                            EditorToolbarSettings.Save();
                        });
                        filesContainer.Add(hideExtensionField);
                    }
                    itemContainer.Add(filesContainer);

                    container.Add(itemContainer);
                }

                return container;
            };

            listView.bindItem = (view, index) =>
            {
                var item = listView.itemsSource[index];
                //var group = item as OpenFileTool.OpenGroup;
                var file = item as OpenFileTool.Item;
                //var groupContainer = view.Q(className: "item-group");
                var separatorContainer = view.Q(className: "item-separator");
                var itemContainer = view.Q(className: "item-file");

                view.userData = item;

                //groupContainer.style.display = DisplayStyle.None;
                itemContainer.style.display = DisplayStyle.None;
                separatorContainer.style.display = DisplayStyle.None;

                if (file.IsSeparator)
                {
                    separatorContainer.style.display = DisplayStyle.Flex;
                    //if (group != null)
                    //{
                    //    groupContainer.style.display = DisplayStyle.Flex;
                    //    var nameField = groupContainer.Q<TextField>(className: "item-group-name");
                    //    nameField.SetValueWithoutNotify(group.name);
                }
                else
                {
                    itemContainer.style.display = DisplayStyle.Flex;

                    itemContainer.Q<Label>(className: "item-file-type-name").text = file.fileType.ToString();

                    var nameField = itemContainer.Q<TextField>(className: "item-file-name");
                    nameField.SetValueWithoutNotify(file.Name);
                    itemContainer.Q<TextField>(className: "item-file-path").SetValueWithoutNotify(file.path);
                    itemContainer.Q<Toggle>(className: "item-file-default").SetValueWithoutNotify(file.IsDefault);
                    itemContainer.Q<EnumField>(className: "item-file-open-method").SetValueWithoutNotify(file.openMethod);

                    var filesContainer = itemContainer.Q(className: "item-file-files");

                    if (file.fileType == OpenFileTool.FileType.Files)
                    {
                        filesContainer.style.display = DisplayStyle.Flex;
                        //itemContainer.Q<EnumField>(className: "item-file-type").SetValueWithoutNotify(file.fileType);
                        itemContainer.Q<TextField>(className: "item-file-path").SetValueWithoutNotify(file.path);
                        itemContainer.Q<TextField>(className: "item-file-include").SetValueWithoutNotify(file.includePattern);
                        itemContainer.Q<TextField>(className: "item-file-exclude").SetValueWithoutNotify(file.excludePattern);
                        itemContainer.Q<Toggle>(className: "item-file-recurve").SetValueWithoutNotify(file.recurve);
                        itemContainer.Q<Toggle>(className: "item-file-hide-extension").SetValueWithoutNotify(file.hideExtension);
                    }
                    else
                    {
                        filesContainer.style.display = DisplayStyle.None;
                    }

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