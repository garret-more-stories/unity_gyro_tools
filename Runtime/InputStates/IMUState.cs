using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.Layouts;
using System.Runtime.InteropServices;

namespace MoreStories.GyroTools
{
    [StructLayout(LayoutKind.Explicit)]
    public struct IMUState : IInputStateTypeInfo
    {
        public static FourCC Format {get; private set;} = new FourCC('I', 'M', 'U', 'S');
        public FourCC format => Format;

        [InputControl(name = "accel", layout = "Vector3", usage = "Acceleration",    displayName = "Accelerometer")]
        [FieldOffset(0)]
        public Vector3 accelerometer;

        [InputControl(name = "gyro",  layout = "Vector3", usage = "AngularVelocity", displayName = "Gyroscope")]
        [FieldOffset(12)]
        public Vector3 gyroscope;
    }
}
