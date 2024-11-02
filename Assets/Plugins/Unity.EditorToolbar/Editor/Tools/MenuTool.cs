using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Linq;
using Unity;

namespace EditorToolbars
{
    [Serializable]
    public class MenuTool : EditorToolbarButton
    {
        [SerializeField, HideInInspector]
        public string menuName;

        public override string Name
        {
            get => base.name == ConfigValueKeyword.Null ? $"Menu: [{menuName}]" : base.Name;
            set => base.Name = value;
        }

        public override string Tooltip
        {
            get => base.tooltip == ConfigValueKeyword.Null ? $"Menu: {menuName}" : base.Tooltip;
            set => base.Tooltip = value;
        }

        static MethodInfo validateMenuItemMethod;
        static Regex menuNameRegex;
        private static Dictionary<string, MenuInfo> allMenus;

        static MethodInfo ValidateMenuItemMethod
        {
            get
            {
                if (validateMenuItemMethod == null)
                {
                    validateMenuItemMethod = typeof(EditorApplication).GetMethod("ValidateMenuItem", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                }
                return validateMenuItemMethod;
            }
        }

        internal static Dictionary<string, MenuInfo> AllMenus
        {
            get
            {
                if (allMenus == null)
                {
                    allMenus = new Dictionary<string, MenuInfo>();
                    foreach (var method in TypeCache.GetMethodsWithAttribute<MenuItem>())
                    {
                        if (method.IsDefined(typeof(ObsoleteAttribute)))
                            continue;
                        var menu = method.GetCustomAttributes<MenuItem>().First();
                        if (string.IsNullOrEmpty(menu.menuItem))
                            continue;
                        string menuName = MenuTool.GetDisplayMenuName(menu.menuItem);
                        if (string.IsNullOrEmpty(menuName))
                            continue;
                        if (menuName.StartsWith("internal:"))
                            continue;
                        MenuInfo menuInfo;

                        if (!allMenus.TryGetValue(menuName, out menuInfo))
                        {
                            menuInfo = new MenuInfo()
                            {
                                id = GetMenuID(menuName),
                                menuName = menuName,
                                displayMenuName = GetDisplayMenuName(menuName),
                            };
                            menuInfo.name = Path.GetFileName(menuInfo.displayMenuName);
                            allMenus[menuInfo.menuName] = menuInfo;
                        }
                        if (menu.validate)
                        {
                            menuInfo.hasValidate = true;
                        }
                    }
                }
                return allMenus;
            }
        }


        public override void Enable()
        {
            if (string.IsNullOrEmpty(menuName))
            {
                IsAvailable = false;
            }
            else
            {
                if (AllMenus.TryGetValue(menuName, out var menu) && menu.hasValidate)
                {
                    IsAvailable = (bool)ValidateMenuItemMethod.Invoke(null, new string[] { menuName });
                }
            }
            base.Enable();
        }

        protected override void OnClick()
        {
            if (string.IsNullOrEmpty(menuName))
                return;

            EditorApplication.ExecuteMenuItem(menuName);
        }

        public static MenuTool Create(string menuName, string group)
        {
            string displayMenuName = GetDisplayMenuName(menuName);
            MenuTool tool = new MenuTool();
            tool.Id = GetMenuID(menuName);
            tool.menuName = menuName;
            //tool.Name = $"Menu: [{displayMenuName}]";
            tool.Name = tool.Id;
            tool.Text = Path.GetFileNameWithoutExtension(displayMenuName);
            tool.Group = group ?? EditorToolbar.GROUP_DEFAULT;
            tool.manualCreated = true;
            tool.Enabled = false;
            return tool;
        }

        public static string GetMenuID(string menuName)
        {
            return $"Menu: [{menuName}]";
        }



        public static string GetDisplayMenuName(string menuName)
        {
            string newMenuName = menuName;
            if (menuNameRegex == null)
                menuNameRegex = new Regex("(?<menu>.*) [%&#]+.+$");
            var m = menuNameRegex.Match(menuName);
            if (m.Success)
            {
                newMenuName = m.Groups["menu"].Value;
                if (string.IsNullOrEmpty(newMenuName))
                    newMenuName = menuName;
            }
            return newMenuName;
        }

        internal class MenuInfo
        {
            public string menuName;
            public string displayMenuName;
            public string name;
            public bool hasValidate;
            public string id;
        }


    }
}