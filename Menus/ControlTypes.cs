using System;
using UnityEditor;
using VRC.SDK3.Avatars.ScriptableObjects;
using Control = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control;
using ControlType = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType;

namespace Zaphkiel.VRCSDKPlus
{
    internal static class ControlTypes
    {
        public const string TypeField = nameof(Control.type);
        public const string NameField = nameof(Control.name);
        public const string IconField = nameof(Control.icon);
        public const string ParameterField = nameof(Control.parameter);
        public const string ValueField = nameof(Control.value);
        public const string SubMenuField = nameof(Control.subMenu);
        public const string SubParametersField = nameof(Control.subParameters);
        public const string LabelsField = nameof(Control.labels);
        public const string ParameterNameField = nameof(Control.Parameter.name);
        public const string LabelNameField = nameof(Control.Label.name);
        public const string LabelIconField = nameof(Control.Label.icon);

        public const int AxisLabelCount = 4;

        public static ControlType Get(SerializedProperty control)
        {
            int value = control.FindPropertyRelative(TypeField).intValue;
            return Enum.IsDefined(typeof(ControlType), value) ? (ControlType)value : ControlType.Button;
        }

        public static void Set(SerializedProperty control, ControlType type) =>
            control.FindPropertyRelative(TypeField).intValue = (int)type;

        public static bool IsSimple(ControlType type) => type == ControlType.Button || type == ControlType.Toggle;

        public static bool IsPuppet(ControlType type) =>
            type == ControlType.RadialPuppet || type == ControlType.TwoAxisPuppet || type == ControlType.FourAxisPuppet;

        public static bool HasAxisLabels(ControlType type) => type == ControlType.TwoAxisPuppet || type == ControlType.FourAxisPuppet;

        public static int SubParameterCount(ControlType type) => type switch
        {
            ControlType.RadialPuppet => 1,
            ControlType.SubMenu => 1,
            ControlType.TwoAxisPuppet => 2,
            ControlType.FourAxisPuppet => 4,
            _ => 0,
        };

        public static string HelpText(ControlType type) => type switch
        {
            ControlType.Button => "Click or hold to activate. The button remains active for a minimum 0.2s.\nWhile active the (Parameter) is set to (Value).\nWhen inactive the (Parameter) is reset to zero.",
            ControlType.Toggle => "Click to toggle on or off.\nWhen turned on the (Parameter) is set to (Value).\nWhen turned off the (Parameter) is reset to zero.",
            ControlType.SubMenu => "Opens another expression menu.\nWhen opened the (Parameter) is set to (Value).\nWhen closed (Parameter) is reset to zero.",
            ControlType.TwoAxisPuppet => "Puppet menu that maps the joystick to two parameters (-1 to +1).\nWhen opened the (Parameter) is set to (Value).\nWhen closed (Parameter) is reset to zero.",
            ControlType.FourAxisPuppet => "Puppet menu that maps the joystick to four parameters (0 to 1).\nWhen opened the (Parameter) is set to (Value).\nWhen closed (Parameter) is reset to zero.",
            ControlType.RadialPuppet => "Puppet menu that sets a value based on joystick rotation. (0 to 1)\nWhen opened the (Parameter) is set to (Value).\nWhen closed (Parameter) is reset to zero.",
            _ => "Unknown control type.",
        };

        public static void Convert(SerializedProperty control, ControlType from, ControlType to)
        {
            if (IsSimple(from) && !IsSimple(to))
                control.FindPropertyRelative(ParameterField).FindPropertyRelative(ParameterNameField).stringValue = string.Empty;
            else if (!IsSimple(from) && IsSimple(to))
                ResetSubParameters(control, to);

            if (from == ControlType.SubMenu || to == ControlType.SubMenu)
                ResetSubParameters(control, to);

            if (IsPuppet(from) && IsPuppet(to))
                ConvertPuppet(control, to);
            else if (IsPuppet(to))
                ResetSubParameters(control, to);

            Set(control, to);
            Repair(control, to);
        }

        public static bool NeedsRepair(SerializedProperty control, ControlType type) =>
            control.FindPropertyRelative(SubParametersField).arraySize < SubParameterCount(type) ||
            HasAxisLabels(type) && control.FindPropertyRelative(LabelsField).arraySize != AxisLabelCount;

        public static void Repair(SerializedProperty control, ControlType type)
        {
            var subParameters = control.FindPropertyRelative(SubParametersField);
            int needed = SubParameterCount(type);
            if (subParameters.arraySize < needed) subParameters.arraySize = needed;

            if (HasAxisLabels(type))
                control.FindPropertyRelative(LabelsField).arraySize = AxisLabelCount;
        }

        static void ResetSubParameters(SerializedProperty control, ControlType type)
        {
            var subParameters = control.FindPropertyRelative(SubParametersField);
            subParameters.ClearArray();
            int count = SubParameterCount(type);
            for (int i = 0; i < count; i++)
            {
                subParameters.InsertArrayElementAtIndex(i);
                subParameters.GetArrayElementAtIndex(i).FindPropertyRelative(ParameterNameField).stringValue = string.Empty;
            }
        }

        static void ConvertPuppet(SerializedProperty control, ControlType to)
        {
            var subParameters = control.FindPropertyRelative(SubParametersField);
            string first = subParameters.arraySize > 0 ? subParameters.GetArrayElementAtIndex(0).FindPropertyRelative(ParameterNameField).stringValue : string.Empty;
            string second = subParameters.arraySize > 1 ? subParameters.GetArrayElementAtIndex(1).FindPropertyRelative(ParameterNameField).stringValue : string.Empty;

            ResetSubParameters(control, to);
            if (subParameters.arraySize > 0) subParameters.GetArrayElementAtIndex(0).FindPropertyRelative(ParameterNameField).stringValue = first;
            if (subParameters.arraySize > 1) subParameters.GetArrayElementAtIndex(1).FindPropertyRelative(ParameterNameField).stringValue = second;
        }
    }
}
