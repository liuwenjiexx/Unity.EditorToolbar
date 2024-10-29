#if NODE_GRAPH

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yanmonet.NodeGraph;
using UnityEditor;
using Yanmonet.NodeGraph.Editor;
using Unity.Async;

namespace Yanmonet.Toolbars.Editor
{
    [Serializable]
    public class GraphTool : EditorToolbarButton
    {
        /// <summary>
        /// 延迟加载资源
        /// </summary>
        [SerializeField]
        private string assetGuid;

        private NodeGraphModel graph;
        private bool running;

        public override string Name
        {
            get => base.name == ConfigValueKeyword.Null ? Graph?.Name : base.Name;
            set => base.Name = value;
        }

        public override string Tooltip
        {
            get => base.tooltip == ConfigValueKeyword.Null ? Graph?.Name : base.Tooltip;
            set => base.Tooltip = value;
        }

        public override string Description
        {
            get => base.description == ConfigValueKeyword.Null ? Graph?.description : base.Description;
            set => base.Description = value;
        }

        public NodeGraphModel Graph
        {
            get
            {
                if (!graph)
                {
                    if (!string.IsNullOrEmpty(assetGuid))
                    {
                        string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
                        if (!string.IsNullOrEmpty(assetPath))
                        {
                            graph = AssetDatabase.LoadAssetAtPath<NodeGraphModel>(assetPath);
                        }
                    }
                }
                return graph;
            }
            set
            {
                graph = value;
                assetGuid = null;
                if (graph != null)
                {
                    var assetPath = AssetDatabase.GetAssetPath(graph);
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        var newGuid = AssetDatabase.AssetPathToGUID(assetPath);
                        if (newGuid != assetGuid)
                        {
                            assetGuid = newGuid;
                        }
                    }

                }
            }
        }

        protected override async void OnClick()
        {
            if (running)
                return;
            var graph = Graph;
            if (!graph)
                return;
            running = true;

            try
            {
                await EditorNodeGraphUtility.Execute(graph);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                running = false;
            }
        }

        [CreateFrom]
        public static EditorToolbar CreateFrom(NodeGraphModel graph)
        {
            if (!graph) return null;
            if ((graph.flags & NodeGraphFlags.NotExecutable) == NodeGraphFlags.NotExecutable) return null;
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(graph.GetInstanceID(), out var assetGuid, out long id);
            if (string.IsNullOrEmpty(assetGuid)) return null;

            GraphTool tool = new GraphTool();
            tool.Graph = graph;

            //if (!string.IsNullOrEmpty(graph.customName))
            //    tool.Name = graph.customName;
            //else
            //    tool.Name = graph.name;

            return tool;
        }

    }
}

#endif