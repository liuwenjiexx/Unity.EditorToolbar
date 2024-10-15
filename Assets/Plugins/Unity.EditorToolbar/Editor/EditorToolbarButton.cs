using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using Unity.Bindings;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

namespace Unity.Toolbars.Editor
{
    public abstract class EditorToolbarButton : EditorToolbar
    {
        private List<Item> items = new List<Item>();

        public Item DefaultItem
        {
            get => GetItems().FirstOrDefault(o => o.IsDefault);
        }

        protected virtual ToolbarStyle Style
        {
            get
            {
                if (GetItems().Count > 1 || (GetItems().Count == 1 && !GetItems()[0].IsDefault))
                    return ToolbarStyle.Menu;
                return ToolbarStyle.Button;
            }
        }
        public override string Text
        {
            get
            {
                if (base.text == ConfigValueKeyword.Null)
                {
                    return DefaultItem?.Name;
                }
                return base.Text;
            }
            set => base.Text = value;
        }

        public virtual List<Item> GetItems()
        {
            return items;
        }

        protected virtual bool CanClick()
        {
            return true;
        }

        protected virtual void OnClick()
        {

        }

        protected virtual void OnCreateMenu(DropdownMenu menu)
        {
        }

        protected virtual void OnCreateContextMenu(DropdownMenu menu)
        {
        }

        protected override void CreateUI(VisualElement parent)
        {
            Button btn;
            if (Style == ToolbarStyle.Menu || Style == ToolbarStyle.MenuButton)
            {
                btn = CreateMenuButton(Text, Icon, OnCreateMenu);
                parent.Add(btn);
            }
            else
            {
                btn = CreateButton(Text, Icon);

                parent.Add(btn);
            }

            var icon = btn.Q(null, "toolbar-button_icon");
            var label = btn.Q<Label>(null, "toolbar-button_label");

            bindingSet.Bind(icon, new Accessor<VisualElement, Texture2D>(
                (o) => o.style.backgroundImage.value.texture,
                (o, v) =>
                {
                    o.style.backgroundImage = v;
                    o.style.display = v ? DisplayStyle.Flex : DisplayStyle.None;
                }), nameof(Icon));

            bindingSet.Bind(btn, (o) => o.tooltip, nameof(Tooltip));

            bindingSet.Bind(btn, o => o.enabledSelf, nameof(IsAvailable));

            bindingSet.Bind(label, new Accessor<Label, string>(
                o => o.text,
                (o, v) =>
                {
                    o.text = v;
                    o.style.display = string.IsNullOrEmpty(v) ? DisplayStyle.None : DisplayStyle.Flex;
                }), nameof(Text));

            btn.AddManipulator(new MenuManipulator((evt) =>
            {
                if (evt.target == btn)
                {
                    if (Style == ToolbarStyle.Menu && DefaultItem == null)
                    {
                        OnCreateMenu(evt.menu);
                    }
                    else
                    {
                        if (CanClick())
                        {
                            OnClick();
                        }
                    }
                }
            }, MouseButton.LeftMouse));


            btn.AddManipulator(new ContextualMenuManipulator((e) =>
            {
                if (e.button != 1) return;
                OnCreateContextMenu(e.menu);
            }));
        }



    }



}
