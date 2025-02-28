using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using JSONClasses;

public class LineTool : Tool, IDownableAngle, IMovable
{
    [Header("Defaults")]
    public float defaultLineWidth = 0.2f;
    public string defaultColor = "#FFFFFF";

    private LineRenderer lineRenderer = null;
    private LineComponent lineComponent;
    private List<float[]> bufferedInputs = new();

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            OnExit();
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (lineComponent != null)
            {
                Destroy(lineComponent.gameObject);
                lineComponent = null;
            }
        }
    }

    public void Down(Vector2 angle)
    {
        if (lineComponent == null)
        {
            Line line = new()
            {
                width = defaultLineWidth,
                coords = new(),
                color = defaultColor,
                origin = Context.currentNodeContent,
                name = "New Line"
            };

            GameObject go = SphereController.CreateLine(line);
            lineComponent = (LineComponent)go.GetComponent<WorldSelectableContainer>().selectable; // .GetComponentInChildren<LineComponent>();
            lineRenderer = go.GetComponentInChildren<LineRenderer>();
        }

        lineComponent.AddCoord(angle);
    }

    public void Move(Vector2 angle)
    {
        if (lineRenderer == null) return;

        List<float[]> clonedList = new(lineComponent.Coords) { new float[] { angle.x, angle.y } };
        var points = clonedList.Select(coord => lineComponent.Line.flipcoords ? new Vector2(coord[1], coord[0]) : new Vector2(coord[0], coord[1])).ToArray();
        List<Vector3> positions = SphereController.CalcPoints(points, LineStrategy.shortestPath);
        
        lineRenderer.positionCount = positions.Count;
        lineRenderer.SetPositions(positions.ToArray());
    }

    public override void OnExit()
    {
        if (lineComponent == null) return;

        lineComponent.Coords = lineComponent.Coords;
        bufferedInputs.Clear();
        Context.editor.ExecuteCommand(new DeleteWorldObjectCommand(lineComponent.GetPoints(), false));
        Context.editor.ExecuteCommand(new SelectCommand(lineComponent.GetPoints(), true));

        lineComponent = null;
        lineRenderer = null;
    }
}
