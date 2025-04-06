using System.Collections.Concurrent;
using System.Threading.Tasks;
using VolumetricSelection2077.Services;

namespace VolumetricSelection2077.TestingStuff;

public class ManualTaskPlayground
{
    private static ConcurrentQueue<TaskCompletionSource<string?>> tasks = new();
    
    private static void RunLoop()
    {
        while (true)
        {
            Task.Delay(1000).Wait();
            Logger.Info("Running...");
            if (tasks.TryDequeue(out var task))
            {
                task.SetResult("result");
                Logger.Info("Done task...");
            }
            else
            {
                Logger.Info("No tasks to run.");
            }
        }
    }

    private static async Task<string?> enqueueTask()
    {
        var task = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        Logger.Info($"Created new task, status is: {task.Task.IsCompleted}");
        tasks.Enqueue(task);
        Logger.Info("Waiting for tasks to complete");
        return await task.Task;
    }
    
    
    public static async Task Run()
    {
        Logger.Info("Starting playground");
        _ = Task.Run(() => RunLoop());
        
        Logger.Info("Enqueuing task");
        var result = await enqueueTask();
        Logger.Info($"Completed task: {result}");
    }
}