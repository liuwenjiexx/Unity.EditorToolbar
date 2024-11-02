using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using Bindings;
using UnityEditor.UIElements.Extension;
using Unity;

namespace EditorToolbars
{

    [Serializable]
    public partial class EditorToolbar : INotifyPropertyChanged
    {

        static Type toolbarType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Toolbar");
        static Type guiViewType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GUIView");

        static PropertyInfo viewVisualTreeProperty = guiViewType.GetProperty("visualTree", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        private string id;

        [SerializeField, HideInInspector]
        protected ConfigValue<bool> enabled = ConfigValueKeyword.Null;

        [SerializeField, HideInInspector]
        protected ConfigValue<string> name = ConfigValueKeyword.Null;

        [SerializeField]
        protected ConfigValue<BuiltinIcon> icon = ConfigValueKeyword.Null;

        [SerializeField]
        protected ConfigValue<string> text = ConfigValueKeyword.Null;

        [SerializeField]
        protected ConfigValue<string> tooltip = ConfigValueKeyword.Null;

        [SerializeField, MultiLine(3)]
        protected ConfigValue<string> description = ConfigValueKeyword.Null;
        /// <summary>
        /// 代码创建的实例为自动，设置面板创建的为手动，手动需要通过数据恢复实例
        /// </summary>

        internal bool manualCreated;

        private int order = 0;
        private bool isAvailable = true;
        protected BindingSet<EditorToolbar> bindingSet;

        static VisualElement toolbarLeftRoot;
        static VisualElement toolbarRightRoot;
        static VisualElement toolbarLeftContainer;
        static VisualElement toolbarRightContainer;
        public static List<EditorToolbar> tools = new List<EditorToolbar>();
        internal static bool initalized;
        internal static bool initalizing;
        internal bool init;


        public const string UserScopePrefix = "User/";
        public const string ProjectScopePrefix = "Project/";

        public const string GROUP_DEFAULT = ProjectScopePrefix + "Default";
        public const string GROUP_SCENE = ProjectScopePrefix + "Scene";
        public const string GROUP_FILE = ProjectScopePrefix + "File";
        public const string GROUP_DATA = ProjectScopePrefix + "Data";

        public const string USER_GROUP_DEFAULT = UserScopePrefix + "Default";

        public string Id { get => id; set => PropertyChanged.Invoke(this, nameof(Id), ref id, value); }

        public virtual string Name { get => name; set => PropertyChanged.Invoke(this, nameof(Name), ref name, value); }

        public virtual string Text { get => text; set => PropertyChanged.Invoke(this, nameof(Text), ref text, value); }

        public virtual Texture2D Icon
        {
            get => icon.Keyword == ConfigValueKeyword.Undefined ? icon.Value.Image : null;
            set => PropertyChanged.Invoke(this, nameof(Icon), ref icon, new BuiltinIcon(value));
        }

        public virtual string Tooltip { get => tooltip; set => PropertyChanged.Invoke(this, nameof(Tooltip), ref tooltip, value); }

        public virtual string Description { get => description; set => PropertyChanged.Invoke(this, nameof(Description), ref description, value); }

        public bool Enabled
        {
            get => enabled;
            set
            {
                if (PropertyChanged.Invoke(this, nameof(Enabled), ref enabled, value))
                {
                    if (init)
                    {
                        if (enabled)
                        {
                            Enable();
                            Refresh();
                        }
                        else
                        {
                            Disable();
                        }
                    }
                }
            }
        }


        [NonSerialized]
        internal ToolbarGroup group;
        public ToolbarGroup Group
        {
            get => group;
            set
            {
                //if (PropertyChanged.Invoke(this, nameof(Group), ref group, value))

                //    if (value!= null)
                //    {
                //        group.Add(this);
                //    }
                //}
                if (group != value)
                {
                    if (value != null)
                    {
                        value.Add(this);
                    }
                    else
                    {
                        if (group != null)
                        {
                            group.Remove(this);
                            group = null;
                        }
                    }
                }
            }
        }

        public int Order
        {
            get => order;
            set => PropertyChanged.Invoke(this, nameof(Order), ref order, value);
        }

        public virtual bool IsAvailable
        {
            get => isAvailable;
            set => PropertyChanged.Invoke(this, nameof(IsAvailable), ref isAvailable, value);
        }

        public bool ManualCreated { get => manualCreated; }

        public virtual object UserSettings { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public EditorToolbar()
        {

        }

        public EditorToolbar(string id)
        {
            this.id = id;
        }


        [InitializeOnLoadMethod]
        static void InitializeOnLoadMethod()
        {
            Initalize();

            EditorApplication.delayCall += DelayInitalize;
        }


        internal static void Initalize()
        {
            if (initalizing || initalized)
                return;
            initalized = false;
            initalizing = true;

            foreach (var tool in tools.ToArray())
            {
                if (tool.group != null)
                {
                    tool.group.Remove(tool);
                }
            }
            tools.Clear();

            //Debug.Log("start count " + EditorToolbarSettings.Groups.SelectMany(o => o.items).Count());
            //DateTime dt = DateTime.Now;
            //foreach (var method in AppDomain.CurrentDomain.GetAssemblies().Referenced(typeof(EditorToolbar).Assembly)
            //    .SelectMany(o => o.GetTypes())
            //    .Where(o => o.IsSealed || o.IsSubclassOf(typeof(EditorToolbar)))
            //    .SelectMany(o => o.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)))
            //{
            //    if (!method.IsDefined(typeof(EditorToolbarAttribute), false))
            //        continue;
            foreach (var method in TypeCache.GetMethodsWithAttribute(typeof(EditorToolbarAttribute)))
            {
                if (!method.IsStatic)
                {
                    Debug.LogError($"Method '{method}' not static");
                    continue;
                }
                if (typeof(EditorToolbar).IsAssignableFrom(method.ReturnType))
                {
                    if (method.GetParameters().Length != 0)
                        continue;
                    EditorToolbar tool = method.Invoke(null, null) as EditorToolbar;
                    if (tool == null)
                        continue;
                    if (string.IsNullOrEmpty(tool.Id))
                    {
                        tool.Id = method.ToString();
                    }
                    if (string.IsNullOrEmpty(tool.Name))
                        tool.Name = method.Name;
                    tool.manualCreated = false;
                    //if (tool.Group == null)
                    //{
                    //    tool.Group = EditorToolbarSettings.DefaultGroup;
                    //}
                    tools.Add(tool);
                }
                else if (typeof(ToolbarGroup).IsAssignableFrom(method.ReturnType))
                {
                    if (method.GetParameters().Length != 0)
                        continue;
                    ToolbarGroup group = method.Invoke(null, null) as ToolbarGroup;
                    if (group == null)
                        continue;

                    foreach (var tool in group.Items)
                    {
                        if (tools.Contains(tool))
                            continue;

                        if (string.IsNullOrEmpty(tool.Id))
                        {
                            tool.Id = GetId($"{tool.GetType().FullName}:{method}");
                        }
                        if (string.IsNullOrEmpty(tool.Name))
                            tool.Name = tool.GetType().Name;

                        tool.manualCreated = false;
                        tools.Add(tool);
                    }
                }
                else
                {
                    Debug.LogError($"{nameof(EditorToolbarAttribute)} method return type is '{nameof(EditorToolbar)}' or '{nameof(ToolbarGroup)}'");
                }

            }

            //Debug.Log(tools.Count + ", " + EditorToolbarSettings.Groups.SelectMany(o => o.items).Count());
            //Debug.Log("EditorToolbar.Load " + (DateTime.Now.Subtract(dt).TotalMilliseconds) + "ms");
            EditorToolbarSettings.Settings.Load();
            initalizing = false;
            initalized = true;

        }


        static void DelayInitalize()
        {

            var toolbars = Resources.FindObjectsOfTypeAll(toolbarType);
            var currentToolbar = toolbars.Length > 0 ? (ScriptableObject)toolbars[0] : null;
            if (currentToolbar != null)
            {
                VisualElement visualTree = null;
                if (viewVisualTreeProperty != null)
                {
                    visualTree = (VisualElement)viewVisualTreeProperty.GetValue(currentToolbar, null);

                }
                else
                {
                    //2020.1
                    var toolbar = currentToolbar;
                    var windowBackendProperty = toolbar.GetType().GetProperty("windowBackend", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    var win = windowBackendProperty.GetValue(toolbar);
                    visualTree = (VisualElement)win.GetType().GetProperty("visualTree").GetValue(win);

                }


                if (visualTree != null)
                {
                    var toolbarZoneLeftAlign = visualTree.Q("ToolbarZoneLeftAlign");
                    var toolbarZoneRightAlign = visualTree.Q("ToolbarZoneRightAlign");

                    if (toolbarZoneLeftAlign != null)
                    {
                        #region Left Toolbar

                        toolbarLeftRoot = new VisualElement();
                        toolbarLeftRoot.style.flexDirection = FlexDirection.Row;

                        toolbarLeftContainer = new VisualElement();
                        toolbarLeftContainer.AddToClassList("toolbar-container");
                        toolbarLeftContainer.AddToClassList("toolbar-container-left");
                        toolbarLeftContainer.style.flexDirection = FlexDirection.Row;

                        EditorToolbarUtility.AddStyle(toolbarLeftContainer, typeof(EditorToolbar), "EditorToolbar");
                        toolbarZoneLeftAlign.Add(toolbarLeftContainer);

                        #endregion

                        #region Right Toolbar

                        toolbarRightContainer = new VisualElement();
                        EditorToolbarUtility.AddStyle(toolbarRightContainer, typeof(EditorToolbar), "EditorToolbar");
                        toolbarRightContainer.AddToClassList("toolbar-container");
                        toolbarRightContainer.AddToClassList("toolbar-container-right");
                        toolbarZoneRightAlign.Add(toolbarRightContainer);

                        #endregion
                    }
                    else
                    {
                        Debug.LogError("toolbarZoneLeftAlign null");
                    }
                }
                else
                {
                    Debug.LogError("visualTree null");
                }
            }

            Rebuild();
            EditorApplication.update += _Update;

        }
        private void Init()
        {
            bindingSet = new BindingSet<EditorToolbar>(this);
            //if (group == null)
            //    Group = EditorToolbarSettings.DefaultGroup;
            init = true;
        }

        //UnityException: FromJsonOverwriteInternal is not allowed to be called during serialization, call it from OnEnable instead. Called from ScriptableObject.
        //ScriptableObject 序列化对象不能使用 OnEnable, OnDisable 名称
        public virtual void Enable()
        {

        }

        public virtual void Disable()
        {
            if (bindingSet != null)
            {
                bindingSet.Unbind();
                bindingSet = null;
            }
        }

        protected bool SetProperty<TValue>(string propertyName, ref TValue field, TValue newValue) => PropertyChanged.Invoke(this, propertyName, ref field, newValue);

        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged.Invoke(this, propertyName);
        }

        static void _Update()
        {
            foreach (var tool in EditorToolbarSettings.AllGroups.SelectMany(o => o.Items))
            {
                tool.OnUpdate();
            }
        }

        public static void Rebuild()
        {

            foreach (var tool in EditorToolbarSettings.AllGroups.SelectMany(o => o.Items))
            {
                if (tool.Enabled && tool.init)
                {
                    tool.Disable();
                    tool.init = false;
                }
            }

            if (toolbarLeftContainer != null)
            {
                toolbarLeftContainer.Clear();
                CreateToolbar(toolbarLeftContainer, EditorToolbarSettings.AllGroups.Where(o => o.Enabled && (o.Position.Keyword == ConfigValueKeyword.Null || o.Position == ToolbarPosition.LeftToolbar)));
            }

            if (toolbarRightContainer != null)
            {
                toolbarRightContainer.Clear();
                CreateToolbar(toolbarRightContainer, EditorToolbarSettings.AllGroups.Where(o => o.Enabled && o.Position == ToolbarPosition.RightToolbar));
            }
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

        static void CreateToolbar(VisualElement container, IEnumerable<ToolbarGroup> groups)
        {

            foreach (var g in groups.ToArray())
            {
                VisualElement elemGroup = null;
                foreach (var toolConfig in g.items)
                {
                    if (!toolConfig.Enabled)
                        continue;

                    EditorToolbar tool;
                    try
                    {
                        tool = toolConfig.GetOrCreateTarget();
                        if (tool == null)
                            continue;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError(ex);
                        continue;
                    }

                    if (elemGroup == null)
                    {
                        elemGroup = g.CreateGroupUI();
                        container.Add(elemGroup);
                    }

                    if (!tool.init)
                    {
                        toolConfig.UpdateTarget();
                        tool.Init();
                        tool.Enable();
                    }
                    tool.CreateUI(elemGroup);
                    tool.bindingSet.Bind();
                    tool.Refresh();
                }
            }
        }

        protected virtual void OnUpdate()
        {

        }

        protected virtual void CreateUI(VisualElement parent)
        {

        }

        protected static Button CreateButton(string text, Texture2D icon)
        {
            ToolbarButton button = new ToolbarButton();
            button.AddToClassList("toolbar-button");

            if (icon && string.IsNullOrEmpty(text))
            {
                //button.AddToClassList("toolbar-icon-button");
            }

            bool hasIcon = false, hasLabel = false;
            var _icon = new VisualElement();
            _icon.AddToClassList("toolbar-button_icon");
            _icon.style.backgroundImage = icon;
            if (!icon)
            {
                _icon.style.display = DisplayStyle.None;
            }
            else
            {
                hasIcon = true;
            }
            button.Add(_icon);


            var label = new Label();
            label.AddToClassList("toolbar-button_label");
            label.text = text;
            if (string.IsNullOrEmpty(text))
            {
                label.style.display = DisplayStyle.None;
            }
            else
            {
                hasLabel = true;
            }
            button.Add(label);


            VisualElement arrow = new VisualElement();
            arrow.AddToClassList("toolbar-button_arrow");
            arrow.style.display = DisplayStyle.None;

            VisualElement arrowSeparator = new VisualElement();
            arrowSeparator.AddToClassList("toolbar-button_arrow_separator");
            arrow.Add(arrowSeparator);

            VisualElement arrowIcon = new VisualElement();
            arrowIcon.AddToClassList("unity-toolbar-menu__arrow");
            arrowIcon.AddToClassList("toolbar-button_arrow_icon");
            arrow.Add(arrowIcon);
            button.Add(arrow);

            if (hasIcon && hasLabel)
            {
                button.AddToClassList("toolbar-button-text-icon");
            }
            else if (hasIcon)
            {
                button.AddToClassList("toolbar-button-icon");
            }
            else if (hasLabel)
            {
                button.AddToClassList("toolbar-button-text");
            }

            return button;
        }
        protected Button CreateContextMenuButton(Action<DropdownMenu> createMenu)
        {
            return CreateContextMenuButton(null, null, createMenu);
        }

        protected Button CreateContextMenuButton(string text, Texture2D icon, Action<DropdownMenu> createMenu)
        {
            Button button = CreateButton(text, icon);

            button.AddManipulator(new ContextualMenuManipulator((e) =>
            {
                createMenu?.Invoke(e.menu);
            }));
            return button;
        }

        protected static Button CreateMenuButton(string text, Texture2D icon, Action<DropdownMenu> createMenu)
        {
            var btn = CreateButton(text, icon);
            btn.AddToClassList("toolbar-button-menu");
            var arrow = btn.Q(null, "toolbar-button_arrow");
            arrow.AddManipulator(new MenuManipulator((evt) =>
            {
                if (arrow == evt.target)
                {
                    createMenu?.Invoke(evt.menu);
                }
            }, MouseButton.LeftMouse));
            arrow.style.display = DisplayStyle.Flex;
            return btn;
        }

        public virtual void Refresh()
        {
            if (!Enabled) return;

            if (bindingSet != null)
            {
                bindingSet.UpdateSourceToTarget();
            }
        }

        public override string ToString()
        {
            return $"{GetType().Name}: {Name}";
        }


        public static string GetId(string text) => Hash32(Encoding.UTF8.GetBytes(text ?? string.Empty)).ToString();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetId(Type type) => Hash32(type.FullName).ToString();


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint Hash32(string text) => Hash32(Encoding.UTF8.GetBytes(text));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint Hash32(Type type) => Hash32(type.FullName);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint Hash32(byte[] buffer)
        {
            int length = buffer.Length;
            uint seed = 0;

            const uint prime1 = 2654435761u;
            const uint prime2 = 2246822519u;
            const uint prime3 = 3266489917u;
            const uint prime4 = 0668265263u;
            const uint prime5 = 0374761393u;

            uint hash = seed + prime5;

            int offset = 0;
            if (length >= 16)
            {
                uint val0 = seed + prime1 + prime2;
                uint val1 = seed + prime2;
                uint val2 = seed + 0;
                uint val3 = seed - prime1;

                int count = length >> 4;
                for (int i = 0; i < count; i++)
                {
                    var pos0 = BitConverter.ToUInt32(buffer, offset + 0);
                    var pos1 = BitConverter.ToUInt32(buffer, offset + 4);
                    var pos2 = BitConverter.ToUInt32(buffer, offset + 8);
                    var pos3 = BitConverter.ToUInt32(buffer, offset + 12);

                    val0 += pos0 * prime2;
                    val0 = (val0 << 13) | (val0 >> (32 - 13));
                    val0 *= prime1;

                    val1 += pos1 * prime2;
                    val1 = (val1 << 13) | (val1 >> (32 - 13));
                    val1 *= prime1;

                    val2 += pos2 * prime2;
                    val2 = (val2 << 13) | (val2 >> (32 - 13));
                    val2 *= prime1;

                    val3 += pos3 * prime2;
                    val3 = (val3 << 13) | (val3 >> (32 - 13));
                    val3 *= prime1;

                    offset += 16;
                }

                hash = ((val0 << 01) | (val0 >> (32 - 01))) +
                       ((val1 << 07) | (val1 >> (32 - 07))) +
                       ((val2 << 12) | (val2 >> (32 - 12))) +
                       ((val3 << 18) | (val3 >> (32 - 18)));
            }

            hash += (uint)length;

            length &= 15;
            while (length >= 4)
            {
                hash += BitConverter.ToUInt32(buffer, offset) * prime3;
                hash = ((hash << 17) | (hash >> (32 - 17))) * prime4;
                offset += 4;
                length -= 4;
            }
            while (length > 0)
            {
                hash += buffer[offset] * prime5;
                hash = ((hash << 11) | (hash >> (32 - 11))) * prime1;
                offset++;
                --length;
            }

            hash ^= hash >> 15;
            hash *= prime2;
            hash ^= hash >> 13;
            hash *= prime3;
            hash ^= hash >> 16;

            return hash;
        }


        [Serializable]
        public class Item
        {
            [SerializeField]
            private string name;
            public string Name { get => name; set => name = value; }

            [SerializeField]
            private bool isDefault;
            public bool IsDefault { get => isDefault; set => isDefault = value; }

            [SerializeField]
            private bool isSeparator;
            public bool IsSeparator { get => isSeparator; set => isSeparator = value; }

            [SerializeField]
            private string description;
            public string Description { get => description; set => description = value; }

            public static readonly Item Separator = new Item() { isSeparator = true };

        }
    }

    public enum ToolbarStyle
    {
        /// <summary>
        /// 按钮样式
        /// </summary>
        Button,
        /// <summary>
        /// 下拉菜单样式
        /// </summary>
        Menu,
        /// <summary>
        /// 按钮和下拉菜单样式
        /// </summary>
        MenuButton,
    }

}