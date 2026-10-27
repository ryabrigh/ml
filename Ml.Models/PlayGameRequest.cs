namespace Ml.Models
{
    public class PlayGameRequest
    {
        public int GameId { get; set; }
        public string Mode { get; set; } = string.Empty;
        public int[]? GameIds { get; set; }
        public int NumberOfGames { get; set; }
    }
}
