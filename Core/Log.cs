using UnityEngine;

namespace Zaphkiel.VRCSDKPlus
{
    internal static class Log
    {
        const string Prefix = "<color=green>[VRCSDK+]</color> ";

        public static void Info(string message) => Debug.Log(Prefix + message);
        public static void Warning(string message) => Debug.LogWarning("[VRCSDK+] " + message);
        public static void Error(string message) => Debug.LogError("[VRCSDK+] " + message);
    }
}
