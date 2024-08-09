using UnityEngine;

public static class ClipboardHelper
{
    public static void CopyToClipboard(string text)
    {
        if (!string.IsNullOrEmpty(text))
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            // Use Unity's built-in clipboard support
            GUIUtility.systemCopyBuffer = text;
#elif UNITY_STANDALONE_WIN
            // Fallback for Windows Standalone if needed
            CopyToClipboardWindows(text);
#else
            Debug.LogWarning("Clipboard copying is not supported on this platform.");
#endif
        }
    }

#if UNITY_STANDALONE_WIN
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool OpenClipboard(System.IntPtr hWndNewOwner);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool CloseClipboard();

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool SetClipboardData(uint uFormat, System.IntPtr hMem);

    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
    private static extern System.IntPtr GlobalAlloc(uint uFlags, System.UIntPtr dwBytes);

    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
    private static extern System.IntPtr GlobalLock(System.IntPtr hMem);

    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalUnlock(System.IntPtr hMem);

    private const uint CF_UNICODETEXT = 13;
    private const uint GMEM_MOVEABLE = 0x0002;

    private static void CopyToClipboardWindows(string text)
    {
        OpenClipboard(System.IntPtr.Zero);
        System.IntPtr hGlobal = GlobalAlloc(GMEM_MOVEABLE, (System.UIntPtr)((text.Length + 1) * 2));
        System.IntPtr pGlobal = GlobalLock(hGlobal);

        for (int i = 0; i < text.Length; i++)
        {
            System.Runtime.InteropServices.Marshal.WriteInt16(pGlobal, i * 2, text[i]);
        }

        System.Runtime.InteropServices.Marshal.WriteInt16(pGlobal, text.Length * 2, 0); // Null-terminate the string
        GlobalUnlock(hGlobal);
        SetClipboardData(CF_UNICODETEXT, hGlobal);
        CloseClipboard();
    }
#endif
}