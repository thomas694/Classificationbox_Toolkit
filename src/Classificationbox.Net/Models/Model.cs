using System.Collections.Generic;

namespace Classificationbox.Net.Models
{
    public class Model
    {
        public string id { get; set; }

        public string name { get; set; }

        public ModelOptions options { get; set; }

        public List<string> classes { get; set; }
    }
}
