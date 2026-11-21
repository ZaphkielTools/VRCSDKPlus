using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Zaphkiel.VRCSDKPlus
{
    internal sealed class IconField : VisualElement
    {
        readonly SerializedProperty property;
        readonly Image preview;

        public IconField(SerializedProperty property, float previewSize)
        {
            this.property = property;
            AddToClassList("vsp-icon-field");

            if (previewSize > 0)
            {
                style.width = previewSize;
                preview = new Image { scaleMode = ScaleMode.ScaleToFit, tooltip = "Drop a texture here" };
                preview.AddToClassList("vsp-icon-preview");
                preview.style.width = previewSize;
                preview.style.height = previewSize;
                preview.RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
                preview.RegisterCallback<DragPerformEvent>(OnDragPerform);
                Add(preview);
            }

            var field = new ObjectField { objectType = typeof(Texture2D), allowSceneObjects = false };
            field.BindProperty(property);
            Add(field);

            if (preview != null)
            {
                Ui.Track(this, property, UpdatePreview);
                UpdatePreview();
            }
        }

        void UpdatePreview() => preview.image = property.objectReferenceValue as Texture2D;

        static Texture2D DraggedTexture()
        {
            foreach (var reference in DragAndDrop.objectReferences)
                if (reference is Texture2D texture)
                    return texture;
            return null;
        }

        static void OnDragUpdated(DragUpdatedEvent evt)
        {
            DragAndDrop.visualMode = DraggedTexture() ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
            evt.StopPropagation();
        }

        void OnDragPerform(DragPerformEvent evt)
        {
            var texture = DraggedTexture();
            if (!texture) return;
            DragAndDrop.AcceptDrag();
            property.objectReferenceValue = texture;
            property.serializedObject.ApplyModifiedProperties();
            evt.StopPropagation();
        }
    }
}
