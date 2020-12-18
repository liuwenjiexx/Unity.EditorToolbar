using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.Toolbars
{
    class PlayTool : EditorToolbarMenuButton
    {
        private GUIContent icon;
        private static string playSceneGuid;

        public override GUIContent Icon
        {
            get
            {
                if (icon == null)
                    icon = new GUIContent(EditorGUIUtility.IconContent("PlayButton On").image, "Play\n\n- shift key open scene\n- control key select play scene");
                return icon;
            }
        }


        public override bool IsAvailable => !Application.isPlaying;


        public static string PlaySceneGuid
        {
            get
            {
                if (playSceneGuid == null)
                {
                    playSceneGuid = PlayerPrefs.GetString("EditorTools.Scene.Play.Guid");
                    if (playSceneGuid == null)
                        playSceneGuid = string.Empty;
                }
                return playSceneGuid;
            }
            set
            {
                if (PlaySceneGuid != value)
                {
                    playSceneGuid = value;
                    PlayerPrefs.SetString("EditorTools.Scene.Play.Guid", playSceneGuid);
                    PlayerPrefs.Save();
                }
            }
        }




        private void OpenScene(object state)
        {
            object[] values = (object[])state;
            string scenePath = (string)values[0];
            bool playing = (bool)values[1];
            bool setPlaySceneIndex = (bool)values[2];

            if (setPlaySceneIndex)
            {
                var scenes = EditorBuildSettings.scenes;
                for (int i = 0; i < scenes.Length; i++)
                {
                    var scene = scenes[i];
                    if (scene.path == scenePath)
                    {
                        PlaySceneGuid = scene.guid.ToString();
                        break;
                    }
                }
                return;
            }

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                AssetDatabase.Refresh();

                if (scenePath != EditorSceneManager.GetActiveScene().path)
                {
                    EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                }
                if (playing)
                    EditorApplication.isPlaying = true;
            }
        }

        public override void OnClick()
        {
            string assetPath = null;
            if (!string.IsNullOrEmpty(PlaySceneGuid))
                assetPath = AssetDatabase.GUIDToAssetPath(PlaySceneGuid);
            if (string.IsNullOrEmpty(assetPath))
            {
                var scenes = EditorBuildSettings.scenes;

                if (scenes.Length > 0)
                {
                    assetPath = scenes[0].path;
                }
            }
            OpenScene(new object[] { assetPath, !Event.current.shift, false });
        }

        public override void OnMenu()
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Select"), false, () =>
            {
                var scenes = EditorBuildSettings.scenes;
                if (scenes.Length > 0)
                {
                    string assetPath = scenes[0].path;
                    EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(assetPath));
                }
            });
            menu.AddSeparator("");
            int i = 0;
            foreach (var scene in EditorBuildSettings.scenes)
            {
                string name = Path.GetFileNameWithoutExtension(scene.path);
                bool selected = false;
                if (!string.IsNullOrEmpty(PlaySceneGuid))
                {
                    selected = scene.guid.ToString() == PlaySceneGuid;
                }
                else if (i == 0)
                {
                    selected = true;
                }

                menu.AddItem(new GUIContent(name), selected, OpenScene, new object[] { scene.path, false, Event.current.control });
                i++;
            }

            menu.ShowAsContext();
        }


    }
}
