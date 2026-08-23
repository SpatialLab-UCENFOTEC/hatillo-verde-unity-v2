using UnityEngine;

public static class VoiceCopilotBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        var tm = Object.FindAnyObjectByType<TransitionManager>();
        if (tm == null) return;
        if (tm.GetComponent<VoiceCopilot>() != null) return;
        tm.gameObject.AddComponent<HatilloVoiceBridge>();
        tm.gameObject.AddComponent<VoiceCopilot>();
        Debug.Log("[VoiceCopilot] attached to " + tm.gameObject.name);
    }
}
