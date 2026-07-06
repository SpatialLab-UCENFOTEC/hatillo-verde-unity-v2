using System;
using UnityEngine;
using UnityEngine.Video;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

// Picks the desktop or mobile video URL for a VideoPlayer before it starts.
// Attach to any GameObject that has a URL-sourced VideoPlayer; by default it
// reads the URL already set on the VideoPlayer as the desktop source and
// derives the mobile source by inserting "-mobile" before the extension.
[RequireComponent(typeof(VideoPlayer))]
public class MobileVideoUrl : MonoBehaviour
{
    private const string MobileSuffix = "-mobile";

    [Tooltip("Leave empty to reuse the URL already set on the VideoPlayer.")]
    [SerializeField] private string desktopUrlOverride;

    [Tooltip("Leave empty to derive it from the desktop URL (VidIntro.mp4 -> VidIntro-mobile.mp4).")]
    [SerializeField] private string mobileUrlOverride;

    [Tooltip("Canvas width (px) at or below which the mobile video is used when the browser is not detected as mobile.")]
    [SerializeField] private int maxCanvasWidthForMobile = 900;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern int HatilloIsMobileBrowser();
#endif

    private void Awake()
    {
        VideoPlayer player = GetComponent<VideoPlayer>();

        string desktopUrl = string.IsNullOrEmpty(desktopUrlOverride) ? player.url : desktopUrlOverride;
        string mobileUrl = string.IsNullOrEmpty(mobileUrlOverride) ? ToMobileUrl(desktopUrl) : mobileUrlOverride;

        player.source = VideoSource.Url;
        player.url = ShouldUseMobile() ? mobileUrl : desktopUrl;

        // The intro player uses Play On Awake; re-issue playback so the new URL
        // is the one that gets prepared instead of the URL baked into the scene.
        if (player.playOnAwake)
        {
            player.Stop();
            player.Play();
        }
    }

    private bool ShouldUseMobile()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (HatilloIsMobileBrowser() == 1)
        {
            return true;
        }
#endif
        return Screen.width > 0 && Screen.width <= maxCanvasWidthForMobile;
    }

    private static string ToMobileUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return url;
        }

        int extensionIndex = url.LastIndexOf('.');
        return extensionIndex < 0 ? url + MobileSuffix : url.Insert(extensionIndex, MobileSuffix);
    }
}
