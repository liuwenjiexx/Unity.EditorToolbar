using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

public class TestWindow : EditorWindow
{
    [MenuItem("Test/Menu Tool")]
    static void show1()
    {
        Debug.Log("Test menu tool ok");
    } 

    [MenuItem("Test/Menu Tool Validate false")]
    static void TestMenuToolValidate()
    {
        Debug.Log("Test menu tool validate false, fail");
    }

    [MenuItem("Test/Menu Tool Validate false", validate = true)]
    static bool TestMenuToolValidate_()
    {
        return false;
    }

    [MenuItem("Test/Win")]
    static void show()
    {
        var win = GetWindow<TestWindow>();
        win.Show();
        EditorApplication.delayCall += () =>
        {
            Button btn = new Button();
            btn.clicked += () =>
            {
                Debug.Log("A2");
            };
            btn.text = "AAA";
            win.rootVisualElement.Add(btn);
            Debug.Log("add 2");
        };
    }

    private void CreateGUI()
    {
        Button btn = new Button();
        btn.clicked += () =>
        {
            Debug.Log("A");
        };
        btn.text = "AAA";

        this.rootVisualElement.Add(btn);
        var toolbarButton = new ToolbarButton();

        toolbarButton.clicked += () =>
        {
            Debug.Log("B");
            btn.SetEnabled(!btn.enabledSelf);
            Debug.Log(btn.enabledSelf + ", " + btn.enabledInHierarchy + ", " + Application.isPlaying);
        };
        toolbarButton.text = "T";
        rootVisualElement.Add(toolbarButton);
    }
}
