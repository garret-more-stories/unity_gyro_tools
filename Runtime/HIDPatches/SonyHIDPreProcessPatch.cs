#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
using System.Reflection;
using HarmonyLib;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Scripting;

namespace MoreStories.GyroTools
{
    [HarmonyPatch]
    [Preserve]
    public static class SonyHIDPreProcessPatch
    {
        [Preserve]
        static MethodBase TargetMethod()
        {
            // Access the private explicit interface implementation via reflection
            return typeof(DualShock4GamepadHID).GetMethod(
                "UnityEngine.InputSystem.LowLevel.IEventPreProcessor.PreProcessEvent",
                BindingFlags.NonPublic | BindingFlags.Instance);
        }

        [Preserve]
        public static unsafe bool Prefix(InputEventPtr eventPtr, ref bool __result) 
            => !(__result = eventPtr.type == DeltaStateEvent.Type); 
    }
}

#endif