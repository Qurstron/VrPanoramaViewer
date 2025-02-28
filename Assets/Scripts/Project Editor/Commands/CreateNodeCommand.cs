using JSONClasses;

public class CreateNodeCommand : CDCommand
{
    Node node;

    public CreateNodeCommand(Node node, bool deleteNode = false) : base(deleteNode)
    {
        this.node = node;
    }

    public override bool CExecute(ProjectContext context)
    {
        if (node.content.Count <= 0)
        {
            node.content.Add(new()
            {
                node = node,
            });
        }
        if (string.IsNullOrEmpty(node.uniqueName))
            node.uniqueName = context.Config.GenerateUniqueName();
        if (string.IsNullOrEmpty(node.name))
        {
            string templateName = "New Node";
            int occurrences = context.Config.nodes.FindAll(node => node.name.Equals(templateName)).Count;
            node.name = templateName + (occurrences < 1 ? "" : " " + (occurrences + 1).ToString());
        }

        node.config ??= context.Config;

        context.Config.nodes.Add(node);
        return true;
    }
    public override void CUndo(ProjectContext context)
    {
        context.Config.nodes.Remove(node);
    }

    public override bool DExecute(ProjectContext context)
    {
        if (context.Config.nodes.Count <= 1) return false;

        context.Config.nodes.Remove(node);
        return true;
    }
    public override void DUndo(ProjectContext context)
    {
        context.Config.nodes.Add(node);
    }
}
