using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace Zaphkiel.VRCSDKPlus
{
    internal sealed class MenuParentsView : VisualElement
    {
        readonly VRCExpressionsMenu menu;
        readonly Label title;
        readonly VisualElement rows;
        bool refreshQueued;

        public MenuParentsView(VRCExpressionsMenu menu)
        {
            this.menu = menu;
            AddToClassList("vsp-box");
            AddToClassList("vsp-parents");

            var header = Ui.Row();
            header.Add(title = new Label());
            title.AddToClassList("vsp-parents-title");
            header.Add(Ui.IconButton(Icons.Refresh, "Search the project again", () =>
            {
                MenuReferences.Rebuild();
                Refresh();
            }));
            Add(header);
            Add(rows = new VisualElement());

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                MenuReferences.Changed += QueueRefresh;
                Undo.undoRedoPerformed += QueueRefresh;
            });
            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                MenuReferences.Changed -= QueueRefresh;
                Undo.undoRedoPerformed -= QueueRefresh;
            });

            Refresh();
        }

        void QueueRefresh()
        {
            if (refreshQueued) return;
            refreshQueued = true;
            schedule.Execute(() =>
            {
                refreshQueued = false;
                Refresh();
            });
        }

        void Refresh()
        {
            rows.Clear();
            var avatars = new List<string>();
            foreach (var avatar in AvatarContext.FindSceneAvatars())
                if (avatar.expressionsMenu == menu) avatars.Add(avatar.gameObject.name);

            if (!MenuReferences.IsReady)
            {
                MenuReferences.EnsureBuilt();
                title.text = "Parent Menus";
                rows.Add(DimLabel("Looking through the project's menus…"));
                AddAvatarRows(avatars);
                return;
            }

            var parents = MenuReferences.FindParents(menu);
            int others = parents.FindAll(link => link.Menu != menu).Count;
            title.text = others switch
            {
                0 => "Parent Menus: none",
                1 => "Parent Menu",
                _ => $"Parent Menus ({others})",
            };

            foreach (var parent in parents) rows.Add(ParentRow(parent));
            AddAvatarRows(avatars);

            if (others == 0 && avatars.Count == 0)
                rows.Add(DimLabel("No menu opens this one as a submenu, and no avatar of the open scenes uses it as its root menu."));
        }

        VisualElement ParentRow(ParentMenuLink link)
        {
            var row = Ui.Row("vsp-parent-row");
            if (link.Menu == menu)
            {
                row.Add(Ui.IconImage(Icons.Warn, "A submenu control pointing to its own menu reopens the same page. It is usually a mistake."));
                var loop = DimLabel($"This menu opens itself via {ControlList(link)}");
                loop.AddToClassList("vsp-grow");
                row.Add(loop);
                return row;
            }

            var button = new Button(() =>
            {
                MenuEditor.RevealSubMenuOnOpen(link.Menu, menu);
                Selection.activeObject = link.Menu;
            })
            {
                text = link.Menu.name,
                tooltip = $"Open {AssetDatabase.GetAssetPath(link.Menu)}",
            };
            button.AddToClassList("vsp-parent-button");
            row.Add(button);

            var via = DimLabel($"via {ControlList(link)}");
            via.AddToClassList("vsp-grow");
            row.Add(via);
            return row;
        }

        static string ControlList(ParentMenuLink link)
        {
            var names = new List<string>(link.ControlNames.Count);
            foreach (string name in link.ControlNames)
            {
                string label = TextUtility.StripRichText(name);
                names.Add(string.IsNullOrEmpty(label) ? "[Unnamed]" : $"\"{label}\"");
            }

            return (names.Count > 1 ? "controls " : string.Empty) + string.Join(", ", names);
        }

        void AddAvatarRows(List<string> avatars)
        {
            foreach (string avatar in avatars)
            {
                var row = Ui.Row("vsp-parent-row");
                var label = new Label($"Root menu of {avatar}") { tooltip = "Expressions Menu of this avatar descriptor (open scenes only)" };
                label.AddToClassList("vsp-parent-avatar");
                row.Add(label);
                rows.Add(row);
            }
        }

        static Label DimLabel(string text)
        {
            var label = new Label(text);
            label.AddToClassList("vsp-dim");
            return label;
        }
    }
}
