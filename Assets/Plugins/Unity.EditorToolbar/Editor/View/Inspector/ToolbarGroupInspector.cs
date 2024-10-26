using System.Collections;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Bindings;

namespace Unity.Toolbars.Editor
{
    [CustomInspector(typeof(ToolbarGroup))]
    public class ToolbarGroupInspector : CustomInspector
    {
        private BindingSet<ToolbarGroup> bindingSet;

        public override void OnEnable()
        {
            base.OnEnable();
            bindingSet = new BindingSet<ToolbarGroup>(group);
            bindingSet.SourcePropertyChanged += BindingSet_SourcePropertyChanged;
        }

        private void BindingSet_SourcePropertyChanged(object sender, Bindings.BindingPropertyChangedEventArgs e)
        {
            EditorToolbarSettings.Save();
        }

        public override void OnDisable()
        {
            if (bindingSet != null)
            {
                bindingSet.Unbind();
                bindingSet = null;
            }
            base.OnDisable();
        }

        public override void CreateInspector(VisualElement root)
        {
            var container = EditorToolbarUtility.LoadUXML("ToolbarGroup");

            //var enabledField = new ConfigValueField<bool>();
            //enabledField.name = "enabled";
            //enabledField.label = "Enabled";
            //bindingSet.Bind(enabledField, nameof(ToolbarGroup.Enabled));
            //container.Add(enabledField);

            var nameField = new TextField();
            nameField.name = "name";
            nameField.label = "Name";
            nameField.isDelayed = true;
            bool isProjectScope = ToolbarGroup.IsProjectScope(group.Name);
            nameField.SetValueWithoutNotify(group.GetShortName());
            nameField.RegisterValueChangedCallback(e =>
            {
                string newName = e.newValue;
                if (!string.IsNullOrEmpty(newName))
                {
                    if (isProjectScope)
                    {
                        newName = EditorToolbar.ProjectScopePrefix + newName;
                    }
                    else
                    {
                        newName = EditorToolbar.UserScopePrefix + newName;
                    }

                    if (group.Name != newName)
                    {
                        group.Name = newName;
                        EditorToolbarSettings.Save();
                    }
                }
            });
            container.Add(nameField);
             
            var p = targetProperty.Copy();
            if (p.NextVisible(true))
            {
                do
                {
                    if (p.name == "name")
                        continue;
                    var propField = new PropertyField(p);
                    propField.RegisterValueChangeCallback(e =>
                    {
                        editor.DiryTarget();
                    });
                    switch (p.name)
                    {
                        case "enabled":
                            container.Insert(0, propField);
                            break;
                        case "desc":
                            propField.style.height = 45;
                            var textInput= propField.Q<TextField>();
                            textInput.multiline = true;
                            break;
                        default:
                            container.Add(propField);
                            break;
                    }
                } while (p.NextVisible(false));
            }
             
            root.Add(container);

            bindingSet.Bind();
        }
    }
}