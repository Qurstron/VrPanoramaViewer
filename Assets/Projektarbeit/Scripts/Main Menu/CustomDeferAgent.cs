using GLTFast;
using System.Threading.Tasks;

public class CustomDeferAgent : IDeferAgent
{
    public async Task BreakPoint()
    {
        await Task.Yield();
    }

    public async Task BreakPoint(float duration)
    {
        await Task.Yield();
    }

    public bool ShouldDefer()
    {
        return true;
    }

    public bool ShouldDefer(float duration)
    {
        return true;
    }
}
