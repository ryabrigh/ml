using Newtonsoft.Json.Linq;

namespace Ml.Models
{
    public class CrudRequest
    {
        public required string Crud { get; set; }
        public int Id { get; set; }
        public required string TableName { get; set; }
        public required JToken CrudData { get; set; }
        public required string Model {  get; set; }
        public string SortField { get; set; } = "";
    }
}
