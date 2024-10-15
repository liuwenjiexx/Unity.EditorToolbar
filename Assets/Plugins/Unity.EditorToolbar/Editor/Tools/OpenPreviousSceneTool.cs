using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEditor;
using System;
using UnityEngine.SceneManagement;
using System.IO;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Unity.Toolbars.Editor
{
    /// <summary>
    /// 打开上一次打开的场景
    /// </summary>
    [Serializable]
    class OpenPreviousSceneTool : EditorToolbarButton
    {
        static OpenPreviousSceneTool instance;

        static int MaxHistoryScene = 5;

        private OpenPreviousSceneTool()
        {
        }

        protected override ToolbarStyle Style => ToolbarStyle.MenuButton;

        public override Texture2D Icon
        {
            get => base.icon == ConfigValueKeyword.Null ? Icons.Scene : base.Icon;
            set => base.Icon = value;
        }

        public override string Tooltip
        {
            get
            {
                string tooltip = base.Tooltip;
                if (base.tooltip == ConfigValueKeyword.Null)
                {
                    if (!string.IsNullOrEmpty(PreviousSceneGuid))
                    {
                        string assetPath = AssetDatabase.GUIDToAssetPath(PreviousSceneGuid);
                        if (!string.IsNullOrEmpty(assetPath))
                        {
                            string sceneName = Path.GetFileNameWithoutExtension(assetPath);
                            tooltip = $"Swtich scene '{sceneName}'";
                        }
                    }
                    if (string.IsNullOrEmpty(tooltip))
                        tooltip = "Swtich Previous Scene";
                }
                else
                {
                    tooltip = base.Tooltip;
                }

                return tooltip;
            }
            set => base.Tooltip = value;
        }

        public new ToolUserSettings UserSettings
        {
            get
            {
                var settings = base.UserSettings as ToolUserSettings;
                if (settings == null)
                {
                    settings = new ToolUserSettings();
                    base.UserSettings = settings;
                }
                return settings;
            }
            set => base.UserSettings = value;
        }


        public string PreviousSceneGuid
        {
            get => UserSettings.PrevSceneGuid;
            set
            {
                if (UserSettings.PrevSceneGuid != value)
                {
                    UserSettings.PrevSceneGuid = value;
                    OnPreviousSceneChanged();
                }
            }
        }
        public string CurrentSceneGuid
        {
            get => UserSettings.CurrentSceneGuid;
            set
            {
                if (UserSettings.CurrentSceneGuid != value)
                {
                    PreviousSceneGuid = CurrentSceneGuid;
                    UserSettings.CurrentSceneGuid = value;
                    if (!string.IsNullOrEmpty(UserSettings.CurrentSceneGuid))
                    {
                        UserSettings.HistorySceneGuids.Add(UserSettings.CurrentSceneGuid);
                        if (UserSettings.HistorySceneGuids.Count > MaxHistoryScene)
                            UserSettings.HistorySceneGuids.RemoveAt(0);
                    }
                }
            }
        }

        public override void Enable()
        {
            EditorSceneManager.activeSceneChangedInEditMode -= EditorSceneManager_activeSceneChangedInEditMode;
            EditorSceneManager.activeSceneChangedInEditMode += EditorSceneManager_activeSceneChangedInEditMode;

            //string scenePath = SceneManager.GetActiveScene().path;
            //UpdateScene(scenePath);
            base.Enable();
            Refresh();
        }

        public override void Disable()
        {
            EditorSceneManager.activeSceneChangedInEditMode -= EditorSceneManager_activeSceneChangedInEditMode;
            base.Disable();
        }

        protected override void OnCreateMenu(DropdownMenu menu)
        {
            for (int i = UserSettings.HistorySceneGuids.Count - 2; i >= 0; i--)
            {
                var guid = UserSettings.HistorySceneGuids[i];
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(assetPath))
                {
                    continue;
                }
                DropdownMenuAction.Status status = DropdownMenuAction.Status.Normal;
                if (SceneManager.GetActiveScene().path == assetPath)
                {
                    status |= DropdownMenuAction.Status.Checked | DropdownMenuAction.Status.Disabled;
                }

                string sceneName = Path.GetFileNameWithoutExtension(assetPath);
                menu.AppendAction(sceneName, act =>
                {
                    OpenScene(assetPath);
                }, status);
            }
        }

        private void EditorSceneManager_activeSceneChangedInEditMode(Scene prev, Scene next)
        {
            if (Application.isPlaying)
                return;
            string path = next.path;
            UpdateScene(path);
        }

        protected override void OnCreateContextMenu(DropdownMenu menu)
        {
            menu.AppendAction("Select", act =>
            {
                string assetPath = SceneManager.GetActiveScene().path;
                var obj = AssetDatabase.LoadMainAssetAtPath(assetPath);
                if (obj)
                {
                    EditorGUIUtility.PingObject(obj);
                }
            });

        }


        void UpdateScene(string scenePath)
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

        protected override void OnUpdate()
        {
            IsAvailable = !Application.isPlaying;
        }

        protected override void OnClick()
        {
            string scenePath = null;
            if (!string.IsNullOrEmpty(PreviousSceneGuid))
                scenePath = AssetDatabase.GUIDToAssetPath(PreviousSceneGuid);
            OpenScene(scenePath);
        }

        void OpenScene(string scenePath)
        {
            if (string.IsNullOrEmpty(scenePath))
                return;
            if (scenePath == SceneManager.GetActiveScene().path)
                return;
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }
        }

        void OnPreviousSceneChanged()
        {
            NotifyPropertyChanged(nameof(Tooltip));
            Refresh();
            EditorToolbarUserSettings.Settings.Save();
        }

        public override void Refresh()
        {
            base.Refresh();
        }


        [EditorToolbar]
        static EditorToolbar OpenPreviousScene()
        {
            OpenPreviousSceneTool tool = new OpenPreviousSceneTool();
            tool.Group = GROUP_SCENE;
            instance = tool;
            return tool;
        }

        [Serializable]
        public class ToolUserSettings
        {
            [SerializeField, HideInInspector]
            private string prevSceneGuid;

            [SerializeField, HideInInspector]
            private string currentSceneGuid;

            [SerializeField, HideInInspector]
            private List<string> historySceneGuids = new List<string>();

            public string PrevSceneGuid { get => prevSceneGuid; set => prevSceneGuid = value; }

            public string CurrentSceneGuid { get => currentSceneGuid; set => currentSceneGuid = value; }

            public List<string> HistorySceneGuids => historySceneGuids;
        }

    }



}