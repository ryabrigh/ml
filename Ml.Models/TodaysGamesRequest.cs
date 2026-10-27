namespace Rby.Models
{
    public class TodaysGamesRequest
    {
        public int Season { get; set; }
        public DateTime GmDate { get; set; }
        public bool QuickCheck { get; set; }
    }
}
