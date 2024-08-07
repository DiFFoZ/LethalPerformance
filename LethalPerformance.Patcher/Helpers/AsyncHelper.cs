using System;
using System.Threading.Tasks;

namespace LethalPerformance.Patcher.Helpers;
internal static class AsyncHelper
{
    public static void Schedule(Func<Task> func)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await func();
            }
            catch (Exception ex)
            {
                LethalPerformancePatcher.Logger.LogError(ex);
            }
        });
    }

    public static void Schedule<T>(Func<T, Task> func, T param1)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await func(param1);
            }
            catch (Exception ex)
            {
                LethalPerformancePatcher.Logger.LogError(ex);
            }
        });
    }
}
