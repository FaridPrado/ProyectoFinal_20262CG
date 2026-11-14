using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
namespace ControlRoomZ03 {
    // Supports both project input configurations without requiring a new package.
    public static class Z03Input {
        public static bool Held(KeyCode key) {
#if ENABLE_INPUT_SYSTEM
            var k=Keyboard.current; if(k==null) return false;
            switch(key) {
                case KeyCode.W:return k.wKey.isPressed; case KeyCode.S:return k.sKey.isPressed;
                case KeyCode.A:return k.aKey.isPressed; case KeyCode.D:return k.dKey.isPressed;
                case KeyCode.LeftShift:return k.leftShiftKey.isPressed; case KeyCode.RightShift:return k.rightShiftKey.isPressed;
                default:return false;
            }
#else
            return Input.GetKey(key);
#endif
        }
        public static bool Down(KeyCode key) {
#if ENABLE_INPUT_SYSTEM
            var k=Keyboard.current; if(k==null)return false;
            switch(key) {
                case KeyCode.E:return k.eKey.wasPressedThisFrame; case KeyCode.Escape:return k.escapeKey.wasPressedThisFrame;
                case KeyCode.R:return k.rKey.wasPressedThisFrame; case KeyCode.H:return k.hKey.wasPressedThisFrame;
                default:return false;
            }
#else
            return Input.GetKeyDown(key);
#endif
        }
        public static bool Click {
            get {
#if ENABLE_INPUT_SYSTEM
                return Mouse.current!=null && Mouse.current.leftButton.wasPressedThisFrame;
#else
                return Input.GetMouseButtonDown(0);
#endif
            }
        }
        public static Vector2 Look {
            get {
#if ENABLE_INPUT_SYSTEM
                return Mouse.current!=null ? Mouse.current.delta.ReadValue()*.12f : Vector2.zero;
#else
                return new Vector2(Input.GetAxisRaw("Mouse X"),Input.GetAxisRaw("Mouse Y"))*2f;
#endif
            }
        }
    }
}
