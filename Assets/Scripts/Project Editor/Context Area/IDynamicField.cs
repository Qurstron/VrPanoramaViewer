/// <summary>
/// Interface for fields that can be edited in realtime.
/// Typically useful for float fields where the user can drag something.
/// </summary>
public interface IDynamicField<T>
{
    /// <summary>
    /// Sets the field without effecting the JSON.
    /// This usually followed by a SetField call when the value should be saved.
    /// </summary>
    public abstract void SetFieldDynamic(ProjectContext context, T value);
}
