using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;
using ValueType = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType;

namespace Zaphkiel.VRCSDKPlus
{
    internal static class ExpressionParameterUtility
    {
        public const string ParametersField = nameof(VRCExpressionParameters.parameters);
        public const string IsEmptyField = nameof(VRCExpressionParameters.isEmpty);
        public const string NameField = nameof(VRCExpressionParameters.Parameter.name);
        public const string ValueTypeField = nameof(VRCExpressionParameters.Parameter.valueType);
        public const string DefaultValueField = nameof(VRCExpressionParameters.Parameter.defaultValue);
        public const string SavedField = nameof(VRCExpressionParameters.Parameter.saved);
        public const string SyncedField = nameof(VRCExpressionParameters.Parameter.networkSynced);
        const string BitWidthField = nameof(VRCExpressionParameters.Parameter.bitWidth);
        const string FlippedField = nameof(VRCExpressionParameters.Parameter.flippedRepresentation);
        const string UseLerpField = nameof(VRCExpressionParameters.Parameter.useCustomLerpDuration);
        const string LerpField = nameof(VRCExpressionParameters.Parameter.customLerpDuration);

        public static VRCExpressionParameters.Parameter Clone(VRCExpressionParameters.Parameter parameter) =>
            JsonUtility.FromJson<VRCExpressionParameters.Parameter>(JsonUtility.ToJson(parameter));

        public static VRCExpressionParameters.Parameter Find(VRCExpressionParameters asset, string name)
        {
            if (!asset || asset.parameters == null || string.IsNullOrEmpty(name)) return null;
            foreach (var parameter in asset.parameters)
                if (parameter != null && parameter.name == name)
                    return parameter;
            return null;
        }

        public static string TypeLabel(ValueType type) => type switch
        {
            ValueType.Int => "int",
            ValueType.Float => "float",
            _ => "bool",
        };

        public static AnimatorControllerParameterType ToControllerType(ValueType type) => type switch
        {
            ValueType.Int => AnimatorControllerParameterType.Int,
            ValueType.Float => AnimatorControllerParameterType.Float,
            _ => AnimatorControllerParameterType.Bool,
        };

        public static ValueType GetValueType(SerializedProperty parameter) =>
            (ValueType)parameter.FindPropertyRelative(ValueTypeField).intValue;

        public static void SetValueType(SerializedProperty parameter, ValueType type) =>
            parameter.FindPropertyRelative(ValueTypeField).intValue = (int)type;

        public static void ResetEncoding(SerializedProperty parameter, ValueType type)
        {
            SetInt(parameter, BitWidthField, type == ValueType.Bool ? 1 : 8);
            SetBool(parameter, FlippedField, false);
        }

        public static float ClampDefault(SerializedProperty parameter, float value)
        {
            var type = GetValueType(parameter);
            bool flipped = parameter.FindPropertyRelative(FlippedField)?.boolValue ?? false;

            switch (type)
            {
                case ValueType.Bool:
                    return value != 0 ? 1 : 0;
                case ValueType.Float:
                    bool signedFloat = !flipped;
                    return Mathf.Clamp(value, signedFloat ? -1f : 0f, 1f);
                default:
                    int bits = Mathf.Clamp(parameter.FindPropertyRelative(BitWidthField)?.intValue ?? 8,
                        VRCExpressionParameters.Parameter.MinBitWidth, VRCExpressionParameters.Parameter.MaxBitWidth);
                    bool signedInt = flipped;
                    int min = signedInt ? -(1 << (bits - 1)) : 0;
                    int max = signedInt ? (1 << (bits - 1)) - 1 : (1 << bits) - 1;
                    return Mathf.Clamp(Mathf.Round(value), min, max);
            }
        }

        public static void Initialize(SerializedProperty parameter, string name, ValueType type)
        {
            parameter.FindPropertyRelative(NameField).stringValue = name;
            SetValueType(parameter, type);
            parameter.FindPropertyRelative(DefaultValueField).floatValue = 0;
            parameter.FindPropertyRelative(SavedField).boolValue = true;
            parameter.FindPropertyRelative(SyncedField).boolValue = true;
            ResetEncoding(parameter, type);
            SetBool(parameter, UseLerpField, false);
            SetFloat(parameter, LerpField, 0);
        }

        public static bool Add(VRCExpressionParameters asset, string name, ValueType type)
        {
            if (!asset || string.IsNullOrEmpty(name)) return false;
            var existing = Find(asset, name);
            if (existing != null)
            {
                Log.Warning($"{asset.name} already has a {TypeLabel(existing.valueType)} parameter named {name}; nothing added.");
                return false;
            }

            using var serialized = new SerializedObject(asset);
            var parameters = serialized.FindProperty(ParametersField);
            int index = parameters.arraySize;
            parameters.arraySize++;
            Initialize(parameters.GetArrayElementAtIndex(index), name, type);
            UpdateIsEmpty(serialized);
            serialized.ApplyModifiedProperties();
            Log.Info($"Added {TypeLabel(type)} parameter {name} to {asset.name}.");
            return true;
        }

        public static void UpdateIsEmpty(SerializedObject serialized)
        {
            var isEmpty = serialized.FindProperty(IsEmptyField);
            if (isEmpty != null) isEmpty.boolValue = serialized.FindProperty(ParametersField).arraySize == 0;
        }

        static void SetInt(SerializedProperty parent, string field, int value)
        {
            var property = parent.FindPropertyRelative(field);
            if (property != null) property.intValue = value;
        }

        static void SetBool(SerializedProperty parent, string field, bool value)
        {
            var property = parent.FindPropertyRelative(field);
            if (property != null) property.boolValue = value;
        }

        static void SetFloat(SerializedProperty parent, string field, float value)
        {
            var property = parent.FindPropertyRelative(field);
            if (property != null) property.floatValue = value;
        }
    }
}
