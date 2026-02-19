using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

/// <summary>
/// Helper สำหรับ Input ที่ทำงานได้ทั้ง Mouse (PC) และ Touch (Android/iOS)
/// ใช้แทน Mouse.current ที่จะเป็น null บน Android
/// </summary>
public static class InputHelper
{
    // ─────────── Primary Pointer (Left Click / Touch) ───────────

    /// <summary>Primary pointer pressed this frame (left click / touch began)</summary>
    public static bool WasPressedThisFrame
    {
        get
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
            return false;
        }
    }

    /// <summary>Primary pointer is held down (left click held / touching screen)</summary>
    public static bool IsPressed
    {
        get
        {
            if (Mouse.current != null && Mouse.current.leftButton.isPressed) return true;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed) return true;
            return false;
        }
    }

    /// <summary>Primary pointer released this frame</summary>
    public static bool WasReleasedThisFrame
    {
        get
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame) return true;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame) return true;
            return false;
        }
    }

    // ─────────── Secondary (Right Click — Mouse only) ───────────

    /// <summary>Right button pressed this frame (Mouse only)</summary>
    public static bool RightWasPressedThisFrame =>
        Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;

    /// <summary>Right button held (Mouse only)</summary>
    public static bool RightIsPressed =>
        Mouse.current != null && Mouse.current.rightButton.isPressed;

    // ─────────── Pointer Position ───────────

    /// <summary>Current pointer position in screen pixels (touch position or mouse position)</summary>
    public static Vector2 PointerPosition
    {
        get
        {
            // Touch takes priority when active
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
                return Touchscreen.current.primaryTouch.position.ReadValue();
            if (Mouse.current != null)
                return Mouse.current.position.ReadValue();
            return Vector2.zero;
        }
    }

    /// <summary>Get pointer position even when not pressed (for preview follow)</summary>
    public static Vector2 PointerPositionAlways
    {
        get
        {
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
                return Touchscreen.current.primaryTouch.position.ReadValue();
            if (Mouse.current != null)
                return Mouse.current.position.ReadValue();
            return Vector2.zero;
        }
    }

    // ─────────── Scroll / Zoom ───────────

    /// <summary>Mouse scroll delta (0 on mobile — use pinch zoom instead)</summary>
    public static float ScrollDelta =>
        Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f;

    // ─────────── Touch Info ───────────

    /// <summary>Number of active touches (0 on PC)</summary>
    public static int TouchCount => Input.touchCount;

    /// <summary>Get touch by index (uses old Input API which works alongside new Input System)</summary>
    public static Touch GetTouch(int index) => Input.GetTouch(index);

    // ─────────── UI Overlap ───────────

    /// <summary>Check if pointer is over UI element (works for both mouse and touch)</summary>
    public static bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;

        // Touch: need to pass finger ID
        if (Input.touchCount > 0)
            return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);

        return EventSystem.current.IsPointerOverGameObject();
    }
}
