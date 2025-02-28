using JSONClasses;
using UnityEngine;

public class CreateEdgeCommand : IDirtyCommand
{
    private Node n1, n2;

    public CreateEdgeCommand(Node n1, Node n2)
    {
        this.n1 = n1;
        this.n2 = n2;
    }

    public bool Execute(ProjectContext context)
    {
        if (n1 == n2) return false;
        if (n1.neighbors.Contains(n2.uniqueName) || n2.neighbors.Contains(n1.uniqueName)) return false;

        n1.neighbors.Add(n2.uniqueName);
        n2.neighbors.Add(n1.uniqueName);

        return true;
    }

    public void Undo(ProjectContext context)
    {
        n1.neighbors.Remove(n2.uniqueName);
        n2.neighbors.Remove(n1.uniqueName);
    }
}
