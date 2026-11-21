using System;
using System.Collections.Generic;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace Zaphkiel.VRCSDKPlus
{
    internal readonly struct ParameterStatus
    {
        public readonly bool IsBlank;
        public readonly bool IsDuplicate;
        public readonly bool IsMissing;
        public readonly AnimatorControllerParameter Match;

        public ParameterStatus(bool isBlank, bool isDuplicate, bool isMissing, AnimatorControllerParameter match)
        {
            IsBlank = isBlank;
            IsDuplicate = isDuplicate;
            IsMissing = isMissing;
            Match = match;
        }

        public bool HasIssue => IsBlank || IsDuplicate || IsMissing;

        public string Message =>
            IsBlank ? "Blank parameter" :
            IsDuplicate ? "Duplicate parameter! May cause issues!" :
            IsMissing ? "Parameter not found in any playable controller of the active avatar" :
            null;
    }

    internal static class ParameterValidation
    {
        public static ParameterStatus[] Evaluate(
            VRCExpressionParameters.Parameter[] parameters,
            IReadOnlyDictionary<string, AnimatorControllerParameter> controllerParameters,
            bool hasAvatar)
        {
            if (parameters == null || parameters.Length == 0) return Array.Empty<ParameterStatus>();

            var counts = new Dictionary<string, int>(parameters.Length, StringComparer.Ordinal);
            foreach (var parameter in parameters)
            {
                string name = parameter?.name;
                if (string.IsNullOrEmpty(name)) continue;
                counts.TryGetValue(name, out int count);
                counts[name] = count + 1;
            }

            var statuses = new ParameterStatus[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                string name = parameters[i]?.name;
                bool blank = string.IsNullOrEmpty(name);
                AnimatorControllerParameter match = null;
                if (!blank) controllerParameters.TryGetValue(name, out match);

                statuses[i] = new ParameterStatus(
                    blank,
                    !blank && counts[name] > 1,
                    hasAvatar && !blank && match == null,
                    match);
            }

            return statuses;
        }

        public static bool[] PlanCleanup(
            VRCExpressionParameters.Parameter[] parameters,
            IReadOnlyDictionary<string, AnimatorControllerParameter> controllerParameters,
            bool hasAvatar)
        {
            if (parameters == null) return Array.Empty<bool>();

            var remove = new bool[parameters.Length];
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < parameters.Length; i++)
            {
                string name = parameters[i]?.name;
                remove[i] = string.IsNullOrEmpty(name)
                            || (hasAvatar && !controllerParameters.ContainsKey(name))
                            || !seen.Add(name);
            }

            return remove;
        }
    }
}
