using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Toolbars.Editor
{

    [CustomInspector(typeof(PlayTool))]
    class PlayToolInspector : CustomInspector
    {
        PlayTool Tool => (PlayTool)target;

        public override void CreateInspector(VisualElement root)
        {
            base.CreateInspector(root);

            ObjectField fldObj = new ObjectField();
            fldObj.objectType = typeof(SceneAsset);
            fldObj.label = "Scene";

            Object scene = null;
            if (!string.IsNullOrEmpty(Tool.SceneGuid))
            {
                scene = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(Tool.SceneGuid));
            }
            fldObj.value = scene;

            fldObj.RegisterValueChangedCallback(e =>
            {
                string guid = null;
                if (e.newValue)
                {
                    guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(e.newValue));
                }

                if (Tool.SceneGuid != guid)
                {
                    Tool.SceneGuid = guid;
                    EditorToolbarSettings.Save();
                }
            });
            root.Add(fldObj);
  
        }
    }
}