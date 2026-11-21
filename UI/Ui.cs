using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Zaphkiel.VRCSDKPlus
{
    internal static class Icons
    {
        public const string Copy = "SaveActive";
        public const string Paste = "Clipboard";
        public const string Move = "MoveTool";
        public const string Place = "DefaultSorting";
        public const string Duplicate = "TreeEditor.Duplicate";
        public const string Help = "_Help";
        public const string Warn = "console.warnicon.sml";
        public const string Error = "console.erroricon.sml";
        public const string Clear = "CrossIcon";
        public const string Folder = "FolderOpened Icon";
        public const string Remove = "Toolbar Minus";
        public const string Refresh = "Refresh";
    }

    internal static class Ui
    {
        const string AssemblyName = "Zaphkiel.Editor.VRCSDKPlus";
        const string StyleSheetPath = "UI/VRCSDKPlus.uss";
        static StyleSheet styleSheet;
        static readonly Dictionary<string, Texture2D> IconCache = new Dictionary<string, Texture2D>();

        public static VisualElement CreateRoot(string className)
        {
            var root = new VisualElement();
            var sheet = LoadStyleSheet();
            if (sheet) root.styleSheets.Add(sheet);
            root.AddToClassList("vsp");
            root.AddToClassList(className);
            return root;
        }

        static StyleSheet LoadStyleSheet()
        {
            if (styleSheet) return styleSheet;
            string asmdef = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(AssemblyName);
            if (string.IsNullOrEmpty(asmdef)) return null;
            string folder = Path.GetDirectoryName(asmdef)?.Replace('\\', '/');
            styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>($"{folder}/{StyleSheetPath}");
            return styleSheet;
        }

        public static Texture2D Icon(string name)
        {
            if (IconCache.TryGetValue(name, out var texture)) return texture;
            texture = EditorGUIUtility.IconContent(name)?.image as Texture2D;
            IconCache[name] = texture;
            return texture;
        }

        public static VisualElement IconImage(string icon, string tooltip = null)
        {
            var image = new VisualElement { tooltip = tooltip, pickingMode = PickingMode.Position };
            image.AddToClassList("vsp-icon");
            SetIcon(image, icon);
            return image;
        }

        public static Button IconButton(string icon, string tooltip, Action<EventBase> onClick)
        {
            var button = new Button { tooltip = tooltip };
            button.clickable.clickedWithEventInfo += onClick;
            button.AddToClassList("vsp-icon-button");
            SetIcon(button, icon);
            return button;
        }

        public static Button IconButton(string icon, string tooltip, Action onClick) => IconButton(icon, tooltip, _ => onClick());

        public static void SetIcon(VisualElement element, string icon) => element.style.backgroundImage = new StyleBackground(Icon(icon));

        public static Button DropdownButton(string tooltip, Action<Button> onClick)
        {
            var button = new Button { text = "▾", tooltip = tooltip };
            button.clicked += () => onClick(button);
            button.AddToClassList("vsp-dropdown-button");
            return button;
        }

        public static VisualElement Row(string className = null)
        {
            var row = new VisualElement();
            row.AddToClassList("vsp-row");
            if (className != null) row.AddToClassList(className);
            return row;
        }

        public static VisualElement Box(string title = null)
        {
            var box = new VisualElement();
            box.AddToClassList("vsp-box");
            if (!string.IsNullOrEmpty(title))
            {
                var label = new Label(title);
                label.AddToClassList("vsp-box-title");
                box.Add(label);
            }

            return box;
        }

        public static Label FieldLabel(string text)
        {
            var label = new Label(text);
            label.AddToClassList("vsp-field-label");
            return label;
        }

        public static VisualElement Placeholder(float height)
        {
            var placeholder = new VisualElement();
            placeholder.AddToClassList("vsp-placeholder");
            placeholder.style.height = height;
            return placeholder;
        }

        public static TextField TextField(SerializedProperty property, string placeholder, bool delayed = false)
        {
            var field = new TextField { isDelayed = delayed };
            field.textEdition.placeholder = placeholder;
            field.AddToClassList("vsp-grow");
            if (property != null) field.BindProperty(property);
            return field;
        }

        public static Label Link(string text, string url)
        {
            var link = new Label(text) { tooltip = url };
            link.AddToClassList("vsp-link");
            link.AddManipulator(new Clickable(() => Application.OpenURL(url)));
            return link;
        }

        public static VisualElement ForkCredits()
        {
            var row = Credits("Unity 6 fork by", ("@Zaphkiel", "https://github.com/ZaphkielTools"));
            row.AddToClassList("vsp-footer--fork");
            return row;
        }

        public static VisualElement Credits(string prefix, params (string Text, string Url)[] links)
        {
            var row = Row("vsp-footer");
            if (!string.IsNullOrEmpty(prefix))
            {
                var label = new Label(prefix);
                label.AddToClassList("vsp-footer-text");
                row.Add(label);
            }

            for (int i = 0; i < links.Length; i++)
            {
                if (i > 0)
                {
                    var separator = new Label("&");
                    separator.AddToClassList("vsp-footer-text");
                    row.Add(separator);
                }

                row.Add(Link(links[i].Text, links[i].Url));
            }

            return row;
        }

        public static void Track(VisualElement parent, SerializedProperty property, Action onChange)
        {
            var host = new VisualElement { name = "vsp-tracker" };
            host.style.display = DisplayStyle.None;
            parent.Add(host);
            host.TrackPropertyValue(property, _ => onChange());
        }

        public static void Track(VisualElement parent, SerializedObject serializedObject, Action onChange)
        {
            var host = new VisualElement { name = "vsp-tracker" };
            host.style.display = DisplayStyle.None;
            parent.Add(host);
            host.TrackSerializedObjectValue(serializedObject, _ => onChange());
        }

        public static void SetVisible(this VisualElement element, bool visible) =>
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        public static bool IsEditingText(VisualElement anyElement)
        {
            var focused = anyElement.focusController?.focusedElement as VisualElement;
            for (var element = focused; element != null; element = element.parent)
                if (element.ClassListContains("unity-base-text-field"))
                    return true;
            return false;
        }

        public static bool IsShiftHeld(EventBase evt) => evt switch
        {
            IMouseEvent mouse => mouse.shiftKey,
            IKeyboardEvent keyboard => keyboard.shiftKey,
            _ => evt?.imguiEvent != null && evt.imguiEvent.shift,
        };
    }
}
