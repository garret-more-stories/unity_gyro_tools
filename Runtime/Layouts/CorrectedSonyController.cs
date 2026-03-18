using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.Layouts;

namespace MoreStories.GyroTools
{
    [InputControlLayout(
    displayName = "Corrected Sony Controller"
)]
    public class CorrectedSonyController: DualShock4GamepadHID
    {
        [InputControl(name = "Foo", layout = "Button")]
        public ButtonControl foo {get; private set;}

    }
}

    