using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;
using Control = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control;
using ControlType = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType;

namespace Zaphkiel.VRCSDKPlus.Tests
{
    sealed class MenuClipboardTests
    {
        readonly List<Object> created = new List<Object>();
        string savedClipboard;

        [SetUp]
        public void SetUp() => savedClipboard = EditorGUIUtility.systemCopyBuffer;

        [TearDown]
        public void TearDown()
        {
            MenuClipboard.CancelMove();
            EditorGUIUtility.systemCopyBuffer = savedClipboard;
            foreach (var obj in created)
                if (obj) Object.DestroyImmediate(obj);
            created.Clear();
        }

        static Control NewControl(string name) => new Control
        {
            name = name,
            type = ControlType.Toggle,
            parameter = new Control.Parameter { name = string.Empty },
            subParameters = new Control.Parameter[0],
            labels = new Control.Label[0],
        };

        VRCExpressionsMenu Menu(params string[] names)
        {
            var menu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
            menu.controls = names.Select(NewControl).ToList();
            created.Add(menu);
            return menu;
        }

        static string Names(VRCExpressionsMenu menu) => string.Join(",", menu.controls.Select(control => control.name));

        static Control Named(VRCExpressionsMenu menu, string name) => menu.controls.Find(control => control.name == name);

        #region Place

        [TestCase("A", 2, "B,C,A,D")]
        [TestCase("D", 0, "A,D,B,C")]
        [TestCase("B", 1, "A,B,C,D")]
        [TestCase("C", -1, "C,A,B,D")]
        public void Place_InTheSameMenu_GoesRightAfterTheTarget(string cut, int afterIndex, string expected)
        {
            var menu = Menu("A", "B", "C", "D");
            MenuClipboard.BeginMove(menu, Named(menu, cut));

            var result = MenuClipboard.Place(menu, afterIndex, out int placed, out var source);

            Assert.AreEqual(PlaceResult.Placed, result);
            Assert.AreEqual(expected, Names(menu));
            Assert.AreEqual(cut, menu.controls[placed].name);
            Assert.AreSame(menu, source);
            Assert.IsFalse(MenuClipboard.IsMoving);
        }

        [Test]
        public void Place_IntoAnotherMenu_MovesTheControl()
        {
            var source = Menu("A", "B");
            var destination = Menu("X", "Y");
            MenuClipboard.BeginMove(source, Named(source, "A"));

            Assert.AreEqual(PlaceResult.Placed, MenuClipboard.Place(destination, 0, out int placed, out _));
            Assert.AreEqual("X,A,Y", Names(destination));
            Assert.AreEqual(1, placed);
            Assert.AreEqual("B", Names(source));
        }

        [Test]
        public void Place_IntoAFullMenu_IsRefused_AndKeepsTheMovePending()
        {
            var source = Menu("A");
            var destination = Menu("1", "2", "3", "4", "5", "6", "7", "8");
            MenuClipboard.BeginMove(source, Named(source, "A"));

            Assert.AreEqual(PlaceResult.MenuFull, MenuClipboard.Place(destination, 0, out _, out _));
            Assert.AreEqual(VRCExpressionsMenu.MAX_CONTROLS, destination.controls.Count);
            Assert.AreEqual("A", Names(source));
            Assert.IsTrue(MenuClipboard.IsMoving);
        }

        [Test]
        public void Place_WithinAFullMenu_NeedsNoFreeSlot()
        {
            var menu = Menu("1", "2", "3", "4", "5", "6", "7", "8");
            MenuClipboard.BeginMove(menu, Named(menu, "1"));

            Assert.IsTrue(MenuClipboard.IsMovingWithin(menu));
            Assert.AreEqual(PlaceResult.Placed, MenuClipboard.Place(menu, 7, out _, out _));
            Assert.AreEqual("2,3,4,5,6,7,8,1", Names(menu));
        }

        [Test]
        public void Place_AfterTheControlWasRemoved_CancelsTheMove()
        {
            var source = Menu("A", "B");
            MenuClipboard.BeginMove(source, Named(source, "A"));
            source.controls.RemoveAt(0);

            Assert.AreEqual(PlaceResult.ControlGone, MenuClipboard.Place(source, 0, out _, out _));
            Assert.IsFalse(MenuClipboard.IsMoving);
            Assert.AreEqual("B", Names(source));
        }

        [Test]
        public void Place_WithoutACut_DoesNothing()
        {
            var menu = Menu("A");
            Assert.AreEqual(PlaceResult.NothingToPlace, MenuClipboard.Place(menu, 0, out _, out _));
            Assert.AreEqual("A", Names(menu));
        }

        [TestCase(0, 10, true, 3, 3)]
        [TestCase(3, -5, false, 2, 0)]
        public void InsertIndex_StaysInTheList(int from, int afterIndex, bool sameMenu, int count, int expected) =>
            Assert.AreEqual(expected, MenuClipboard.InsertIndex(from, afterIndex, sameMenu, count));

        #endregion

        #region Copy and paste

        [Test]
        public void CopyPaste_KeepsEveryFieldAndAssetReference()
        {
            const string folder = "Assets/__VRCSDKPlusClipboardTests";
            AssetDatabase.CreateFolder("Assets", "__VRCSDKPlusClipboardTests");
            try
            {
                var subMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                AssetDatabase.CreateAsset(subMenu, $"{folder}/SubMenu.asset");
                var icon = new Texture2D(2, 2);
                AssetDatabase.CreateAsset(icon, $"{folder}/Icon.asset");
                CopyPaste(subMenu, icon);
            }
            finally
            {
                AssetDatabase.DeleteAsset(folder);
            }
        }

        static void CopyPaste(VRCExpressionsMenu subMenu, Texture2D icon)
        {
            var control = new Control
            {
                name = "<b>Hat</b>",
                type = ControlType.SubMenu,
                icon = icon,
                value = 3,
                parameter = new Control.Parameter { name = "HatOpen" },
                subMenu = subMenu,
                subParameters = new[] { new Control.Parameter { name = "Unused" } },
                labels = new Control.Label[0],
            };

            MenuClipboard.Copy(control);
            Assert.IsTrue(MenuClipboard.HasControl);
            var pasted = MenuClipboard.Paste();

            Assert.AreNotSame(control, pasted);
            Assert.AreEqual(control.name, pasted.name);
            Assert.AreEqual(control.type, pasted.type);
            Assert.AreEqual(control.value, pasted.value);
            Assert.AreEqual("HatOpen", pasted.parameter.name);
            Assert.AreEqual("Unused", pasted.subParameters.Single().name);
            AssertSameAsset(subMenu, pasted.subMenu);
            AssertSameAsset(icon, pasted.icon);
        }

        static void AssertSameAsset(Object expected, Object actual)
        {
            Assert.IsTrue(actual, $"the {expected.GetType().Name} reference was lost");
            Assert.IsTrue(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(expected, out string expectedGuid, out long expectedId));
            Assert.IsTrue(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(actual, out string actualGuid, out long actualId));
            Assert.AreEqual(expectedGuid, actualGuid);
            Assert.AreEqual(expectedId, actualId);
        }

        [Test]
        public void Paste_ReadsCopiesOfTheOriginalVrcsdkPlus()
        {
            EditorGUIUtility.systemCopyBuffer = "[TAG=VSP_CONTROL]" + JsonUtility.ToJson(NewControl("Old"));
            Assert.IsTrue(MenuClipboard.HasControl);
            Assert.AreEqual("Old", MenuClipboard.Paste()?.name);
        }

        [Test]
        public void Paste_IgnoresOtherText()
        {
            EditorGUIUtility.systemCopyBuffer = "Hello";
            Assert.IsFalse(MenuClipboard.HasControl);
            Assert.IsNull(MenuClipboard.Paste());
        }

        #endregion
    }
}
