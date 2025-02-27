using JSONClasses;
using UnityEngine;

public class ExecludeAddOn : ConfigActor
{
    public void ExcludeCurrentAddOn()
    {
        if (Context.selectedObjects.Count != 1) return;
        AddOn addOn = Context.selectedObjects[0].AddOn;
        if (addOn == null) return;
        
        Context.editor.ExecuteCommand(new CDExcludeCommand(addOn, false));
    }
}
