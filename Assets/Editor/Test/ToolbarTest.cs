using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using EditorToolbars;
using UnityEngine;


public static class ToolbarTest
{

    //[OnToolbarGUI]
    static void OnFirstPlay()
    {
        if (GUILayout.Button("Test"))
        {

        }
    }
    /*
    [EditorToolbar]
    static EditorToolbar IconButton()
    {
        CommandLineTool tool = new CommandLineTool()
        {
            Group = 1,
            Icon = EditorToolbar.Icons.OpenedFolder,
        };
        return tool;
    }
    [EditorToolbar]
    static EditorToolbar IconLabel()
    {
        CommandLineTool tool = new CommandLineTool()
        {
            Group = 1,
            Icon = EditorToolbar.Icons.OpenedFolder,
            Text = "Label",
        };
        return tool;
    }
    [EditorToolbar]
    static EditorToolbar IconMenu()
    {
        CommandLineTool tool = new CommandLineTool()
        {
            Group = 1,
            Icon = EditorToolbar.Icons.OpenedFolder,
            IsMenuStyle = true,
        };
        return tool;
    }
    [EditorToolbar]
    static EditorToolbar IconLabelMenu()
    {
        CommandLineTool tool = new CommandLineTool()
        {
            Group = 1,
            Icon = EditorToolbar.Icons.OpenedFolder,
            IsMenuStyle = true,
            Text = "Label",
        };
        return tool;
    }
    */



    [EditorToolbar]
    static EditorToolbar OpenFileTool()
    {
        OpenFileTool tool = new OpenFileTool()
        {
            Group = EditorToolbar.GROUP_FILE,
            Order = 3,
        };
        tool.Group.Position = ToolbarPosition.RightToolbar;
        //AddGroup 为菜单分割线
        tool.AddAssetFolder("Assets/Scripts",openMethod: EditorToolbars.OpenFileTool.OpenMethod.Ping)
            .AddSeparator()
            .AddAssetFileFromFolder("Assets/Example/Scenes", include: ".unity$", recurve: false)
            .AddSeparator()
            .AddAssetFileFromFolder("Assets/Atlas", include: ".spriteatlas$", recurve: true);

        return tool;
    }

    [EditorToolbar]
    static EditorToolbar OpenProjectDirectory()
    {
        CommandLineTool tool = new CommandLineTool()
        {
            Text = "Project Directory",
            Tooltip = "Open Project Directory",
            Group = EditorToolbar.GROUP_FILE,
            Order = 2,
        };

        tool.Add("explorer.exe", "project://", name: "Project Directory");

        return tool;
    }
    [EditorToolbar]
    static EditorToolbar OpenExcelButton()
    {
        CommandLineTool tool = new CommandLineTool()
        {
            Text = "Data 1",
            Group = EditorToolbar.GROUP_DATA,
        };

        tool.Add("project://Data/Data1.xlsx");

        return tool;
    }
    [EditorToolbar]
    static EditorToolbar OpenExcelMenu2()
    {
        CommandLineTool tool = new CommandLineTool()
        {
            Text = "Data",
            Tooltip = "Open Data Table",
            Group = EditorToolbar.GROUP_DATA,
        };
        //isDefault: true, 点击工具时执行的命令
        tool.Add("project://Data/Data1.xlsx", name: "Data 1", isDefault: true)
            .AddSeparator()
            .Add("project://Data/Data2.xlsx", name: "Data 2");

        return tool;
    }

}
