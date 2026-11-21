using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace Zaphkiel.VRCSDKPlus
{
    internal readonly struct ParentMenuLink
    {
        public readonly VRCExpressionsMenu Menu;
        public readonly IReadOnlyList<string> ControlNames;

        public ParentMenuLink(VRCExpressionsMenu menu, IReadOnlyList<string> controlNames)
        {
            Menu = menu;
            ControlNames = controlNames;
        }
    }

    internal static class MenuReferences
    {
        const long FrameBudgetMs = 25;

        static readonly Dictionary<string, HashSet<string>> ParentsByChild = new Dictionary<string, HashSet<string>>();
        static readonly Dictionary<string, string[]> ChildrenByParent = new Dictionary<string, string[]>();
        static readonly Dictionary<string, string> GuidByPath = new Dictionary<string, string>();
        static readonly Queue<string> Pending = new Queue<string>();
        static bool building;

        public static event Action Changed;

        public static bool IsReady { get; private set; }

        public static void EnsureBuilt()
        {
            if (IsReady || building) return;
            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(VRCExpressionsMenu)}"))
                Pending.Enqueue(guid);
            StartProcessing();
        }

        static void StartProcessing()
        {
            if (building) return;
            building = true;
            EditorApplication.update += BuildStep;
        }

        public static void Rebuild()
        {
            EditorApplication.update -= BuildStep;
            ParentsByChild.Clear();
            ChildrenByParent.Clear();
            GuidByPath.Clear();
            Pending.Clear();
            IsReady = false;
            building = false;
            EnsureBuilt();
        }

        static void BuildStep()
        {
            var watch = Stopwatch.StartNew();
            while (Pending.Count > 0 && watch.ElapsedMilliseconds < FrameBudgetMs)
                Index(Pending.Dequeue());
            if (Pending.Count > 0) return;

            EditorApplication.update -= BuildStep;
            building = false;
            IsReady = true;
            Changed?.Invoke();
        }

        public static List<ParentMenuLink> FindParents(VRCExpressionsMenu menu)
        {
            var links = new List<ParentMenuLink>();
            if (!menu) return links;

            var candidates = new HashSet<VRCExpressionsMenu> { menu };
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(menu));
            if (ParentsByChild.TryGetValue(guid, out var parents))
            {
                foreach (string parent in parents)
                {
                    var candidate = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(AssetDatabase.GUIDToAssetPath(parent));
                    if (candidate) candidates.Add(candidate);
                }
            }

            foreach (var loaded in Resources.FindObjectsOfTypeAll<VRCExpressionsMenu>())
                if (EditorUtility.IsPersistent(loaded) && EditorUtility.IsDirty(loaded))
                    candidates.Add(loaded);

            foreach (var candidate in candidates)
            {
                if (candidate.controls == null) continue;
                List<string> controlNames = null;
                foreach (var control in candidate.controls)
                    if (control != null && control.subMenu == menu)
                        (controlNames ??= new List<string>()).Add(control.name);
                if (controlNames != null) links.Add(new ParentMenuLink(candidate, controlNames));
            }

            links.Sort((a, b) => string.Compare(a.Menu.name, b.Menu.name, StringComparison.OrdinalIgnoreCase));
            return links;
        }

        static void Index(string parentGuid)
        {
            Unindex(parentGuid);
            string path = AssetDatabase.GUIDToAssetPath(parentGuid);
            if (string.IsNullOrEmpty(path) || AssetDatabase.GetMainAssetTypeAtPath(path) != typeof(VRCExpressionsMenu)) return;

            GuidByPath[path] = parentGuid;
            var children = new List<string>();
            foreach (string dependency in AssetDatabase.GetDependencies(path, false))
            {
                if (dependency == path) continue;
                string child = AssetDatabase.AssetPathToGUID(dependency);
                if (string.IsNullOrEmpty(child)) continue;

                children.Add(child);
                if (!ParentsByChild.TryGetValue(child, out var parents))
                    ParentsByChild[child] = parents = new HashSet<string>();
                parents.Add(parentGuid);
            }

            ChildrenByParent[parentGuid] = children.ToArray();
        }

        static void Unindex(string parentGuid)
        {
            if (!ChildrenByParent.TryGetValue(parentGuid, out var children)) return;
            foreach (string child in children)
                if (ParentsByChild.TryGetValue(child, out var parents))
                    parents.Remove(parentGuid);
            ChildrenByParent.Remove(parentGuid);
        }

        internal static void OnAssetsChanged(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (!IsReady && !building) return;

            bool removed = false;
            foreach (string path in deleted)
            {
                if (!GuidByPath.TryGetValue(path, out string guid)) continue;
                Unindex(guid);
                GuidByPath.Remove(path);
                removed = true;
            }

            foreach (string path in movedFrom)
                GuidByPath.Remove(path);

            bool queued = false;
            foreach (string path in imported) queued |= Enqueue(path);
            foreach (string path in moved) queued |= Enqueue(path);

            if (queued) StartProcessing();
            else if (removed && IsReady) Changed?.Invoke();
        }

        static bool Enqueue(string path)
        {
            if (!path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)) return false;
            string guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid)) return false;
            Pending.Enqueue(guid);
            return true;
        }
    }

    internal sealed class MenuReferencePostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom) =>
            MenuReferences.OnAssetsChanged(imported, deleted, moved, movedFrom);
    }
}
