using System.Collections.Generic;

namespace PzWorkshopUploaderCLI.Models
{
    internal class WorkshopFileData
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public IList<string> Tags { get; set; }
        public int Visibility { get; set; }
    }
}
