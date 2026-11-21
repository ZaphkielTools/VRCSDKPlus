using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace Zaphkiel.VRCSDKPlus
{
    [InitializeOnLoad]
    internal static class EditorOverride
    {
        const string EnabledPrefPrefix = "Zaphkiel.VRCSDKPlus.Enabled.";
        const BindingFlags AnyMember = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        static EditorOverride()
        {
            EditorApplication.delayCall += ApplyPreferences;
        }

        static void ApplyPreferences()
        {
            if (IsEnabled(typeof(ParametersEditor))) Swap(typeof(VRCExpressionParameters), typeof(ParametersEditor));
            if (IsEnabled(typeof(MenuEditor))) Swap(typeof(VRCExpressionsMenu), typeof(MenuEditor));
        }

        [MenuItem("CONTEXT/VRCExpressionParameters/[SDK+] Toggle Editor", false, 899)]
        static void ToggleParametersEditor() => Toggle(typeof(VRCExpressionParameters), typeof(ParametersEditor));

        [MenuItem("CONTEXT/VRCExpressionsMenu/[SDK+] Toggle Editor", false, 899)]
        static void ToggleMenuEditor() => Toggle(typeof(VRCExpressionsMenu), typeof(MenuEditor));

        internal static bool IsEnabled(Type editor) => EditorPrefs.GetBool(EnabledPrefPrefix + editor.Name, true);

        static void Toggle(Type inspected, Type ours)
        {
            bool enable = !IsEnabled(ours);
            var editor = enable ? ours : FindOtherEditor(inspected, ours);
            if (editor == null)
            {
                Log.Warning($"No other editor for {inspected.Name} was found; keeping the VRCSDK+ one.");
                return;
            }

            EditorPrefs.SetBool(EnabledPrefPrefix + ours.Name, enable);
            Swap(inspected, editor);
        }

        internal static Type FindOtherEditor(Type inspected, Type ours)
        {
            var inspectedField = typeof(CustomEditor).GetField("m_InspectedType", AnyMember);
            if (inspectedField == null) return null;

            foreach (var type in TypeCache.GetTypesWithAttribute<CustomEditor>())
            {
                if (type == ours) continue;
                foreach (var attribute in type.GetCustomAttributes<CustomEditor>(false))
                    if ((Type)inspectedField.GetValue(attribute) == inspected)
                        return type;
            }

            return null;
        }

        internal static bool Swap(Type inspected, Type editor)
        {
            try
            {
                var attributesType = typeof(Editor).Assembly.GetType("UnityEditor.CustomEditorAttributes", true);
                var legacyEditors = attributesType.GetField("kSCustomEditors", BindingFlags.Static | BindingFlags.NonPublic);
                if (legacyEditors != null) SwapLegacy(attributesType, legacyEditors, inspected, editor);
                else SwapUnity6(attributesType, inspected, editor);

                var inspectorWindow = typeof(Editor).Assembly.GetType("UnityEditor.InspectorWindow", true);
                inspectorWindow.GetMethod("RefreshInspectors", BindingFlags.NonPublic | BindingFlags.Static)?.Invoke(null, null);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Log.Error($"Failed to set the {inspected.Name} inspector.");
                return false;
            }
        }

        static void SwapUnity6(Type attributesType, Type inspected, Type editor)
        {
            object instance = attributesType.GetProperty("instance", AnyMember).GetValue(null);
            object cache = attributesType.GetField("m_Cache", AnyMember).GetValue(instance);
            var cacheField = cache.GetType().GetField("m_CustomEditorCache", AnyMember);
            var dictionary = (IDictionary)cacheField.GetValue(cache);

            Type entryType = attributesType.GetNestedType("MonoEditorType", BindingFlags.Public | BindingFlags.NonPublic);
            FieldInfo entryEditor = entryType.GetField("inspectorType", AnyMember);
            Type storageType = cacheField.FieldType.GetGenericArguments()[1];
            FieldInfo editorsField = storageType.GetField("customEditors", AnyMember);
            Type listType = typeof(System.Collections.Generic.List<>).MakeGenericType(entryType);

            object storage;
            if (dictionary.Contains(inspected))
            {
                storage = dictionary[inspected];
            }
            else
            {
                storage = Activator.CreateInstance(storageType);
                editorsField.SetValue(storage, Activator.CreateInstance(listType));
                storageType.GetField("customEditorsMultiEdition", AnyMember).SetValue(storage, Activator.CreateInstance(listType));
                dictionary[inspected] = storage;
            }

            var editors = (IList)editorsField.GetValue(storage);
            object entry = null;
            for (int i = editors.Count - 1; i >= 0; i--)
            {
                if ((Type)entryEditor.GetValue(editors[i]) != editor) continue;
                entry ??= editors[i];
                editors.RemoveAt(i);
            }

            entry ??= Activator.CreateInstance(entryType, AnyMember, null, new object[] { editor, null, false, false }, null);
            editors.Insert(0, entry);
        }

        static void SwapLegacy(Type attributesType, FieldInfo editorsField, Type inspected, Type editor)
        {
            var initialized = attributesType.GetField("s_Initialized", BindingFlags.Static | BindingFlags.NonPublic);
            if (initialized != null && !(bool)initialized.GetValue(null))
            {
                attributesType.GetMethod("Rebuild", BindingFlags.Static | BindingFlags.NonPublic)?.Invoke(null, null);
                initialized.SetValue(null, true);
            }

            var entryType = attributesType.GetNestedType("MonoEditorType", BindingFlags.Public | BindingFlags.NonPublic);
            var inspectorField = entryType.GetField("m_InspectorType", BindingFlags.Public | BindingFlags.Instance);
            var editors = ((IDictionary)editorsField.GetValue(null))[inspected] as IList;
            if (editors == null || editors.Count == 0) return;
            inspectorField.SetValue(editors[0], editor);
        }
    }
}
