namespace Ml.Models
{
    public class Product
    {
        public bool active { get; set; }
        public int id { get; set; }
        public string name { get; set; } = "";
        public decimal amount { get; set; }
        public int area { get; set; }
        [NoDb]
        public string areaname { get; set; } = "";
        public int order {  get; set; }
        public int portion { get; set; }
        [NoDb]
        public string portionname { get; set; } = "";
        public int store { get; set; }
        [NoDb]
        public string storename { get; set; } = "";
    }
}
