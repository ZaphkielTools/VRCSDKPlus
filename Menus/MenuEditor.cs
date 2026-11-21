using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.SDK3.Avatars.ScriptableObjects;
using ControlType = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType;

namespace Zaphkiel.VRCSDKPlus
{
    internal sealed class MenuEditor : Editor, IHasCustomMenu
    {
        const int MaxControls = VRCExpressionsMenu.MAX_CONTROLS;
        const string CompactModePref = "VSP_Compact";
        const string DuplicateTooltip = "Duplicate (Ctrl+D). Hold Shift to keep the same parameter";
        const int NoDetails = -2;

        readonly List<int> rows = new List<int>();
        SerializedProperty controls;
        ListView list;
        VisualElement details;
        Label title;
        Button copyButton;
        Button duplicateButton;
        Button moveButton;
        Button addButton;
        int knownSize = -1;
        int detailsIndex = NoDetails;
        int detailsType = NoDetails;
        bool detailsCompact;

        static VRCExpressionsMenu revealParent;
        static VRCExpressionsMenu revealChild;

        internal static void RevealSubMenuOnOpen(VRCExpressionsMenu parent, VRCExpressionsMenu child)
        {
            revealParent = parent;
            revealChild = child;
        }

        static bool CompactMode
        {
            get => EditorPrefs.GetBool(CompactModePref, false);
            set => EditorPrefs.SetBool(CompactModePref, value);
        }

        VRCExpressionsMenu Menu => (VRCExpressionsMenu)target;
        int SelectedIndex => list.selectedIndex >= 0 && list.selectedIndex < controls.arraySize ? list.selectedIndex : -1;
        bool IsFull => controls.arraySize >= MaxControls;

        public override VisualElement CreateInspectorGUI()
        {
            MenuHistory.Visit(Menu);
            controls = serializedObject.FindProperty(nameof(VRCExpressionsMenu.controls));

            var root = Ui.CreateRoot("vsp-menu");
            root.Add(MenuHistory.CreateBar(Menu));
            root.Add(new MenuParentsView(Menu));
            root.Add(new AvatarSelector(avatar => avatar.expressionsMenu == Menu,
                avatar => avatar && avatar.expressionParameters ? null : Messages.MissingParameters));
            root.Add(CreateHeader());
            root.Add(list = CreateList());
            root.Add(details = new VisualElement());
            root.Add(Ui.Credits("Editor by", ("@fox_score", "https://github.com/foxscore"), ("@Dreadrith", "https://github.com/Dreadrith")));
            root.Add(Ui.ForkCredits());

            Ui.Track(root, serializedObject, OnMenuChanged);
            root.RegisterCallback<AttachToPanelEvent>(_ =>
            {
                AvatarContext.Changed += RebuildDetailsNow;
                MenuClipboard.MoveStateChanged += UpdateHeader;
                Undo.undoRedoPerformed += OnUndoRedo;
            });
            root.RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                AvatarContext.Changed -= RebuildDetailsNow;
                MenuClipboard.MoveStateChanged -= UpdateHeader;
                Undo.undoRedoPerformed -= OnUndoRedo;
            });
            list.RegisterCallback<ValidateCommandEvent>(OnValidateCommand);
            list.RegisterCallback<ExecuteCommandEvent>(OnExecuteCommand);
            list.RegisterCallback<KeyDownEvent>(OnKeyDown);

            RebuildRows();
            int initial = revealParent == Menu ? IndexOfSubMenu(revealChild) : -1;
            revealParent = revealChild = null;
            SelectAndShow(controls.arraySize > 0 ? Mathf.Max(0, initial) : -1);
            UpdateHeader();
            return root;
        }

        public void AddItemsToMenu(GenericMenu menu) =>
            menu.AddItem(new GUIContent("Compact Mode"), CompactMode, () =>
            {
                CompactMode = !CompactMode;
                RebuildDetailsNow();
            });

        #region Building

        VisualElement CreateHeader()
        {
            var header = Ui.Row("vsp-box");
            header.Add(title = new Label());
            title.AddToClassList("vsp-controls-title");

            copyButton = Ui.IconButton(Icons.Copy, "Copy (Ctrl+C)", () => Copy(SelectedIndex));
            Button pasteButton = null;
            pasteButton = Ui.IconButton(Icons.Paste, "Paste (Ctrl+V)", () => OpenPasteMenu(pasteButton));
            duplicateButton = Ui.IconButton(Icons.Duplicate, DuplicateTooltip, evt => Duplicate(SelectedIndex, Ui.IsShiftHeld(evt)));
            moveButton = Ui.IconButton(Icons.Move, "Cut (Ctrl+X)", evt => MoveOrPlace(Ui.IsShiftHeld(evt)));

            header.Add(copyButton);
            header.Add(pasteButton);
            header.Add(duplicateButton);
            header.Add(moveButton);
            return header;
        }

        ListView CreateList()
        {
            var view = new ListView
            {
                itemsSource = rows,
                fixedItemHeight = 20,
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight,
                reorderable = true,
                reorderMode = ListViewReorderMode.Animated,
                showAddRemoveFooter = true,
                showBorder = true,
                selectionType = SelectionType.Single,
                makeItem = () => new ControlRow(this),
                bindItem = (element, index) => ((ControlRow)element).Bind(rows[index]),
                onAdd = _ => AddControl(),
                onRemove = _ => Delete(SelectedIndex),
            };
            view.selectedIndicesChanged += _ =>
            {
                RebuildDetails(false);
                UpdateHeader();
            };
            view.itemsChosen += _ => OpenSubMenu(SelectedIndex);
            view.itemIndexChanged += OnReordered;
            addButton = view.Q<Button>(BaseListView.footerAddButtonName);
            return view;
        }

        #endregion

        #region Refreshing

        void OnMenuChanged()
        {
            if (controls.arraySize != knownSize) RebuildRows();
            else list.RefreshItems();
            RebuildDetails(false);
            UpdateHeader();
        }

        void OnUndoRedo()
        {
            serializedObject.Update();
            RebuildRows();
            RebuildDetails(true);
            UpdateHeader();
        }

        void RebuildRows()
        {
            rows.Clear();
            for (int i = 0; i < controls.arraySize; i++) rows.Add(i);
            knownSize = controls.arraySize;
            list.RefreshItems();
            if (list.selectedIndex >= rows.Count) list.SetSelection(rows.Count - 1);
        }

        void RebuildDetailsNow() => RebuildDetails(true);

        internal void RebuildDetails(bool force)
        {
            if (details == null) return;
            int index = SelectedIndex;
            var control = index >= 0 ? controls.GetArrayElementAtIndex(index) : null;
            int type = control != null ? (int)ControlTypes.Get(control) : -1;
            bool compact = CompactMode;
            if (!force && index == detailsIndex && type == detailsType && compact == detailsCompact) return;

            detailsIndex = index;
            detailsType = type;
            detailsCompact = compact;
            details.Clear();
            details.Add(ControlDetails.Build(this, control, Menu, AvatarContext.Selected ? AvatarContext.Selected.expressionParameters : null, compact));
        }

        void UpdateHeader()
        {
            if (title == null) return;
            bool hasSelection = SelectedIndex >= 0;
            bool moving = MenuClipboard.IsMoving;

            title.text = $"Controls ({controls.arraySize} / {MaxControls})";
            copyButton.SetEnabled(hasSelection);
            duplicateButton.SetEnabled(hasSelection && !IsFull);
            duplicateButton.tooltip = IsFull ? Messages.MenuFull : DuplicateTooltip;
            Ui.SetIcon(moveButton, moving ? Icons.Place : Icons.Move);
            moveButton.tooltip = moving ? "Place the cut control after the selection (Ctrl+V). Hold Shift to go back to its menu" : "Cut (Ctrl+X)";
            moveButton.SetEnabled(moving || hasSelection);
            addButton?.SetEnabled(!IsFull);
        }

        #endregion

        #region Control actions

        internal void ChangeType(SerializedProperty control, ControlType from, ControlType to)
        {
            ControlTypes.Convert(control, from, to);
            serializedObject.ApplyModifiedProperties();
            list.RefreshItems();
            RebuildDetails(true);
        }

        void SelectAndShow(int index)
        {
            detailsIndex = NoDetails;
            if (index >= 0 && index < rows.Count)
            {
                list.SetSelection(index);
                list.ScrollToItem(index);
            }

            RebuildDetails(false);
        }

        bool CanAdd()
        {
            if (!IsFull) return true;
            Log.Warning(Messages.MenuFull);
            return false;
        }

        void AddControl()
        {
            if (!CanAdd()) return;

            int index = controls.arraySize;
            controls.arraySize++;
            var control = controls.GetArrayElementAtIndex(index);
            control.FindPropertyRelative(ControlTypes.NameField).stringValue = "New Control";
            control.FindPropertyRelative(ControlTypes.IconField).objectReferenceValue = null;
            control.FindPropertyRelative(ControlTypes.ParameterField).FindPropertyRelative(ControlTypes.ParameterNameField).stringValue = string.Empty;
            ControlTypes.Set(control, ControlType.Toggle);
            control.FindPropertyRelative(ControlTypes.SubMenuField).objectReferenceValue = null;
            control.FindPropertyRelative(ControlTypes.LabelsField).ClearArray();
            control.FindPropertyRelative(ControlTypes.SubParametersField).ClearArray();
            control.FindPropertyRelative(ControlTypes.ValueField).floatValue = 1;
            serializedObject.ApplyModifiedProperties();

            RebuildRows();
            SelectAndShow(index);
        }

        internal void Delete(int index)
        {
            if (index < 0 || index >= controls.arraySize) return;
            controls.DeleteArrayElementAtIndex(index);
            serializedObject.ApplyModifiedProperties();
            RebuildRows();
            SelectAndShow(Mathf.Min(index, rows.Count - 1));
        }

        internal void Copy(int index)
        {
            if (Menu.controls != null && index >= 0 && index < Menu.controls.Count) MenuClipboard.Copy(Menu.controls[index]);
        }

        void OpenPasteMenu(VisualElement anchor)
        {
            int index = SelectedIndex;
            bool canPaste = MenuClipboard.HasControl;
            var menu = new GenericMenu();
            if (canPaste && index >= 0) menu.AddItem(new GUIContent("Paste values"), false, () => PasteValues(index));
            else menu.AddDisabledItem(new GUIContent("Paste values"));
            if (canPaste && !IsFull) menu.AddItem(new GUIContent("Insert as new"), false, () => PasteAsNew(index));
            else menu.AddDisabledItem(new GUIContent(IsFull ? "Insert as new (menu full)" : "Insert as new"));
            menu.DropDown(anchor.worldBound);
        }

        void EditControls(string undoName, Action<List<VRCExpressionsMenu.Control>> change)
        {
            Undo.RecordObject(Menu, undoName);
            Menu.controls ??= new List<VRCExpressionsMenu.Control>();
            change(Menu.controls);
            EditorUtility.SetDirty(Menu);
            serializedObject.Update();
        }

        internal void PasteValues(int index)
        {
            var control = MenuClipboard.Paste();
            if (control == null || index < 0 || index >= controls.arraySize) return;
            EditControls("Paste Control Values", items => items[index] = control);
            list.RefreshItems();
            RebuildDetails(true);
        }

        internal void PasteAsNew(int afterIndex)
        {
            var control = MenuClipboard.Paste();
            if (control == null || !CanAdd()) return;

            int index = Mathf.Clamp(afterIndex + 1, 0, controls.arraySize);
            EditControls("Insert Control", items => items.Insert(index, control));
            RebuildRows();
            SelectAndShow(index);
        }

        internal void Duplicate(int index, bool keepParameter)
        {
            if (index < 0 || index >= controls.arraySize || !CanAdd()) return;

            controls.InsertArrayElementAtIndex(index);
            var copy = controls.GetArrayElementAtIndex(index + 1);
            var name = copy.FindPropertyRelative(ControlTypes.NameField);
            string original = name.stringValue;
            name.stringValue = TextUtility.MakeUnique(original, candidate => candidate != original, false);

            if (!keepParameter && ControlTypes.IsSimple(ControlTypes.Get(copy)))
            {
                var parameterName = copy.FindPropertyRelative(ControlTypes.ParameterField).FindPropertyRelative(ControlTypes.ParameterNameField);
                var parameters = AvatarContext.Selected ? AvatarContext.Selected.expressionParameters : null;
                var parameter = ExpressionParameterUtility.Find(parameters, parameterName.stringValue);
                if (parameter != null)
                {
                    string parameterOriginal = parameterName.stringValue;
                    if (parameter.valueType == VRCExpressionParameters.ValueType.Bool)
                    {
                        parameterName.stringValue = TextUtility.MakeUnique(parameterOriginal, candidate => candidate != parameterOriginal, false);
                    }
                    else
                    {
                        var value = copy.FindPropertyRelative(ControlTypes.ValueField);
                        if (Mathf.Approximately(value.floatValue, Mathf.Round(value.floatValue))) value.floatValue++;
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
            RebuildRows();
            SelectAndShow(index + 1);
        }

        void MoveOrPlace(bool returnToSource)
        {
            if (MenuClipboard.IsMoving) Place(SelectedIndex, returnToSource);
            else StartMove(SelectedIndex);
        }

        internal void StartMove(int index)
        {
            if (Menu.controls != null && index >= 0 && index < Menu.controls.Count) MenuClipboard.BeginMove(Menu, Menu.controls[index]);
        }

        bool CanPlaceHere => MenuClipboard.IsMovingWithin(Menu) || !IsFull;

        internal void Place(int afterIndex, bool returnToSource)
        {
            if (afterIndex < 0) afterIndex = controls.arraySize - 1;
            switch (MenuClipboard.Place(Menu, afterIndex, out int placed, out var source))
            {
                case PlaceResult.Placed:
                    serializedObject.Update();
                    RebuildRows();
                    SelectAndShow(placed);
                    if (returnToSource && source && source != Menu) Selection.activeObject = source;
                    break;
                case PlaceResult.MenuFull:
                    Log.Warning(Messages.MenuFull);
                    break;
                case PlaceResult.ControlGone:
                    Log.Warning("The cut control is no longer in its menu (deleted or undone). The move was cancelled.");
                    break;
                case PlaceResult.NothingToPlace:
                    break;
            }
        }

        int IndexOfSubMenu(VRCExpressionsMenu subMenu)
        {
            if (!subMenu || Menu.controls == null) return -1;
            for (int i = 0; i < Menu.controls.Count; i++)
                if (Menu.controls[i]?.subMenu == subMenu)
                    return i;
            return -1;
        }

        void OpenSubMenu(int index)
        {
            if (Menu.controls == null || index < 0 || index >= Menu.controls.Count) return;
            var control = Menu.controls[index];
            if (control.type == ControlType.SubMenu && control.subMenu) Selection.activeObject = control.subMenu;
        }

        void OnReordered(int from, int to)
        {
            if (from == to) return;
            controls.MoveArrayElement(from, to);
            serializedObject.ApplyModifiedProperties();
            RebuildRows();
            SelectAndShow(to);
        }

        #endregion

        #region Keyboard

        static bool IsMenuCommand(string command) => command switch
        {
            "Copy" or "Cut" or "Paste" or "Duplicate" or "SoftDelete" or "Delete" => true,
            _ => false,
        };

        void OnValidateCommand(ValidateCommandEvent evt)
        {
            if (IsMenuCommand(evt.commandName) && !Ui.IsEditingText(list)) evt.StopPropagation();
        }

        void OnExecuteCommand(ExecuteCommandEvent evt)
        {
            if (!IsMenuCommand(evt.commandName) || Ui.IsEditingText(list)) return;
            evt.StopPropagation();
            int index = SelectedIndex;
            bool shift = Ui.IsShiftHeld(evt);

            switch (evt.commandName)
            {
                case "Copy":
                    Copy(index);
                    break;
                case "Cut":
                    StartMove(index);
                    break;
                case "Paste":
                    if (MenuClipboard.IsMoving) Place(index, shift);
                    else PasteAsNew(index);
                    break;
                case "Duplicate":
                    Duplicate(index, shift);
                    break;
                case "SoftDelete":
                case "Delete":
                    Delete(index);
                    break;
            }
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Escape || !MenuClipboard.IsMoving) return;
            MenuClipboard.CancelMove();
            evt.StopPropagation();
        }

        #endregion

        sealed class ControlRow : VisualElement
        {
            readonly MenuEditor editor;
            readonly Label nameLabel = new Label();
            readonly Label typeLabel = new Label();
            int index = -1;

            public ControlRow(MenuEditor editor)
            {
                this.editor = editor;
                AddToClassList("vsp-control-row");
                nameLabel.AddToClassList("vsp-control-name");
                nameLabel.enableRichText = true;
                typeLabel.AddToClassList("vsp-control-type");
                Add(nameLabel);
                Add(typeLabel);
                Add(Ui.IconButton(Icons.Remove, "Remove this control", () => editor.Delete(index)));
                this.AddManipulator(new ContextualMenuManipulator(BuildContextMenu));
            }

            public void Bind(int controlIndex)
            {
                index = controlIndex;
                var control = editor.controls.GetArrayElementAtIndex(controlIndex);
                string name = control.FindPropertyRelative(ControlTypes.NameField).stringValue;
                bool unnamed = string.IsNullOrEmpty(name);
                nameLabel.text = unnamed ? "[Unnamed]" : name;
                typeLabel.text = ControlTypes.Get(control).ToString();
                EnableInClassList("vsp-control-row--unnamed", unnamed);
            }

            static DropdownMenuAction.Status StatusFor(bool enabled) =>
                enabled ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;

            void BuildContextMenu(ContextualMenuPopulateEvent evt)
            {
                int i = index;
                if (i < 0) return;
                bool hasCopy = MenuClipboard.HasControl;

                evt.menu.AppendAction("Cut", _ => editor.StartMove(i));
                evt.menu.AppendAction("Copy", _ => editor.Copy(i));
                evt.menu.AppendAction("Paste/Values", _ => editor.PasteValues(i), StatusFor(hasCopy));
                evt.menu.AppendAction("Paste/As New", _ => editor.PasteAsNew(i), StatusFor(hasCopy && !editor.IsFull));
                if (MenuClipboard.IsMoving) evt.menu.AppendAction("Place Here", _ => editor.Place(i, false), StatusFor(editor.CanPlaceHere));
                evt.menu.AppendSeparator();
                evt.menu.AppendAction("Duplicate", _ => editor.Duplicate(i, false), StatusFor(!editor.IsFull));
                evt.menu.AppendAction("Delete", _ => editor.Delete(i));
            }
        }
    }
}
