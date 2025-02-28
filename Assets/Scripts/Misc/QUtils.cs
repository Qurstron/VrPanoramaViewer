using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Color = UnityEngine.Color;
using ColorUtility = UnityEngine.ColorUtility;

/// <summary>
/// Collection of useful functions
/// </summary>
public static class QUtils
{
#if UNITY_STANDALONE_WIN
    [DllImport("user32.dll", EntryPoint = "SetWindowText")]
    public static extern bool SetWindowText(IntPtr hwnd, string lpString);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
#endif
    private static string originalWindowTitle;
    private static string windowTitle;

    /// <summary>
    /// Parses a string to a UnityEngine.Color
    /// </summary>
    /// <param name="str">The string to be parsed</param>
    /// <returns>The parsed color</returns>
    /// <exception cref="Exception"></exception>
    public static Color StringToColor(string str)
    {
        // TryParseHtmlString needs a '#' before the color
        if (!str.StartsWith("#")) str = "#" + str;
        if (ColorUtility.TryParseHtmlString(str, out Color color)) return color;
        else throw new Exception($"Could not convert Color: {color} to Unity Color object");
    }
    /// <summary>
    /// Adds a Component to a GameObject if it dosn't already contians it
    /// </summary>
    /// <returns>The attatched component</returns>
    public static T GetOrAddComponent<T>(Component component) where T : Component
    {
        return GetOrAddComponent<T>(component.gameObject);
    }
    /// <summary>
    /// Adds a Component to a GameObject if it dosn't already contians it
    /// </summary>
    /// <returns>The attatched component</returns>
    public static T GetOrAddComponent<T>(GameObject go) where T : Component
    {
        T comp = go.GetComponent<T>();
        if (!comp)
            comp = go.AddComponent<T>();
        return comp;
    }
    /// <summary>
    /// A modified copy from TMP_Dropdown.cs CreateBlocker(...).
    /// Creates a blocker below a specified Canvas, so that click can be detected and blocked
    /// </summary>
    /// <param name="top">The Cavnas that should not be blocked. Needs to have a SortingOrder of anything grater than 0</param>
    /// <param name="onBlock">
    /// Callback when a Click is blocked by the blocker.
    /// First argument is the blocker GameObject.
    /// </param>
    /// <returns>The Blocker Object</returns>
    public static GameObject CreateUIBlocker(Canvas top, UnityAction<GameObject> onBlock)
    {
        Canvas rootCanvas = top.transform.root.GetComponent<Canvas>();

        // Create blocker GameObject.
        GameObject blocker = new GameObject("Blocker");

        // Setup blocker RectTransform to cover entire root canvas area.
        RectTransform blockerRect = blocker.AddComponent<RectTransform>();
        blockerRect.SetParent(rootCanvas.transform, false);
        blockerRect.anchorMin = Vector3.zero;
        blockerRect.anchorMax = Vector3.one;
        blockerRect.sizeDelta = Vector2.zero;

        // Make blocker be in separate canvas in same layer as dropdown and in layer just below it.
        Canvas blockerCanvas = blocker.AddComponent<Canvas>();
        blockerCanvas.overrideSorting = true;
        //Canvas dropdownCanvas = m_Dropdown.GetComponent<Canvas>();
        blockerCanvas.sortingLayerID = top.sortingLayerID;
        blockerCanvas.sortingOrder = top.sortingOrder - 1;

        // Find the Canvas that this dropdown is a part of
        Canvas parentCanvas = null;
        Transform parentTransform = top.transform.parent;
        while (parentTransform != null)
        {
            parentCanvas = parentTransform.GetComponent<Canvas>();
            if (parentCanvas != null)
                break;

            parentTransform = parentTransform.parent;
        }

        // If we have a parent canvas, apply the same raycasters as the parent for consistency.
        if (parentCanvas != null)
        {
            Component[] components = parentCanvas.GetComponents<BaseRaycaster>();
            for (int i = 0; i < components.Length; i++)
            {
                Type raycasterType = components[i].GetType();
                if (blocker.GetComponent(raycasterType) == null)
                {
                    blocker.AddComponent(raycasterType);
                }
            }
        }
        else
        {
            // Add raycaster since it's needed to block.
            GetOrAddComponent<GraphicRaycaster>(blocker);
        }

        // Add image since it's needed to block, but make it clear.
        Image blockerImage = blocker.AddComponent<Image>();
        blockerImage.color = Color.clear;

        // Add button since it's needed to block, and to close the dropdown when blocking area is clicked.
        Button blockerButton = blocker.AddComponent<Button>();
        blockerButton.onClick.AddListener(() => onBlock(blocker));

        return blocker;
    }
    /// <summary>
    /// Formats a hexadecimal string according to the flags
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public static string FormatHexColor(Color color, ColorHexFlags flags = defaultColorHexFlags)
    {
        return FormatHexColor(color.ToHexString(), flags);
    }
    /// <summary>
    /// Formats a hexadecimal string according to the flags
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public static string FormatHexColor(string hex, ColorHexFlags flags = defaultColorHexFlags)
    {
        if (string.IsNullOrEmpty(hex)) throw new ArgumentNullException(nameof(hex));
        if (hex.Length > 9) hex = hex[..9];
        if (!Regex.IsMatch(hex, @"^#?(?:[0-9a-fA-F]{3,4}){1,2}$")) throw new ArgumentException("String is not of format html color");
        
        if (hex[0] != '#') hex = '#' + hex;
        hex.ToUpper();
        // Expand compact hex
        if (Regex.IsMatch(hex, @"^#?(?:[0-9a-fA-F]{3,4})$"))
        {
            string newHex = "";
            for (int i = hex[0] == '#' ? 1 : 0; i < hex.Length; i++)
            {
                newHex += hex[i] + hex[i];
            }
            hex = newHex;
        }
        if (hex.Length <= 7) hex += "FF";

        // Process flags
        if (flags.HasFlag(ColorHexFlags.NoAlpha))
        {
            hex = hex[..^2];
        }
        else if (flags.HasFlag(ColorHexFlags.TryNoAlpha))
        {
            if (hex[^1] == 'F' && hex[^2] == 'F') hex = hex[..^2];
        }
        if (flags.HasFlag(ColorHexFlags.TryCompact))
        {
            string newHex = "";
            bool isCompactable = true;
            for (int i = 0; i < hex.Length - 1; i += 2)
            {
                if (hex[i] == hex[i + 1]) newHex += hex[i];
                else
                {
                    isCompactable = false;
                    break;
                }
            }
            if (isCompactable) hex = newHex;
        }
        if (flags.HasFlag(ColorHexFlags.LeadingHashtag))
        {
            if (hex[0] != '#') hex.Prepend('#');
        }
        if (flags.HasFlag(ColorHexFlags.Lowercase))
        {
            hex.ToLower();
        }

        return hex;
    }
    public const ColorHexFlags defaultColorHexFlags = ColorHexFlags.LeadingHashtag | ColorHexFlags.TryNoAlpha;
    public enum ColorHexFlags
    {
        LeadingHashtag = 1,
        /// <summary>Removes the Alpha component from hex string if Alpha is 1</summary>
        TryNoAlpha = 2,
        /// <summary>Removes the Alpha component. Overrides TryNoAlpha</summary>
        NoAlpha = 4,
        /// <summary>Shortens the hex string if no information is lost</summary>
        TryCompact = 8,
        Lowercase = 16,
    }
    /// <summary>
    /// Changes the Window Title to a specified one
    /// </summary>
    /// <param name="title">If null resets to project name</param>
    /// <remarks>Currently only works on windows</remarks>
    public static void ChangeWindowTitle(string title)
    {
#if UNITY_EDITOR
        Debug.Log($"Change Title to {title}");
        return;
#endif
#if UNITY_STANDALONE_WIN
        if (string.IsNullOrEmpty(windowTitle)) windowTitle = Application.productName;
        if (windowTitle.Equals(title)) return;

        // https://issuetracker.unity3d.com/issues/system-dot-diagnostics-dot-process-dot-getcurrentprocess-dot-mainwindowhandle-always-returns-0-on-builds
        IntPtr windowPtr = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;

        if (string.IsNullOrEmpty(originalWindowTitle))
        {
            StringBuilder sb = new();
            GetWindowText(windowPtr, sb, 512);
            originalWindowTitle = sb.ToString();
        }
        
        SetWindowText(windowPtr, title);
        windowTitle = title;
#else
        Debug.LogError("Renaming the Windos is currently only available on Windows");
#endif
    }
    /// <summary>
    /// Restores the window title if it was changed with ChangeWindowTitle(...)
    /// </summary>
    public static void ResetWindowTitle()
    {
        ChangeWindowTitle(originalWindowTitle);
    }

    /// <summary>
    /// Calculates the luminance of a color
    /// </summary>
    public static float GetLuminance(Color color)
    {
        // https://stackoverflow.com/questions/3942878/how-to-decide-font-color-in-white-or-black-depending-on-background-color
        float luminance = 0;
        luminance += 0.2126f * ((color.r <= 0.04045f) ? color.r / 12.92f : Mathf.Pow((color.r + 0.055f) / 1.055f, 2.4f));
        luminance += 0.7152f * ((color.g <= 0.04045f) ? color.g / 12.92f : Mathf.Pow((color.g + 0.055f) / 1.055f, 2.4f));
        luminance += 0.0722f * ((color.b <= 0.04045f) ? color.b / 12.92f : Mathf.Pow((color.b + 0.055f) / 1.055f, 2.4f));
        return luminance;
    }
    //https://stackoverflow.com/questions/1395205/better-way-to-check-if-a-path-is-a-file-or-a-directory
    /// <summary>
    /// Checks if a source is located in a destination
    /// </summary>
    public static bool SrcPathContainsDestPath(string src, string dest)
    {
        DirectoryInfo di1 = new DirectoryInfo(src);
        DirectoryInfo di2 = new DirectoryInfo(dest);
        bool isParent = false;
        while (di1 != null)
        {
            if (di1.FullName == di2.FullName)
            {
                isParent = true;
                break;
            }
            else di1 = di1.Parent;
        }
        return isParent;
    }
}
