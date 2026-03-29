using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;

namespace MoreStories.GyroTools
{
    [InputControlLayout(stateType = typeof(IMUState))]
    public class IMUControl : InputControl<IMUState>
    {
        [InputControl(name = "accel", offset = 0, layout = "Vector3", usage = "Acceleration",    displayName = "Accelerometer")]
        public Vector3Control accel { get; private set; }

        [InputControl(name = "gyro", offset = 12,  layout = "Vector3", usage = "AngularVelocity", displayName = "Gyroscope")]
        public Vector3Control gyro  { get; private set; }

        protected override void FinishSetup()
        {
            accel = GetChildControl<Vector3Control>("accel");
            gyro  = GetChildControl<Vector3Control>("gyro");

            base.FinishSetup();
        }

        public override unsafe IMUState ReadUnprocessedValueFromState(void* statePtr)
        {
            return new IMUState
            {
                accelerometer = accel. ReadUnprocessedValueFromStateWithCaching(statePtr),
                gyroscope     = gyro.  ReadUnprocessedValueFromStateWithCaching(statePtr)
            };
        }
}
}
