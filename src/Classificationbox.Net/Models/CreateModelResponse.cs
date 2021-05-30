using System.Collections.Generic;

namespace Classificationbox.Net.Models
{
    public class CreateModelResponse
    {
        public bool success { get; set; }

        public string id { get; set; }

        public string name { get; set; }

        public List<string> classes { get; set; }

        public string error { get; set; }
    }
}
