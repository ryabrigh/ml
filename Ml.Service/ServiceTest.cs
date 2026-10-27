using Ml.Models;

namespace Ml.Service
{
    public static class ServiceTest
    {
        public static TestResults RunTests(List<Game> games, int expectedGames)
        {
            ArgumentNullException.ThrowIfNull(games, nameof(games));

            // tests
            // average scores
            // average number of each event with success rates
            TestResults results = new();

            // do we have x games returned
            if (games == null || games.Count != expectedGames)
            {
                results.Narrative.Add("FAIL - games is null or not the correct number");
                results.FailCount++;
            }
            else
            {
                results.Narrative.Add("Correct number of games");
            }

            if (games != null)
            {
                int gameEventCountFail = 0;
                int gameEventFlowFail = 0;
                List<OutcomeCount> outcomes = [];
                foreach (Game game in games)
                {
                    // are all flows from A to B logical
                    if (game.GameEvents != null)
                    {
                        GameEvent[] eventsAsArray = [.. game.GameEvents];
                        for (int i = 0; i < eventsAsArray.Length; i++)
                        {
                            GameEvent thisEvent = eventsAsArray[i];
                            if (i < eventsAsArray.Length - 1)
                            {
                                GameEvent nextEvent = eventsAsArray[i + 1];
                                // only do this up to the penultimate event so as to always be able to get a next event

                                string[] acceptableNextEventTypes = thisEvent.EventType switch
                                {
                                    "KO" => ["OP", "SC", "LO", "HT", "FT"],
                                    "OP" => ["SC", "LO", "Conv", "KO", "OP", "RK", "HT", "FT"],
                                    "SC" => ["OP", "Pen", "PK", "HT", "FT"],
                                    "LO" => ["OP", "Pen", "PK", "HT", "FT"],
                                    "RK" => ["OP", "Pen", "PK", "HT", "FT"],
                                    "Pen" => ["OP", "SC", "LO", "Conv", "RK", "HT", "FT"],
                                    "PK" => ["KO", "HT", "FT"],
                                    "Conv" => ["KO", "HT", "FT"],
                                    "HT" => ["SC", "LO", "Conv", "KO", "OP", "RK", "HT", "FT"],
                                    _ => [""],
                                };
                                if (!acceptableNextEventTypes.Contains(nextEvent.EventType))
                                {
                                    results.Narrative.Add("FAIL - game id " + game.Id + " has a flow from " + thisEvent.EventType + " to "
                                        + nextEvent.EventType + " at minute " + nextEvent.HalfMinute + " which is wrong");
                                    AddGameToFailedGames(game, ref results);
                                    gameEventFlowFail++;
                                }
                            }
                        }
                        if (game.EventOutcomes != null)
                        {
                            GroupOutcomes(game.EventOutcomes, ref outcomes);
                        }
                    }                    
                }

                List<string> reportedOutcomes = [];
                foreach (OutcomeCount outcomeCount in outcomes)
                {
                    reportedOutcomes.Add(outcomeCount.Outcome + " - " + outcomeCount.Count);
                }
                results.EventOutcome = reportedOutcomes;

                // average num tries, pens and convs

                int totalTries = games.Select(g => g.Tries1 + g.Tries2).Sum();
                int totalPKs = games.Select(g => g.Penalties1 + g.Penalties2).Sum();
                int totalConvs = games.Select(g => g.Conversions1 + g.Conversions2).Sum();
                int totalScores = games.Select(g => g.Sc1 + g.Sc2).Sum();

                results.AverageTries = (double)totalTries / games.Count;
                results.AveragePKs = (double)totalPKs / games.Count;
                results.AverageConvs = (double)totalConvs / games.Count;
                results.AverageScores = (double)totalScores / games.Count;

                // event outcomes

                // just try doing macro outcomes - of 80 events, how many kicks etc.
                // then try to drill down 

                if (gameEventCountFail > 0)
                {
                    results.FailCount += gameEventCountFail;
                }
                else
                {
                    results.Narrative.Add("All games have 80 events");
                }
                if (gameEventFlowFail > 0)
                {
                    results.FailCount += gameEventFlowFail;
                }
                else
                {
                    results.Narrative.Add("All games have correct flow");
                }
            }
            return results;
        }

        private static void AddGameToFailedGames(Game game, ref TestResults results)
        {
            bool gameAlreadyFailed = results.FailedGames.Where(g => g.Id == game.Id).Any();
            if (!gameAlreadyFailed)
            {
                results.FailedGames.Add(game);
            }
        }

        private static void GroupOutcomes(List<string> gameOutcomes, ref List<OutcomeCount> existingOutcomes)
        {
            foreach (string gameOutcome in gameOutcomes)
            {
                OutcomeCount? found = existingOutcomes.Where(g => g.Outcome == gameOutcome).FirstOrDefault();
                if (found != null)
                {
                    found.Count++;
                }
                else
                {
                    existingOutcomes.Add(new OutcomeCount()
                    {
                        Count = 1,
                        Outcome = gameOutcome
                    });
                }
            }
        }

        public class OutcomeCount()
        {
            public string Outcome { get; set; } = "";
            public int Count { get; set; }
        }
    }
}
