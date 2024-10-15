using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.ComponentModel;
using UnityEditor;
using Unity.UI.Editor;
using UnityEngine.UIElements;

namespace Unity.Toolbars.Editor
{
    [Serializable]
    public class ToolbarGroup : INotifyPropertyChanged, ISerializationCallbackReceiver
    {
        internal ToolbarGroup()
        {

        }

        [SerializeField]
        private string name;
        public string Name { get => name; set => PropertyChanged.Invoke(this, nameof(Name), ref name, value); }

        [SerializeField]
        private ConfigValue<bool> enabled = ConfigValueKeyword.Null;
        public ConfigValue<bool> Enabled { get => enabled; set => PropertyChanged.Invoke(this, nameof(Enabled), ref enabled, value); }

        [SerializeField]
        private ConfigValue<ToolbarPosition> position = ConfigValueKeyword.Null;
        public ConfigValue<ToolbarPosition> Position { get => position; set => PropertyChanged.Invoke(this, nameof(Position), ref position, value); }

        protected BuiltinIcon defaultIcon;
        //Override Icon
        [SerializeField]
        protected ConfigValue<BuiltinIcon> icon = ConfigValueKeyword.Null;
        public virtual Texture2D Icon
        {
            get => defaultIcon.Image;
            set
            {
                if (defaultIcon.Image != value)
                {
                    PropertyChanged.Invoke(this, nameof(Icon), ref defaultIcon, new BuiltinIcon(value));
                }
            }
        }


        public virtual Texture2D OverrideIcon
        {
            get => icon.Keyword == ConfigValueKeyword.Undefined ? icon.Value.Image : null;
            set => PropertyChanged.Invoke(this, nameof(Icon), ref icon, new BuiltinIcon(value));
        }

        protected string defaultText;
        [SerializeField]
        private ConfigValue<string> text = ConfigValueKeyword.Null;
        public string Text
        {
            get => defaultText;
            set => PropertyChanged.Invoke(this, nameof(Text), ref defaultText, value);
        }

        public ConfigValue<string> OverrideText
        {
            get => text.Keyword == ConfigValueKeyword.Undefined ? text.Value : null;
            set => PropertyChanged.Invoke(this, nameof(Text), ref text, value);
        }


        private string defaultTooltip;
        [SerializeField]
        private ConfigValue<string> tooltip = ConfigValueKeyword.Null;
        public ConfigValue<string> Tooltip
        {
            get => defaultTooltip;
            set => PropertyChanged.Invoke(this, nameof(Tooltip), ref defaultTooltip, value);
        }

        public ConfigValue<string> OverrideTooltip
        {
            get => tooltip.Keyword == ConfigValueKeyword.Undefined ? tooltip.Value : null;
            set => PropertyChanged.Invoke(this, nameof(Tooltip), ref tooltip, value);
        }


        private string defaultDescription;
        [SerializeField, MultiLine(3)]
        private ConfigValue<string> description = ConfigValueKeyword.Null;
        public ConfigValue<string> Description
        {
            get => defaultDescription;
            set => PropertyChanged.Invoke(this, nameof(Description), ref defaultDescription, value);
        }
        public ConfigValue<string> OverrideDescription
        {
            get => description.Keyword == ConfigValueKeyword.Undefined ? description.Value : null;
            set => PropertyChanged.Invoke(this, nameof(Description), ref description, value);
        }

        [SerializeField, HideInInspector]
        internal bool manualCreated;


        [SerializeField, HideInInspector]
        internal List<ToolbarConfig> items = new List<ToolbarConfig>();

        public IEnumerable<EditorToolbar> Items { get => items.Select(o => o.Target).Where(o => o != null); }


        public event PropertyChangedEventHandler PropertyChanged;


        public static bool IsProjectScope(string groupName)
        {
            return !groupName.StartsWith(EditorToolbar.UserScopePrefix);
        }

        public void Add(EditorToolbar tool)
        {
            if (tool.Group == this)
                return;

            ToolbarConfig config;
            if (tool.Group != null)
            {
                config = tool.Group.GetToolConfig(tool);
                tool.Group.Remove(tool);
            }
            else
            {
                config = GetToolConfig(tool);
                if (config != null)
                {
                    config.Target = tool;
                    if (tool.group != this)
                    {
                        tool.group = this;
                    }
                    return;
                }


            }

            config = new ToolbarConfig(tool);
            int index = -1;

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                int order = item.Target != null ? item.Target.Order : 0;
                if (tool.Order < order)
                {
                    index = i;
                    break;
                }
            }

            if (index == -1)
            {
                items.Add(config);
            }
            else
            {
                items.Insert(index, config);
            }
            tool.group = this;
        }

        public bool Remove(string toolId)
        {
            if (string.IsNullOrEmpty(toolId))
                return false;
            int index;
            index = items.FindIndex(o => o.Id == toolId);
            if (index >= 0)
            {
                items.RemoveAt(index);
                return true;
            }
            return false;
        }

        public bool Remove(EditorToolbar tool)
        {
            if (tool == null)
                return false;
            int index;
            index = items.FindIndex(o => o.Id == tool.Id || o.Target == tool);
            if (index >= 0)
            {
                items.RemoveAt(index);
                return true;
            }
            return false;
        }

        internal ToolbarConfig GetToolConfig(string toolId)
        {
            if (string.IsNullOrEmpty(toolId))
                return null;
            return items.FirstOrDefault(o => o.Id == toolId);
        }
        internal ToolbarConfig GetToolConfig(EditorToolbar tool)
        {
            if (tool == null)
                return null;
            return items.FirstOrDefault(o => (tool.Id != null && o.Id == tool.Id) || o.Target == tool);
        }

        public VisualElement CreateGroupUI()
        {
            var view = new VisualElement();
            view.AddToClassList("toolbar-group");
            view.tooltip = OverrideTooltip.Keyword == ConfigValueKeyword.Undefined ? OverrideTooltip.Value : Tooltip;

            var text = OverrideText.Keyword == ConfigValueKeyword.Undefined ? OverrideText.Value : Text;
            bool hasIcon = false, hasText = false;
            if (!string.IsNullOrEmpty(text))
            {
                Label label = new Label();
                label.AddToClassList("toolbar-group-text");
                label.text = text;
                view.Add(label);
                hasText = true;
            }

            var icon = OverrideIcon ?? Icon;
            if (icon != null)
            {
                Image image = new Image();
                image.AddToClassList("toolbar-group-icon");
                image.image = icon;
                view.Add(image);
                hasIcon = true;
            }
            if (hasIcon && hasText)
            {
                view.AddToClassList("toolbar-group-text-icon");
            }

            return view;
        }


        public static implicit operator ToolbarGroup(string name)
        {
            var g = EditorToolbarSettings.AllGroups.FirstOrDefault(o => o.name == name);
            if (g == null)
            {
                g = new ToolbarGroup();
                g.name = name;
                g.position = ToolbarPosition.LeftToolbar;
                if (IsProjectScope(name))
                {
                    EditorToolbarSettings.Groups.Add(g);
                }
                else
                {
                    EditorToolbarUserSettings.Groups.Add(g);
                }

            }
            return g;
        }

        public string GetShortName()
        {
            return GetShortName(Name);
        }

        public static string GetShortName(string name)
        {
            var parts = name.Split(new char[] { '/' }, 2);

            if (parts.Length > 1)
                return parts[1];
            return parts[0];
        }

        public void OnBeforeSerialize()
        {

            //foreach (var tool in Items)
            //{
            //    ToolbarConfig itemConfig = new ToolbarConfig()
            //    {
            //        id = tool.Id,
            //        type = tool.GetType().AssemblyQualifiedName,
            //        data = EditorJsonUtility.ToJson(tool),
            //        manualCreated = tool.manualCreated,
            //    };
            //    var index = items.FindIndex(o => o.id == tool.Id);

            //    if (index >= 0)
            //    {
            //        items[index] = itemConfig;
            //    }
            //    else
            //    {
            //        items.Add(itemConfig);
            //    }
            //}



            //for (int i = 0; i < items.Count; i++)
            //{
            //    if (!items[i].manualCreated)
            //    {
            //        var tool = Items.Find(o => o.Id == items[i].id);
            //        if (tool == null)
            //        {
            //            items.RemoveAt(i);
            //            i--;
            //        }
            //    }
            //}

        }

        public void OnAfterDeserialize()
        {

        }

        public override string ToString()
        {
            return Name;
        }
    }
}