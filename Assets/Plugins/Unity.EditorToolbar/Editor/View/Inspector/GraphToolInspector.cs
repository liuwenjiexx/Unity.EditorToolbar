#if NODE_GRAPH
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Yanmonet.NodeGraph;

namespace Yanmonet.Toolbars.Editor
{

    [CustomInspector(typeof(GraphTool))]
    class GraphToolInspector : CustomInspector
    {
        GraphTool Tool => (GraphTool)target;

        public override void CreateInspector(VisualElement root)
        {
            base.CreateInspector(root);

            ObjectField fldObj = new ObjectField();

            fldObj.objectType = typeof(NodeGraphModel);
            fldObj.label = "Graph";
            fldObj.value = Tool.Graph;
            fldObj.RegisterValueChangedCallback(e =>
            {
                var graph = e.newValue as NodeGraphModel;
                Tool.Graph = graph;
                EditorToolbarSettings.Save();
            });
            root.Add(fldObj);
        }
    }
}

#endif