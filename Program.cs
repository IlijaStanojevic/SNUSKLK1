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

        system.JobCompleted += async (job, result) =>
        {
            Console.WriteLine($"COMPLETED {job.Id} = {result}");
            await File.AppendAllTextAsync("log.txt",
                $"{DateTime.Now} COMPLETED {job.Id} {result}\n");
        };

        system.JobFailed += async (job) =>
        {
            Console.WriteLine($"FAILED {job.Id}");
            await File.AppendAllTextAsync("log.txt",
                $"{DateTime.Now} FAILED {job.Id} ABORT\n");
        };

        foreach (var job in config.Jobs)
        {
            system.Submit(job);
        }

        Console.WriteLine($"Loaded {config.Jobs.Count} initial jobs");

        var rand = new Random();

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

        Console.WriteLine("System running... Press ENTER to exit");
        Console.ReadLine();
    }
}