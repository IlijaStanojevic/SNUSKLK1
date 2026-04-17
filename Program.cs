using SNUSKLK1.Models;
using SNUSKLK1;

public class Program
{
    static void Main(string[] args)
    {
        var config = Config.Load("../../../../SystemConfig.xml");

        var system = new ProcessingSystem(
            config.WorkerCount,
            config.MaxQueueSize);
        // success job event
        system.JobCompleted += async (job, result) =>
        {
            Console.WriteLine($"COMPLETED {job.Id} = {result}");
            await File.AppendAllTextAsync("log.txt",
                $"{DateTime.Now} COMPLETED {job.Id} {result}\n");
        };
        // failed job event
        system.JobFailed += async (job) =>
        {
            Console.WriteLine($"FAILED {job.Id}");
            await File.AppendAllTextAsync("log.txt",
                $"{DateTime.Now} FAILED {job.Id} ABORT\n");
        };

        // initial jobs - iz config file
        foreach (var job in config.Jobs)
        {
            system.Submit(job);
        }

        Console.WriteLine($"Loaded {config.Jobs.Count} initial jobs");

/*        Console.WriteLine("TOP 3 JOBS: ");
        var topJobs = system.GetTopJobs(3);

        foreach (var job in topJobs)
        {
            Console.WriteLine($"{job.Id} | {job.Type} | Priority: {job.Priority} | Payload: {job.Payload}");
        }*/


        var rand = new Random();
        // random job lopp
        for (int i = 0; i < config.WorkerCount; i++)
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    var job = new Job
                    {
                        Id = Guid.NewGuid(),
                        Type = rand.Next(2) == 0 ? JobType.Prime : JobType.IO,
                        Priority = rand.Next(1, 5)
                    };

                    if (job.Type == JobType.Prime)
                    {
                        job.Payload = $"{rand.Next(5000, 15000)},{rand.Next(1, 4)}";
                    }
                    else
                    {
                        job.Payload = $"{rand.Next(200, 3000)}";
                    }

                    try
                    {
                        system.Submit(job);
                        Console.WriteLine("Submitted random job");
                    }
                    catch
                    {
                        Console.WriteLine("Queue full, skipping...");
                    }

                    await Task.Delay(rand.Next(300, 1000));
                }
            });
        }

        RunTest(system).GetAwaiter().GetResult();
        Console.WriteLine("System running... Press ENTER to exit");
        Console.ReadLine();
    }

    // testiranje
    static async Task RunTest(ProcessingSystem system)
    {
        Console.WriteLine("Running test...");

        var job = new Job
        {
            Id = Guid.NewGuid(),
            Type = JobType.IO,
            Payload = "500",
            Priority = 1
        };

        var handle = system.Submit(job);

        int result = await handle.Result;

        Console.WriteLine($"TEST DONE: {result}");
    }
}