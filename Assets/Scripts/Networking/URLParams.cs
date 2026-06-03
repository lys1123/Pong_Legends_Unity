using System.Runtime.InteropServices;
using UnityEngine;

namespace PongLegends
{
    public static class URLParams
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern string GetURLParam(string key);
        [DllImport("__Internal")] private static extern void   CopyTextToClipboard(string text);
        [DllImport("__Internal")] private static extern string GetPageOrigin();
#endif

        public static string Get(string key)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return GetURLParam(key) ?? string.Empty;
#else
            return string.Empty;
#endif
        }

        public static void CopyToClipboard(string text)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            CopyTextToClipboard(text);
#else
            GUIUtility.systemCopyBuffer = text;
#endif
        }

        // Returns e.g. "https://mygame.com/play" — the page URL without query string.
        public static string PageOrigin()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return GetPageOrigin() ?? string.Empty;
#else
            return "http://localhost";
#endif
        }
    }
}
