using System;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;
using Object = UnityEngine.Object;

namespace Zaphkiel.VRCSDKPlus
{
    internal enum PlaceResult
    {
        Placed,
        NothingToPlace,
        ControlGone,
        MenuFull,
    }

    internal static class MenuClipboard
    {
        const string Prefix = "[TAG=VSP_CONTROL_V2]";
        const string LegacyPrefix = "[TAG=VSP_CONTROL]";

        static VRCExpressionsMenu moveSource;
        static VRCExpressionsMenu.Control moveControl;

        public static event Action MoveStateChanged;

        public static bool HasControl
        {
            get
            {
                string buffer = EditorGUIUtility.systemCopyBuffer;
                return buffer != null && (buffer.StartsWith(Prefix, StringComparison.Ordinal) || buffer.StartsWith(LegacyPrefix, StringComparison.Ordinal));
            }
        }

        public static bool IsMoving => moveSource && moveControl != null;

        public static bool IsMovingWithin(VRCExpressionsMenu menu) => IsMoving && moveSource == menu;

        public static void Copy(VRCExpressionsMenu.Control control) =>
            EditorGUIUtility.systemCopyBuffer = Prefix + EditorJsonUtility.ToJson(control);

        public static VRCExpressionsMenu.Control Paste()
        {
            string buffer = EditorGUIUtility.systemCopyBuffer;
            if (buffer == null) return null;

            if (buffer.StartsWith(Prefix, StringComparison.Ordinal))
            {
                var control = new VRCExpressionsMenu.Control();
                EditorJsonUtility.FromJsonOverwrite(buffer.Substring(Prefix.Length), control);
                return control;
            }

            return buffer.StartsWith(LegacyPrefix, StringComparison.Ordinal)
                ? JsonUtility.FromJson<VRCExpressionsMenu.Control>(buffer.Substring(LegacyPrefix.Length))
                : null;
        }

        public static void BeginMove(VRCExpressionsMenu menu, VRCExpressionsMenu.Control control)
        {
            moveSource = menu;
            moveControl = control;
            MoveStateChanged?.Invoke();
        }

        public static void CancelMove()
        {
            if (!IsMoving) return;
            moveSource = null;
            moveControl = null;
            MoveStateChanged?.Invoke();
        }

        public static PlaceResult Place(VRCExpressionsMenu destination, int afterIndex, out int placedIndex, out VRCExpressionsMenu source)
        {
            placedIndex = -1;
            source = moveSource;
            if (!IsMoving || !destination) return PlaceResult.NothingToPlace;

            int from = moveSource.controls.IndexOf(moveControl);
            if (from < 0)
            {
                CancelMove();
                return PlaceResult.ControlGone;
            }

            bool sameMenu = moveSource == destination;
            if (!sameMenu && destination.controls.Count >= VRCExpressionsMenu.MAX_CONTROLS) return PlaceResult.MenuFull;

            Undo.RecordObjects(new Object[] { destination, moveSource }, "Move Control");
            moveSource.controls.RemoveAt(from);
            placedIndex = InsertIndex(from, afterIndex, sameMenu, destination.controls.Count);
            destination.controls.Insert(placedIndex, moveControl);
            EditorUtility.SetDirty(moveSource);
            EditorUtility.SetDirty(destination);

            CancelMove();
            return PlaceResult.Placed;
        }

        internal static int InsertIndex(int from, int afterIndex, bool sameMenu, int countAfterRemoval)
        {
            int insert = sameMenu && from <= afterIndex ? afterIndex : afterIndex + 1;
            return Mathf.Clamp(insert, 0, countAfterRemoval);
        }
    }
}
