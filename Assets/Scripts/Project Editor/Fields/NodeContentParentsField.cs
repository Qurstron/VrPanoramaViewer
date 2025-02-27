using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "NodeContentParentField", menuName = "ConfigFields/NodeContentParentField")]
public class NodeContentParentField : ConfigField<List<int>>
{
    public override List<int> GetField(ProjectContext context)
    {
        return context.currentNodeContent.categoryParentIndices;
    }
    public override List<int> SetField(ProjectContext context, List<int> value)
    {
        if (context.currentNodeContent.categoryParentIndices.SequenceEqual(value))
            return context.currentNodeContent.categoryParentIndices;

        context.currentNodeContent.categoryParentIndices = value;
        onFieldChange.Invoke(value);

        // A check if the ParentIndices change acutally affects the NC would be nice
        context.OnNodeContentChange.Invoke();

        return value;
    }
}
