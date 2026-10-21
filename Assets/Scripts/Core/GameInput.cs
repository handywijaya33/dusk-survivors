using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

/// <summary>Movement input that works with either the legacy Input Manager or the new Input System.</summary>
public static class GameInput
{
    public static Vector2 Move()
    {
        Vector2 v = Vector2.zero;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var k = Keyboard.current;
        if (k != null)
        {
            if (k.aKey.isPressed || k.leftArrowKey.isPressed) v.x -= 1f;
            if (k.dKey.isPressed || k.rightArrowKey.isPressed) v.x += 1f;
            if (k.sKey.isPressed || k.downArrowKey.isPressed) v.y -= 1f;
            if (k.wKey.isPressed || k.upArrowKey.isPressed) v.y += 1f;
        }
        var g = Gamepad.current;
        if (g != null) v += g.leftStick.ReadValue();
#else
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) v.x -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) v.x += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v.y -= 1f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) v.y += 1f;
#endif
        return Vector2.ClampMagnitude(v, 1f);
    }
}
