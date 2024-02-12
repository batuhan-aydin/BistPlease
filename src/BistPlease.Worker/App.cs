using Quartz;

namespace BistPlease.Worker;

public class App
{
    private readonly ISchedulerFactory _schedulerFactory;
    public App(ISchedulerFactory schedulerFactory)
    {
        _schedulerFactory = schedulerFactory;
    }

    public async Task Run(string[] args)
    {
        var scheduler = await _schedulerFactory.GetScheduler();

        // and start it off
        await scheduler.Start();

        // some sleep to show what's happening
        await Task.Delay(Timeout.Infinite);

        // and last shut down the scheduler when you are ready to close your program
        await scheduler.Shutdown();
    }
}
