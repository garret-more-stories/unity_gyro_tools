using UnityEngine;
using UnityEngine.InputSystem;

namespace MoreStories.GyroTools
{
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
#endif
    public class ScaleIMU : InputProcessor<IMUState>
    {

        /// <summary>
        /// Scale vector to apply to the accel's <c>x</c> axis. Defaults to 1.
        /// </summary>
        [Tooltip("Scale factor to multiply the incoming accelerometer's x by.")]
        public float accelX = 1;

         /// <summary>
        /// Scale vector to apply to the accel's <c>y</c> axis. Defaults to 1.
        /// </summary>
        [Tooltip("Scale factor to multiply the incoming accelerometer's y by.")]
        public float accelY = 1;

         /// <summary>
        /// Scale vector to apply to the accel's <c>z</c> axis. Defaults to 1.
        /// </summary>
        [Tooltip("Scale factor to multiply the incoming accelerometer's z by.")]
        public float accelZ = 1;

       /// <summary>
        /// Scale vector to apply to the gyro's <c>x</c> axis. Defaults to 1.
        /// </summary>
        [Tooltip("Scale factor to multiply the incoming gyroscope's x by.")]
        public float gyroX = 1;

         /// <summary>
        /// Scale vector to apply to the gyro's <c>y</c> axis. Defaults to 1.
        /// </summary>
        [Tooltip("Scale factor to multiply the incoming gyroscope's y by.")]
        public float gyroY = 1;

         /// <summary>
        /// Scale vector to apply to the gyro's <c>z</c> axis. Defaults to 1.
        /// </summary>
        [Tooltip("Scale factor to multiply the incoming gyroscope's z by.")]
        public float gyroZ = 1;

#if UNITY_EDITOR
        static ScaleIMU()
        {
            Initialize();
        }
#endif
        [RuntimeInitializeOnLoadMethod]
        static void Initialize()
        {
            InputSystem.RegisterProcessor<ScaleIMU>();
        }

        public override IMUState Process(IMUState value, InputControl control)
        {
            value.accelerometer.x *= accelX;
            value.accelerometer.y *= accelY;
            value.accelerometer.z *= accelZ;

            value.gyroscope.x     *= gyroX;
            value.gyroscope.y     *= gyroY;
            value.gyroscope.z     *= gyroZ;

            return value;
        }

        public override string ToString() => $"ScaleIMU(accelX={accelX},accelY.y={accelY},accelZ={accelZ}, "
                                           + $"gyroX={gyroX}, gyroY={gyroY}, gyroZ={gyroZ})";
    }
}
