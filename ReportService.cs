using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using SNUSKLK1.Models;
using System.Xml.Linq;

namespace SNUSKLK1
{
    public class JobExecutionInfo
    {
        public JobType Type { get; set; }
        public bool Success { get; set; }
        public long DurationMs { get; set; }
    }

    public class ReportService
    {
        private readonly ConcurrentBag<JobExecutionInfo> _history = new();

        public void Record(Job job, bool success, long durationMs)
        {
            _history.Add(new JobExecutionInfo
            {
                Type = job.Type,
                Success = success,
                DurationMs = durationMs
            });
        }

        public void GenerateReport()
        {
            var now = DateTime.Now;

            var grouped = _history.GroupBy(x => x.Type);

            var report = new XElement("Report",
                new XAttribute("timestamp", now));
            // izvestaj format
            foreach (var g in grouped)
            {
                var typeEl = new XElement("JobType",
                    new XAttribute("type", g.Key),
                    new XElement("Total", g.Count()),
                    new XElement("Success", g.Count(x => x.Success)),
                    new XElement("Failed", g.Count(x => !x.Success)),
                    new XElement("AverageDurationMs",
                        g.Where(x => x.Success).DefaultIfEmpty()
                         .Average(x => x == null ? 0 : x.DurationMs))
                );

                report.Add(typeEl);
            }

            SaveWithRotation(report);
        }

        private void SaveWithRotation(XElement report)
        {
            string dir = "reports";
            Directory.CreateDirectory(dir);

            var files = Directory.GetFiles(dir, "report_*.xml")
                                 .OrderBy(f => f)
                                 .ToList();
            // delete oldest report
            if (files.Count >= 10)
            {
                File.Delete(files.First()); 
            }

            string fileName = $"{dir}/report_{DateTime.Now:yyyyMMdd_HHmmss}.xml";
            report.Save(fileName);
        }
    }
}
