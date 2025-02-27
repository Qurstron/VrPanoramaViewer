using System.IO;
using UnityEngine;

public class FileInputField : StringInputField
{
    [SerializeField] private string relativeDestination;
    [SerializeField] private SelectFileButtonExtension selectFileButton;
    [SerializeField] private bool rejectForginFiles = false;

    protected override void OnContextChange()
    {
        base.OnContextChange();
        selectFileButton.defaultPath = Path.Combine(Context.Config.path, relativeDestination);
        Debug.Log("Pain");
    }

    public override void Submit(string value)
    {
        string localPath;

        if (QUtils.SrcPathContainsDestPath(value, Context.Config.path))
        {
            localPath = Path.GetRelativePath(Context.Config.path, value);
        }
        else if (rejectForginFiles)
        {
            throw new System.Exception("Forgin files not allowed");
        }
        else
        {
            localPath = Path.Combine(relativeDestination, Path.GetFileName(value));
            string fullPath = Path.Combine(Context.Config.path, localPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.Copy(value, fullPath);
        }

        base.Submit(localPath);
    }
}
