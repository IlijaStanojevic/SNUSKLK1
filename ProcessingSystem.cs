using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SNUSKLK1.Models;
using System.Collections.Concurrent;
namespace SNUSKLK1
{

    public class ProcessingSystem
    {
        private readonly PriorityQueue<Job, int> _queue = new();
        private readonly object _lock = new();
        private readonly SemaphoreSlim _signal = new(0);

        private readonly int _maxQueueSize;

        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<int>> _results = new();

        private readonly ReportService _reportService = new();

        public event Func<Job, int, Task> JobCompleted;
        public event Func<Job, Task> JobFailed;

        public ProcessingSystem(int workerCount, int maxQueueSize)
        {
            _maxQueueSize = maxQueueSize;
            Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(TimeSpan.FromMinutes(1));
                    // za testiranje 10s
                    //await Task.Delay(TimeSpan.FromSeconds(10));
                    _reportService.GenerateReport();
                }
            });

            for (int i = 0; i < workerCount; i++)
            {
                Task.Run(WorkerLoop);
            }
        }

        public JobHandle Submit(Job job)
        {
            lock (_lock)
            {
                // indempotentnost
                if (_results.ContainsKey(job.Id))
                {
                    return new JobHandle
                    {
                        Id = job.Id,
                        Result = _results[job.Id].Task
                    };
                }

                if (_queue.Count >= _maxQueueSize)
                    throw new Exception("Queue full");

                var tcs = new TaskCompletionSource<int>();
                _results[job.Id] = tcs;

                _queue.Enqueue(job, job.Priority);
                _signal.Release();

                return new JobHandle
                {
                    Id = job.Id,
                    Result = tcs.Task
                };
            }
        }

        private async Task WorkerLoop()
        {
            while (true)
            {
                await _signal.WaitAsync();

                Job job;
                // thread safe za dequeue 
                lock (_lock)
                {
                    job = _queue.Dequeue();
                }

                await ProcessJob(job);
            }
        }

        private async Task ProcessJob(Job job)
        {
            int retries = 0;
            while (retries < 3)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();

                try
                {
                    var processingTask = JobExecutor.Execute(job);

                    var completed = await Task.WhenAny(processingTask, Task.Delay(2000));

                    if (completed != processingTask)
                        throw new TimeoutException();

                    int result = await processingTask;

                    sw.Stop();

                    _results[job.Id].SetResult(result);

                    _reportService.Record(job, true, sw.ElapsedMilliseconds);

                    if (JobCompleted != null)
                        await JobCompleted(job, result);

                    return;
                }
                catch
                {
                    sw.Stop();
                    retries++;

                    if (retries >= 3)
                    {
                        _results[job.Id].SetResult(-1);

                        _reportService.Record(job, false, sw.ElapsedMilliseconds);

                        if (JobFailed != null)
                            await JobFailed(job);
                    }
                }
            }
        }

        public IEnumerable<Job> GetTopJobs(int n)
        {
            lock (_lock)
            {
                return _queue.UnorderedItems
                    .OrderBy(x => x.Priority)
                    .Take(n)
                    .Select(x => x.Element)
                    .ToList();
            }
        }

        public Job GetJob(Guid id)
        {
            lock (_lock)
            {
                return _queue.UnorderedItems
                    .Select(x => x.Element)
                    .FirstOrDefault(j => j.Id == id);
            }
        }
    }

}
