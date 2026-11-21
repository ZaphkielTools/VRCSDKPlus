using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using static Zaphkiel.VRCSDKPlus.ExpressionParameterUtility;

namespace Zaphkiel.VRCSDKPlus
{
    internal sealed class ParametersEditor : Editor
    {
        const int MaxCost = VRCExpressionParameters.MAX_PARAMETER_COST;
        const float RowHeight = 22;

        readonly List<int> rows = new List<int>();
        readonly HashSet<ParameterCell> boundCells = new HashSet<ParameterCell>();
        Dictionary<string, AnimatorControllerParameter> controllerParameters =
            new Dictionary<string, AnimatorControllerParameter>(StringComparer.Ordinal);
        ParameterStatus[] statuses = Array.Empty<ParameterStatus>();

        MultiColumnListView list;
        ProgressBar memoryBar;
        ToolbarSearchField searchField;
        VisualElement cleanupBox;
        Label cleanupLabel;
        string search = string.Empty;
        int knownSize = -1;
        int refreshedDirtyCount = -1;

        internal VRCExpressionParameters Asset => (VRCExpressionParameters)target;
        internal SerializedProperty Parameters { get; private set; }
        internal VRCAvatarDescriptor Avatar => AvatarContext.Selected;
        internal bool HasAvatar => Avatar != null;
        internal IReadOnlyDictionary<string, AnimatorControllerParameter> ControllerParameters => controllerParameters;

        internal ParameterStatus StatusAt(int index) =>
            index >= 0 && index < statuses.Length ? statuses[index] : default;

        internal void ApplyChanges() => serializedObject.ApplyModifiedProperties();

        internal VRCExpressionParameters.Parameter[] AssetParameters => Asset.parameters ?? Array.Empty<VRCExpressionParameters.Parameter>();

        public override VisualElement CreateInspectorGUI()
        {
            Parameters = serializedObject.FindProperty(ParametersField);

            var root = Ui.CreateRoot("vsp-parameters");
            root.Add(new AvatarSelector(avatar => avatar.expressionParameters == Asset));
            root.Add(memoryBar = CreateMemoryBar());
            root.Add(searchField = CreateSearchField());
            root.Add(list = CreateList());
            root.Add(cleanupBox = CreateCleanupBox());
            root.Add(CreateMergeField());
            root.Add(Ui.Credits(null, ("Made by @Dreadrith ♡", "https://github.com/Dreadrith")));
            root.Add(Ui.ForkCredits());

            Ui.Track(root, serializedObject, OnTrackedChange);
            root.RegisterCallback<AttachToPanelEvent>(_ =>
            {
                AvatarContext.Changed += OnAvatarChanged;
                Undo.undoRedoPerformed += OnUndoRedo;
            });
            root.RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                AvatarContext.Changed -= OnAvatarChanged;
                Undo.undoRedoPerformed -= OnUndoRedo;
            });
            root.RegisterCallback<ValidateCommandEvent>(OnValidateCommand);
            root.RegisterCallback<ExecuteCommandEvent>(OnExecuteCommand);

            OnAvatarChanged();
            return root;
        }

        #region Building

        ProgressBar CreateMemoryBar()
        {
            var bar = new ProgressBar { lowValue = 0, highValue = MaxCost, tooltip = "Synced parameter memory, as counted by the VRChat SDK (bit widths included)" };
            bar.AddToClassList("vsp-memory");
            return bar;
        }

        ToolbarSearchField CreateSearchField()
        {
            var field = new ToolbarSearchField { tooltip = "Filter parameters by name (Ctrl+F)" };
            field.AddToClassList("vsp-search");
            field.RegisterValueChangedCallback(evt =>
            {
                search = evt.newValue ?? string.Empty;
                RebuildRows();
            });
            return field;
        }

        MultiColumnListView CreateList()
        {
            var columns = new Columns
            {
                MakeColumn("name", "Name", 150, true, () => new NameCell(this)),
                MakeColumn("type", "Type", 62, false, () => new TypeCell(this)),
                MakeColumn("default", "Default", 60, false, () => new DefaultCell(this)),
                MakeColumn("saved", "Saved", 52, false, () => new ToggleCell(this, SavedField, "Keeps its value when loading the avatar or changing worlds")),
                MakeColumn("synced", "Synced", 56, false, () => new ToggleCell(this, SyncedField, "Sent over the network to remote users. Counts towards the synced bits")),
                MakeColumn("remove", string.Empty, 26, false, () => new RemoveCell(this)),
            };

            var view = new MultiColumnListView(columns)
            {
                itemsSource = rows,
                fixedItemHeight = RowHeight,
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight,
                reorderable = true,
                reorderMode = ListViewReorderMode.Animated,
                showAddRemoveFooter = true,
                showBorder = true,
                selectionType = SelectionType.Single,
                horizontalScrollingEnabled = false,
                onAdd = _ => AddParameter(),
                onRemove = _ =>
                {
                    int index = SelectedIndex;
                    if (index >= 0) DeleteParameter(index);
                },
            };
            view.itemIndexChanged += OnReordered;
            view.AddToClassList("vsp-parameter-list");
            return view;
        }

        Column MakeColumn(string name, string title, float width, bool stretch, Func<ParameterCell> makeCell) => new Column
        {
            name = name,
            title = title,
            width = width,
            minWidth = stretch ? 120 : width,
            stretchable = stretch,
            resizable = stretch || width > 30,
            sortable = false,
            makeCell = makeCell,
            bindCell = (element, row) =>
            {
                var cell = (ParameterCell)element;
                cell.Bind(rows[row]);
                boundCells.Add(cell);
            },
            unbindCell = (element, _) =>
            {
                var cell = (ParameterCell)element;
                cell.Unbind();
                boundCells.Remove(cell);
            },
        };

        VisualElement CreateCleanupBox()
        {
            var box = Ui.Box();
            box.AddToClassList("vsp-row");
            box.AddToClassList("vsp-cleanup");
            box.Add(Ui.IconImage(Icons.Warn));
            box.Add(cleanupLabel = new Label());
            var button = new Button(CleanupParameters) { text = "Cleanup", tooltip = "Remove blank and duplicate parameters, and those no controller of the active avatar declares" };
            button.AddToClassList("vsp-small-button");
            box.Add(button);
            return box;
        }

        VisualElement CreateMergeField()
        {
            var box = Ui.Box();
            var field = new ObjectField("Merge Parameters")
            {
                objectType = typeof(VRCExpressionParameters),
                allowSceneObjects = false,
                tooltip = "Drop another Expression Parameters asset to append all of its parameters here",
            };
            field.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue is VRCExpressionParameters source) Merge(source);
                field.SetValueWithoutNotify(null);
            });
            box.Add(field);
            return box;
        }

        #endregion

        #region Refreshing

        void OnAvatarChanged()
        {
            controllerParameters = AvatarContext.GetControllerParameters(Avatar);
            OnAssetChanged(true);
        }

        void OnUndoRedo()
        {
            serializedObject.Update();
            OnAssetChanged(true);
        }

        void OnTrackedChange()
        {
            if (EditorUtility.GetDirtyCount(Asset) != refreshedDirtyCount) OnAssetChanged(false);
        }

        void OnAssetChanged(bool rebuildRows)
        {
            if (list == null || Parameters == null) return;

            refreshedDirtyCount = EditorUtility.GetDirtyCount(Asset);
            statuses = ParameterValidation.Evaluate(AssetParameters, controllerParameters, HasAvatar);
            if (rebuildRows || Parameters.arraySize != knownSize) RebuildRows();
            else foreach (var cell in boundCells) cell.Refresh();

            UpdateMemoryBar();
            UpdateCleanupBox();
        }

        void RebuildRows()
        {
            rows.Clear();
            var parameters = AssetParameters;
            bool filtering = !string.IsNullOrEmpty(search);
            for (int i = 0; i < parameters.Length; i++)
                if (!filtering || TextUtility.ContainsIgnoreCase(parameters[i]?.name, search))
                    rows.Add(i);

            knownSize = Parameters.arraySize;
            list.reorderable = !filtering;
            list.RefreshItems();
        }

        void UpdateMemoryBar()
        {
            int cost = Asset.parameters == null ? 0 : Asset.CalcTotalCost();
            memoryBar.value = Mathf.Min(cost, MaxCost);
            memoryBar.title = $"{cost} / {MaxCost} Synced Bits";
            memoryBar.EnableInClassList("vsp-memory--high", cost > MaxCost / 2 && cost <= MaxCost);
            memoryBar.EnableInClassList("vsp-memory--over", cost > MaxCost);
        }

        void UpdateCleanupBox()
        {
            int issues = 0;
            foreach (var status in statuses)
                if (status.HasIssue) issues++;

            cleanupBox.SetVisible(issues > 0);
            cleanupLabel.text = issues == 1
                ? "1 parameter is blank, duplicated, or missing from the avatar's controllers."
                : $"{issues} parameters are blank, duplicated, or missing from the avatar's controllers.";
        }

        #endregion

        #region Actions

        int SelectedIndex => list.selectedIndex >= 0 && list.selectedIndex < rows.Count ? rows[list.selectedIndex] : -1;

        void Select(int index)
        {
            int row = rows.IndexOf(index);
            if (row < 0) return;
            list.SetSelection(row);
            list.ScrollToItem(row);
        }

        void ClearSearch()
        {
            if (string.IsNullOrEmpty(search)) return;
            search = string.Empty;
            searchField.SetValueWithoutNotify(string.Empty);
        }

        void AddParameter()
        {
            ClearSearch();
            int index = Parameters.arraySize;
            Parameters.InsertArrayElementAtIndex(index);
            if (index == 0) ExpressionParameterUtility.Initialize(Parameters.GetArrayElementAtIndex(0), string.Empty, VRCExpressionParameters.ValueType.Int);
            MakeNameUnique(index);
            UpdateIsEmpty(serializedObject);
            ApplyChanges();
            OnAssetChanged(true);
            Select(index);
        }

        internal void DuplicateParameter(int index)
        {
            if (index < 0 || index >= Parameters.arraySize) return;
            Parameters.InsertArrayElementAtIndex(index);
            MakeNameUnique(index + 1);
            ApplyChanges();
            OnAssetChanged(true);
            Select(index + 1);
        }

        internal void DeleteParameter(int index)
        {
            if (index < 0 || index >= Parameters.arraySize) return;
            Parameters.DeleteArrayElementAtIndex(index);
            UpdateIsEmpty(serializedObject);
            ApplyChanges();
            OnAssetChanged(true);
        }

        internal void SetName(int index, string name)
        {
            if (index < 0 || index >= Parameters.arraySize) return;
            Parameters.GetArrayElementAtIndex(index).FindPropertyRelative(NameField).stringValue = name;
            ApplyChanges();
        }

        void MakeNameUnique(int index)
        {
            var taken = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < Parameters.arraySize; i++)
                if (i != index) taken.Add(Parameters.GetArrayElementAtIndex(i).FindPropertyRelative(NameField).stringValue);

            var name = Parameters.GetArrayElementAtIndex(index).FindPropertyRelative(NameField);
            name.stringValue = TextUtility.MakeUnique(name.stringValue, candidate => !taken.Contains(candidate));
        }

        void OnReordered(int from, int to)
        {
            if (from == to) return;
            Parameters.MoveArrayElement(from, to);
            ApplyChanges();
            OnAssetChanged(true);
            list.SetSelection(to);
        }

        internal void AddToController(int index, VRCAvatarDescriptor.AnimLayerType layer, string layerLabel)
        {
            var controller = AvatarContext.GetPlayableController(Avatar, layer);
            var parameters = Asset.parameters;
            if (!controller || parameters == null || index < 0 || index >= parameters.Length) return;

            var parameter = parameters[index];
            foreach (var existing in controller.parameters)
                if (existing.name == parameter.name)
                {
                    AvatarContext.NotifyChanged();
                    return;
                }

            var type = ToControllerType(parameter.valueType);
            Undo.RecordObject(controller, "Add Controller Parameter");
            controller.AddParameter(new AnimatorControllerParameter
            {
                name = parameter.name,
                type = type,
                defaultFloat = parameter.defaultValue,
                defaultInt = (int)parameter.defaultValue,
                defaultBool = parameter.defaultValue > 0,
            });
            EditorUtility.SetDirty(controller);
            Log.Info($"Added {type} {parameter.name} to the {layerLabel} controller.");
            AvatarContext.NotifyChanged();
        }

        void CleanupParameters()
        {
            var parameters = AssetParameters;
            var remove = ParameterValidation.PlanCleanup(parameters, controllerParameters, HasAvatar);
            var names = new List<string>();
            for (int i = 0; i < remove.Length; i++)
                if (remove[i]) names.Add(string.IsNullOrEmpty(parameters[i]?.name) ? $"(blank, #{i})" : parameters[i].name);
            if (names.Count == 0) return;

            const int Shown = 12;
            string list = string.Join("\n", names.GetRange(0, Math.Min(Shown, names.Count)));
            if (names.Count > Shown) list += $"\n… and {names.Count - Shown} more";
            bool confirmed = EditorUtility.DisplayDialog("VRCSDK+ Cleanup",
                $"Remove {names.Count} parameter(s)?\n\n{list}\n\n" +
                "Parameters that Modular Avatar or VRCFury add to the controllers at build time count as missing here. " +
                "Undo (Ctrl+Z) restores them.",
                "Remove", "Cancel");
            if (!confirmed) return;

            for (int i = remove.Length - 1; i >= 0; i--)
                if (remove[i]) Parameters.DeleteArrayElementAtIndex(i);

            UpdateIsEmpty(serializedObject);
            ApplyChanges();
            OnAssetChanged(true);
            Log.Info($"Cleanup removed {names.Count} parameter(s): {string.Join(", ", names)}.");
        }

        void Merge(VRCExpressionParameters source)
        {
            if (source == Asset || source.parameters == null || source.parameters.Length == 0) return;

            EditAsset("Merge Parameters", asset =>
            {
                var merged = new List<VRCExpressionParameters.Parameter>(AssetParameters);
                foreach (var parameter in source.parameters)
                    if (parameter != null) merged.Add(Clone(parameter));
                asset.parameters = merged.ToArray();
                asset.isEmpty = false;
            });
            Log.Info($"Merged {source.parameters.Length} parameter(s) from {source.name}.");
        }

        void EditAsset(string undoName, Action<VRCExpressionParameters> change)
        {
            ApplyChanges();
            Undo.RecordObject(Asset, undoName);
            change(Asset);
            EditorUtility.SetDirty(Asset);
            serializedObject.Update();
            OnAssetChanged(true);
        }

        #endregion

        #region Keyboard

        void OnValidateCommand(ValidateCommandEvent evt)
        {
            if (AcceptsCommand(evt.commandName, evt.target as VisualElement)) evt.StopPropagation();
        }

        void OnExecuteCommand(ExecuteCommandEvent evt)
        {
            if (!AcceptsCommand(evt.commandName, evt.target as VisualElement)) return;
            evt.StopPropagation();

            switch (evt.commandName)
            {
                case "Find":
                    searchField.Focus();
                    break;
                case "Duplicate":
                    DuplicateParameter(SelectedIndex);
                    break;
                case "SoftDelete":
                case "Delete":
                    DeleteParameter(SelectedIndex);
                    break;
            }
        }

        bool AcceptsCommand(string command, VisualElement target)
        {
            if (command == "Find") return true;
            if (target == null || Ui.IsEditingText(target) || SelectedIndex < 0) return false;
            return command == "Duplicate" || command == "SoftDelete" || command == "Delete";
        }

        #endregion
    }
}
