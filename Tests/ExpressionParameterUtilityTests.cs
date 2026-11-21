using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using VRC.SDK3.Avatars.ScriptableObjects;
using Parameter = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.Parameter;
using ValueType = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType;

namespace Zaphkiel.VRCSDKPlus.Tests
{
    sealed class ExpressionParameterUtilityTests
    {
        VRCExpressionParameters asset;
        SerializedObject serialized;

        [SetUp]
        public void SetUp() => asset = ScriptableObject.CreateInstance<VRCExpressionParameters>();

        [TearDown]
        public void TearDown()
        {
            serialized?.Dispose();
            serialized = null;
            Object.DestroyImmediate(asset);
        }

        SerializedProperty SingleParameter(ValueType type, int bitWidth, bool flipped)
        {
            asset.parameters = new[] { new Parameter { name = "P", valueType = type } };
            serialized = new SerializedObject(asset);
            var parameter = serialized.FindProperty(nameof(VRCExpressionParameters.parameters)).GetArrayElementAtIndex(0);
            parameter.FindPropertyRelative(nameof(Parameter.bitWidth)).intValue = bitWidth;
            parameter.FindPropertyRelative(nameof(Parameter.flippedRepresentation)).boolValue = flipped;
            return parameter;
        }

        [TestCase(ValueType.Bool, 1, false, 0.3f, 1f)]
        [TestCase(ValueType.Bool, 1, false, 0f, 0f)]
        [TestCase(ValueType.Float, 8, false, -2f, -1f)]
        [TestCase(ValueType.Float, 8, false, 2f, 1f)]
        [TestCase(ValueType.Float, 8, true, -0.5f, 0f)]
        [TestCase(ValueType.Int, 8, false, 300f, 255f)]
        [TestCase(ValueType.Int, 8, false, -3f, 0f)]
        [TestCase(ValueType.Int, 8, false, 2.6f, 3f)]
        [TestCase(ValueType.Int, 4, false, 20f, 15f)]
        [TestCase(ValueType.Int, 8, true, -200f, -128f)]
        [TestCase(ValueType.Int, 8, true, 200f, 127f)]
        public void ClampDefault_FitsTheTypeAndEncoding(ValueType type, int bitWidth, bool flipped, float value, float expected) =>
            Assert.AreEqual(expected, ExpressionParameterUtility.ClampDefault(SingleParameter(type, bitWidth, flipped), value));

        [Test]
        public void Add_AppendsAFreshSdkParameter()
        {
            asset.parameters = new Parameter[0];
            asset.isEmpty = true;

            Assert.IsTrue(ExpressionParameterUtility.Add(asset, "Hat", ValueType.Int));

            var added = ExpressionParameterUtility.Find(asset, "Hat");
            Assert.IsNotNull(added);
            Assert.AreEqual(ValueType.Int, added.valueType);
            Assert.AreEqual(0f, added.defaultValue);
            Assert.IsTrue(added.saved);
            Assert.IsTrue(added.networkSynced);
            Assert.AreEqual(8, added.bitWidth);
            Assert.IsFalse(asset.isEmpty, "isEmpty follows the list, or the SDK editor refills it");
        }

        [Test]
        public void Add_GivesBoolsOneBit()
        {
            asset.parameters = new Parameter[0];
            ExpressionParameterUtility.Add(asset, "Hat", ValueType.Bool);
            Assert.AreEqual(1, ExpressionParameterUtility.Find(asset, "Hat").bitWidth);
        }

        [Test]
        public void Add_RefusesAnExistingName_WhateverItsType()
        {
            asset.parameters = new[] { new Parameter { name = "Hat", valueType = ValueType.Bool } };
            LogAssert.Expect(LogType.Warning, new Regex("already has a bool parameter named Hat"));

            Assert.IsFalse(ExpressionParameterUtility.Add(asset, "Hat", ValueType.Int));
            Assert.AreEqual(1, asset.parameters.Length);
        }

        [Test]
        public void Clone_CarriesTheSdkEncodingFields()
        {
            var original = new Parameter
            {
                name = "Hat",
                valueType = ValueType.Int,
                bitWidth = 4,
                flippedRepresentation = true,
                useCustomLerpDuration = true,
                customLerpDuration = 0.5f,
            };

            var clone = ExpressionParameterUtility.Clone(original);

            Assert.AreNotSame(original, clone);
            Assert.AreEqual(original.name, clone.name);
            Assert.AreEqual(original.bitWidth, clone.bitWidth);
            Assert.AreEqual(original.flippedRepresentation, clone.flippedRepresentation);
            Assert.AreEqual(original.useCustomLerpDuration, clone.useCustomLerpDuration);
            Assert.AreEqual(original.customLerpDuration, clone.customLerpDuration);
        }
    }
}
