namespace UnityEditor.Toolbars
{
    public abstract class EditorToolbarMenuButton : EditorToolbarButton
    {
        public abstract void OnMenu();

        public override void OnGUI()
        {
            GUIMenuButton(Icon, OnClick, OnMenu);
        }
    }
}