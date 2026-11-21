using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Zaphkiel.VRCSDKPlus
{
    internal sealed class StyleField : VisualElement
    {
        readonly SerializedProperty nameProperty;
        readonly ColorField color = new ColorField { showAlpha = false, tooltip = "Text color" };
        readonly Button bold;
        readonly Button italic;

        public StyleField(SerializedProperty name)
        {
            nameProperty = name;
            AddToClassList("vsp-row");
            color.AddToClassList("vsp-grow");
            color.RegisterValueChangedCallback(evt => Write(RichTextStyle.SetColor(nameProperty.stringValue, evt.newValue)));
            bold = LetterToggle("<b>B</b>", "Bold", 'b');
            italic = LetterToggle("<i>I</i>", "Italic", 'i');
            Add(color);
            Add(bold);
            Add(italic);
            Ui.Track(this, nameProperty, Refresh);
            Refresh();
        }

        Button LetterToggle(string text, string tooltip, char tag)
        {
            var button = new Button { text = text, tooltip = tooltip };
            button.clicked += () => Write(RichTextStyle.Set(nameProperty.stringValue, tag, !RichTextStyle.Has(nameProperty.stringValue, tag)));
            button.AddToClassList("vsp-letter-toggle");
            return button;
        }

        void Write(string value)
        {
            nameProperty.stringValue = value;
            nameProperty.serializedObject.ApplyModifiedProperties();
        }

        void Refresh()
        {
            string text = nameProperty.stringValue ?? string.Empty;
            color.SetValueWithoutNotify(RichTextStyle.GetColor(text));
            bold.EnableInClassList("vsp-letter-toggle--on", RichTextStyle.Has(text, 'b'));
            italic.EnableInClassList("vsp-letter-toggle--on", RichTextStyle.Has(text, 'i'));
        }
    }
}
