using JSONClasses;
using System;
using TMPro;
using UnityEngine;

public class InfoPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text nodes;
    [SerializeField] private TMP_Text size;
    [SerializeField] private TMP_Text error;

    private void Start()
    {
        gameObject.SetActive(false);
    }

    public void DisplayInfo(PanoramaMenuEntry entry)
    {
        nodes.text = entry.nodeCount.ToString();
        size.text = SizeSuffix(entry.size, 0);

        if (entry.error == PanoramaMenuEntry.Error.Validation) error.text = entry.config.GetAllProblems();
        else if (entry.error == PanoramaMenuEntry.Error.Undefined) error.text = entry.customError;
        else if (entry.HasError) error.text = $"Error: {entry.error}";
        else error.text = "";

        gameObject.SetActive(true);
        QUtils.CreateUIBlocker(GetComponent<Canvas>(), blocker =>
        {
            gameObject.SetActive(false);
            Destroy(blocker);
        });
    }

    // https://stackoverflow.com/questions/14488796/does-net-provide-an-easy-way-convert-bytes-to-kb-mb-gb-etc
    static readonly string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
    static string SizeSuffix(Int64 value, int decimalPlaces = 1)
    {
        if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException("decimalPlaces"); }
        if (value < 0) { return "-" + SizeSuffix(-value, decimalPlaces); }
        if (value == 0) { return string.Format("{0:n" + decimalPlaces + "} bytes", 0); }

        // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
        int mag = (int)Math.Log(value, 1024);

        // 1L << (mag * 10) == 2 ^ (10 * mag) 
        // [i.e. the number of bytes in the unit corresponding to mag]
        decimal adjustedSize = (decimal)value / (1L << (mag * 10));

        // make adjustment when the value is large enough that
        // it would round up to 1000 or more
        if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
        {
            mag += 1;
            adjustedSize /= 1024;
        }

        return string.Format("{0:n" + decimalPlaces + "} {1}",
            adjustedSize,
            SizeSuffixes[mag]);
    }
}
