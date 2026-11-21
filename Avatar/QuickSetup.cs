using System.Reflection;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using EyeLook = VRC.SDK3.Avatars.Components.VRCAvatarDescriptor.CustomEyeLookSettings;
using EyeRotations = VRC.SDK3.Avatars.Components.VRCAvatarDescriptor.CustomEyeLookSettings.EyeRotations;

namespace Zaphkiel.VRCSDKPlus
{
    internal static class QuickSetup
    {
        const float AxisValue = 0.1305262f;
        const float WValue = 0.9914449f;

        const float EyeAboveHeadRatio = 0.0357f;
        const float ForwardRatio = 0.0547f;

        static VRCAvatarDescriptor lipSyncTarget;

        [MenuItem("CONTEXT/VRCAvatarDescriptor/[SDK+] Quick Setup", false, 650)]
        static void Run(MenuCommand command)
        {
            var descriptor = (VRCAvatarDescriptor)command.context;
            var animator = descriptor.GetComponent<Animator>();
            using var serialized = new SerializedObject(descriptor);

            if (animator && animator.isHuman)
            {
                FindEyes(animator, out var leftEye, out var rightEye);
                var head = animator.GetBoneTransform(HumanBodyBones.Head);

                if ((leftEye && rightEye) || head)
                    serialized.FindProperty(nameof(VRCAvatarDescriptor.ViewPosition)).vector3Value = ViewPosition(descriptor.transform, head, leftEye, rightEye);
                else
                    Log.Warning("Quick Setup: the avatar has neither eye nor head bones mapped. The view position was not changed.");

                if (leftEye && rightEye) SetupEyes(serialized, descriptor, leftEye, rightEye);
            }
            else
            {
                Log.Warning("Quick Setup: the avatar has no humanoid Animator. Only the lip sync was set up.");
            }

            serialized.ApplyModifiedProperties();
            lipSyncTarget = descriptor;
            EditorApplication.delayCall -= AutoDetectLipSync;
            EditorApplication.delayCall += AutoDetectLipSync;
        }

        static void FindEyes(Animator animator, out Transform leftEye, out Transform rightEye)
        {
            leftEye = animator.GetBoneTransform(HumanBodyBones.LeftEye);
            rightEye = animator.GetBoneTransform(HumanBodyBones.RightEye);
            if (!leftEye || !rightEye) return;

            var namedLeft = leftEye.parent ? leftEye.parent.Find("LeftEye") : null;
            var namedRight = rightEye.parent ? rightEye.parent.Find("RightEye") : null;
            if (namedLeft) leftEye = namedLeft;
            if (namedRight) rightEye = namedRight;
        }

        static Vector3 ViewPosition(Transform root, Transform head, Transform leftEye, Transform rightEye)
        {
            Vector3 local;
            if (leftEye && rightEye)
            {
                local = root.InverseTransformPoint((leftEye.position + rightEye.position) / 2);
            }
            else
            {
                var headLocal = root.InverseTransformPoint(head.position);
                local = new Vector3(headLocal.x, headLocal.y * (1 + EyeAboveHeadRatio), 0);
            }

            return new Vector3(Mathf.Approximately(local.x, 0) ? 0 : local.x, local.y, (local.z + ForwardRatio * local.y) / 2);
        }

        static void SetupEyes(SerializedObject serialized, VRCAvatarDescriptor descriptor, Transform leftEye, Transform rightEye)
        {
            var eyes = serialized.FindProperty(nameof(VRCAvatarDescriptor.customEyeLookSettings));
            serialized.FindProperty(nameof(VRCAvatarDescriptor.enableEyeLook)).boolValue = true;
            eyes.FindPropertyRelative(nameof(EyeLook.leftEye)).objectReferenceValue = leftEye;
            eyes.FindPropertyRelative(nameof(EyeLook.rightEye)).objectReferenceValue = rightEye;

            SetBothEyes(eyes.FindPropertyRelative(nameof(EyeLook.eyesLookingUp)), new Quaternion(-AxisValue, 0, 0, WValue));
            SetBothEyes(eyes.FindPropertyRelative(nameof(EyeLook.eyesLookingRight)), new Quaternion(0, AxisValue, 0, WValue));
            SetBothEyes(eyes.FindPropertyRelative(nameof(EyeLook.eyesLookingDown)), new Quaternion(AxisValue, 0, 0, WValue));
            SetBothEyes(eyes.FindPropertyRelative(nameof(EyeLook.eyesLookingLeft)), new Quaternion(0, -AxisValue, 0, WValue));

            SkinnedMeshRenderer body = null;
            for (int i = 0; i < descriptor.transform.childCount && !body; i++)
                body = descriptor.transform.GetChild(i).GetComponent<SkinnedMeshRenderer>();
            if (!body || !body.sharedMesh) return;

            int blink = body.sharedMesh.GetBlendShapeIndex("Blink");
            if (blink < 0) return;

            eyes.FindPropertyRelative(nameof(EyeLook.eyelidType)).intValue = (int)VRCAvatarDescriptor.EyelidType.Blendshapes;
            eyes.FindPropertyRelative(nameof(EyeLook.eyelidsSkinnedMesh)).objectReferenceValue = body;
            var blendShapes = eyes.FindPropertyRelative(nameof(EyeLook.eyelidsBlendshapes));
            blendShapes.arraySize = 3;
            blendShapes.GetArrayElementAtIndex(0).intValue = blink;
            blendShapes.GetArrayElementAtIndex(1).intValue = -1;
            blendShapes.GetArrayElementAtIndex(2).intValue = -1;
        }

        static void SetBothEyes(SerializedProperty state, Quaternion rotation)
        {
            state.FindPropertyRelative(nameof(EyeRotations.left)).quaternionValue = rotation;
            state.FindPropertyRelative(nameof(EyeRotations.right)).quaternionValue = rotation;
        }

        static void AutoDetectLipSync()
        {
            EditorApplication.delayCall -= AutoDetectLipSync;
            var descriptor = lipSyncTarget;
            lipSyncTarget = null;
            if (!descriptor) return;

            foreach (var type in TypeCache.GetTypesDerivedFrom<Editor>())
            {
                if (type.Name != "AvatarDescriptorEditor3") continue;
                var method = type.GetMethod("AutoDetectLipSync", BindingFlags.NonPublic | BindingFlags.Instance);
                if (method == null) break;

                foreach (var editor in Resources.FindObjectsOfTypeAll(type))
                {
                    if (((Editor)editor).target != descriptor) continue;
                    method.Invoke(editor, null);
                    return;
                }
            }

            Log.Warning("Quick Setup: the lip sync was not auto-detected, as no inspector shows this avatar descriptor.");
        }
    }
}
