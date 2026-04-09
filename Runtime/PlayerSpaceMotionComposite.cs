using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;

namespace MoreStories.GyroTools
{
    /// <summary>
    /// This is a composite binding that transforms gyroscope and accelerometer values into Player Space
    /// and computes a 2D vector representing an angular velocity expressed in degrees.
    /// It can also be used with Unity's built-in gyro and accel actions for mobile devices and console platforms.
    /// </summary>
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
#endif
    [DisplayStringFormat("{gyroscope} & {accelerometer}")]
    public class PlayerSpaceMotionComposite : InputBindingComposite<Vector2>
    {
        [InputControl(layout = "Vector3")]
        public int accelerometer;

        [InputControl(layout = "Vector3")]
        public int gyroscope;

        public float sensitivity = 1f;

        private float _lastCall = Time.unscaledTime;
        private Vector3 _gravity;

#if UNITY_EDITOR
        static PlayerSpaceMotionComposite()
        {
            Initialize();
        }
#endif
        [RuntimeInitializeOnLoadMethod]
        static void Initialize()
        {
            InputSystem.RegisterBindingComposite<PlayerSpaceMotionComposite>();
        }

        // Implementation adapted from http://gyrowiki.jibbsmart.com/blog:player-space-gyro-and-alternatives-explained
        public override Vector2 ReadValue(ref InputBindingCompositeContext context)
        {
            const float accelInfluence = .02f;
            const float yawRelaxFactor = 1.41f;

            var time = Time.unscaledTime;
            var deltaTime = time - _lastCall;

            _lastCall = time;

            var gyro = context.ReadValue<Vector3, Vector3MagnitudeComparer>(gyroscope);
            var accel = context.ReadValue<Vector3, Vector3MagnitudeComparer>(accelerometer);

            var rotation = Quaternion.AngleAxis(Vector3.Magnitude(gyro) * deltaTime, -gyro);

            // rotate gravity vector
            _gravity = rotation * _gravity;

            // nudge towards gravity according to current acceleration
            var newGravity = -accel;
            _gravity += (newGravity - _gravity) * accelInfluence;

            var gravNorm = _gravity.normalized;

            // use world yaw for yaw direction, local combined yaw for magnitude
            var worldYaw = gyro.y * gravNorm.y + gyro.z * gravNorm.z; // dot product but just yaw and roll

            return new Vector2(
                -1f * Mathf.Sign(worldYaw) * Mathf.Min(Mathf.Abs(worldYaw) * yawRelaxFactor, new Vector2(gyro.y, gyro.z).magnitude) * sensitivity * Mathf.Rad2Deg * deltaTime,
                -1f * gyro.x * sensitivity * Mathf.Rad2Deg * deltaTime
            );
        }
    }
}