using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JSONClasses;

public class ChangeRootNodeCommand : ICommand
{
    private Node rootNode;
    private Node oldRootNode;

    public ChangeRootNodeCommand(Node rootNode)
    {
        this.rootNode = rootNode;
    }

    public bool Execute(ProjectContext context)
    {
        oldRootNode = context.Config.rootNode;
        context.Config.rootNode = rootNode;
        return true;
    }

    public void Undo(ProjectContext context)
    {
        context.Config.rootNode = oldRootNode;
    }
}
