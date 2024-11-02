using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EditorToolbars
{
    [Serializable]
    public class ToolbarInspectorObject : ScriptableObject
    {
        [SerializeField]
        public SelectionObjectType targetType;

        [SerializeField]
        public EditorToolbar tool;

        [SerializeField]
        internal ToolbarConfig toolConfig;

        [SerializeField]
        public ToolbarGroup toolGroup;
         
        public SelectionObjectType TargetType => targetType;

        public EditorToolbar Tool
        {
            get => tool;
            set
            {
                bool changed = false;
                if (targetType != SelectionObjectType.Tool)
                {
                    targetType = SelectionObjectType.Tool;
                    //toolGroup = null;
                    changed = true;
                }

                if (tool != value)
                {
                    tool = value; 
                      toolConfig = EditorToolbarUtility.FindToolConfig(tool, out var group);
                    changed = true;
                }
                if (changed)
                {
                    NotifySelectionChange();
                }
            }
        }

        public ToolbarGroup ToolGroup
        {
            get => toolGroup;
            set
            {
                bool changed = false;
                if (targetType != SelectionObjectType.ToolGroup)
                {
                    targetType = SelectionObjectType.ToolGroup;
                    //tool = null;
                    changed = true;
                }

                if (toolGroup != value)
                {
                    toolGroup = value;
                    changed = true;
                }
                if (changed)
                {
                    NotifySelectionChange();
                }
            }
        }

        public object Target => targetType switch { SelectionObjectType.ToolGroup => toolGroup, _ => tool };

        public event Action SelectionChanged;

        private void OnDisable()
        {
            Clear();
        }

        public void NotifySelectionChange()
        {
            SelectionChanged?.Invoke();
        }

        public void Clear()
        {
            if (targetType == SelectionObjectType.Tool)
            {
                Tool = null;
                toolConfig = null;
            }
            if (targetType == SelectionObjectType.ToolGroup)
            {
                ToolGroup = null;
            }
        }

        public enum SelectionObjectType
        {
            Tool,
            ToolGroup,
        }

    }
}