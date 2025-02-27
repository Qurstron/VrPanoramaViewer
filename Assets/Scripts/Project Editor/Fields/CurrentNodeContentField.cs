using JSONClasses;
using System;
using Unity.Mathematics;
using Unity.Mathematics.Geometry;
using UnityEngine;

[CreateAssetMenu(fileName = "CurrentNodeContentField", menuName = "ConfigFields/CurrentNodeContentField")]
public class CurrentNodeContentField : ConfigField<int>
{
    // this is -1, because of how the index is handelt and indicates the first NodeContent of the current node
    private int index = -1;
    private Node bufferedNode = null;

    private void OnEnable()
    {
        index = -1;
        bufferedNode = null;
    }
    public override int GetField(ProjectContext context)
    {
        return index;
    }
    public override int SetField(ProjectContext context, int value)
    {
        index = value;

        if (index < 0)
        {
            int indexCalc = -index - 1;

            context.currentNode ??= bufferedNode;
            //bufferedNode = context.currentNode;

            for (int i = context.currentNode.content.Count; i <= indexCalc; i++)
            {
                NodeContent nc = new()
                {
                    node = context.currentNode,
                    indexInNode = i
                };
                context.currentNode.content.Add(nc);
            }
            context.currentNodeContent = context.currentNode.content[indexCalc];
            context.OnNodeContentChange.Invoke();
        }
        else
        {
            bufferedNode = context.currentNode;
            context.currentNodeContent = context.Config.categoryParents[index];
            context.currentNode = null;
            context.OnNodeChange.Invoke();
        }

        onFieldChange.Invoke(value);
        return value;
    }
}
