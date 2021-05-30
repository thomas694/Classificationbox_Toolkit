using System.Collections.Generic;

namespace Classificationbox.Net.Models
{
    public class ModelStatistics
    {
        public bool success { get; set; }

        public int examples { get; set; }

        public int predictions { get; set; }

        public List<StatisticsClass> classes { get; set; }

        public string error { get; set; }
    }
}
