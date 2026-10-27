namespace Ml.Models
{
    public class TestResults
    {
        public List<string> Narrative { get; set; }
        public int FailCount {  get; set; }
        public List<Game> FailedGames { get; set; }
        public double AverageTries { get; set; }
        public double AveragePKs { get; set; }
        public double AverageConvs { get; set; }
        public double AverageScores { get; set; }

        public List<string> EventOutcome {  get; set; }

        public TestResults()
        {
            Narrative = [];
            FailedGames = [];
            EventOutcome = [];
        }
    }
}
