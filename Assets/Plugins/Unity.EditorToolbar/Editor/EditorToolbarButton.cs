using UnityEngine;

namespace UnityEditor.Toolbars
{
    public abstract class EditorToolbarButton : EditorToolbar
    {
        public abstract GUIContent Icon
        {
            get;
        }

        public abstract void OnClick();


        public override void OnGUI()
        {
            GUIButton(Icon, OnClick);
        }

    }

}
