using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Executes multiple commands as one, which also only creates one undo entry
/// </summary>
public class MultiCommand : IDirtyCommand
{
    private readonly List<ICommand> commands;
    private readonly bool ignoreRefuses;
    private readonly List<ICommand> successfulCommands = new();
    private readonly string name;

    /// <param name="ignoreRefuses">
    /// Ignores if an individiual command fails, 
    /// but the multicommand will still fail if all commands fail
    /// </param>
    public MultiCommand(string name, bool ignoreRefuses = false, params ICommand[] commands)
    {
        this.name = name;
        this.commands = new(commands);
        this.ignoreRefuses = ignoreRefuses;

        if (commands.Length <= 1)
            Debug.LogWarning("MultiCommand with only 1 or less commands created. Is this intentional?");
    }

    public bool Execute(ProjectContext context)
    {
        if (commands.Count <= 0) return false;

        for (int i = 0; i < commands.Count; i++)
        {
            if (commands[i].Execute(context))
            {
                successfulCommands.Add(commands[i]);
            }
            else if (!ignoreRefuses)
            {
                for (int j = i - 1; j >= 0; j--)
                {
                    commands[j].Undo(context);
                }
                return false;
            }
        }

        return successfulCommands.Count > 0;
    }
    public void Undo(ProjectContext context)
    {
        foreach (ICommand command in successfulCommands.AsEnumerable().Reverse())
        {
            command.Undo(context);
        }
        successfulCommands.Clear();
        //for(int i = commands.Length - 1; i >= 0; i--)
        //{
        //    commands[i].Undo(context);
        //}
    }

    public void AppendCommand(ICommand command)
    {
        commands.Add(command);
    }

    public override string ToString()
    {
        return $"MultiCommand {name} ({commands.Count})";
    }
}
