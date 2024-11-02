using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace EditorToolbars
{
    public static partial class EditorToolbarUtility
    {
        private static ToolbarInspectorObject inspectorObject;
        internal static ToolbarInspectorObject InspectorObject => inspectorObject = inspectorObject ?? ScriptableObject.CreateInstance<ToolbarInspectorObject>();

        public static bool IsProjectWorkspace
        {
            get => EditorToolbarUserSettings.IsProjectWorkspace;
            set
            {
                if (EditorToolbarUserSettings.IsProjectWorkspace != value)
                {
                    EditorToolbarUserSettings.IsProjectWorkspace = value;
                    EditorToolbarUserSettings.Provider.Save();
                    WorkspaceChanged?.Invoke();
                }
            }
        }

        public static Action WorkspaceChanged;

        public static IEnumerable<ToolbarGroup> AllGroups => EditorToolbarSettings.Groups.Concat(EditorToolbarUserSettings.Groups);

        public static event Action<EditorToolbar> ToolAdded;
        public static event Action<EditorToolbar> ToolRemoved;

        public static event Action<ToolbarGroup> GroupChanged;

        public static string GetDefaultGroupName()
        {
            if (IsProjectWorkspace)
                return EditorToolbar.GROUP_DEFAULT;
            return EditorToolbar.USER_GROUP_DEFAULT;
        }


        internal static void AddTool(EditorToolbar tool, Action<ToolbarConfig> callback=null)
        {
            if (string.IsNullOrEmpty(tool.Id))
            {
                tool.Id = Guid.NewGuid().ToString();
            }
            tool.manualCreated = true;
            if (tool.Group == null)
                tool.Group = EditorToolbar.GROUP_DEFAULT;

            var toolConfig = EditorToolbarUtility.FindToolConfig(tool, out var group);
            toolConfig.Target = tool;
            if (tool.Enabled)
            {
                toolConfig.Enabled = true;
            }
            callback?.Invoke(toolConfig);

            ToolAdded?.Invoke(tool);

            EditorToolbarSettings.Save();
            if (tool.Enabled)
            {
                EditorToolbar.Rebuild();
            }
        }


        internal static void RemoveTool(EditorToolbar tool)
        {
            if (!tool.manualCreated)
                return;
            bool chagned = false;
            bool enabled = tool.Enabled;
            tool.Enabled = false;

            if (tool.Group != null)
            {
                tool.Group = null;
                chagned = true;
            }

            if (InspectorObject.TargetType == ToolbarInspectorObject.SelectionObjectType.Tool)
            {
                if (InspectorObject.Tool == tool)
                {
                    InspectorObject.Tool = null;
                }
            }

            if (chagned)
            {
                EditorToolbarSettings.Save();
                if (enabled)
                {
                    EditorToolbar.Rebuild();
                }
                ToolRemoved?.Invoke(tool);
            }
        }

        internal static void OnGroupChanged(ToolbarGroup group)
        {
            GroupChanged?.Invoke(group);
        }

        internal static void Select(EditorToolbar tool)
        {
            InspectorObject.Tool = tool;
            SelectInspectorObject();
        }
        internal static void Select(ToolbarGroup group)
        {
            InspectorObject.ToolGroup = group;
            SelectInspectorObject();
        }
        private static void SelectInspectorObject()
        {
            if (InspectorObject.Target != null)
            {
                if (Selection.activeObject != InspectorObject)
                    Selection.activeObject = InspectorObject;
            }
            else
            {
                if (Selection.activeObject != null && Selection.activeObject is ToolbarInspectorObject)
                    Selection.activeObject = null;
            }
        }



        static Regex projectSchemeRegex;

        public static string ReplaceProjectScheme(string url)
        {
            if (string.IsNullOrEmpty(url))
                return url;

            if (projectSchemeRegex == null)
                projectSchemeRegex = new Regex("project://", RegexOptions.IgnoreCase);

            string projectPath = null;

            return projectSchemeRegex.Replace(url, m =>
            {
                if (projectPath == null)
                {
                    projectPath = Path.GetFullPath(".").Replace('\\', '/');
                    if (!projectPath.EndsWith('/'))
                        projectPath += "/";
                }
                return projectPath;
            });
        }



        public static ToolbarGroup FindGroup(string groupName)
        {
            return EditorToolbarSettings.AllGroups.FirstOrDefault(o => o.Name == groupName);
        }

        internal static ToolbarConfig FindToolConfig(EditorToolbar tool, out ToolbarGroup group)
        {
            group = null;
            if (tool == null)
                return null;
            foreach (var g in AllGroups)
            {
                var item = g.GetToolConfig(tool);
                if (item != null)
                {
                    group = g;
                    return item;
                }
            }
            return null;
        }

        internal static ToolbarConfig FindToolConfig(string toolId, out ToolbarGroup group)
        {
            group = null;
            if (string.IsNullOrEmpty(toolId))
                return null;
            foreach (var g in AllGroups)
            {
                var item = g.GetToolConfig(toolId);
                if (item != null)
                {
                    group = g;
                    return item;
                }
            }
            return null;
        }

        public static EditorToolbar FindTool(string toolId)
        {
            return EditorToolbar.tools.FirstOrDefault(o => o.Id == toolId);
        }


    }
}