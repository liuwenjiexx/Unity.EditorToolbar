using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace EditorToolbars
{
    class MenuManipulator : MouseManipulator
    {
        private Action<ContextualMenuPopulateEvent> createMenu;
        private MouseButton button;
        private bool showMenu;
        public MenuManipulator(Action<ContextualMenuPopulateEvent> createMenu, MouseButton button = MouseButton.RightMouse)
        {
            this.createMenu = createMenu;
            this.button = button;
            activators.Add(new ManipulatorActivationFilter { button = button });
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            target.RegisterCallback<ContextualMenuPopulateEvent>(OnContextualMenuEvent);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            target.UnregisterCallback<ContextualMenuPopulateEvent>(OnContextualMenuEvent);
        }

        void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.button != (int)button)
                return;
            if (CanStartManipulation(evt))
            {
                //if (target.panel?.contextualMenuManager != null)
                //{
                //    target.panel.contextualMenuManager.DisplayMenu(e, target);
                //}
                showMenu = true;
                evt.StopPropagation();
                evt.PreventDefault();
            }
        }
        void OnMouseUp(MouseUpEvent evt)
        {
            if (evt.button != (int)button)
                return;
            if (CanStartManipulation(evt))
            {
                // if (showMenu)
                {
                    if (target.panel?.contextualMenuManager != null)
                    {
                        target.panel.contextualMenuManager.DisplayMenu(evt, target);
                    }
                }
                evt.StopPropagation();
                evt.PreventDefault();
            }
        }


        void OnContextualMenuEvent(ContextualMenuPopulateEvent evt)
        {
            if (evt.button != (int)button)
                return;
            createMenu?.Invoke(evt);
        }


    }
}