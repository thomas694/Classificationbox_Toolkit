using System.Collections.Generic;

namespace Classificationbox.Net.Models
{
    public class ListModelsResponse
    {
        public bool success { get; set; }

        public List<Model> models { get; set; }

        public string error { get; set; }
    }
}
