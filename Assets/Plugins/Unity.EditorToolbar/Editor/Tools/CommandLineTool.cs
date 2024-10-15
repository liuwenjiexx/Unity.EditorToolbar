using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;
using UnityEditor;
using System.Collections;

namespace Unity.Toolbars.Editor
{

    [Serializable]
    public class CommandLineTool : EditorToolbarButton
    {
        [SerializeField]
        private List<Item> items = new List<Item>();

        public List<Item> Items => items;

        public override List<EditorToolbar.Item> GetItems()
        {
            return Items.ToList<EditorToolbar.Item>();
        }

        protected override void OnClick()
        {
            var defaultItem = (Item)DefaultItem;
            if (defaultItem != null)
            {
                ExecuteCommandLine(defaultItem);
            }
        }


        protected override void OnCreateMenu(DropdownMenu menu)
        {
            foreach (Item item in Items)
            {
                if (item.IsSeparator)
                {
                    menu.AppendSeparator();
                    continue;
                }

                menu.AppendAction(item.Name, (menuItem) =>
                {
                    ExecuteCommandLine(item);
                });
            }
        }
         

        static void ExecuteCommandLine(Item item)
        {
            try
            {
                string file = item.CommandName, argument = item.Arguments;

                file = EditorToolbarUtility.ReplaceProjectScheme(file);
                argument = EditorToolbarUtility.ReplaceProjectScheme(argument);

                Process.Start(file, argument);
                item.Callback?.Invoke();
            }
            catch
            {
                Debug.LogError($"Execute command error. file: '{item.CommandName}', argument: '{item.Arguments}'");
                throw;
            }
        }

        public CommandLineTool Add(string commandName, string argument = null, bool isDefault = false, string name = null, Action callback = null)
        {
            Items.Add(new Item() { Name = name, CommandName = commandName, Arguments = argument, IsDefault = isDefault, Callback = callback });
            return this;
        }

        public CommandLineTool AddSeparator()
        {
            Items.Add(new Item() { IsSeparator = true });
            return this;
        }

        [Serializable]
        public new class Item : EditorToolbar.Item
        {
            public Action Callback;

            [SerializeField]
            private string commandName;
            public string CommandName { get => commandName; set => commandName = value; }

            [SerializeField]
            private string arguments;
            public string Arguments { get => arguments; set => arguments = value; }

        }


    }

}