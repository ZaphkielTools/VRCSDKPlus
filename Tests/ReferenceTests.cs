using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.TestTools;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using Control = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control;
using ControlType = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType;

namespace Zaphkiel.VRCSDKPlus.Tests
{
    sealed class AvatarContextTests
    {
        [Test]
        public void GetControllerParameters_ReadsThroughOverrideControllers()
        {
            var controller = new AnimatorController();
            controller.AddParameter("Hat", AnimatorControllerParameterType.Bool);
            var overrideController = new AnimatorOverrideController(controller);
            var avatar = new GameObject("VRCSDKPlus Test Avatar");
            try
            {
                var descriptor = avatar.AddComponent<VRCAvatarDescriptor>();
                descriptor.baseAnimationLayers = new[]
                {
                    new VRCAvatarDescriptor.CustomAnimLayer { type = VRCAvatarDescriptor.AnimLayerType.FX, animatorController = overrideController },
                };

                var parameters = AvatarContext.GetControllerParameters(descriptor);

                Assert.IsTrue(parameters.ContainsKey("Hat"));
                Assert.AreEqual(AnimatorControllerParameterType.Bool, parameters["Hat"].type);
            }
            finally
            {
                Object.DestroyImmediate(avatar);
                Object.DestroyImmediate(overrideController);
                Object.DestroyImmediate(controller);
            }
        }

        [Test]
        public void GetControllerParameters_AcceptsADescriptorWithoutLayers()
        {
            var avatar = new GameObject("VRCSDKPlus Test Avatar");
            try
            {
                var descriptor = avatar.AddComponent<VRCAvatarDescriptor>();
                descriptor.baseAnimationLayers = null;
                descriptor.specialAnimationLayers = null;
                Assert.IsEmpty(AvatarContext.GetControllerParameters(descriptor));
            }
            finally
            {
                Object.DestroyImmediate(avatar);
            }
        }
    }

    sealed class MenuReferencesTests
    {
        const string Folder = "Assets/__VRCSDKPlusTests";

        [SetUp]
        public void SetUp()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "__VRCSDKPlusTests");
        }

        [TearDown]
        public void TearDown() => AssetDatabase.DeleteAsset(Folder);

        static VRCExpressionsMenu CreateMenu(string name, VRCExpressionsMenu subMenu = null, string controlName = null)
        {
            var menu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
            menu.controls = new List<Control>();
            if (subMenu) menu.controls.Add(SubMenuControl(subMenu, controlName));
            AssetDatabase.CreateAsset(menu, $"{Folder}/{name}.asset");
            return menu;
        }

        static Control SubMenuControl(VRCExpressionsMenu subMenu, string name) => new Control
        {
            name = name,
            type = ControlType.SubMenu,
            subMenu = subMenu,
            parameter = new Control.Parameter { name = string.Empty },
            subParameters = new Control.Parameter[0],
            labels = new Control.Label[0],
        };

        [UnityTest]
        public IEnumerator FindParents_ListsSavedParents_OnceIndexed()
        {
            var child = CreateMenu("Child");
            var parent = CreateMenu("Parent", child, "Open Child");
            AssetDatabase.SaveAssets();

            MenuReferences.Rebuild();
            for (int frame = 0; frame < 3000 && !MenuReferences.IsReady; frame++)
                yield return null;
            Assert.IsTrue(MenuReferences.IsReady, "the index did not finish building");

            var links = MenuReferences.FindParents(child);
            Assert.AreEqual(1, links.Count);
            Assert.AreSame(parent, links[0].Menu);
            CollectionAssert.AreEqual(new[] { "Open Child" }, links[0].ControlNames);
            Assert.IsEmpty(MenuReferences.FindParents(parent));
        }

        [Test]
        public void FindParents_SeesUnsavedLinks()
        {
            var child = CreateMenu("Child");
            var parent = CreateMenu("Parent");
            parent.controls.Add(SubMenuControl(child, "Not Saved Yet"));
            EditorUtility.SetDirty(parent);

            var links = MenuReferences.FindParents(child);

            Assert.AreEqual(1, links.Count);
            Assert.AreSame(parent, links[0].Menu);
        }

        [Test]
        public void FindParents_ReportsAMenuThatOpensItself()
        {
            var menu = CreateMenu("Loop");
            menu.controls.Add(SubMenuControl(menu, "Again"));

            var links = MenuReferences.FindParents(menu);

            Assert.AreEqual(1, links.Count);
            Assert.AreSame(menu, links[0].Menu);
        }
    }
}
