using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Unity.Toolbars.Editor
{
    public partial class EditorToolbar
    {
        public class Icons
        {
            public static Texture2D PlayButtonOn { get => GetIcon("PlayButton On"); }

            public static Texture2D Scene { get => GetIcon("BuildSettings.Editor.Small"); }

            public static Texture2D OpenedFolder
            {
                get
                {
#if UNITY_2020_1_OR_NEWER
                    return GetIcon("Folder Icon");
#else
                    return GetIcon("OpenedFolder On Icon");
#endif
                }
            }
            static Texture2D GetIcon(string name)
            {
                return EditorGUIUtility.IconContent(name)?.image as Texture2D;
            }
        }

    }
}