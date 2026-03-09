using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.Layouts;

namespace MoreStories.GyroTools
{
    public struct IMUState : IInputStateTypeInfo
    {
        public FourCC format => new FourCC('I', 'M', 'U', 'S');

        [InputControl(name = "accel", layout = "Vector3", usage = "Acceleration",    displayName = "Accelerometer")]
        public Vector3 accelerometer;

        [InputControl(name = "gyro",  layout = "Vector3", usage = "AngularVelocity", displayName = "Gyroscope")]
        public Vector3 gyroscope;
    }
}
