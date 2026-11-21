using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using VRC.SDK3.Avatars.Components;

namespace Zaphkiel.VRCSDKPlus
{
    internal sealed class AvatarSelector : VisualElement
    {
        readonly PopupField<VRCAvatarDescriptor> popup;
        readonly Label emptyLabel;
        readonly VisualElement warning;
        readonly Func<VRCAvatarDescriptor, string> warningFor;
        List<VRCAvatarDescriptor> avatars = new List<VRCAvatarDescriptor>();
        bool refreshQueued;

        public AvatarSelector(Predicate<VRCAvatarDescriptor> preferred, Func<VRCAvatarDescriptor, string> warningFor = null)
        {
            this.warningFor = warningFor;
            AddToClassList("vsp-avatar");
            AddToClassList("vsp-box");
            AddToClassList("vsp-row");

            popup = new PopupField<VRCAvatarDescriptor>("Active Avatar")
            {
                tooltip = "Warnings and auto-fill are based on this avatar",
                formatSelectedValueCallback = AvatarName,
                formatListItemCallback = AvatarName,
            };
            popup.AddToClassList("vsp-grow");
            popup.RegisterValueChangedCallback(evt =>
            {
                if (!evt.newValue) return;
                AvatarContext.Select(evt.newValue);
                EditorGUIUtility.PingObject(evt.newValue);
            });

            emptyLabel = new Label("Active Avatar: no avatar descriptor in the open scenes");
            emptyLabel.AddToClassList("vsp-grow");
            warning = Ui.IconImage(Icons.Error);

            Add(popup);
            Add(emptyLabel);
            Add(warning);
            Add(Ui.IconButton(Icons.Refresh, "Find avatars again and re-read their controllers", () =>
            {
                Refresh();
                AvatarContext.NotifyChanged();
            }));

            avatars = AvatarContext.FindSceneAvatars();
            AvatarContext.Resolve(avatars, preferred);
            Populate();

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                EditorApplication.hierarchyChanged += QueueRefresh;
                AvatarContext.Changed += SyncSelection;
            });
            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                EditorApplication.hierarchyChanged -= QueueRefresh;
                AvatarContext.Changed -= SyncSelection;
            });
        }

        static string AvatarName(VRCAvatarDescriptor avatar) => avatar ? avatar.gameObject.name : "(missing)";

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
            avatars = AvatarContext.FindSceneAvatars();
            AvatarContext.Resolve(avatars);
            Populate();
        }

        void Populate()
        {
            bool hasAvatars = avatars.Count > 0;
            popup.SetVisible(hasAvatars);
            emptyLabel.SetVisible(!hasAvatars);
            popup.SetEnabled(avatars.Count > 1);
            popup.choices = avatars;
            SyncSelection();
        }

        void SyncSelection()
        {
            var selected = AvatarContext.Selected;
            if (selected && avatars.Contains(selected)) popup.SetValueWithoutNotify(selected);

            string message = warningFor?.Invoke(selected);
            warning.tooltip = message;
            warning.SetVisible(!string.IsNullOrEmpty(message));
        }
    }
}
