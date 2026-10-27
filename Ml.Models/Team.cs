namespace Ml.Models
{
    public class Team
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public int ActiveFrom { get; set;  }
        public string HColour { get; set; } = string.Empty;
        public string AColour { get; set; } = string.Empty;
		public string HColourHex { get; set; } = string.Empty;
		public string AColourHex { get; set; } = string.Empty;
	}
}
