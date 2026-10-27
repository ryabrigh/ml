using Amazon.DynamoDBv2.Model;
using Ml.IData;
using Ml.Models;

namespace Ml.Service
{
    public class ScheduleBLL(IRepository repository)
    {
        public async Task<IEnumerable<Game>> GetScheduleForSeason(int season)
        {
            IEnumerable<Game> allGames = await repository.GetAllRecords<Game>("RbyGm");
            IEnumerable<Team> allTeams = await repository.GetAllRecords<Team>("RbyTm");

            IEnumerable<Game> join1 = allGames.Join(allTeams, g => g.Tm1, t => t.Id, (g, t) => AddTeamNameToGame(g, 1, t.Name));
            IEnumerable<Game> join2 = join1.Join(allTeams, g => g.Tm2, t => t.Id, (g, t) => AddTeamNameToGame(g, 2, t.Name));
            return join2.Where(g => g.Season == season).OrderBy(g => g.GmDate);
        }

        public async Task<IEnumerable<Game>> GetTodaysGames(int season, DateTime gmDate)
        {
            IEnumerable<Game> allGames = await GetScheduleForSeason(season);
            IEnumerable<Game> todaysGames = allGames.Where(g => g.GmDate == gmDate).OrderBy(g => g.Id);
            IEnumerable<Record> standings = await GetStandings(season);
            foreach (Game game in todaysGames)
            {
                game.Rank1 = standings.Where(s => s.TeamName == game.TeamName1)?.FirstOrDefault()?.Pos ?? 0;
                game.Rank2 = standings.Where(s => s.TeamName == game.TeamName2)?.FirstOrDefault()?.Pos ?? 0;
            }
            return todaysGames;
        }

        public async Task<DateTime?> GetNextGameDate(int season, DateTime gmDate)
        {
            IEnumerable<Game> allGames = await GetScheduleForSeason(season);
            return allGames.Where(g => g.GmDate >= gmDate && !g.Played)?.OrderBy(g => g.GmDate)?.FirstOrDefault()?.GmDate;
        }

        public async Task<IEnumerable<Record>> GetStandings(int season)
        {
            List<Record> records = [];
            IEnumerable<Game> allGames = await GetScheduleForSeason(season);
            IEnumerable<Team> allTeams = await repository.GetAllRecords<Team>("RbyTm");
            IEnumerable<Team> allTeamsActiveFromThisSeason = allTeams.Where(t => t.ActiveFrom <= season);
            foreach (Team team in allTeamsActiveFromThisSeason)
            {
                IEnumerable<Game> gamesPlayed = allGames.Where(g => g.Tm1 == team.Id || g.Tm2 == team.Id);
                IEnumerable<Game> gamesWon = allGames.Where(g => g.Played && ((g.Tm1 == team.Id && g.Sc1 > g.Sc2) || (g.Tm2 == team.Id && g.Sc2 > g.Sc1)));
                IEnumerable<Game> gamesDrawn = allGames.Where(g => g.Played && ((g.Tm1 == team.Id && g.Sc1 == g.Sc2) || (g.Tm2 == team.Id && g.Sc2 == g.Sc1)));
                IEnumerable<Game> gamesLost = allGames.Where(g => g.Played && ((g.Tm1 == team.Id && g.Sc1 < g.Sc2) || (g.Tm2 == team.Id && g.Sc2 < g.Sc1)));
                int pointsFor = allGames.Where(g => g.Played && ((g.Tm1 == team.Id))).Sum(g => g.Sc1) + allGames.Where(g => g.Played && ((g.Tm2 == team.Id))).Sum(g => g.Sc2);
                int pointsAgainst = allGames.Where(g => g.Played && ((g.Tm1 == team.Id))).Sum(g => g.Sc2) + allGames.Where(g => g.Played && ((g.Tm2 == team.Id))).Sum(g => g.Sc1);
                int losingBonusPointsTm1 = allGames.Where(g => g.Played && g.Tm1 == team.Id && g.Sc1 < g.Sc2 && g.Sc2 - g.Sc1 <= 7)?.Count() ?? 0;
                int losingBonusPointsTm2 = allGames.Where(g => g.Played && g.Tm2 == team.Id && g.Sc2 < g.Sc1 && g.Sc1 - g.Sc2 <= 7)?.Count() ?? 0;
                int losingBonusPoints = losingBonusPointsTm1 + losingBonusPointsTm2;
                int tryBonusPoints = allGames.Where(g => g.Played && ((g.Tm1 == team.Id && g.Tries1 >= 4) || (g.Tm2 == team.Id && g.Tries2 >= 4)))?.Count() ?? 0;
                string seasonYear = allGames.First() == null ? "" : allGames.First().GmDate.Year.ToString();

                records.Add(new Record
                {
                    Season = season,
                    TeamName = team.Name,
                    P = gamesPlayed.Any() ? gamesPlayed.Count() : 0,
                    W = gamesWon.Any() ? gamesWon.Count() : 0,
                    D = gamesDrawn.Any() ? gamesDrawn.Count() : 0,
                    L = gamesLost.Any() ? gamesLost.Count() : 0,
                    F = pointsFor,
                    A = pointsAgainst,
                    BP = losingBonusPoints + tryBonusPoints,
                    Year = seasonYear
                });
            }
            foreach (Record record in records)
            {
                record.Pts = (record.W * 4) + (record.D * 2) + record.BP;
            }
            List<Record> orderedRecords = [.. records.OrderByDescending(r => r.Pts).ThenByDescending(r => r.F - r.A)]; // points difference
            for (int i = 0; i < orderedRecords.Count; i++)
            {
                orderedRecords[i].Pos = i + 1;
            }
            return orderedRecords.AsEnumerable();
        }

        private static Game AddTeamNameToGame(Game game, int tm, string name)
        {
            if (tm == 1)
            {
                game.TeamName1 = name;
            }
            else
            {
                game.TeamName2 = name;
            }
            return game;
        }
    }
}
