namespace Ml.Models
{
    public class GameEvent
    {
        public int HalfMinute { get; set; }
        public string EventType { get; set; } = "";
        public int TeamId { get; set; }
        public string TeamName { get; set; } = "";
        public bool Success { get; set; }
        public int Sc1 { get; set; }
        public int Sc2 { get; set; }
        public string[]? Description { get; set; }
        public int Possession { get; set; }
        public int PosX { get; set; }
        public bool IsTry {  get; set; }
    }
}
