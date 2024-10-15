using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Bindings;
using Unity.Editor;
using Unity.UI.Editor;

namespace Unity.Toolbars.Editor
{
    [CustomEditor(typeof(ToolbarInspectorObject))]
    public class ToolbarInspector : UnityEditor.Editor
    {
        ToolbarInspectorObject inspectorObject;
        VisualElement root;
        GUIStyle tileStyle;
        private Type dataType;
        CustomInspector customInspector;
        VisualElement propertyContainer;
        VisualElement customPropertyContainer;
        List<SerializedProperty> serializedProperties = new List<SerializedProperty>();
        bool isBinding;
        SerializedProperty targetProperty;
        DateTime? nextSaveTime;
        ToolbarConfig toolConfig;
        BindingSet<EditorToolbar> toolBindingSet;
        BindingSet<object> toolUserSettingsBindingSet;
        BindingSet<ToolbarConfig> toolConfigBindingSet;


        private void OnEnable()
        {
            inspectorObject = target as ToolbarInspectorObject;
            if (!inspectorObject)
                return;
            inspectorObject.SelectionChanged += OnTargetChange;

            root = new VisualElement();
            EditorToolbarUtility.AddStyle(root, typeof(ToolbarInspector));

            root.Add(new IMGUIContainer(() => { if (Event.current.type == EventType.Repaint) Update(); }));

            propertyContainer = new VisualElement();
            root.Add(propertyContainer);

            customPropertyContainer = new VisualElement();
            root.Add(customPropertyContainer);

            OnTargetChange();
        }

        private void OnDisable()
        {
            if (!inspectorObject)
                return;
            inspectorObject.SelectionChanged -= OnTargetChange;
            foreach (var property in serializedProperties)
            {
                property.Dispose();
            }
            serializedProperties.Clear();
            Clear();
        }
        public override VisualElement CreateInspectorGUI()
        {
            return root;
        }
        protected override void OnHeaderGUI()
        {
            string title;
            if (tileStyle == null)
            {
                tileStyle = new GUIStyle(EditorStyles.largeLabel);
                tileStyle.padding = new RectOffset(20, 0, 10, 5);
            }

            if (dataType == null)
            {
                title = "Null";
            }
            else
            {
                title = dataType.Name;
            }
            title += $" ({ObjectNames.NicifyVariableName(target.GetType().Name)})";
            GUILayout.Label(title, tileStyle);
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(20);
                using (new GUILayout.VerticalScope())
                {

                }
            }
            GUILayout.Space(10);
        }

        void Clear()
        {
            dataType = null;
            targetProperty = null;
            if (customInspector != null)
            {
                customInspector.OnDisable();
                customInspector = null;
            }
            if (toolConfig != null)
            {
                toolConfig.PropertyChanged -= ToolConfig_PropertyChanged;
                toolConfig = null;
            }
            if (toolConfigBindingSet != null)
            {
                toolConfigBindingSet.Unbind();
                toolConfigBindingSet = null;
            }

            if (toolUserSettingsBindingSet != null)
            {
                toolUserSettingsBindingSet.Unbind();
                toolUserSettingsBindingSet = null;
            }

            if (toolBindingSet != null)
            {
                toolBindingSet.Unbind();
                toolBindingSet = null;
            }

            root.Unbind();

            propertyContainer.Clear();
            customPropertyContainer.Clear();
            //customPropertyContainer.SetEnabled(false);

        }

        void OnTargetChange()
        {
            Clear();

            if (inspectorObject.TargetType == ToolbarInspectorObject.SelectionObjectType.ToolGroup)
            {
                if (inspectorObject.ToolGroup != null)
                    targetProperty = serializedObject.FindProperty("toolGroup");
                //customPropertyContainer.SetEnabled(true);
            }
            else
            {
                if (inspectorObject.Tool != null)
                {
                    targetProperty = serializedObject.FindProperty("tool");
                    toolConfig = EditorToolbarUtility.FindToolConfig(inspectorObject.Tool, out var group);
                    if (toolConfig == null)
                        return;
                    //customPropertyContainer.SetEnabled(toolConfig.ManualCreated);
                    toolConfig.PropertyChanged += ToolConfig_PropertyChanged;
                    toolBindingSet = new BindingSet<EditorToolbar>(inspectorObject.Tool);
                    toolBindingSet.SourcePropertyChanged += (s, e) =>
                    {
                        DiryTarget();
                    };
                    if (inspectorObject.Tool.UserSettings != null)
                    {
                        toolUserSettingsBindingSet = new BindingSet<object>(inspectorObject.Tool.UserSettings);
                        toolUserSettingsBindingSet.SourcePropertyChanged += (s, e) =>
                        {
                            DiryTarget();
                        };
                    }
                    toolConfigBindingSet = new BindingSet<ToolbarConfig>(toolConfig);
                }
            }

            var target = inspectorObject.Target;
            if (target != null)
            {
                dataType = target.GetType();
                Type inspectorType = CustomInspector.GetInspectorType(dataType);
                if (inspectorType != null)
                {
                    customInspector = Activator.CreateInstance(inspectorType) as CustomInspector;
                }

                if (customInspector != null)
                {
                    customInspector.root = customPropertyContainer;
                    customInspector.editor = this;
                    customInspector.targetProperty = targetProperty;

                    var tool = target as EditorToolbar;
                    if (tool != null)
                    {
                        customInspector.target = tool;
                    }
                    else
                    {
                        var g = target as ToolbarGroup;
                        customInspector.group = g;
                    }
                    customInspector.OnEnable();
                }

            }

            CreateBaseInspector(propertyContainer);

            if (customInspector != null)
            {
                customInspector.CreateInspector(customPropertyContainer);
            }
            else
            {
                CreateDefaultInspector(customPropertyContainer);
            }


            isBinding = true;
            toolBindingSet?.Bind();
            toolConfigBindingSet?.Bind();
            root.Bind(serializedObject);

            //调用 Bind 之后 90ms 内触发 RegisterValueChangeCallback
            root.schedule.Execute(() =>
            {
                //bind complete
                isBinding = false;
            }).ExecuteLater(200);

        }

        private void ToolConfig_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            DiryTarget();
        }

        private void CreateBaseInspector(VisualElement root)
        {

            if (inspectorObject.TargetType == ToolbarInspectorObject.SelectionObjectType.Tool)
            {
                var tool = inspectorObject.Tool;
                if (tool != null)
                {
                    var toolConfig = EditorToolbarUtility.FindToolConfig(inspectorObject.Tool, out var group);

                    var p = serializedObject.FindProperty("toolConfig").Copy();
                    if (p.NextVisible(true))
                    {
                        int depth = p.depth;
                        do
                        {
                            if (p.depth < depth)
                                break;
                            var propField = new PropertyField(p);
                            propField.RegisterValueChangeCallback(e =>
                            {
                                DiryTarget();
                            });
                            root.Add(propField);
                        } while (p.NextVisible(false));
                    }
                    root.Bind(serializedObject);

                    toolConfigBindingSet.CreateBinding(root);
                }
            }
        }

        public void CreateDefaultInspector(VisualElement root)
        {

            if (targetProperty != null && targetProperty.serializedObject.targetObject)
            {

                var property = targetProperty.Copy();
                if (property.NextVisible(true))
                {
                    int depth = property.depth;
                    do
                    {
                        if (property.depth < depth)
                            break;
                        var p = property.Copy();
                        var inputField = new PropertyField(p);
                        root.Add(inputField);
                        //field.BindProperty(property);

                        inputField.RegisterValueChangeCallback(e =>
                        {
                            var f = e.currentTarget as PropertyField;
                            if (!isBinding)
                            {
                                DiryTarget();
                            }
                        });
                    } while (property.NextVisible(false));
                }
            }

            var tool = inspectorObject.tool;
            if (tool != null)
            {
                MethodInfo bindMethod = null;
                foreach (var method in toolBindingSet.GetType().GetMethods())
                {
                    if (method.Name == nameof(toolBindingSet.Bind) && method.IsGenericMethod)
                    {
                        var ps = method.GetParameters();
                        var valueType = method.GetGenericArguments()[0];
                        if (ps.Length == 3 && ps[0].ParameterType == typeof(INotifyValueChanged<>).MakeGenericType(valueType))
                        {
                            bindMethod = method;
                            break;
                        }
                    }
                }

                foreach (var item in EditorUIUtility.CreateInputFields(tool,
                    filter: mInfo =>
                    {
                        if (mInfo.DeclaringType == typeof(EditorToolbar))
                            return false;
                        return true;
                    }))
                {
                    var inputField = item.Item1;
                    var mInfo = item.Item2;
                    var notifyType = inputField.GetType().FindByGenericTypeDefinition(typeof(INotifyValueChanged<>));
                    if (notifyType != null)
                    {
                        Type valueType = notifyType.GetGenericArguments()[0];
                        bindMethod.MakeGenericMethod(valueType).Invoke(toolBindingSet, new object[] { inputField, mInfo.Name, BindingMode.TwoWay });
                    }
                    root.Add(inputField);
                }

                if (tool.UserSettings != null)
                {
                    foreach (var method in toolUserSettingsBindingSet.GetType().GetMethods())
                    {
                        if (method.Name == nameof(toolBindingSet.Bind) && method.IsGenericMethod)
                        {
                            var ps = method.GetParameters();
                            var valueType = method.GetGenericArguments()[0];
                            if (ps.Length == 3 && ps[0].ParameterType == typeof(INotifyValueChanged<>).MakeGenericType(valueType))
                            {
                                bindMethod = method;
                                break;
                            }
                        }
                    }
                    Label userHeader = null;

                    foreach (var item in EditorUIUtility.CreateInputFields(tool.UserSettings))
                    {
                        var inputField = item.Item1;
                        var mInfo = item.Item2;
                        var notifyType = inputField.GetType().FindByGenericTypeDefinition(typeof(INotifyValueChanged<>));
                        if (notifyType != null)
                        {
                            Type valueType = notifyType.GetGenericArguments()[0];
                            bindMethod.MakeGenericMethod(valueType).Invoke(toolUserSettingsBindingSet, new object[] { inputField, mInfo.Name, BindingMode.TwoWay });
                        }

                        if (userHeader == null)
                        {
                            userHeader = new Label();
                            userHeader.AddToClassList("user-settings-header");
                            userHeader.text = "User Settings";
                            root.Add(userHeader);
                        }

                        root.Add(inputField);
                    }

                }
            }
        }

        void Update()
        {
            if (!serializedObject.targetObject)
                return;

            if (nextSaveTime.HasValue && nextSaveTime < DateTime.Now)
            {
                nextSaveTime = null;
                EditorToolbarSettings.Save();
                EditorToolbar.Rebuild();
                EditorToolbarSettingsProvider.updateList = true;
            }
        }

        public void DiryTarget()
        {
            nextSaveTime = DateTime.Now.AddSeconds(0.5f);
        }

    }
}