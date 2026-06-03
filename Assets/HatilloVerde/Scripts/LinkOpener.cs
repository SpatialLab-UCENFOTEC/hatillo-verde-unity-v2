using UnityEngine;
using System.Runtime.InteropServices;

public class LinkOpener : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void OpenURL(string url);
#endif

    // Assign URL from Inspector or call OpenLink() with a URL directly
    [Header("URL")]
    public string url;

    public void OpenLink()
    {
        Open(url);
    }

    public void OpenLink(string targetUrl)
    {
        Open(targetUrl);
    }

    private static void Open(string targetUrl)
    {
        if (string.IsNullOrEmpty(targetUrl)) return;

#if UNITY_WEBGL && !UNITY_EDITOR
        OpenURL(targetUrl);
#else
        Application.OpenURL(targetUrl);
#endif
    }
}
