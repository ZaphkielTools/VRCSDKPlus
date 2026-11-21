using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Zaphkiel.VRCSDKPlus.Tests
{
    sealed class TextUtilityTests
    {
        static bool Free(string candidate, params string[] taken) => System.Array.IndexOf(taken, candidate) < 0;

        [Test]
        public void MakeUnique_ReturnsTheNameWhenFree() =>
            Assert.AreEqual("Hair", TextUtility.MakeUnique("Hair", c => Free(c)));

        [Test]
        public void MakeUnique_CountsUpTheTrailingNumber() =>
            Assert.AreEqual("Hair 4", TextUtility.MakeUnique("Hair 2", c => Free(c, "Hair 2", "Hair 3")));

        [Test]
        public void MakeUnique_KeepsLeadingZeros() =>
            Assert.AreEqual("Item 02", TextUtility.MakeUnique("Item 01", c => Free(c, "Item 01")));

        [Test]
        public void MakeUnique_AppendsANumberWhenThereIsNone() =>
            Assert.AreEqual("Hair 1", TextUtility.MakeUnique("Hair", c => Free(c, "Hair")));

        [Test]
        public void MakeUnique_CanLeaveNamesWithoutNumberAlone() =>
            Assert.AreEqual("Hair", TextUtility.MakeUnique("Hair", c => Free(c, "Hair"), false));

        [Test]
        public void StripRichText_RemovesEveryTag() =>
            Assert.AreEqual("Hat", TextUtility.StripRichText("<b><color=#FF0000>Hat</color></b>"));

        [TestCase("Hat/Cap: Red", "Hat-Cap- Red")]
        [TestCase("  Hat  ", "Hat")]
        [TestCase("   ", "Unnamed")]
        [TestCase(null, "Unnamed")]
        public void ToFileName_ReplacesInvalidCharacters(string text, string expected) =>
            Assert.AreEqual(expected, TextUtility.ToFileName(text));
    }

    sealed class RichTextStyleTests
    {
        [Test]
        public void Set_AddsAndRemovesATag()
        {
            string bold = RichTextStyle.Set("Hat", 'b', true);
            Assert.AreEqual("<b>Hat</b>", bold);
            Assert.IsTrue(RichTextStyle.Has(bold, 'b'));
            Assert.AreEqual(bold, RichTextStyle.Set(bold, 'b', true), "setting a present tag again changes nothing");
            Assert.AreEqual("Hat", RichTextStyle.Set(bold, 'b', false));
        }

        [Test]
        public void SetColor_ReplacesTheCurrentColor()
        {
            string red = RichTextStyle.SetColor("<i>Hat</i>", Color.red);
            Assert.AreEqual("<color=#FF0000><i>Hat</i></color>", red);
            Assert.AreEqual("<color=#0000FF><i>Hat</i></color>", RichTextStyle.SetColor(red, Color.blue));
        }

        [Test]
        public void GetColor_ReadsTheColorTag_OrDefaultsToWhite()
        {
            Assert.AreEqual(Color.red, RichTextStyle.GetColor("<color=#FF0000>Hat</color>"));
            Assert.AreEqual(Color.white, RichTextStyle.GetColor("Hat"));
            Assert.AreEqual(Color.white, RichTextStyle.GetColor("<color=#FF0000>Hat"), "an unclosed tag is not a color");
        }
    }

    sealed class ParameterValidationTests
    {
        static readonly VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.Parameter[] Parameters =
        {
            Make("A"), Make(""), Make("A"), Make("B"),
        };

        static readonly Dictionary<string, AnimatorControllerParameter> Controller =
            new Dictionary<string, AnimatorControllerParameter> { ["A"] = new AnimatorControllerParameter { name = "A" } };

        static VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.Parameter Make(string name) =>
            new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.Parameter { name = name };

        [Test]
        public void Evaluate_FlagsBlankDuplicateAndMissingParameters()
        {
            var statuses = ParameterValidation.Evaluate(Parameters, Controller, true);
            Assert.IsTrue(statuses[0].IsDuplicate);
            Assert.AreEqual("A", statuses[0].Match?.name);
            Assert.IsTrue(statuses[1].IsBlank);
            Assert.IsTrue(statuses[2].IsDuplicate);
            Assert.IsTrue(statuses[3].IsMissing);
        }

        [Test]
        public void Evaluate_WithoutAvatar_ReportsNothingMissing() =>
            Assert.IsFalse(ParameterValidation.Evaluate(Parameters, Controller, false)[3].IsMissing);

        [Test]
        public void PlanCleanup_KeepsTheFirstOfEachValidName()
        {
            CollectionAssert.AreEqual(new[] { false, true, true, true }, ParameterValidation.PlanCleanup(Parameters, Controller, true));
            CollectionAssert.AreEqual(new[] { false, true, true, false }, ParameterValidation.PlanCleanup(Parameters, Controller, false));
        }

        [Test]
        public void NullParameters_GiveEmptyResults()
        {
            Assert.IsEmpty(ParameterValidation.Evaluate(null, Controller, true));
            Assert.IsEmpty(ParameterValidation.PlanCleanup(null, Controller, true));
        }
    }
}
