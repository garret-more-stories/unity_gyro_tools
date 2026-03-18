using System.Reflection;
using HarmonyLib;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.LowLevel;

namespace MoreStories.GyroTools
{
    [HarmonyPatch]
    public static class SonyHIDPreProcessPatch
    {
        static MethodBase TargetMethod()
        {
            // Access the private explicit interface implementation via reflection
            return typeof(DualShock4GamepadHID).GetMethod(
                "UnityEngine.InputSystem.LowLevel.IEventPreProcessor.PreProcessEvent",
                BindingFlags.NonPublic | BindingFlags.Instance);
        }

        // Will uncomment the second bool once I rewrite the timestamp system
        public static unsafe bool Prefix(InputEventPtr eventPtr, ref bool __result) 
            => !(__result = eventPtr.type == DeltaStateEvent.Type); //&& DeltaStateEvent.From(eventPtr)->stateFormat == IMUState.Format);
    }
}
