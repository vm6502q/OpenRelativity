using UnityEngine;
using UnityEngine.InputSystem;

namespace OpenRelativity
{
    /// <summary>
    /// Player input, read from the Input System package.
    ///
    /// These are the axes and buttons that the legacy Input Manager used to define, for this project,
    /// ("Horizontal", "Vertical", "Mouse X", "Mouse Y", "Fire3", and so on, in "ProjectSettings/InputManager.asset",)
    /// with the same keys, the same gamepad controls, and the same value conventions, so that the
    /// controller scripts' math (and the tuning in existing scenes and prefabs) carries over unchanged.
    /// </summary>
    public static class RelativityInput
    {
        // "Sensitivity" and "gravity" of the legacy "Horizontal" and "Vertical" key axes, in units per second
        private const float keyAxisSensitivity = 3;
        private const float keyAxisGravity = 3;
        // "Sensitivity" of the legacy "Mouse X" and "Mouse Y" mouse movement axes, in units per pixel
        private const float mouseAxisSensitivity = 1000;

        private static int lastUpdateFrame = -1;
        private static float horizontalKeys;
        private static float verticalKeys;

        /// <summary>
        /// "Horizontal": A/D or left/right arrow keys, (smoothed,) or the gamepad's left stick
        /// </summary>
        public static float Horizontal
        {
            get
            {
                UpdateKeyAxes();
                Gamepad gamepad = Gamepad.current;
                return LargestMagnitude(horizontalKeys, gamepad != null ? gamepad.leftStick.x.ReadValue() : 0);
            }
        }

        /// <summary>
        /// "Vertical": W/S or up/down arrow keys, (smoothed,) or the gamepad's left stick.
        /// (As with the legacy joystick axis, pushing the stick up is negative. See "PlayerController.inverted".)
        /// </summary>
        public static float Vertical
        {
            get
            {
                UpdateKeyAxes();
                Gamepad gamepad = Gamepad.current;
                return LargestMagnitude(verticalKeys, gamepad != null ? -gamepad.leftStick.y.ReadValue() : 0);
            }
        }

        /// <summary>
        /// "Mouse X": mouse movement since last frame, or the gamepad's right stick
        /// </summary>
        public static float LookX
        {
            get
            {
                Mouse mouse = Mouse.current;
                Gamepad gamepad = Gamepad.current;
                return LargestMagnitude(
                    mouse != null ? mouse.delta.x.ReadValue() * mouseAxisSensitivity : 0,
                    gamepad != null ? gamepad.rightStick.x.ReadValue() : 0);
            }
        }

        /// <summary>
        /// "Mouse Y": mouse movement since last frame, or the gamepad's right stick.
        /// (As with the legacy joystick axis, pushing the stick up is negative. See "PlayerController.inverted".)
        /// </summary>
        public static float LookY
        {
            get
            {
                Mouse mouse = Mouse.current;
                Gamepad gamepad = Gamepad.current;
                return LargestMagnitude(
                    mouse != null ? mouse.delta.y.ReadValue() * mouseAxisSensitivity : 0,
                    gamepad != null ? -gamepad.rightStick.y.ReadValue() : 0);
            }
        }

        /// <summary>
        /// "Fire3": Escape key, or the gamepad's start button
        /// </summary>
        public static bool IsMenuPressed
        {
            get
            {
                return IsKeyPressed(Key.Escape) || (Gamepad.current != null && Gamepad.current.startButton.isPressed);
            }
        }

        /// <summary>
        /// "Invert Button": Y key, or the gamepad's north button
        /// </summary>
        public static bool IsInvertPressed
        {
            get
            {
                return IsKeyPressed(Key.Y) || (Gamepad.current != null && Gamepad.current.buttonNorth.isPressed);
            }
        }

        /// <summary>
        /// "Shader": B key, or the gamepad's east button
        /// </summary>
        public static bool IsShaderTogglePressed
        {
            get
            {
                return IsKeyPressed(Key.B) || (Gamepad.current != null && Gamepad.current.buttonEast.isPressed);
            }
        }

        /// <summary>
        /// "Speed of Light": N key (positive) or M key (negative)
        /// </summary>
        public static float SpeedOfLightChange
        {
            get
            {
                return (IsKeyPressed(Key.N) ? 1 : 0) - (IsKeyPressed(Key.M) ? 1 : 0);
            }
        }

        /// <summary>
        /// Whether "Input.GetJoystickNames()" would have had any entries
        /// </summary>
        public static bool IsJoystickConnected
        {
            get
            {
                return Gamepad.all.Count > 0 || Joystick.all.Count > 0;
            }
        }

        /// <summary>
        /// "Input.GetMouseButton(0)": the left mouse button, or (as the legacy Input Manager simulated) a touch on a touchscreen
        /// </summary>
        public static bool IsPointerPressed
        {
            get
            {
                Mouse mouse = Mouse.current;
                if (mouse != null && mouse.leftButton.isPressed)
                {
                    return true;
                }

                Touchscreen touchscreen = Touchscreen.current;
                return touchscreen != null && touchscreen.primaryTouch.press.isPressed;
            }
        }

        /// <summary>
        /// "Input.GetKey()", by (physical) key position
        /// </summary>
        public static bool IsKeyPressed(Key key)
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard[key].isPressed;
        }

        // The legacy Input Manager reported whichever of the same-named axes had the largest magnitude.
        private static float LargestMagnitude(float a, float b)
        {
            return Mathf.Abs(b) > Mathf.Abs(a) ? b : a;
        }

        // The legacy Input Manager ramped key axes toward +/-1 at their "sensitivity," let them fall back
        // to 0 at their "gravity," and (with "snap") zeroed them on reversing direction.
        private static void UpdateKeyAxes()
        {
            int frame = Time.frameCount;
            if (frame == lastUpdateFrame)
            {
                return;
            }
            // (If nothing read the axes for a while, they've had time to settle.)
            float deltaTime = (frame == lastUpdateFrame + 1) ? Time.unscaledDeltaTime : float.MaxValue;
            lastUpdateFrame = frame;

            float horizontalTarget = (IsKeyPressed(Key.D) || IsKeyPressed(Key.RightArrow) ? 1 : 0)
                - (IsKeyPressed(Key.A) || IsKeyPressed(Key.LeftArrow) ? 1 : 0);
            float verticalTarget = (IsKeyPressed(Key.W) || IsKeyPressed(Key.UpArrow) ? 1 : 0)
                - (IsKeyPressed(Key.S) || IsKeyPressed(Key.DownArrow) ? 1 : 0);

            horizontalKeys = SmoothKeyAxis(horizontalKeys, horizontalTarget, deltaTime);
            verticalKeys = SmoothKeyAxis(verticalKeys, verticalTarget, deltaTime);
        }

        private static float SmoothKeyAxis(float value, float target, float deltaTime)
        {
            if (target == 0)
            {
                return deltaTime == float.MaxValue ? 0 : Mathf.MoveTowards(value, 0, keyAxisGravity * deltaTime);
            }

            if (deltaTime == float.MaxValue)
            {
                // (Start the ramp from rest.)
                return 0;
            }

            if (value != 0 && Mathf.Sign(value) != Mathf.Sign(target))
            {
                value = 0;
            }

            return Mathf.MoveTowards(value, target, keyAxisSensitivity * deltaTime);
        }
    }
}
