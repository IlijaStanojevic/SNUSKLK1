using SNUSKLK1.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SNUSKLK1
{

    public class Config
    {
        public int WorkerCount { get; set; }
        public int MaxQueueSize { get; set; }
        public List<Job> Jobs { get; set; } = new();

        public static Config Load(string path)
        {
            var doc = XDocument.Load(path);

            var config = new Config
            {
                WorkerCount = int.Parse(doc.Root.Element("WorkerCount").Value),
                MaxQueueSize = int.Parse(doc.Root.Element("MaxQueueSize").Value)
            };
            // read xml payload
            foreach (var jobEl in doc.Root.Element("Jobs").Elements("Job"))
            {
                config.Jobs.Add(new Job
                {
                    Id = Guid.NewGuid(),
                    Type = Enum.Parse<JobType>(jobEl.Attribute("Type").Value),
                    Payload = NormalizePayload(jobEl.Attribute("Payload").Value),
                    Priority = int.Parse(jobEl.Attribute("Priority").Value)
                });
            }

            return config;
        }

        private static string NormalizePayload(string raw)
        {


            raw = raw.Replace("_", "");

            if (raw.StartsWith("numbers"))
            {
                var parts = raw.Split(',');

                int max = int.Parse(parts[0].Split(':')[1]);
                int threads = int.Parse(parts[1].Split(':')[1]);

                return $"{max},{threads}";
            }
            else if (raw.StartsWith("delay"))
            {
                return raw.Split(':')[1];
            }
            // uvek je dobar format
            throw new Exception("Invalid payload format");
        }
    }
}
