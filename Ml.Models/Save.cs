namespace Rby.Models
{
    public class Save
    {
        public int Id { get; set; }
        public DateTime GmDate { get; set; }
        public required string Name { get; set; }
        public int PlayerTm { get; set; }
        public int Season { get;set;  }

        [NoDb]
        public string SaveDateAsString
        {
            get
            {
                return GmDate.ToString("dd/MM/yyyy");
            }
        }

    }
}
