using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using JSONClasses;

public class LabelTool : Tool, IDownableAngle
{
    public void Down(Vector2 angle)
    {
        var label = new Label
        {
            pos = new float[] { angle.x, angle.y },
            origin = Context.currentNodeContent,
            name = "New Label"
        };
        var labelComponent = SphereController.CreateLabel(label).GetComponentInChildren<WorldSelectableContainer>().selectable;

        Context.editor.ExecuteCommand(new DeleteWorldObjectCommand(labelComponent.GetPoints(), false));
        Context.editor.ExecuteCommand(new SelectCommand(labelComponent.GetPoints(), true));
    }
}
