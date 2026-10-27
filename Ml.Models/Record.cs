namespace Rby.Models
{
    public class Record
    {
        public int Season { get; set; }
        public required string TeamName { get; set; }
        public int P {  get; set; }
        public int W { get; set; }
        public int D { get; set; }
        public int L { get; set; }
        public int F { get; set; }
        public int A { get; set; }
        public int Pts { get; set; }
        public int Pos { get; set; }
        public int BP { get; set; }
        public string Year { get; set; } = "";
    }
}
