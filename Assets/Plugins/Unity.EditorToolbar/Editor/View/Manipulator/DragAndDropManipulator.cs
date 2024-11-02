using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace EditorToolbars
{

    class DragAndDropManipulator : MouseManipulator
    {
        private Func<Object[], string[], bool, bool> Handler;

        public DragAndDropManipulator(Func<Object[], string[], bool, bool> handler)
        {
            this.Handler = handler;
        }

        public DragAndDropVisualMode VisualMode { get; set; } = DragAndDropVisualMode.Link;

        public string DragOverStyleClass { get; set; } = "DragOver";

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);
            target.RegisterCallback<DragPerformEvent>(OnDragPerformEvent);
            target.RegisterCallback<DragLeaveEvent>(OnDragLeaveEvent);
            target.RegisterCallback<DragExitedEvent>(OnDragExitedEvent);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);
            target.UnregisterCallback<DragPerformEvent>(OnDragPerformEvent);
            target.UnregisterCallback<DragLeaveEvent>(OnDragLeaveEvent);
            target.UnregisterCallback<DragExitedEvent>(OnDragExitedEvent);
        }

        void OnDragUpdatedEvent(DragUpdatedEvent e)
        {
            var elem = e.currentTarget as VisualElement;
            if (elem != null)
            {
                var validate = Handler(DragAndDrop.objectReferences, DragAndDrop.paths, false);
                if (validate)
                {
                    DragAndDrop.visualMode = VisualMode;
                    elem.AddToClassList(DragOverStyleClass);
                }
                else
                {
                    elem.RemoveFromClassList(DragOverStyleClass);
                }
            }
        }

        void OnDragPerformEvent(DragPerformEvent e)
        {

            var elem = e.currentTarget as VisualElement;
            if (elem != null)
            {
                var validate = Handler(DragAndDrop.objectReferences, DragAndDrop.paths, true);
                if (validate)
                {
                    DragAndDrop.AcceptDrag();
                }
            }
        }

        void OnDragLeaveEvent(DragLeaveEvent e)
        {
            var elem = e.currentTarget as VisualElement;
            if (elem != null)
            {
                elem.RemoveFromClassList(DragOverStyleClass);
            }
        }

        void OnDragExitedEvent(DragExitedEvent e)
        {
            var elem = e.currentTarget as VisualElement;
            if (elem != null)
            {
                elem.RemoveFromClassList(DragOverStyleClass);
            }
        }

    }
}
