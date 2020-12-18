using UnityEditor.SceneManagement;
using UnityEngine;


namespace UnityEditor.Toolbars
{
    class OpenPreviousSceneTool : EditorToolbarButton
    {
        private GUIContent icon;
        private static string prevSceneGuid;
        private static string currentSceneGuid;

        public OpenPreviousSceneTool()
        {
            EditorSceneManager.activeSceneChangedInEditMode -= EditorSceneManager_activeSceneChangedInEditMode;
            EditorSceneManager.activeSceneChangedInEditMode += EditorSceneManager_activeSceneChangedInEditMode;
            string scenePath = EditorSceneManager.GetActiveScene().path;
            UpdateScene(scenePath);

        }

        private static void EditorSceneManager_activeSceneChangedInEditMode(UnityEngine.SceneManagement.Scene prev, UnityEngine.SceneManagement.Scene next)
        {
            string path = next.path;
            UpdateScene(path);
        }

        public override GUIContent Icon
        {
            get
            {
                if (icon == null)
                    icon = new GUIContent(EditorGUIUtility.IconContent("BuildSettings.Editor.Small").image, "Switch previous scene");
                return icon;
            }
        }

        public override bool IsAvailable => !Application.isPlaying;
        public static string PlayerPrefsKeyPrefix = "UnityEditor.Toolbars.";
        public static string PreviousSceneGuidKey = PlayerPrefsKeyPrefix + typeof(OpenPreviousSceneTool).Name + ".Previous.Guid";
        public static string CurrentSceneGuidKey = PlayerPrefsKeyPrefix + typeof(OpenPreviousSceneTool).Name + ".Current.Guid";
        public static string PreviousSceneGuid
        {
            get
            {
                if (prevSceneGuid == null)
                {
                    prevSceneGuid = PlayerPrefs.GetString(PreviousSceneGuidKey);
                    if (prevSceneGuid == null)
                        prevSceneGuid = string.Empty;
                }
                return prevSceneGuid;
            }
            set
            {
                if (prevSceneGuid != value)
                {
                    prevSceneGuid = value;
                    PlayerPrefs.SetString(PreviousSceneGuidKey, prevSceneGuid);
                    PlayerPrefs.Save();
                }
            }
        }
        public static string CurrentSceneGuid
        {
            get
            {
                if (currentSceneGuid == null)
                {
                    currentSceneGuid = PlayerPrefs.GetString(CurrentSceneGuidKey);
                    if (currentSceneGuid == null)
                        currentSceneGuid = string.Empty;
                }
                return currentSceneGuid;
            }
            set
            {
                if (currentSceneGuid != value)
                {
                    PreviousSceneGuid = CurrentSceneGuid;
                    currentSceneGuid = value;
                    PlayerPrefs.SetString(CurrentSceneGuidKey, currentSceneGuid);
                    PlayerPrefs.Save();
                }
            }
        }

        static void UpdateScene(string scenePath)
        {

            if (!string.IsNullOrEmpty(scenePath))
            {
                string guid = AssetDatabase.AssetPathToGUID(scenePath);
                if (!string.IsNullOrEmpty(guid) && guid != CurrentSceneGuid)
                {
                    CurrentSceneGuid = guid;
                }
            }
        }


        public override void OnClick()
        {
            string scenePath = null;
            if (!string.IsNullOrEmpty(PreviousSceneGuid))
                scenePath = AssetDatabase.GUIDToAssetPath(PreviousSceneGuid);
            if (!string.IsNullOrEmpty(scenePath))
            {
                if (scenePath != EditorSceneManager.GetActiveScene().path)
                {
                    EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                }

            }
        }
    }
}