// See https://aka.ms/new-console-template for more information
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

        Console.WriteLine("System running... Press ENTER to exit");
        Console.ReadLine(); 
    }



}
