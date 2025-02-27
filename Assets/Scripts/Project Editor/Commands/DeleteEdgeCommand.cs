using JSONClasses;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DeleteEdgeCommand : IDirtyCommand
{
    private readonly Node n1;
    private readonly Node n2;
    private readonly bool surpressUpdate;
    private List<List<int>> indicesN1, indicesN2;

    public DeleteEdgeCommand(Node n1, Node n2, bool surpressUpdate = false)
    {
        this.n1 = n1;
        this.n2 = n2;
        this.surpressUpdate = surpressUpdate;
    }

    public bool Execute(ProjectContext context)
    {
        if (n1 == n2) return false;

        indicesN1 = n1.content.Select(c => new List<int>(c.categoryParentIndices)).ToList();
        indicesN2 = n2.content.Select(c => new List<int>(c.categoryParentIndices)).ToList();

        bool wasChanged = ShiftContentParents(n1, n2.uniqueName) || ShiftContentParents(n2, n1.uniqueName);
        n1.neighbors.Remove(n2.uniqueName);
        n2.neighbors.Remove(n1.uniqueName);

        if (wasChanged && !surpressUpdate)
            context.OnNodeContentChange.Invoke();

        return true;
    }

    public void Undo(ProjectContext context)
    {
        n1.neighbors.Add(n2.uniqueName);
        n2.neighbors.Add(n1.uniqueName);

        for (int i = 0; i < n1.content.Count; i++)
        {
            n1.content[i].categoryParentIndices = indicesN1[i];
        }
        for (int i = 0; i < n2.content.Count; i++)
        {
            n2.content[i].categoryParentIndices = indicesN2[i];
        }

        context.OnNodeContentChange.Invoke();
    }

    /// <summary>
    /// Removes the neighbor node index from node and shifts the categoryParentIndices accordingly
    /// </summary>
    /// <returns>True if a NodeContent was changed in the process</returns>
    private bool ShiftContentParents(Node node, string neighbor)
    {
        int neighborIndex = -node.neighbors.FindIndex(n => n.Equals(neighbor)) - 1;
        bool result = false;

        foreach (NodeContent content in node.content)
        {
            if (content.categoryParentIndices.RemoveAll(i => i == neighborIndex) > 0)
                result = true;

            for (int i = 0; i < content.categoryParentIndices.Count; i++)
            {
                if (content.categoryParentIndices[i] < neighborIndex)
                {
                    content.categoryParentIndices[i]++;
                }
            }
        }

        return result;
    }
}
