using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.SDK3.Avatars.ScriptableObjects;
using ControlType = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType;
using ValueType = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType;

namespace Zaphkiel.VRCSDKPlus
{
    internal static class ControlDetails
    {
        const float IconSize = 96;
        const float CompactIconSize = 60;
        const float AxisIconSize = 58;

        static readonly string[] AxisNames = { "Up", "Right", "Down", "Left" };

        sealed class Context
        {
            public MenuEditor Editor;
            public SerializedProperty Control;
            public VRCExpressionsMenu Menu;
            public VRCExpressionParameters Parameters;
            public SerializedObject ParametersObject;

            public SerializedProperty SubParameter(int index) =>
                Control.FindPropertyRelative(ControlTypes.SubParametersField).GetArrayElementAtIndex(index);

            public SerializedProperty Label(int index) =>
                Control.FindPropertyRelative(ControlTypes.LabelsField).GetArrayElementAtIndex(index);

            public ControlParameterField Field(string label, SerializedProperty parameter, ValueType? filter, bool required,
                SerializedProperty value = null) =>
                new ControlParameterField(label, parameter, Parameters, ParametersObject, filter, required, value);
        }

        public static VisualElement Build(MenuEditor editor, SerializedProperty control, VRCExpressionsMenu menu,
            VRCExpressionParameters parameters, bool compact)
        {
            var panel = new VisualElement();
            panel.AddToClassList("vsp-details");

            if (control == null)
            {
                panel.Add(Ui.Placeholder(compact ? 66 : 150));
                panel.Add(Ui.Placeholder(24));
                return panel;
            }

            var context = new Context
            {
                Editor = editor,
                Control = control,
                Menu = menu,
                Parameters = parameters,
                ParametersObject = parameters ? new SerializedObject(parameters) : null,
            };
            panel.RegisterCallback<DetachFromPanelEvent>(_ => context.ParametersObject?.Dispose());

            var type = ControlTypes.Get(control);
            panel.Add(compact ? CompactHeader(context, type) : Header(context, type));

            if (ControlTypes.NeedsRepair(control, type))
            {
                panel.Add(RepairBox(context, type));
                return panel;
            }

            if (!compact)
            {
                var parameterBox = Ui.Box();
                parameterBox.Add(MainParameter(context, type, "Parameter"));
                panel.Add(parameterBox);
            }

            switch (type)
            {
                case ControlType.RadialPuppet:
                    var radial = Ui.Box();
                    radial.Add(context.Field("Rotation", context.SubParameter(0), ValueType.Float, true));
                    panel.Add(radial);
                    break;
                case ControlType.SubMenu:
                    panel.Add(SubMenuBox(context));
                    break;
                case ControlType.TwoAxisPuppet:
                    if (compact) panel.Add(CompactAxes(context, ("Horizontal", 0, new[] { 3, 1 }), ("Vertical", 1, new[] { 2, 0 })));
                    else AddAxes(panel, context, "Horizontal", "Vertical");
                    break;
                case ControlType.FourAxisPuppet:
                    if (compact) panel.Add(CompactAxes(context, ("Up", 0, new[] { 0 }), ("Right", 1, new[] { 1 }), ("Down", 2, new[] { 2 }), ("Left", 3, new[] { 3 })));
                    else AddAxes(panel, context, AxisNames);
                    break;
            }

            return panel;
        }

        static ControlParameterField MainParameter(Context context, ControlType type, string label) =>
            context.Field(label, context.Control.FindPropertyRelative(ControlTypes.ParameterField), null,
                ControlTypes.IsSimple(type), context.Control.FindPropertyRelative(ControlTypes.ValueField));

        static VisualElement RepairBox(Context context, ControlType type)
        {
            var box = Ui.Box();
            box.Add(new HelpBox($"This {type} control is missing some of its axis parameters or labels (old or hand-edited data).", HelpBoxMessageType.Warning));
            var repair = new Button(() =>
            {
                ControlTypes.Repair(context.Control, type);
                context.Control.serializedObject.ApplyModifiedProperties();
                Undo.SetCurrentGroupName("Repair Control");
                context.Editor.RebuildDetails(true);
            })
            {
                text = "Repair",
                tooltip = "Add the missing entries. Undo (Ctrl+Z) reverts it.",
            };
            repair.AddToClassList("vsp-small-button");
            box.Add(repair);
            return box;
        }

        #region Header

        static VisualElement Header(Context context, ControlType type)
        {
            var box = Ui.Box();
            var row = Ui.Row();
            row.style.alignItems = Align.FlexStart;

            var fields = new VisualElement();
            fields.AddToClassList("vsp-grow");
            fields.Add(LabeledRow("Name", Ui.TextField(context.Control.FindPropertyRelative(ControlTypes.NameField), "Name")));
            fields.Add(LabeledRow("Type", TypeField(context, type)));
            fields.Add(LabeledRow("Style", new StyleField(context.Control.FindPropertyRelative(ControlTypes.NameField))));
            row.Add(fields);
            row.Add(new IconField(context.Control.FindPropertyRelative(ControlTypes.IconField), IconSize));
            box.Add(row);

            box.Add(new HelpBox(ControlTypes.HelpText(type), HelpBoxMessageType.Info));
            return box;
        }

        static VisualElement CompactHeader(Context context, ControlType type)
        {
            var box = Ui.Box();
            var row = Ui.Row();
            row.style.alignItems = Align.FlexStart;

            var fields = new VisualElement();
            fields.AddToClassList("vsp-grow");

            var first = Ui.Row();
            first.Add(Ui.TextField(context.Control.FindPropertyRelative(ControlTypes.NameField), "Name"));
            var typeField = TypeField(context, type);
            typeField.AddToClassList("vsp-grow");
            first.Add(typeField);
            first.Add(Ui.IconImage(Icons.Help, ControlTypes.HelpText(type)));
            fields.Add(first);

            if (!ControlTypes.NeedsRepair(context.Control, type)) fields.Add(MainParameter(context, type, null));
            fields.Add(new StyleField(context.Control.FindPropertyRelative(ControlTypes.NameField)));
            row.Add(fields);
            row.Add(new IconField(context.Control.FindPropertyRelative(ControlTypes.IconField), CompactIconSize));
            box.Add(row);
            return box;
        }

        static VisualElement LabeledRow(string label, VisualElement field)
        {
            var row = Ui.Row();
            row.Add(Ui.FieldLabel(label));
            field.AddToClassList("vsp-grow");
            row.Add(field);
            return row;
        }

        static EnumField TypeField(Context context, ControlType type)
        {
            var field = new EnumField(type);
            var control = context.Control;
            field.RegisterValueChangedCallback(evt =>
            {
                if (evt.previousValue is ControlType from && evt.newValue is ControlType to && from != to)
                    context.Editor.ChangeType(control, from, to);
            });
            return field;
        }

        #endregion

        #region Type-specific sections

        static VisualElement SubMenuBox(Context context)
        {
            var subMenu = context.Control.FindPropertyRelative(ControlTypes.SubMenuField);
            var name = context.Control.FindPropertyRelative(ControlTypes.NameField);
            var box = Ui.Box();
            var row = Ui.Row();

            var field = new ObjectField("Sub Menu") { objectType = typeof(VRCExpressionsMenu), allowSceneObjects = false };
            field.BindProperty(subMenu);
            field.AddToClassList("vsp-grow");
            row.Add(field);

            var create = new Button(() => CreateSubMenu(context.Menu, subMenu, name)) { text = "New", tooltip = "Create a new menu asset next to this one" };
            create.AddToClassList("vsp-small-button");
            row.Add(create);
            var warning = Ui.IconImage(Icons.Warn, "Submenu is empty. This control has no use.");
            row.Add(warning);
            var open = Ui.IconButton(Icons.Folder, "Open the submenu", () => Selection.activeObject = subMenu.objectReferenceValue);
            row.Add(open);
            var clear = Ui.IconButton(Icons.Clear, "Clear", () =>
            {
                subMenu.objectReferenceValue = null;
                subMenu.serializedObject.ApplyModifiedProperties();
            });
            row.Add(clear);

            void Refresh()
            {
                bool empty = subMenu.objectReferenceValue == null;
                create.SetVisible(empty);
                warning.SetVisible(empty);
                open.SetEnabled(!empty);
                clear.SetEnabled(!empty);
            }

            Ui.Track(row, subMenu, Refresh);
            Refresh();
            box.Add(row);
            return box;
        }

        static void CreateSubMenu(VRCExpressionsMenu parent, SerializedProperty subMenu, SerializedProperty controlName)
        {
            string parentPath = AssetDatabase.GetAssetPath(parent);
            string folder = string.IsNullOrEmpty(parentPath) ? "Assets" : Path.GetDirectoryName(parentPath)?.Replace('\\', '/');
            string label = TextUtility.StripRichText(controlName.stringValue);
            string fileName = TextUtility.ToFileName(string.IsNullOrWhiteSpace(label) ? $"{parent.name} SubMenu" : $"{label} Menu");
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{fileName}.asset");

            var created = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
            created.controls ??= new List<VRCExpressionsMenu.Control>();
            AssetDatabase.CreateAsset(created, path);
            subMenu.objectReferenceValue = created;
            subMenu.serializedObject.ApplyModifiedProperties();
            Log.Info($"Created submenu {path}.");
        }

        static void AddAxes(VisualElement panel, Context context, params string[] axes)
        {
            var parametersBox = Ui.Box("Axis Parameters");
            for (int i = 0; i < axes.Length; i++)
                parametersBox.Add(context.Field(axes[i], context.SubParameter(i), ValueType.Float, true));
            panel.Add(parametersBox);

            var labelsBox = Ui.Box("Customization");
            var row = Ui.Row();
            row.style.alignItems = Align.FlexStart;
            for (int i = 0; i < AxisNames.Length; i++)
                row.Add(AxisLabel(context.Label(i), AxisNames[i]));
            labelsBox.Add(row);
            panel.Add(labelsBox);
        }

        static VisualElement AxisLabel(SerializedProperty label, string axis)
        {
            var element = new VisualElement();
            element.AddToClassList("vsp-axis-label");
            element.AddToClassList("vsp-box");
            var caption = new Label($"Axis: {axis}");
            caption.AddToClassList("vsp-axis-caption");
            element.Add(caption);
            element.Add(Ui.TextField(label.FindPropertyRelative(ControlTypes.LabelNameField), axis));
            var icon = new IconField(label.FindPropertyRelative(ControlTypes.LabelIconField), AxisIconSize);
            icon.style.alignSelf = Align.Center;
            icon.style.marginLeft = 0;
            element.Add(icon);
            return element;
        }

        static VisualElement CompactAxes(Context context, params (string Axis, int SubParameter, int[] Labels)[] rows)
        {
            var box = Ui.Box();
            foreach (var (axis, subParameter, labels) in rows)
            {
                var row = Ui.Row();
                var field = context.Field(axis, context.SubParameter(subParameter), ValueType.Float, true);
                field.AddToClassList("vsp-grow");
                row.Add(field);
                box.Add(row);

                var labelRow = labels.Length > 1 ? Ui.Row() : row;
                foreach (int index in labels)
                    labelRow.Add(CompactLabel(context.Label(index), AxisNames[index]));
                if (labelRow != row) box.Add(labelRow);
            }

            return box;
        }

        static VisualElement CompactLabel(SerializedProperty label, string axis)
        {
            var row = Ui.Row("vsp-compact-label");
            row.Add(Ui.TextField(label.FindPropertyRelative(ControlTypes.LabelNameField), axis));
            row.Add(new IconField(label.FindPropertyRelative(ControlTypes.LabelIconField), 0));
            return row;
        }

        #endregion
    }
}
