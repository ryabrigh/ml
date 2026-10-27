using Rby.Service;
using Rby.Data;
using Rby.Models;
using Newtonsoft.Json.Linq;
using Amazon.Lambda.APIGatewayEvents;
using Newtonsoft.Json;
using Amazon.DynamoDBv2.Model;
using System.Net;

namespace Rby.Test
{
	[TestClass]
	public class PlayTests
	{
		private readonly PlayBLL playBLL = new(new RepositoryMock());

		[TestMethod]
		public async Task PlayByPlay()
		{
			Game game = new() { Id = 1, Colours = ["Black", "White"], GmDate = new DateTime(2025, 1, 1), 
				Played = false, Tm1 = 1, Tm2 = 2, TeamName1 = "Team1", TeamName2 = "Team2" };

			(Game, PutItemResponse) response = await playBLL.Play(game, "PlayByPlay");
			Assert.AreEqual(HttpStatusCode.OK, response.Item2.HttpStatusCode);
			ValidateGameEvents([game]);
		}

		[TestMethod]
		public async Task PlayByPlayMultiple()
		{
			Game[] games = [
			new(){Id = 1,Colours = ["Black", "White"],GmDate = new DateTime(2025, 1, 1),Played = false,Tm1 = 1,Tm2 = 2,TeamName1 = "Team1",TeamName2 = "Team2"},
			new(){Id = 2,Colours = ["Red", "Green"],GmDate = new DateTime(2026, 2, 2),Played = false,Tm1 = 3,Tm2 = 4,TeamName1 = "Team3",TeamName2 = "Team4"}
			];

			List<Game> returnedGames = [];
			foreach (Game game in games)
			{
				(Game, PutItemResponse) response = await playBLL.Play(game, "PlayByPlayMultiple");
				Assert.AreEqual(HttpStatusCode.OK, response.Item2.HttpStatusCode);
				returnedGames.Add(response.Item1);
			}
			ValidateGameEvents([..returnedGames]);
		}

		[TestMethod]
		public async Task PbpBulkTest()
		{
			Game[] games = [
			new(){Id = 1,Colours = ["Black", "White"],GmDate = new DateTime(2025, 1, 1),Played = false,Tm1 = 1,Tm2 = 2,TeamName1 = "Team1",TeamName2 = "Team2"},
			new(){Id = 2,Colours = ["Red", "Green"],GmDate = new DateTime(2026, 2, 2),Played = false,Tm1 = 3,Tm2 = 4,TeamName1 = "Team3",TeamName2 = "Team4"}
			];

			List<Game> returnedGames = [];
			foreach (Game game in games)
			{
				(Game, PutItemResponse) response = await playBLL.Play(game, "PbpBulkTest");
				Assert.AreEqual(HttpStatusCode.OK, response.Item2.HttpStatusCode);
				returnedGames.Add(response.Item1);
			}
			ValidateGameEvents([.. returnedGames]);
		}

		public static void ValidateGameEvents(Game[] games)
		{
            foreach (Game game in games)
            {
				Assert.AreEqual(game.GameEvents?.Where(e => e.IsTry).Count(), game.Tries1 + game.Tries2);
				Assert.AreEqual(game.GameEvents?.Where(e => e.EventType == "PK" && e.Success).Count(), game.Penalties1 + game.Penalties2);
				Assert.AreEqual((game.Tries1 * 5) + (game.Penalties1 * 3) + (game.Conversions1 * 2), game.Sc1);
				Assert.AreEqual((game.Tries2 * 5) + (game.Penalties2 * 3) + (game.Conversions2 * 2), game.Sc2);
            }
			TestResults playTestResults = ServiceTest.RunTests([.. games], games.Length);
			Assert.AreEqual(0, playTestResults.FailCount);
		}
	}
}
