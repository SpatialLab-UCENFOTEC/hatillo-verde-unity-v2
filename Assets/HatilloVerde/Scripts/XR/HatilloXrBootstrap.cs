using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.UI;
using UnityEngine.XR;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using UnityEngine.XR.Interaction.Toolkit.UI;
using TMPro;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

/// <summary>
/// Quest / OpenXR: replace the desktop camera with an XR Origin at the same viewpoint,
/// controller ray for POIs, world-space overlay canvas, grip push-to-talk.
/// WebGL / desktop without a headset keep the original camera.
/// </summary>
public class HatilloXrBootstrap : MonoBehaviour
{
    const float WorldCanvasScale = 0.001f;
    const float CanvasDistance = 1.45f;

    Transform _rightRay;
    InputAction _trigger;
    InputAction _grip;
    bool _wasTrigger;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (!ShouldEnableXr())
            return;
        if (Object.FindAnyObjectByType<TransitionManager>() == null)
            return;
        if (Object.FindAnyObjectByType<HatilloXrBootstrap>() != null)
            return;

        var go = new GameObject("HatilloXrBootstrap");
        DontDestroyOnLoad(go);
        go.AddComponent<HatilloXrBootstrap>();
    }

    static bool ShouldEnableXr()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return true;
#else
        try
        {
            return XRSettings.enabled || XRSettings.isDeviceActive;
        }
        catch
        {
            return false;
        }
#endif
    }

    IEnumerator Start()
    {
#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            Permission.RequestUserPermission(Permission.Microphone);
#endif
        BuildRig();
        yield return null;
        ConvertOverlayCanvases();
        EnsureXrEventSystem();
        SkipIntro();
        CreateStatusHud();
        HatilloXrRig.IsActive = true;
        Debug.Log("[HatilloXR] rig ready");
    }

    void BuildRig()
    {
        var oldCam = Camera.main;
        Vector3 pos = oldCam != null ? oldCam.transform.position : new Vector3(-77.007f, 8.309f, 18.13f);
        Quaternion rot = oldCam != null ? oldCam.transform.rotation : Quaternion.Euler(-8f, 111.974f, 0f);

        if (FindAnyObjectByType<XRInteractionManager>() == null)
            new GameObject("XR Interaction Manager").AddComponent<XRInteractionManager>();

        var originGo = new GameObject("XR Origin");
        originGo.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, rot.eulerAngles.y, 0f));
        var origin = originGo.AddComponent<XROrigin>();

        var offset = new GameObject("Camera Offset");
        offset.transform.SetParent(originGo.transform, false);
        offset.transform.localPosition = new Vector3(0f, 0f, 0f);

        var camGo = new GameObject("XR Camera");
        camGo.tag = "MainCamera";
        camGo.transform.SetParent(offset.transform, false);
        camGo.transform.localRotation = Quaternion.Euler(rot.eulerAngles.x, 0f, 0f);
        var cam = camGo.AddComponent<Camera>();
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 600f;
        cam.fieldOfView = 70f;
        camGo.AddComponent<AudioListener>();
        var urp = camGo.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        urp.renderPostProcessing = false;
        urp.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.None;

        var headTpd = camGo.AddComponent<TrackedPoseDriver>();
        BindPose(headTpd, "<XRHMD>/centerEyePosition", "<XRHMD>/centerEyeRotation");

        origin.Origin = originGo;
        origin.CameraFloorOffsetObject = offset;
        origin.Camera = cam;
        origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Device;
        HatilloXrRig.IsActive = true;

        if (oldCam != null && oldCam.gameObject != camGo)
        {
            foreach (var mb in oldCam.GetComponents<MonoBehaviour>())
            {
                if (mb != null && mb.GetType().Name == "BK_FreeCamera")
                    mb.enabled = false;
            }
            oldCam.enabled = false;
            var listener = oldCam.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = false;
            oldCam.tag = "Untagged";
        }

        var rightGo = new GameObject("Right Controller");
        rightGo.transform.SetParent(offset.transform, false);
        var rightTpd = rightGo.AddComponent<TrackedPoseDriver>();
        BindPose(rightTpd, "<XRController>{RightHand}/devicePosition", "<XRController>{RightHand}/deviceRotation");

        var ray = rightGo.AddComponent<XRRayInteractor>();
        ray.maxRaycastDistance = 40f;
        var line = rightGo.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.startWidth = 0.005f;
        line.endWidth = 0.001f;
        line.useWorldSpace = true;
        line.material = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color"));
        line.startColor = new Color(1f, 1f, 0.6f, 0.9f);
        line.endColor = new Color(1f, 1f, 0.6f, 0.2f);
        var visual = rightGo.AddComponent<XRInteractorLineVisual>();
        visual.lineWidth = 0.005f;

        _rightRay = rightGo.transform;
        HatilloXrRig.HeadCamera = cam;
        HatilloXrRig.RightRayOrigin = _rightRay;

        _trigger = new InputAction("xr-trigger", InputActionType.Button, "<XRController>{RightHand}/triggerPressed");
        _grip = new InputAction("xr-grip", InputActionType.Button, "<XRController>{RightHand}/gripPressed");
        _trigger.Enable();
        _grip.Enable();
    }

    static void BindPose(TrackedPoseDriver tpd, string posPath, string rotPath)
    {
        var pos = new InputAction("pos", InputActionType.Value, posPath);
        pos.expectedControlType = "Vector3";
        var rot = new InputAction("rot", InputActionType.Value, rotPath);
        rot.expectedControlType = "Quaternion";
        pos.Enable();
        rot.Enable();
        tpd.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
        tpd.positionInput = new InputActionProperty(pos);
        tpd.rotationInput = new InputActionProperty(rot);
    }

    void ConvertOverlayCanvases()
    {
        var cam = HatilloXrRig.HeadCamera;
        if (cam == null) return;

        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                continue;
            if (canvas.GetComponentInParent<HatilloXrBootstrap>() != null)
                continue;

            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = cam;
            var rt = canvas.GetComponent<RectTransform>();
            rt.localScale = Vector3.one * WorldCanvasScale;
            PlaceInFront(rt, cam.transform);

            var graphic = canvas.GetComponent<GraphicRaycaster>();
            if (graphic != null) graphic.enabled = false;
            if (canvas.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
                canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
        }

        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (canvas.renderMode != RenderMode.WorldSpace)
                continue;
            if (canvas.worldCamera == null)
                canvas.worldCamera = cam;
            if (canvas.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
                canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
        }
    }

    static void PlaceInFront(RectTransform rt, Transform head)
    {
        Vector3 flatFwd = head.forward;
        flatFwd.y = 0f;
        if (flatFwd.sqrMagnitude < 0.01f) flatFwd = head.forward;
        flatFwd.Normalize();
        rt.position = head.position + flatFwd * CanvasDistance + Vector3.up * 0.15f;
        // World-space UI faces -Z; look at the player, not away.
        rt.rotation = Quaternion.LookRotation(-flatFwd, Vector3.up);
    }

    static void EnsureXrEventSystem()
    {
        var es = EventSystem.current;
        if (es == null)
        {
            var go = new GameObject("EventSystem");
            es = go.AddComponent<EventSystem>();
        }

        var xrUi = es.GetComponent<XRUIInputModule>();
        if (xrUi == null)
            xrUi = es.gameObject.AddComponent<XRUIInputModule>();
        xrUi.enabled = true;

        var standalone = es.GetComponent<StandaloneInputModule>();
        if (standalone != null) standalone.enabled = false;
        var inputSys = es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        if (inputSys != null) inputSys.enabled = false;
    }

    static void SkipIntro()
    {
        var intro = FindAnyObjectByType<IntroManager>(FindObjectsInactive.Include);
        if (intro != null)
            intro.SkipStraightToExperience();
    }

    static void CreateStatusHud()
    {
        var cam = HatilloXrRig.HeadCamera;
        if (cam == null) return;
        var go = new GameObject("XrVoiceHud");
        go.transform.SetParent(cam.transform, false);
        go.transform.localPosition = new Vector3(0f, -0.22f, 1.1f);
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one * 0.018f;
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 10f;
        tmp.color = Color.white;
        tmp.text = "Aprieta el grip y habla";
        HatilloXrRig.StatusLabel = tmp;
    }

    void Update()
    {
        bool trigger = _trigger != null && _trigger.IsPressed();
        HatilloXrRig.RightTriggerPressedThisFrame = trigger && !_wasTrigger;
        _wasTrigger = trigger;
        HatilloXrRig.RightGripHeld = _grip != null && _grip.IsPressed();
    }

    void OnDestroy()
    {
        _trigger?.Disable();
        _grip?.Disable();
        HatilloXrRig.IsActive = false;
        HatilloXrRig.HeadCamera = null;
        HatilloXrRig.RightRayOrigin = null;
    }
}
