using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GUIExtensions;
using UnityEngine;

namespace UnityEditor.Toolbars
{


    class EditorToolbarSettingsWindow : EditorWindow
    {

        [MenuItem("Window/General/Toolbar")]
        static void OpenSettings()
        {
            GetWindow<EditorToolbarSettingsWindow>().Show();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Toolbar Settings");
        }

        private void OnGUI()
        {
            using (var checker = new EditorGUI.ChangeCheckScope())
            {

                foreach (var g in EditorToolbar.toolbars.OrderBy(o => o.Order)
                    .OrderBy(o => o.Position == ToolbarPosition.LeftToolbar ? 0 : 1)
                    .GroupBy(o => o.Position))
                {
                    GUILayout.Space(5);
                    switch (g.Key)
                    {
                        case ToolbarPosition.LeftToolbar:
                            GUILayout.Label("Left");
                            break;
                        case ToolbarPosition.RightToolbar:
                            GUILayout.Label("Right");
                            break;
                    }

                    foreach (var toolbar in g)
                    {
                        using (var header = new EditorGUILayoutx.Scopes.FoldoutHeaderGroupScope(false, new GUIContent(toolbar.GetType().Name)))
                        {
                            if (header.Visiable)
                            {
                                toolbar.IsEnabled = EditorGUILayout.Toggle("Enabled", toolbar.IsEnabled);
                                toolbar.Position = (ToolbarPosition)EditorGUILayout.EnumPopup("Position", toolbar.Position);
                                toolbar.Order = EditorGUILayout.IntField("Order", toolbar.Order);
                                //toolbar.Space = EditorGUILayout.Toggle("Space", toolbar.Space);
                            }
                        }
                    }
                }

                if (checker.changed)
                {
                    EditorToolbarSettings.Settings.Save(EditorToolbar.toolbars);
                }
            }

        }

    }
}
