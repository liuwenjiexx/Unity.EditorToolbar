using System;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using UnityEditor;
using System.Linq;

namespace Unity.Toolbars.Editor
{

    [Serializable]
    public class PlayTool : EditorToolbarButton
    {
        [SerializeField, HideInInspector]
        private string sceneGuid;

        private PlayTool()
        {
        }


        public string SceneGuid
        {
            get => sceneGuid;
            set
            {
                if (SetProperty(nameof(SceneGuid), ref sceneGuid, value))
                {
                    Refresh();
                }
            }
        }

        public string AssetPath
        {
            get
            {
                string assetPath = null;
                string guid = SceneGuid;
                if (!string.IsNullOrEmpty(guid))
                {
                    assetPath = AssetDatabase.GUIDToAssetPath(guid);
                }
                return assetPath;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    SceneGuid = AssetDatabase.AssetPathToGUID(value);
                }
                else
                {
                    SceneGuid = null;
                }
            }
        }

        public override string Tooltip
        {
            get
            {
                if (base.tooltip == ConfigValueKeyword.Null)
                {
                    if (!string.IsNullOrEmpty(AssetPath))
                    {
                        return $"Play '{Path.GetFileNameWithoutExtension(AssetPath)}'";
                    }
                }
                return base.Tooltip;
            }
            set => base.Tooltip = value;
        }

        public override bool IsAvailable
        {
            get => base.IsAvailable && !string.IsNullOrEmpty(SceneGuid);
            set => base.IsAvailable = value;
        }

        public bool IsSceneOpened
        {
            get
            {
                if (AssetPath == EditorSceneManager.GetActiveScene().path)
                {
                    return true;
                }
                return false;
            }
        }
        public override Texture2D Icon
        {
            get => base.icon == ConfigValueKeyword.Null ? Icons.PlayButtonOn : base.Icon;
            set => base.Icon = value;
        }

        public void Play()
        {
            string scenePath = AssetPath;
            if (!IsSceneOpened)
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                }
                else
                {
                    return;
                }
            }
            EditorApplication.isPlaying = true;
        }

        public void OpenScene()
        {
            if (!IsAvailable)
                return;

            string scenePath = AssetPath;
            if (IsSceneOpened)
                return;

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }
        }

        protected override void OnClick()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                return;
            }
            string assetPath = null;
            if (!string.IsNullOrEmpty(SceneGuid))
                assetPath = AssetDatabase.GUIDToAssetPath(SceneGuid);

            if (string.IsNullOrEmpty(assetPath))
                return;

            Play();
        }

        protected override void OnCreateContextMenu(DropdownMenu menu)
        {
            string path = EditorSceneManager.GetActiveScene().path;

            if (!string.IsNullOrEmpty(path))
            {/*
                menu.AppendAction($"Set Start Play '{Path.GetFileNameWithoutExtension(path)}'", (menuItem) =>
                {
                    string path = EditorSceneManager.GetActiveScene().path;
                    if (!string.IsNullOrEmpty(path))
                    {
                        SetDefaultScene(path);
                    }
                });*/
            }
            menu.AppendAction("Select", (menuItem) =>
            {
                string path = AssetPath;
                if (!string.IsNullOrEmpty(path))
                {
                    var obj = AssetDatabase.LoadMainAssetAtPath(path);
                    if (obj)
                    {
                        EditorGUIUtility.PingObject(obj);
                    }
                }
            });

            menu.AppendAction("Open", (menuItem) =>
            {
                OpenScene();
            });

            //   menu.AppendSeparator();

            /*   int i = 0;
               foreach (var scene in EditorBuildSettings.scenes
                   .Where(o => !string.IsNullOrEmpty(o.path))
                   .OrderBy(o => Path.GetFileNameWithoutExtension(o.path)))
               {
                   string name = Path.GetFileNameWithoutExtension(scene.path);
                   bool selected = false;
                   if (!string.IsNullOrEmpty(SceneGuid))
                   {
                       selected = scene.guid.ToString() == SceneGuid;
                   }
                   else if (i == 0)
                   {
                       selected = true;
                   }

                   menu.AppendAction(name, (o) =>
                   {
                       OpenScene((string)o.userData, false, o.eventInfo.modifiers == EventModifiers.Control);
                   }, (o) => selected ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal, scene.path);
                   i++;
               }*/
        }

        [EditorToolbar]
        static EditorToolbar PlayScene()
        {
            PlayTool tool = new PlayTool();
            tool.Group = GROUP_SCENE;
            return tool;
        }
    }
}
