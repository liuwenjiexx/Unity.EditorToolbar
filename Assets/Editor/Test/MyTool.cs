using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using EditorToolbars;
using System.Linq;

public class MyTool : EditorToolbarButton
{
    public MyTool()
    {
        Text = "MyTool"; 
    }

    protected override void OnClick()
    {
        Debug.Log("Hello World");
    }

    [EditorToolbar]
    static EditorToolbar CreateMyTool()
    {
        MyTool tool = new MyTool();
        return tool;
    }
}


public class MyMenuTool : EditorToolbarButton
{
    [SerializeField]
    List<Item> items = new List<Item>();

    public MyMenuTool()
    {
        Text = "My Menu";
    }

    public override List<EditorToolbar.Item> GetItems()
    {
        return items.ToList<EditorToolbar.Item>();
    }

    protected override void OnCreateMenu(DropdownMenu menu)
    {
        foreach (var item in items)
        {
            if (item.IsSeparator)
            {
                menu.AppendSeparator();
                continue;
            }

            menu.AppendAction(item.Name, (o) =>
            {
                item.Execute(this);
            });
        }
    }

    [EditorToolbar]
    static EditorToolbar CreateMyMenu()
    {
        MyMenuTool tool = new MyMenuTool();
        tool.items.Add(new Item() { Name = "Item1" });
        tool.items.Add(new Item() { IsSeparator = true });
        tool.items.Add(new Item() { Name = "Item2" });

        return tool;
    }

    new class Item : EditorToolbar.Item
    {
        public void Execute(EditorToolbar tool)
        {
            Debug.Log($"MyMenuTool: {Name}");
        }
    }

}