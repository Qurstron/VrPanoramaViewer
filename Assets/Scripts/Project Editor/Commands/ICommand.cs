using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICommand
{
    /// <returns>true if command has been succesfully executed</returns>
    public abstract bool Execute(ProjectContext context);
    public abstract void Undo(ProjectContext context);
}
public interface IDirtyCommand : ICommand { }
