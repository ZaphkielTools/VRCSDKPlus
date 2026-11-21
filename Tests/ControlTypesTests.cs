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
    sealed class ControlTypesTests
    {
        VRCExpressionsMenu menu;
        SerializedObject serialized;

        [TearDown]
        public void TearDown()
        {
            serialized?.Dispose();
            serialized = null;
            if (menu) Object.DestroyImmediate(menu);
        }

        SerializedProperty CreateControl(ControlType type, params string[] subParameters)
        {
            menu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
            menu.controls = new List<Control>
            {
                new Control
                {
                    name = "Test",
                    type = type,
                    parameter = new Control.Parameter { name = "Main" },
                    subParameters = subParameters.Select(name => new Control.Parameter { name = name }).ToArray(),
                    labels = new Control.Label[0],
                },
            };
            serialized = new SerializedObject(menu);
            return serialized.FindProperty(nameof(VRCExpressionsMenu.controls)).GetArrayElementAtIndex(0);
        }

        Control Applied()
        {
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return menu.controls[0];
        }

        [Test]
        public void Get_ReadsTheEnumValue_NotItsDeclarationIndex()
        {
            var control = CreateControl(ControlType.RadialPuppet, "Rotation");
            Assert.AreEqual(ControlType.RadialPuppet, ControlTypes.Get(control));
            Assert.AreEqual((int)ControlType.RadialPuppet, control.FindPropertyRelative(ControlTypes.TypeField).intValue);
        }

        [Test]
        public void Set_WritesTheEnumValue()
        {
            var control = CreateControl(ControlType.Toggle);
            ControlTypes.Set(control, ControlType.FourAxisPuppet);
            Assert.AreEqual(ControlType.FourAxisPuppet, Applied().type);
        }

        [Test]
        public void Get_FallsBackToButtonForUnknownValues()
        {
            var control = CreateControl(ControlType.Toggle);
            control.FindPropertyRelative(ControlTypes.TypeField).intValue = 0;
            Assert.AreEqual(ControlType.Button, ControlTypes.Get(control));
        }

        [Test]
        public void Convert_PuppetToPuppet_KeepsTheFirstTwoAxes()
        {
            var control = CreateControl(ControlType.TwoAxisPuppet, "Horizontal", "Vertical");
            ControlTypes.Convert(control, ControlType.TwoAxisPuppet, ControlType.FourAxisPuppet);

            var result = Applied();
            Assert.AreEqual(ControlType.FourAxisPuppet, result.type);
            CollectionAssert.AreEqual(new[] { "Horizontal", "Vertical", "", "" }, result.subParameters.Select(p => p.name));
            Assert.AreEqual(ControlTypes.AxisLabelCount, result.labels.Length);
        }

        [TestCase(ControlType.RadialPuppet, 1)]
        [TestCase(ControlType.SubMenu, 1)]
        [TestCase(ControlType.TwoAxisPuppet, 2)]
        [TestCase(ControlType.FourAxisPuppet, 4)]
        public void Convert_FromToggle_GivesTheSubParametersTheTypeReads(ControlType to, int count)
        {
            var control = CreateControl(ControlType.Toggle);
            ControlTypes.Convert(control, ControlType.Toggle, to);
            Assert.AreEqual(count, Applied().subParameters.Length);
            Assert.IsFalse(ControlTypes.NeedsRepair(control, to));
        }

        [Test]
        public void NeedsRepair_DetectsMissingAxes_AndRepairKeepsWhatIsThere()
        {
            var control = CreateControl(ControlType.FourAxisPuppet, "Up");
            Assert.IsTrue(ControlTypes.NeedsRepair(control, ControlType.FourAxisPuppet));

            ControlTypes.Repair(control, ControlType.FourAxisPuppet);

            Assert.IsFalse(ControlTypes.NeedsRepair(control, ControlType.FourAxisPuppet));
            var result = Applied();
            Assert.AreEqual(4, result.subParameters.Length);
            Assert.AreEqual("Up", result.subParameters[0].name);
            Assert.AreEqual(ControlTypes.AxisLabelCount, result.labels.Length);
        }

        [Test]
        public void NeedsRepair_IsFalseForSimpleControls() =>
            Assert.IsFalse(ControlTypes.NeedsRepair(CreateControl(ControlType.Toggle), ControlType.Toggle));
    }
}
