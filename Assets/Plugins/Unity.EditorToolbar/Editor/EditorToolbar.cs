using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using System.Reflection.Extensions;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{

    [InitializeOnLoad]
    public class EditorToolbar
    {

        static Type toolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");
        static Type guiViewType = typeof(Editor).Assembly.GetType("UnityEditor.GUIView");

        static PropertyInfo viewVisualTreeProperty = guiViewType.GetProperty("visualTree", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        //static FieldInfo imguiContainerOnGuiField = typeof(IMGUIContainer).GetField("m_OnGUIHandler", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        static ScriptableObject currentToolbar;

        static int toolCount = 4;
        static GUIStyle commandStyle = null;

        public static event Action OnToolbarGUI;
        public static event Action OnToolbarLeftGUI;
        public static event Action OnToolbarRightGUI;
        static IMGUIContainer guiContainer;

        public const string PackageName = "Unity.EditorToolbar";
        private static GUIStyle toolbarButtonStyle;
        private static GUIStyle toolbarMenuButtonStyle;

        private static string packageDir;
        public static string PackageDir
        {
            get
            {
                if (string.IsNullOrEmpty(packageDir))
                    packageDir = GetPackageDirectory(PackageName);
                return packageDir;
            }
        }

        static GUIStyle ToolbarButtonStyle
        {
            get
            {
                if (toolbarButtonStyle == null)
                {
                    toolbarButtonStyle = new GUIStyle(EditorStyles.toolbarButton);
                    toolbarButtonStyle.padding = new RectOffset(5, 5, 2, 2);
                }
                return toolbarButtonStyle;
            }
        }
        static GUIStyle ToolbarMenuButtonStyle
        {
            get
            {
                if (toolbarMenuButtonStyle == null)
                {
                    toolbarMenuButtonStyle = new GUIStyle(EditorStyles.toolbarButton);
                    toolbarMenuButtonStyle.padding = new RectOffset(9, 5, 2, 2);
                }
                return toolbarMenuButtonStyle;
            }
        }

        public static List<EditorToolbar> toolbars = new List<EditorToolbar>();

        static EditorToolbar()
        {

            foreach (var type in AppDomain.CurrentDomain.GetAssemblies().Referenced(typeof(EditorToolbar).Assembly)
                .SelectMany(o => o.GetTypes())
                .Where(o => o != typeof(EditorToolbar) && !o.IsAbstract && typeof(EditorToolbar).IsAssignableFrom(o)))
            {
                EditorToolbar toolbar = (EditorToolbar)Activator.CreateInstance(type);
                toolbars.Add(toolbar);
            }

            EditorToolbarSettings.Settings.Load(toolbars);

            EditorApplication.delayCall += Initalize;
        }

        static void Initalize()
        {

            var toolbars = Resources.FindObjectsOfTypeAll(toolbarType);
            currentToolbar = toolbars.Length > 0 ? (ScriptableObject)toolbars[0] : null;
            if (currentToolbar != null)
            {
                if (viewVisualTreeProperty != null)
                {
                    var visualTree = (VisualElement)viewVisualTreeProperty.GetValue(currentToolbar, null);
                    guiContainer = (IMGUIContainer)visualTree[0];
                    guiContainer.onGUIHandler -= _OnGUI;
                    guiContainer.onGUIHandler += _OnGUI;
                }
                else
                {
                    //2020.1
                    var toolbar = currentToolbar;
                    var windowBackendProperty = toolbar.GetType().GetProperty("windowBackend", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    var win = windowBackendProperty.GetValue(toolbar);
                    var visualTree = (VisualElement)win.GetType().GetProperty("visualTree").GetValue(win);

                    if (visualTree != null)
                    {
                        guiContainer = (IMGUIContainer)visualTree[0];
                        guiContainer.onGUIHandler -= _OnGUI;
                        guiContainer.onGUIHandler += _OnGUI;
                    }
                }


                //var handler = (Action)imguiContainerOnGuiField.GetValue(guiContainer);
                //bool b =  == handler;

                //imguiContainerOnGuiField.SetValue(guiContainer, handler);
                //Button btn = new UnityEngine.UIElements.Button(() =>
                // {
                //     Debug.Log("FFF");
                // });
                //btn.text = "A";
                //visualTree.Add(btn);
            }
        }


        [SerializeField]
        private bool enabled = false;
        public virtual bool IsEnabled
        {
            get => enabled;
            set => enabled = value;
        }

        [SerializeField]
        private ToolbarPosition position = ToolbarPosition.LeftToolbar;
        public virtual ToolbarPosition Position
        {
            get => position;
            set => position = value;
        }

        [SerializeField]
        private int order = 0;
        public virtual int Order
        {
            get => order;
            set => order = value;
        }

        public virtual bool IsAvailable
        {
            get => true;
        }

        [SerializeField]
        private bool space;

        public bool Space
        {
            get => space;
            set => space = value;
        }

        public GUIContent GetGUIConent(string icon, string text, string tooltip)
        {
            if (!string.IsNullOrEmpty(icon))
            {
                var iconContent = EditorGUIUtility.IconContent(icon);
                if (iconContent != null && iconContent.image)
                    return new GUIContent(text, iconContent.image, tooltip);
            }
            return new GUIContent(text, tooltip);
        }


        public virtual void OnGUI()
        {

        }

        static void _OnGUI()
        {
            guiContainer.MarkDirtyRepaint();
            //OnToolbarGUI?.Invoke();

            if (commandStyle == null)
            {
                commandStyle = new GUIStyle("CommandLeft");
            }

            var screenWidth = EditorGUIUtility.currentViewWidth;

            // Following calculations match code reflected from Toolbar.OldOnGUI()
            float playButtonsPosition = (screenWidth - 100) / 2;

            Rect leftRect = new Rect(0, 0, screenWidth, Screen.height);
            leftRect.xMin += 10; // Spacing left
            leftRect.xMin += 32 * toolCount; // Tool buttons
            leftRect.xMin += 20; // Spacing between tools and pivot
            leftRect.xMin += 64 * 2; // Pivot buttons
            leftRect.xMax = playButtonsPosition - 20;

            Rect rightRect = new Rect(0, 0, screenWidth, Screen.height);
            rightRect.xMin = playButtonsPosition - 20;
            rightRect.xMin += commandStyle.fixedWidth * 3; // Play buttons
            rightRect.xMax = screenWidth;
            rightRect.xMax -= 10; // Spacing right
            rightRect.xMax -= 80; // Layout
            rightRect.xMax -= 10; // Spacing between layout and layers
            rightRect.xMax -= 80; // Layers
            rightRect.xMax -= 20; // Spacing between layers and account
            rightRect.xMax -= 80; // Account
            rightRect.xMax -= 10; // Spacing between account and cloud
            rightRect.xMax -= 32; // Cloud
            rightRect.xMax -= 10; // Spacing between cloud and collab
            rightRect.xMax -= 0; // Colab

            // Add spacing around existing controls
            leftRect.xMin += 10;
            leftRect.xMax -= 10;
            rightRect.xMin += 10;
            rightRect.xMax -= 10;

            // Add top and bottom margins
            leftRect.y = 5;
            leftRect.height = 24;
            rightRect.y = 5;
            rightRect.height = 24;

            if (leftRect.width > 0)
            {
                using (new GUILayout.AreaScope(leftRect))
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    GUIToolbar(toolbars.OrderBy(o => o.Order)
                        .Where(o => o.Position == ToolbarPosition.LeftToolbar));

                    OnToolbarLeftGUI?.Invoke();
                }
            }
            if (rightRect.width > 0)
            {
                using (new GUILayout.AreaScope(rightRect))
                using (new GUILayout.HorizontalScope())
                {
                    OnToolbarRightGUI?.Invoke();
                    GUIToolbar(toolbars.OrderBy(o => o.Order)
                        .Where(o => o.Position == ToolbarPosition.RightToolbar));
                    GUILayout.FlexibleSpace();
                }
            }
        }

        static void GUIToolbar(IEnumerable<EditorToolbar> toolbars)
        {
            Color color = GUI.color;
            GUI.color *= new Color(1f, 1f, 1f, 0.8f);
            foreach (var toolbar in toolbars)
            {
                if (!toolbar.IsEnabled)
                    continue;

                var enabled = GUI.enabled;
                GUI.enabled = toolbar.IsAvailable;
                toolbar.OnGUI();
                GUI.enabled = enabled;
                //if (toolbar.Space)
                //    GUILayout.Space(16);
            }
            GUI.color = color;
        }



        //2020/9/1
        private static string GetPackageDirectory(string packageName)
        {
            string path = Path.Combine("Packages", packageName);
            if (Directory.Exists(path) && File.Exists(Path.Combine(path, "package.json")))
                return path;

            foreach (var dir in Directory.GetDirectories("Assets", "*", SearchOption.AllDirectories))
            {
                if (string.Equals(Path.GetFileName(dir), packageName, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (File.Exists(Path.Combine(dir, "package.json")))
                        return dir;
                }
            }

            foreach (var pkgPath in Directory.GetFiles("Assets", "package.json", SearchOption.AllDirectories))
            {
                try
                {
                    if (JsonUtility.FromJson<UnityPackage>(File.ReadAllText(pkgPath, System.Text.Encoding.UTF8)).name == packageName)
                    {
                        return Path.GetDirectoryName(pkgPath);
                    }
                }
                catch { }
            }

            return null;
        }
        [Serializable]
        class UnityPackage
        {
            public string name;
        }


        public void GUIButton(GUIContent content, Action onClick)
        {
            Rect rect = GetRect(content, ToolbarButtonStyle);

            GUI.BeginGroup(rect);

            //GUI.color = new Color(1, 1, 1, 0.3f);
            if (GUI.Button(new Rect(0, 0, rect.width, rect.height), content, EditorStyles.toolbarButton))
            {
                onClick?.Invoke();
            }
            //GUI.color = Color.white;

            GUI.EndGroup();
        }

        Rect GetRect(GUIContent content, GUIStyle style)
        {
            int width = 4;
            if (!string.IsNullOrEmpty(content.text))
                width += (int)style.CalcSize(new GUIContent(content.text)).x;
            if (content.image != null)
                width += 28;
            Rect rect = GUILayoutUtility.GetRect(content, style, GUILayout.Width(width));
            return rect;
        }

        public void GUIMenuButton(GUIContent content, Action onClick, Action onMenu)
        {
            Rect rect = GetRect(content, ToolbarMenuButtonStyle);
            GUIStyle menuBtnStyle = null;
            Rect menuBtnRect = new Rect();
            GUI.BeginGroup(rect);
            Matrix4x4 matrix;

            menuBtnStyle = new GUIStyle("label");
            menuBtnStyle.padding = new RectOffset(0, 0, 0, 0);
            menuBtnStyle.fontSize += 3;
            menuBtnStyle.margin = new RectOffset();
            int menuBtnWidth = 10, menuBtnHeight = 10;

            matrix = GUI.matrix;
            menuBtnRect = new Rect(0, rect.height - menuBtnHeight, menuBtnWidth, menuBtnHeight);
            var rect1 = new Rect(menuBtnRect.x, menuBtnRect.y, menuBtnRect.width, menuBtnRect.height);
            //GUIUtility.RotateAroundPivot(45, rect1.center);
            //GUI.matrix = Matrix4x4.TRS(new Vector3(-2+ rect1.center.x, 3+ rect1.center.y, 0), Quaternion.Euler(0, 0, 45), Vector3.one)*GUI.matrix;
            if (GUI.Button(rect1, GUIContent.none, "button"))
            {
                GUI.matrix = matrix;
                onMenu?.Invoke();
            }
            else
                GUI.matrix = matrix;

            //GUI.color = new Color(1, 1, 1, 0.3f);
            if (GUI.Button(new Rect(0, 0, rect.width, rect.height), content, EditorStyles.toolbarButton))
            {
                onClick?.Invoke();
            }
            //GUI.color = Color.white;

            GUI.Label(menuBtnRect, "◣", menuBtnStyle);

            GUI.EndGroup();
        }




    }

}