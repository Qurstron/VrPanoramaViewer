using JSONClasses;

public interface IConfigable
{
    public abstract void SetConfig(Config config, bool tryKeepIndices = false);
    public Config Config { get; }
}
