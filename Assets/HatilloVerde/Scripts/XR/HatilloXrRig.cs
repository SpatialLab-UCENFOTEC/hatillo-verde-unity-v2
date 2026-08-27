using TMPro;
using UnityEngine;

/// <summary>
/// Shared XR state for SelectionManager, voice, and UI. Filled by HatilloXrBootstrap.
/// </summary>
public static class HatilloXrRig
{
    public static bool IsActive { get; set; }
    public static Camera HeadCamera { get; set; }
    public static Transform RightRayOrigin { get; set; }

    public static bool TryGetRightRay(out Ray ray)
    {
        ray = default;
        if (!IsActive || RightRayOrigin == null)
            return false;
        ray = new Ray(RightRayOrigin.position, RightRayOrigin.forward);
        return true;
    }

    public static bool RightTriggerPressedThisFrame { get; set; }
    public static bool RightGripHeld { get; set; }
    public static TMP_Text StatusLabel { get; set; }
}
