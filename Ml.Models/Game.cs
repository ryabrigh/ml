namespace Ml.Models
{
    public class Game
    {
        public int Id { get; set; }
        public DateTime GmDate { get; set; }
        public int Sc1 { get; set; }
        public int Sc2 { get; set; }
        public int Tm1 { get; set; }
        public int Tm2 { get; set; }
        public int Season { get;set;  }
        public int Round { get; set; }
        public bool Played { get; set; }

        [NoDb]
        public string GmDateAsString { 
            get
            {
                return GmDate.ToString("dd/MM/yyyy");
            }
        }

        [NoDb]
        public string? TeamName1 { get; set; }
        [NoDb]
        public string? TeamName2 { get; set; }
        public int Tries1 { get; set; }
        public int Penalties1 { get; set; }
        public int Conversions1 { get; set; }
        public int Tries2 { get; set; }
        public int Penalties2 { get; set; }
        public int Conversions2 { get; set; }
        [NoDb]
        public int Rank1 { get; set; }
        [NoDb]
        public int Rank2 { get; set; }
        [NoDb]
        public List<GameEvent>? GameEvents { get; set; }
        [NoDb]
        public List<string>? EventOutcomes { get; set; }
		[NoDb]
		public string HColour1 { get; set; } = string.Empty;
		[NoDb]
		public string AColour1 { get; set; } = string.Empty;
		[NoDb]
		public string HColourHex1 { get; set; } = string.Empty;
		[NoDb]
		public string AColourHex1 { get; set; } = string.Empty;
		[NoDb]
		public string HColour2 { get; set; } = string.Empty;
		[NoDb]
		public string AColour2 { get; set; } = string.Empty;
		[NoDb]
		public string HColourHex2 { get; set; } = string.Empty;
		[NoDb]
		public string AColourHex2 { get; set; } = string.Empty;
        [NoDb]
        public string[] Colours { get; set; } = [];
        [NoDb]
        public string[] ColourHex { get; set; } = [];
	}
}
