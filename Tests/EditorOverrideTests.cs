using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;
using Object = UnityEngine.Object;

namespace Zaphkiel.VRCSDKPlus.Tests
{
    sealed class EditorOverrideTests
    {
        static Type CreatedEditorType(Type inspected)
        {
            var target = ScriptableObject.CreateInstance(inspected);
            var editor = Editor.CreateEditor(target);
            try
            {
                return editor ? editor.GetType() : null;
            }
            finally
            {
                if (editor) Object.DestroyImmediate(editor);
                Object.DestroyImmediate(target);
            }
        }

        [TestCase(typeof(VRCExpressionsMenu), typeof(MenuEditor))]
        [TestCase(typeof(VRCExpressionParameters), typeof(ParametersEditor))]
        public void OurInspectorIsTheOneUnityCreates(Type inspected, Type ours)
        {
            if (!EditorOverride.IsEnabled(ours)) Assert.Ignore($"{ours.Name} is turned off with [SDK+] Toggle Editor.");
            Assert.AreEqual(ours, CreatedEditorType(inspected));
        }

        [TestCase(typeof(VRCExpressionsMenu), typeof(MenuEditor))]
        [TestCase(typeof(VRCExpressionParameters), typeof(ParametersEditor))]
        public void ToggleEditor_SwapsToTheSdkInspectorAndBack(Type inspected, Type ours)
        {
            var sdk = EditorOverride.FindOtherEditor(inspected, ours);
            Assert.IsNotNull(sdk, "the SDK's own inspector was not found");
            bool wasEnabled = EditorOverride.IsEnabled(ours);
            try
            {
                Assert.IsTrue(EditorOverride.Swap(inspected, sdk));
                Assert.AreEqual(sdk, CreatedEditorType(inspected));
                Assert.IsTrue(EditorOverride.Swap(inspected, ours));
                Assert.AreEqual(ours, CreatedEditorType(inspected));
            }
            finally
            {
                EditorOverride.Swap(inspected, wasEnabled ? ours : sdk);
            }
        }
    }
}
