using System.Text.Json;

namespace MonoBase.Models
{
    public class DynamicEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("n");

        public string CollectionName { get; set; } = string.Empty;

         public string JsonData { get; set; } = "{}";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}