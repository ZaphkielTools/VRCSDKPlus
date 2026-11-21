using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace Zaphkiel.VRCSDKPlus
{
    internal static class MenuHistory
    {
        static readonly LinkedList<VRCExpressionsMenu> Menus = new LinkedList<VRCExpressionsMenu>();
        static LinkedListNode<VRCExpressionsMenu> current;

        public static bool CanGoBack => current?.Previous != null;
        public static bool CanGoForward => current?.Next != null;

        public static void Visit(VRCExpressionsMenu menu)
        {
            if (!menu || current?.Value == menu) return;

            while (current != null && current.Next != null)
                Menus.Remove(current.Next);
            current = Menus.AddLast(menu);
        }

        public static void GoFirst() => Navigate(() => Menus.First);
        public static void GoBack() => Navigate(() => current?.Previous);
        public static void GoForward() => Navigate(() => current?.Next);
        public static void GoLast() => Navigate(() => Menus.Last);

        static void Navigate(Func<LinkedListNode<VRCExpressionsMenu>> target)
        {
            RemoveDeletedMenus();
            var node = target();
            if (node == null) return;
            current = node;
            Selection.activeObject = node.Value;
        }

        static void RemoveDeletedMenus()
        {
            for (var node = Menus.First; node != null;)
            {
                var next = node.Next;
                if (!node.Value)
                {
                    if (node == current) current = node.Previous ?? next;
                    Menus.Remove(node);
                }

                node = next;
            }
        }

        public static VisualElement CreateBar(VRCExpressionsMenu menu)
        {
            var bar = Ui.Row("vsp-box");
            bar.Add(HistoryButton("«", "First menu", CanGoBack, GoFirst));
            bar.Add(HistoryButton("‹", "Previous menu", CanGoBack, GoBack));

            var title = new Button(() => EditorGUIUtility.PingObject(menu)) { text = menu.name, tooltip = "Show in the Project window" };
            title.AddToClassList("vsp-history-title");
            bar.Add(title);

            bar.Add(HistoryButton("›", "Next menu", CanGoForward, GoForward));
            bar.Add(HistoryButton("»", "Last menu", CanGoForward, GoLast));
            return bar;
        }

        static Button HistoryButton(string text, string tooltip, bool enabled, Action onClick)
        {
            var button = new Button(onClick) { text = text, tooltip = tooltip };
            button.AddToClassList("vsp-history-button");
            button.SetEnabled(enabled);
            return button;
        }
    }
}
