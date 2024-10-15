using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.UI.Editor;

namespace Unity.Toolbars.Editor
{

    [Serializable]
    internal class ToolbarConfig : INotifyPropertyChanged, ISerializationCallbackReceiver
    {

        public ToolbarConfig() { }

        public ToolbarConfig(EditorToolbar target)
        {
            this.target = target;
            this.id = target.Id;
            this.enabled = target.Enabled;
            this.name = target.Name;
        }


        private bool init;
        public bool Initialized { get => init; set => init = value; }

        [SerializeField, HideInInspector]
        private string id;
        public string Id { get => id; set => id = value; }

        [SerializeField, HideInInspector]
        private string type;
        public string Type { get => type; set => type = value; }


        // 不能用<see cref="YMFramework.SerializableObject{T}"/> 序列化, 工具实例由外部创建        
        [SerializeField, HideInInspector]
        private string data;
        public string Data { get => data; set => data = value; }


        [SerializeField]
        private ConfigValue<bool> enabled = ConfigValueKeyword.Null;
        public ConfigValue<bool> Enabled { get => enabled; set => PropertyChanged.Invoke(this, nameof(Enabled), ref enabled, value); }

        [SerializeField]
        private ConfigValue<string> name = ConfigValueKeyword.Null;
        public ConfigValue<string> Name { get => name; set => PropertyChanged.Invoke(this, nameof(Name), ref name, value); }

        //   [ItemDrawer(typeof(BuiltinIconField), InitalizeMethod = nameof(InitalizeIconField))]
        [SerializeField, HideInInspector]
        private ConfigValue<BuiltinIcon> icon = ConfigValueKeyword.Null;
        //public ConfigValue<Texture2D> Icon
        //{
        //    get => new ConfigValue<Texture2D>(icon.Keyword, icon.Value.Image);
        //    set
        //    {
        //        ConfigValue<BuiltinIcon> newValue;
        //        if (value.Keyword == ConfigValueKeyword.Undefined)
        //        {
        //            newValue = new ConfigValue<BuiltinIcon>(new BuiltinIcon(value.Value));
        //        }
        //        else
        //        {
        //            newValue = new ConfigValue<BuiltinIcon>(value.Keyword);
        //        }
        //        PropertyChanged.Invoke(this, nameof(Icon), ref icon, newValue);
        //    }
        //}

        [SerializeField, HideInInspector]
        private ConfigValue<string> text = ConfigValueKeyword.Null;
        //public ConfigValue<string> Text { get => text; set => PropertyChanged.Invoke(this, nameof(Text), ref text, value); }

        [SerializeField, HideInInspector]
        private ConfigValue<string> tooltip = ConfigValueKeyword.Null;
        //public ConfigValue<string> Tooltip { get => tooltip; set => PropertyChanged.Invoke(this, nameof(Tooltip), ref tooltip, value); }


        [SerializeField, HideInInspector]
        private bool manualCreated;
        public bool ManualCreated { get => manualCreated; set => PropertyChanged.Invoke(this, nameof(ManualCreated), ref manualCreated, value); }

        [NonSerialized]
        private bool available;
        public bool Available { get => available; set => available = value; }

        [NonSerialized]
        private bool instanceCreated;
        public bool InstanceCreated { get => instanceCreated; set => PropertyChanged.Invoke(this, nameof(InstanceCreated), ref instanceCreated, value); }

        [SerializeField, MultiLine(3), HideInInspector]
        private ConfigValue<string> description = ConfigValueKeyword.Null;
        //public ConfigValue<string> Description { get => description; set => PropertyChanged.Invoke(this, nameof(Description), ref description, value); }

        //[SerializeField]
        private SettingsScope scope = SettingsScope.Project;
        public SettingsScope Scope { get => scope; set => PropertyChanged.Invoke(this, nameof(Scope), ref scope, value); }


        [NonSerialized]
        internal EditorToolbar target;
        public event PropertyChangedEventHandler PropertyChanged;

        public EditorToolbar Target { get => target; set => target = value; }

        public void Initalize()
        {
            var target = Target;
            if (target == null)
                return;

            if (string.IsNullOrEmpty(target.Id))
            {
                target.Id = Guid.NewGuid().ToString();
                target.manualCreated = true;
            }

            Id = target.Id;
            Type = target.GetType().AssemblyQualifiedName;
            ManualCreated = target.manualCreated;
            Enabled = target.Enabled;
            //Tooltip = target.Tooltip;
            //Text = target.Text;
            Initialized = true;
        }

        static void InitalizeIconField(BuiltinIconField field, string fieldName)
        {
            field.AllowBuiltin = true;
            //field.style.height = 24;
            //field.RegisterValueChangedCallback(e =>
            //{
            //    Texture2D newIcon = e.newValue;
            //    field.schedule.Execute(() =>
            //    {
            //        var inspectorObject = Selection.activeObject as ToolbarInspectorObject;
            //        if (inspectorObject)
            //        {
            //            var toolConfig = inspectorObject.toolConfig;
            //            toolConfig.iconName = null;
            //            if (newIcon)
            //            {
            //            }
            //        }
            //    });
            //});
        }

        public void UpdateTarget()
        {
            var target = Target;
            if (target == null)
                return;
            target.Id = Id;
            target.manualCreated = manualCreated;

            //target.name = name;
            //target.text = text;
            //target.enabled = enabled;
            //target.icon = icon;
            //target.tooltip = tooltip;
            //target.description = description;

            if (Name.Keyword == ConfigValueKeyword.Undefined)
            {
                target.Name = Name;
            }
            if (Enabled.Keyword == ConfigValueKeyword.Undefined)
            {
                target.Enabled = Enabled;
            }
            //if (Icon.Keyword == ConfigValueKeyword.Undefined)
            //{
            //    target.Icon = Icon;
            //}
            //if (Text.Keyword == ConfigValueKeyword.Undefined)
            //{
            //    target.Text = Text;
            //}
            //if (Tooltip.Keyword == ConfigValueKeyword.Undefined)
            //{
            //    target.Tooltip = Tooltip;
            //}
            //if (Description.Keyword == ConfigValueKeyword.Undefined)
            //{
            //    target.Description = Description;
            //}
        }


        public EditorToolbar GetOrCreateTarget()
        {
            if (target != null)
                return target;
            EditorToolbar tool = null;
            if (!string.IsNullOrEmpty(Type))
            {
                Type type = System.Type.GetType(Type);
                if (type != null)
                    tool = Activator.CreateInstance(type) as EditorToolbar;
            }
            target = tool;
            if (tool != null)
            {
                if (!string.IsNullOrEmpty(Data))
                {
                    EditorJsonUtility.FromJsonOverwrite(Data, tool);
                }
                UpdateTarget();
            }

            return target;
        }

        public void OnBeforeSerialize()
        {

        }

        public void OnAfterDeserialize()
        {

        }

        public override string ToString()
        {
            return $"{Id}({Name}) Target:{(Target == null ? "null" : Target.GetType().Name)}";
        }
    }
}
