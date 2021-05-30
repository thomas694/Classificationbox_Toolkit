namespace Classificationbox.Net.Models
{
    public class GetModelResponse
    {
        public bool success { get; set; }

        public Model model { get; set; }

        public string error { get; set; }
    }
}
