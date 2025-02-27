using JSONClasses;

public class RenameObjectCommand : IDirtyCommand
{
    private readonly Validatable validatable;
    private readonly string name;
    private string oldName;

    public RenameObjectCommand(Validatable validatable, string name)
    {
        this.validatable = validatable;
        this.name = name;
    }

    public bool Execute(ProjectContext context)
    {
        if (name == validatable.name)
            return false;
        if (string.IsNullOrWhiteSpace(name))
            return false;

        oldName = validatable.name;
        validatable.name = name;
        context.OnNodeContentChange.Invoke();

        return true;
    }

    public void Undo(ProjectContext context)
    {
        validatable.name = oldName;
        context.OnNodeContentChange.Invoke();
    }
}
