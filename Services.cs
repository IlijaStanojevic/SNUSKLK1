using SNUSKLK1.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SNUSKLK1
{
    public static class JobExecutor
    {
        public static Task<int> Execute(Job job)
        {
            return job.Type switch
            {
                JobType.Prime => Task.Run(() => ProcessPrime(job.Payload)),
                JobType.IO => Task.Run(() => ProcessIO(job.Payload)),
                _ => throw new NotImplementedException()
            };
        }

        private static int ProcessIO(string payload)
        {
            int delay = int.Parse(payload);
            Thread.Sleep(delay);
            return new Random().Next(0, 101);
        }
        public static int ProcessPrime(string payload)
        {
            var parts = payload.Split(',');
            int max = int.Parse(parts[0]);
            int threads = Math.Clamp(int.Parse(parts[1]), 1, 8);

            int count = 0;

            Parallel.For(2, max + 1, new ParallelOptions
            {
                MaxDegreeOfParallelism = threads
            },
            i =>
            {
                if (IsPrime(i))
                    Interlocked.Increment(ref count);
            });

            return count;
        }

        private static bool IsPrime(int n)
        {
            if (n < 2) return false;

            for (int i = 2; i * i <= n; i++)
                if (n % i == 0) return false;

            return true;
        }
    }

}
