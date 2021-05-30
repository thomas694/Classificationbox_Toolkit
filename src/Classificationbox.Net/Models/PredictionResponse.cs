using System.Collections.Generic;

namespace Classificationbox.Net.Models
{
    public class PredictionResponse
    {
        public bool success { get; set; }

        public List<ScoreClass> classes { get; set; }

        public string error { get; set; }
    }
}
