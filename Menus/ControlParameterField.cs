using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.SDK3.Avatars.ScriptableObjects;
using ValueType = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType;

namespace Zaphkiel.VRCSDKPlus
{
    internal sealed class ControlParameterField : VisualElement
    {
        readonly SerializedProperty nameProperty;
        readonly SerializedProperty valueProperty;
        readonly VRCExpressionParameters parameters;
        readonly ValueType? filter;
        readonly bool required;

        readonly Button pickButton;
        readonly Button addButton;
        readonly VisualElement warning;
        readonly IntegerField intField;
        readonly FloatField floatField;
        readonly TextField boolField;
        readonly Button setOneButton;

        public ControlParameterField(string label, SerializedProperty parameter, VRCExpressionParameters parameters,
            SerializedObject parametersObject, ValueType? filter, bool required, SerializedProperty value = null)
        {
            nameProperty = parameter.FindPropertyRelative(ControlTypes.ParameterNameField);
            valueProperty = value;
            this.parameters = parameters;
            this.filter = filter;
            this.required = required;

            AddToClassList("vsp-row");
            AddToClassList("vsp-parameter-field");
            if (!string.IsNullOrEmpty(label)) Add(Ui.FieldLabel(label));

            Add(Ui.TextField(nameProperty, "Parameter"));
            Add(pickButton = Ui.DropdownButton("Pick a parameter of the active avatar", OpenPicker));
            addButton = new Button(OpenAddMenu) { text = "Add", tooltip = "Add this parameter to the active avatar's expression parameters" };
            addButton.AddToClassList("vsp-small-button");
            Add(addButton);
            Add(warning = Ui.IconImage(Icons.Warn));

            if (valueProperty != null)
            {
                intField = ValueField(new IntegerField { isDelayed = true, tooltip = "Value (0 to 255)" });
                floatField = ValueField(new FloatField { isDelayed = true, tooltip = "Value" });
                boolField = ValueField(new TextField { tooltip = "A button or toggle sets a bool parameter to true with a value of 1" });
                boolField.SetEnabled(false);
                setOneButton = new Button(() => WriteValue(1)) { text = "Set 1", tooltip = "A bool parameter needs a value of 1 to turn on" };
                setOneButton.AddToClassList("vsp-small-button");
                Add(setOneButton);
                intField.RegisterValueChangedCallback(evt => WriteValue(evt.newValue));
                floatField.RegisterValueChangedCallback(evt => WriteValue(evt.newValue));
                Ui.Track(this, valueProperty, Refresh);
            }

            Ui.Track(this, nameProperty, Refresh);
            if (parametersObject != null) Ui.Track(this, parametersObject, Refresh);
            Refresh();
        }

        T ValueField<T>(T field) where T : VisualElement
        {
            field.AddToClassList("vsp-value-field");
            Add(field);
            return field;
        }

        void Refresh()
        {
            string name = nameProperty.stringValue;
            bool isEmpty = string.IsNullOrEmpty(name);
            var found = ExpressionParameterUtility.Find(parameters, name);
            bool isMissing = parameters && !isEmpty && found == null;
            bool isWrongType = found != null && filter.HasValue && found.valueType != filter.Value;
            var match = isWrongType ? null : found;

            string message =
                !parameters ? Messages.MissingParameters :
                isMissing ? $"Parameter ({name}) not found on the active avatar descriptor" :
                isWrongType ? $"Parameter ({name}) is a {ExpressionParameterUtility.TypeLabel(found.valueType)}; this needs a {ExpressionParameterUtility.TypeLabel(filter.Value)}" :
                required && isEmpty ? "Parameter is blank. Control may be dysfunctional." :
                null;

            warning.SetVisible(message != null);
            warning.tooltip = message;
            addButton.SetVisible(isMissing);
            pickButton.SetEnabled(parameters && parameters.parameters != null && parameters.parameters.Length > 0);

            if (valueProperty != null) RefreshValue(match?.valueType);
        }

        void RefreshValue(ValueType? type)
        {
            float value = valueProperty.floatValue;
            intField.SetVisible(type == ValueType.Int);
            floatField.SetVisible(type != ValueType.Int && type != ValueType.Bool);
            boolField.SetVisible(type == ValueType.Bool);
            setOneButton.SetVisible(type == ValueType.Bool && value != 1);

            intField.SetValueWithoutNotify(Mathf.RoundToInt(value));
            floatField.SetValueWithoutNotify(value);
            boolField.SetValueWithoutNotify(value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        void WriteValue(float value)
        {
            var found = ExpressionParameterUtility.Find(parameters, nameProperty.stringValue);
            valueProperty.floatValue = found?.valueType switch
            {
                ValueType.Int => Mathf.Clamp(Mathf.Round(value), 0, 255),
                ValueType.Float => Mathf.Clamp(value, -1, 1),
                ValueType.Bool => 1,
                _ => Mathf.Clamp(value, -1, 255),
            };
            valueProperty.serializedObject.ApplyModifiedProperties();
        }

        void OpenPicker(Button button)
        {
            if (!parameters || parameters.parameters == null) return;

            string current = nameProperty.stringValue;
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("[None]"), string.IsNullOrEmpty(current), () => Pick(null));
            menu.AddSeparator(string.Empty);
            foreach (var parameter in parameters.parameters)
            {
                if (parameter == null || string.IsNullOrEmpty(parameter.name)) continue;
                if (filter.HasValue && parameter.valueType != filter.Value) continue;
                var picked = parameter;
                menu.AddItem(new GUIContent($"{picked.name} [{ExpressionParameterUtility.TypeLabel(picked.valueType)}]"), picked.name == current, () => Pick(picked));
            }

            menu.DropDown(button.worldBound);
        }

        void Pick(VRCExpressionParameters.Parameter parameter)
        {
            nameProperty.stringValue = parameter?.name ?? string.Empty;
            if (valueProperty != null && parameter?.valueType == ValueType.Bool) valueProperty.floatValue = 1;
            nameProperty.serializedObject.ApplyModifiedProperties();
        }

        void OpenAddMenu()
        {
            string name = nameProperty.stringValue;
            if (!parameters || string.IsNullOrEmpty(name)) return;

            if (filter.HasValue)
            {
                ExpressionParameterUtility.Add(parameters, name, filter.Value);
                return;
            }

            var menu = new GenericMenu();
            foreach (ValueType type in Enum.GetValues(typeof(ValueType)))
            {
                var captured = type;
                menu.AddItem(new GUIContent(type.ToString()), false, () => ExpressionParameterUtility.Add(parameters, name, captured));
            }

            menu.DropDown(addButton.worldBound);
        }
    }
}
