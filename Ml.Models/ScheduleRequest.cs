namespace Ml.Models
{
    public class ScheduleRequest
    {
        public required string Operation { get; set; }
        public int Season { get; set; }
        public DateTime StartDate { get; set; }
    }
}
