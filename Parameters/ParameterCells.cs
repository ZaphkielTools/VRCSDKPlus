using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using ValueType = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType;
using static Zaphkiel.VRCSDKPlus.ExpressionParameterUtility;

namespace Zaphkiel.VRCSDKPlus
{
    internal abstract class ParameterCell : VisualElement
    {
        protected readonly ParametersEditor Owner;

        protected ParameterCell(ParametersEditor editor, string className)
        {
            Owner = editor;
            AddToClassList("vsp-cell");
            AddToClassList(className);
            this.AddManipulator(new ContextualMenuManipulator(BuildContextMenu));
        }

        public int Index { get; private set; } = -1;
        protected SerializedProperty Element { get; private set; }

        public void Bind(int index)
        {
            Index = index;
            Element = Owner.Parameters.GetArrayElementAtIndex(index);
            OnBind();
            Refresh();
        }

        public void Unbind()
        {
            OnUnbind();
            Element = null;
            Index = -1;
        }

        protected virtual void OnBind() { }
        protected virtual void OnUnbind() { }
        public virtual void Refresh() { }

        void BuildContextMenu(ContextualMenuPopulateEvent evt)
        {
            if (Index < 0) return;
            int index = Index;
            evt.menu.AppendAction("Duplicate", _ => Owner.DuplicateParameter(index));
            evt.menu.AppendSeparator();
            evt.menu.AppendAction("Delete", _ => Owner.DeleteParameter(index));
        }
    }

    internal sealed class NameCell : ParameterCell
    {
        readonly Button pickButton;
        readonly TextField field;
        readonly Label hint;
        readonly Button addButton;
        readonly VisualElement warning;

        public NameCell(ParametersEditor editor) : base(editor, "vsp-name-cell")
        {
            pickButton = Ui.DropdownButton("Pick a parameter from the active avatar's controllers", OpenPicker);
            field = new TextField();
            field.textEdition.placeholder = "Name";
            field.AddToClassList("vsp-name-field");
            hint = new Label { tooltip = "Type of this parameter in the avatar's controllers" };
            hint.AddToClassList("vsp-type-hint");
            addButton = new Button { text = "Add", tooltip = "Add this parameter to one of the avatar's controllers" };
            addButton.clicked += () => OpenAddMenu();
            addButton.AddToClassList("vsp-small-button");
            warning = Ui.IconImage(Icons.Warn);

            Add(pickButton);
            Add(field);
            Add(hint);
            Add(addButton);
            Add(warning);
        }

        protected override void OnBind() => field.BindProperty(Element.FindPropertyRelative(NameField));
        protected override void OnUnbind() => field.Unbind();

        public override void Refresh()
        {
            if (Index < 0) return;
            var status = Owner.StatusAt(Index);
            bool hasAvatar = Owner.HasAvatar;

            hint.text = !hasAvatar ? string.Empty : status.Match != null ? $"({status.Match.type})" : "(?)";
            hint.SetVisible(hasAvatar);
            addButton.SetVisible(hasAvatar && status.IsMissing);
            warning.SetVisible(status.HasIssue);
            warning.tooltip = status.Message;
            pickButton.SetEnabled(Owner.ControllerParameters.Count > 0);
        }

        void OpenPicker(Button button)
        {
            int index = Index;
            var used = new HashSet<string>(StringComparer.Ordinal);
            foreach (var parameter in Owner.AssetParameters)
                if (parameter != null) used.Add(parameter.name);

            var options = new List<AnimatorControllerParameter>();
            foreach (var parameter in Owner.ControllerParameters.Values)
                if (!used.Contains(parameter.name)) options.Add(parameter);
            if (options.Count == 0) return;

            options.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            new ControllerParameterDropdown(options, name => Owner.SetName(index, name)).Show(button.worldBound);
        }

        void OpenAddMenu()
        {
            int index = Index;
            var menu = new GenericMenu();
            foreach (var (label, type) in AvatarContext.PlayableLayers)
            {
                if (AvatarContext.GetPlayableController(Owner.Avatar, type))
                    menu.AddItem(new GUIContent(label), false, () => Owner.AddToController(index, type, label));
                else
                    menu.AddDisabledItem(new GUIContent(label));
            }

            menu.DropDown(addButton.worldBound);
        }
    }

    internal sealed class TypeCell : ParameterCell
    {
        readonly EnumField field = new EnumField(ValueType.Int);

        public TypeCell(ParametersEditor editor) : base(editor, "vsp-cell--stretch")
        {
            field.RegisterValueChangedCallback(OnChanged);
            Add(field);
        }

        public override void Refresh()
        {
            if (Element == null) return;
            field.SetValueWithoutNotify(GetValueType(Element));
        }

        void OnChanged(ChangeEvent<Enum> evt)
        {
            if (Element == null || evt.newValue is not ValueType type || Equals(evt.previousValue, evt.newValue)) return;
            SetValueType(Element, type);
            ResetEncoding(Element, type);
            var defaultValue = Element.FindPropertyRelative(DefaultValueField);
            defaultValue.floatValue = ClampDefault(Element, defaultValue.floatValue);
            Owner.ApplyChanges();
        }
    }

    internal sealed class DefaultCell : ParameterCell
    {
        readonly Toggle boolField = new Toggle();
        readonly IntegerField intField = new IntegerField { isDelayed = true };
        readonly FloatField floatField = new FloatField { isDelayed = true };

        public DefaultCell(ParametersEditor editor) : base(editor, "vsp-cell--stretch")
        {
            boolField.RegisterValueChangedCallback(evt => Write(evt.newValue ? 1 : 0));
            intField.RegisterValueChangedCallback(evt => Write(evt.newValue));
            floatField.RegisterValueChangedCallback(evt => Write(evt.newValue));
            Add(boolField);
            Add(intField);
            Add(floatField);
        }

        public override void Refresh()
        {
            if (Element == null) return;
            var type = GetValueType(Element);
            float value = Element.FindPropertyRelative(DefaultValueField).floatValue;

            boolField.SetVisible(type == ValueType.Bool);
            intField.SetVisible(type == ValueType.Int);
            floatField.SetVisible(type == ValueType.Float);
            boolField.SetValueWithoutNotify(value != 0);
            intField.SetValueWithoutNotify(Mathf.RoundToInt(value));
            floatField.SetValueWithoutNotify(value);
        }

        void Write(float value)
        {
            if (Element == null) return;
            var property = Element.FindPropertyRelative(DefaultValueField);
            property.floatValue = ClampDefault(Element, value);
            Owner.ApplyChanges();
            Refresh();
        }
    }

    internal sealed class ToggleCell : ParameterCell
    {
        readonly string fieldName;
        readonly Toggle toggle = new Toggle();

        public ToggleCell(ParametersEditor editor, string fieldName, string tooltip) : base(editor, "vsp-cell--center")
        {
            this.fieldName = fieldName;
            toggle.tooltip = tooltip;
            Add(toggle);
        }

        protected override void OnBind() => toggle.BindProperty(Element.FindPropertyRelative(fieldName));
        protected override void OnUnbind() => toggle.Unbind();
    }

    internal sealed class RemoveCell : ParameterCell
    {
        public RemoveCell(ParametersEditor editor) : base(editor, "vsp-cell--center")
        {
            Add(Ui.IconButton(Icons.Remove, "Remove this parameter", () =>
            {
                if (Index >= 0) Owner.DeleteParameter(Index);
            }));
        }
    }
}
