using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;
using UnityEngine.UIElements;
using System.Linq;
using System.Reflection;
using UnityEditor.UIElements;

namespace Unity.Toolbars.Editor
{
    public class CustomInspectorAttribute : Attribute
    {
        public CustomInspectorAttribute(Type inspectedType)
        {
            this.InspectedType = inspectedType;
        }

        public Type InspectedType { get; set; }
    }

    public abstract class CustomInspector
    {
        public VisualElement root;
        public EditorToolbar target;
        public ToolbarGroup group;
        public ToolbarInspector editor;
        public SerializedProperty targetProperty;


        private static Dictionary<Type, Type> inspectorTypes;

        public virtual void OnEnable()
        {
        }
         
        public virtual void CreateInspector(VisualElement root)
        {
            editor.CreateDefaultInspector(root);
        }


        public virtual void OnUpdate()
        {

        }


        public virtual void OnDisable()
        {

        }


        public static Type GetInspectorType(Type inspectedType)
        {
            if (inspectorTypes == null)
            {
                inspectorTypes = new Dictionary<Type, Type>();

                foreach (var type in TypeCache.GetTypesWithAttribute<CustomInspectorAttribute>()
                    .Where(o => !o.IsAbstract))
                {
                    var attr = type.GetCustomAttribute<CustomInspectorAttribute>();
                    if (attr == null)
                        continue;
                    if (attr.InspectedType == null)
                        continue;
                    inspectorTypes[attr.InspectedType] = type;
                }
            }

            Type t = inspectedType;
            while (t != null)
            {
                if (inspectorTypes.TryGetValue(t, out var editorType))
                {
                    return editorType;
                }

                t = t.BaseType;
            }

            return null;
        }

    }

}