using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.Layouts;

namespace MoreStories.GyroTools
{
    public struct IMUState : IInputStateTypeInfo
    {
        public static FourCC Format {get; private set;} = new FourCC('I', 'M', 'U', 'S');
        public FourCC format => Format;

        public Vector3 this[IMUType type]
        {
            get=> type switch
            {
                IMUType.Accelerometer => accelerometer,
                IMUType.Gyroscope     => gyroscope,
                _                     => gyroscope
            };
            set
            {
                switch(type)
                {
                    case IMUType.Accelerometer:
                        accelerometer = value;
                        break;
                    case IMUType.Gyroscope:
                        gyroscope = value;
                        break;
                }
            }
        }

        [InputControl(name = "accel", offset = 0, layout = "Vector3", usage = "Acceleration",    displayName = "Accelerometer")]
        public Vector3 accelerometer;

        [InputControl(name = "gyro", offset = 12,  layout = "Vector3", usage = "AngularVelocity", displayName = "Gyroscope")]
        public Vector3 gyroscope;

    }
}
