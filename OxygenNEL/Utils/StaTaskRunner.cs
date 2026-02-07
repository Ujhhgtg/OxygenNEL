using System;
using System.Threading;
using System.Threading.Tasks;

namespace OxygenNEL.Utils;

public static class StaTaskRunner
{
    public static Task<T> RunOnStaAsync<T>(Func<T> func)
    {
        var tcs = new TaskCompletionSource<T>();
        var thread = new Thread(() =>
        {
            try
            {
                var r = func();
                tcs.TrySetResult(r);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        })
        {
            IsBackground = true
        };
        try { thread.SetApartmentState(ApartmentState.STA); } catch { }
        thread.Start();
        return tcs.Task;
    }
}
