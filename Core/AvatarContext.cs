using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace Zaphkiel.VRCSDKPlus
{
    internal static class AvatarContext
    {
        public static readonly (string Label, VRCAvatarDescriptor.AnimLayerType Type)[] PlayableLayers =
        {
            ("Base", VRCAvatarDescriptor.AnimLayerType.Base),
            ("Additive", VRCAvatarDescriptor.AnimLayerType.Additive),
            ("Gesture", VRCAvatarDescriptor.AnimLayerType.Gesture),
            ("Action", VRCAvatarDescriptor.AnimLayerType.Action),
            ("FX", VRCAvatarDescriptor.AnimLayerType.FX),
            ("Sitting", VRCAvatarDescriptor.AnimLayerType.Sitting),
            ("TPose", VRCAvatarDescriptor.AnimLayerType.TPose),
            ("IKPose", VRCAvatarDescriptor.AnimLayerType.IKPose),
        };

        static VRCAvatarDescriptor selected;

        public static event Action Changed;

        public static VRCAvatarDescriptor Selected => selected ? selected : null;

        public static void Select(VRCAvatarDescriptor avatar)
        {
            if (selected == avatar) return;
            selected = avatar;
            Changed?.Invoke();
        }

        public static void NotifyChanged() => Changed?.Invoke();

        public static List<VRCAvatarDescriptor> FindSceneAvatars()
        {
            var found = UnityEngine.Object.FindObjectsByType<VRCAvatarDescriptor>(FindObjectsSortMode.InstanceID);
            var avatars = new List<VRCAvatarDescriptor>(found.Length);
            foreach (var avatar in found)
                if (avatar) avatars.Add(avatar);
            return avatars;
        }

        public static void Resolve(List<VRCAvatarDescriptor> avatars, Predicate<VRCAvatarDescriptor> preferred = null)
        {
            var match = preferred != null ? avatars.Find(preferred) : null;
            if (match)
            {
                Select(match);
                return;
            }

            var current = Selected;
            if (current && avatars.Contains(current)) return;
            Select(avatars.Count > 0 ? avatars[0] : null);
        }

        public static Dictionary<string, AnimatorControllerParameter> GetControllerParameters(VRCAvatarDescriptor avatar)
        {
            var parameters = new Dictionary<string, AnimatorControllerParameter>(StringComparer.Ordinal);
            if (!avatar) return parameters;

            var visited = new HashSet<RuntimeAnimatorController>();

            void Collect(RuntimeAnimatorController runtime)
            {
                while (runtime is AnimatorOverrideController overrideController)
                    runtime = overrideController.runtimeAnimatorController;
                if (!runtime || !visited.Add(runtime)) return;
                var controller = runtime as AnimatorController ??
                                 AssetDatabase.LoadAssetAtPath<AnimatorController>(AssetDatabase.GetAssetPath(runtime));
                if (!controller) return;

                foreach (var parameter in controller.parameters)
                    parameters.TryAdd(parameter.name, parameter);
            }

            foreach (var layer in avatar.baseAnimationLayers ?? Array.Empty<VRCAvatarDescriptor.CustomAnimLayer>()) Collect(layer.animatorController);
            foreach (var layer in avatar.specialAnimationLayers ?? Array.Empty<VRCAvatarDescriptor.CustomAnimLayer>()) Collect(layer.animatorController);
            foreach (var animator in avatar.GetComponentsInChildren<Animator>(true)) Collect(animator.runtimeAnimatorController);
            return parameters;
        }

        public static AnimatorController GetPlayableController(VRCAvatarDescriptor avatar, VRCAvatarDescriptor.AnimLayerType type)
        {
            if (!avatar) return null;
            foreach (var layer in avatar.baseAnimationLayers ?? Array.Empty<VRCAvatarDescriptor.CustomAnimLayer>())
                if (layer.type == type) return layer.animatorController as AnimatorController;
            foreach (var layer in avatar.specialAnimationLayers ?? Array.Empty<VRCAvatarDescriptor.CustomAnimLayer>())
                if (layer.type == type) return layer.animatorController as AnimatorController;
            return null;
        }
    }
}
