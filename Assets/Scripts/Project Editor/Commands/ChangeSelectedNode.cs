using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JSONClasses;
using System;

public class ChangeSelectedNode : ICommand
{
    private Node node;
    private Node oldNode;

    public ChangeSelectedNode(Node node)
    {
        this.node = node;
    }

    public bool Execute(ProjectContext context)
    {
        oldNode = context.currentNode;
        if (node == oldNode) return false;
        SetCurrentNode(context, node);

        return true;
    }

    public void Undo(ProjectContext context)
    {
        SetCurrentNode(context, oldNode);
    }

    private void SetCurrentNode(ProjectContext context, Node node)
    {
        int index = Math.Max(context.currentNodeContent.indexInNode, 0);

        node.content ??= new();
        if (node.content.Count <= index)
            node.content.Add(new());

        context.currentNode = node;
        context.currentNodeContent = node.content[index];
        context.OnNodeChange.Invoke();
    }
}
