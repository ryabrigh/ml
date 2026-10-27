using Rby.Service;
using Rby.Data;
using Rby.Models;
using Newtonsoft.Json.Linq;
using Amazon.Lambda.APIGatewayEvents;
using Newtonsoft.Json;

namespace Rby.Test
{
	[TestClass]
	public class ServiceTests
	{
		private readonly RbyServiceBLL serviceBLL = new(new RepositoryMock());

		[TestMethod]
		public async Task GetHealthCheck()
		{
			HealthCheckRequest healthCheckRequest = new()
			{
				RequestName = "serviceTestHC"
			};
			JToken jToken = JObject.FromObject(healthCheckRequest);
			APIGatewayProxyResponse response = await serviceBLL.GetHealthCheck(jToken, null);
			string responseMessage = response.Body;
			Console.WriteLine(responseMessage);
			if (response.StatusCode != 200)
			{
				// error in processing
				throw new Exception(responseMessage);
			}
			else
			{
				Assert.IsNotNull(responseMessage);
			}
		}

		[TestMethod]
		public async Task Crud_C()
		{
			Save save = new() { Id = 1, Name = "Save3", PlayerTm = 3, Season = 3, GmDate = new DateTime(2027,3,3) };
			JToken saveAsJson = JObject.FromObject(save);

			CrudRequest crudRequest = new()
			{
				Id = 1,
				Model = "Save",
				TableName = "RbySave",
				Crud = "C",
				CrudData = saveAsJson
			};
			JToken jToken = JObject.FromObject(crudRequest);
			APIGatewayProxyResponse response = await serviceBLL.Crud(jToken, null);
			Console.WriteLine(response.Body);
			Assert.AreEqual(200, response.StatusCode);
		}

		[TestMethod]
		public async Task Crud_R()
		{
			CrudRequest crudRequest = new()
			{
				Id = 1,
				Model = "Team",
				TableName = "RbyTm",
				Crud = "R",
				CrudData = ""
			};
			JToken jToken = JObject.FromObject(crudRequest);
			APIGatewayProxyResponse response = await serviceBLL.Crud(jToken, null);
			Assert.AreEqual(200, response.StatusCode);
			Team? team = JsonConvert.DeserializeObject<Team>(response.Body);
			Assert.IsNotNull(team);
			Assert.AreEqual(1, team.Id);
		}

		[TestMethod]
		public async Task Crud_U()
		{
			Save save = new() { Id = 1, Name = "Save3", PlayerTm = 3, Season = 3, GmDate = new DateTime(2027, 3, 3) };
			JToken saveAsJson = JObject.FromObject(save);

			CrudRequest crudRequest = new()
			{
				Id = 1,
				Model = "Save",
				TableName = "RbySave",
				Crud = "U",
				CrudData = saveAsJson
			};
			JToken jToken = JObject.FromObject(crudRequest);
			APIGatewayProxyResponse response = await serviceBLL.Crud(jToken, null);
			Console.WriteLine(response.Body);
			Assert.AreEqual(200, response.StatusCode);
		}

		[TestMethod]
		public async Task Play_CantFindGame()
		{
			PlayGameRequest playGameRequest = new()
			{
				GameId = 4,
				Mode = "PlayByPlay"
			};
			JToken jToken = JObject.FromObject(playGameRequest);
			APIGatewayProxyResponse response = await serviceBLL.PlayGame(jToken, null);
			if (response.StatusCode != 200) { Console.WriteLine(response.Body); }
			Assert.AreEqual(500, response.StatusCode);
			string message = response.Body;
			Assert.Contains("Can't find game for id: " + playGameRequest.GameId, message);
		}

		[TestMethod]
		public async Task Play_AlreadyPlayed()
		{
			PlayGameRequest playGameRequest = new()
			{
				GameId = 1,
				Mode = "PlayByPlay"
			};
			JToken jToken = JObject.FromObject(playGameRequest);
			APIGatewayProxyResponse response = await serviceBLL.PlayGame(jToken, null);
			if (response.StatusCode != 200) { Console.WriteLine(response.Body); }
			Assert.AreEqual(500, response.StatusCode);
			string message = response.Body;
			Assert.Contains("This game has already been played id: 1", message);
		}

		[TestMethod]
		public async Task Play_PlayByPlay()
		{
			PlayGameRequest playGameRequest = new()
			{
				GameId = 2,
				Mode = "PlayByPlay"
			};
			JToken jToken = JObject.FromObject(playGameRequest);
			APIGatewayProxyResponse response = await serviceBLL.PlayGame(jToken, null);
			if (response.StatusCode != 200) { Console.WriteLine(response.Body); }
			Assert.AreEqual(200, response.StatusCode);
			List<Game>? returnedGames = JsonConvert.DeserializeObject<List<Game>>(response.Body);
			Assert.IsNotNull(returnedGames);
			Assert.HasCount(1, returnedGames);
			PlayTests.ValidateGameEvents([.. returnedGames]);
		}

		[TestMethod]
		public async Task Play_PlayByPlayMultiple()
		{
			PlayGameRequest playGameRequest = new()
			{
				GameIds = [2, 3],
				Mode = "PlayByPlayMultiple"
			};
			JToken jToken = JObject.FromObject(playGameRequest);
			APIGatewayProxyResponse response = await serviceBLL.PlayGame(jToken, null);
			if (response.StatusCode != 200) { Console.WriteLine(response.Body); }
			Assert.AreEqual(200, response.StatusCode);
			List<Game>? returnedGames = JsonConvert.DeserializeObject<List<Game>>(response.Body);
			Assert.IsNotNull(returnedGames);
			Assert.HasCount(2, returnedGames);
			PlayTests.ValidateGameEvents([.. returnedGames]);
		}

		[TestMethod]
		public async Task Play_PbpBulkTest()
		{
			PlayGameRequest playGameRequest = new()
			{
				GameIds = [2, 3],
				Mode = "PbpBulkTest",
				NumberOfGames = 3
			};
			JToken jToken = JObject.FromObject(playGameRequest);
			APIGatewayProxyResponse response = await serviceBLL.PlayGame(jToken, null);
			if (response.StatusCode != 200) { Console.WriteLine(response.Body); }
			Assert.AreEqual(200, response.StatusCode);
		}

		[TestMethod]
		public void CalculateColours()
		{
			Game game1 = new()
			{
				HColour1 = "Black",
				HColourHex1 = "#11111",
				HColour2 = "White",
				HColourHex2 = "#22222",
				AColour1 = "White",
				AColourHex1 = "#33333",
				AColour2 = "Black",
				AColourHex2 = "#44444"
			};
			Game game2 = new()
			{
				HColour1 = "Red",
				HColourHex1 = "#11111",
				HColour2 = "Blue",
				HColourHex2 = "#22222",
				AColour1 = "Red",
				AColourHex1 = "#33333",
				AColour2 = "Red",
				AColourHex2 = "#44444"
			};
			Game game3 = new()
			{
				HColour1 = "Red",
				HColourHex1 = "#11111",
				HColour2 = "Blue",
				HColourHex2 = "#22222",
				AColour1 = "Green",
				AColourHex1 = "#33333",
				AColour2 = "Yellow",
				AColourHex2 = "#44444"
			};
			RbyServiceBLL.CalculateColours(ref game1);
			RbyServiceBLL.CalculateColours(ref game2);
			RbyServiceBLL.CalculateColours(ref game3);

			string[] coloursGame1 = ["Black", "White"];
			string[] colourhexGame1 = ["#11111", "#33333"];
			string[] coloursGame2 = ["Blue", "Red"];
			string[] colourhexGame2 = ["#22222", "#44444"];
			string[] coloursGame3 = ["Red", "Yellow"];
			string[] colourhexGame3 = ["#11111", "#44444"];
			CollectionAssert.AreEqual(coloursGame1, game1.Colours);
			CollectionAssert.AreEqual(colourhexGame1, game1.ColourHex);
			CollectionAssert.AreEqual(coloursGame2, game2.Colours);
			CollectionAssert.AreEqual(colourhexGame2, game2.ColourHex);
			CollectionAssert.AreEqual(coloursGame3, game3.Colours);
			CollectionAssert.AreEqual(colourhexGame3, game3.ColourHex);
		}

		[TestMethod]
		public async Task Schedule_ImproperRequest()
		{
			HealthCheckRequest improperRequest = new()
			{
				RequestName = "serviceTestHC"
			};
			JToken jToken = JObject.FromObject(improperRequest);
			APIGatewayProxyResponse response = await serviceBLL.Schedule(jToken, null);
			if (response.StatusCode != 200) { Console.WriteLine(response.Body); }
			Assert.AreEqual(500, response.StatusCode);
			string message = response.Body;
			Assert.Contains("Request did not contain a proper schedule request object", message);
		}

		[TestMethod]
		public async Task Schedule_BadOperation()
		{
			ScheduleRequest request = new()
			{
				Operation = "BadOperation"
			};
			JToken jToken = JObject.FromObject(request);
			APIGatewayProxyResponse response = await serviceBLL.Schedule(jToken, null);
			if (response.StatusCode != 200) { Console.WriteLine(response.Body); }
			Assert.AreEqual(500, response.StatusCode);
			string message = response.Body;
			Assert.Contains("scheduleRequest.Operation of: BadOperation not recognised", message);
		}

		[TestMethod]
		public async Task Schedule_Get()
		{
			ScheduleRequest request = new()
			{
				Operation = "Get",
				Season = 1
			};
			JToken jToken = JObject.FromObject(request);
			APIGatewayProxyResponse response = await serviceBLL.Schedule(jToken, null);
			if (response.StatusCode != 200) { Console.WriteLine(response.Body); }
			Assert.AreEqual(200, response.StatusCode);
			Assert.IsNotNull(response.Body);
			IEnumerable<Game>? season = JsonConvert.DeserializeObject<IEnumerable<Game>?>(response.Body);
			Assert.IsNotNull(season);
			Assert.AreEqual(1, season.Count());
			Assert.AreEqual("Team1", season.FirstOrDefault()?.TeamName1);
			Assert.AreEqual("Team2", season.FirstOrDefault()?.TeamName2);
		}

		[TestMethod]
		public async Task Schedule_Create_AlreadyExists()
		{
			ScheduleRequest request = new()
			{
				Operation = "Create",
				Season = 1
			};
			JToken jToken = JObject.FromObject(request);
			APIGatewayProxyResponse response = await serviceBLL.Schedule(jToken, null);
			if (response.StatusCode != 200) { Console.WriteLine(response.Body); }
			Assert.AreEqual(500, response.StatusCode);
			string message = response.Body;
			Assert.Contains("Season already exists so can't create", message);
		}

		[TestMethod]
		public async Task Schedule_Create()
		{
			ScheduleRequest request = new()
			{
				Operation = "Create",
				Season = 4
			};
			JToken jToken = JObject.FromObject(request);
			APIGatewayProxyResponse response = await serviceBLL.Schedule(jToken, null);
			if (response.StatusCode != 200) { Console.WriteLine(response.Body); }
			Assert.AreEqual(200, response.StatusCode);
			string[] games = response.Body.Split('/');
			// remove the last one because it comes with a trailing /
			games = games.Take(games.Length - 1).ToArray();
			Assert.HasCount(12, games);
			string firstGame = games.FirstOrDefault() ?? "";
			Assert.Contains("Round 1", firstGame.Split(":")[0]);
			string lastGame = games.LastOrDefault() ?? "";
			Assert.Contains("Round 6", lastGame.Split(":")[0]);
			foreach (string game in games)
			{
				string[] gameParts = game.Split(":");
				Assert.HasCount(3, gameParts);
				Assert.Contains("Round", gameParts[0]);
				Assert.IsInstanceOfType(Int32.Parse(gameParts[1]), typeof(int));
				Assert.IsInstanceOfType(Int32.Parse(gameParts[2]), typeof(int));
			}
		}

		[TestMethod]
		public async Task TodaysGames_ImproperRequest()
		{
			HealthCheckRequest improperRequest = new()
			{
				RequestName = "serviceTestHC"
			};
			JToken jToken = JObject.FromObject(improperRequest);
			APIGatewayProxyResponse response = await serviceBLL.TodaysGames(jToken, null);
			if (response.StatusCode != 200) { Console.WriteLine(response.Body); }
			Assert.AreEqual(500, response.StatusCode);
			string message = response.Body;
			Assert.Contains("Request did not contain a proper todaysGamesRequest object", message);
		}

		[TestMethod]
		public async Task TodaysGames_QuickCheck_NextGameDate()
		{
			TodaysGamesRequest request = new()
			{
				QuickCheck = true,
				Season = 2,
				GmDate = new DateTime(2025, 12, 31)
			};
			JToken jToken = JObject.FromObject(request);
			APIGatewayProxyResponse response = await serviceBLL.TodaysGames(jToken, null);
			if (response.StatusCode != 200) { Console.WriteLine(response.Body); }
			Assert.AreEqual(200, response.StatusCode);
			Assert.IsNotNull(response.Body);
			DateTime? nextGameDate = JsonConvert.DeserializeObject<DateTime?>(response.Body);
			Assert.IsNotNull(nextGameDate);
			Assert.AreEqual(new DateTime(2026, 2, 2), nextGameDate);
		}

		[TestMethod]
		public async Task TodaysGames_QuickCheck_NoNextGameDate()
		{
			TodaysGamesRequest request = new()
			{
				QuickCheck = true,
				Season = 1,
				GmDate = new DateTime(2030, 12, 31)
			};
			JToken jToken = JObject.FromObject(request);
			APIGatewayProxyResponse response = await serviceBLL.TodaysGames(jToken, null);
			if (response.StatusCode != 200) { Console.WriteLine(response.Body); }
			Assert.AreEqual(200, response.StatusCode);
			Assert.IsNotNull(response.Body);
			Assert.AreEqual("null", response.Body);
		}

		[TestMethod]
		public async Task TodaysGames_NextGameDate()
		{
			TodaysGamesRequest request = new()
			{
				QuickCheck = false,
				Season = 2,
				GmDate = new DateTime(2026, 2, 2)
			};
			JToken jToken = JObject.FromObject(request);
			APIGatewayProxyResponse response = await serviceBLL.TodaysGames(jToken, null);
			if (response.StatusCode != 200) { Console.WriteLine(response.Body); }
			Assert.AreEqual(200, response.StatusCode);
			Assert.IsNotNull(response.Body);
			IEnumerable<Game>? todaysGames = JsonConvert.DeserializeObject<IEnumerable<Game>?>(response.Body);
			Assert.IsNotNull(todaysGames);
			Assert.AreEqual(1, todaysGames.Count());
			Assert.AreEqual("Team3", todaysGames.FirstOrDefault()?.TeamName1);
			Assert.AreEqual("Team4", todaysGames.FirstOrDefault()?.TeamName2);
		}

		[TestMethod]
		public async Task TodaysGames_NoNextGameDate()
		{
			TodaysGamesRequest request = new()
			{
				QuickCheck = false,
				Season = 2,
				GmDate = new DateTime(2026, 2, 3)
			};
			JToken jToken = JObject.FromObject(request);
			APIGatewayProxyResponse response = await serviceBLL.TodaysGames(jToken, null);
			if (response.StatusCode != 200) { Console.WriteLine(response.Body); }
			Assert.AreEqual(200, response.StatusCode);
			Assert.IsNotNull(response.Body);
			IEnumerable<Game>? todaysGames = JsonConvert.DeserializeObject<IEnumerable<Game>?>(response.Body);
			Assert.IsNotNull(todaysGames);
			Assert.AreEqual(0, todaysGames.Count());
		}

		[TestMethod]
		public async Task Standings_ImproperRequest()
		{
			HealthCheckRequest improperRequest = new()
			{
				RequestName = "serviceTestHC"
			};
			JToken jToken = JObject.FromObject(improperRequest);
			APIGatewayProxyResponse response = await serviceBLL.Standings(jToken, null);
			if (response.StatusCode != 200) { Console.WriteLine(response.Body); }
			Assert.AreEqual(500, response.StatusCode);
			string message = response.Body;
			Assert.Contains("Request did not contain a proper standingsRequest object", message);
		}

		[TestMethod]
		public async Task Standings()
		{
			StandingsRequest request = new()
			{
				Season = 1
			};
			JToken jToken = JObject.FromObject(request);
			APIGatewayProxyResponse response = await serviceBLL.Standings(jToken, null);
			if (response.StatusCode != 200) { Console.WriteLine(response.Body); }
			Assert.AreEqual(200, response.StatusCode);
			Assert.IsNotNull(response.Body);
			IEnumerable<Record>? standings = JsonConvert.DeserializeObject<IEnumerable<Record>?>(response.Body);
			Assert.IsNotNull(standings);
			Assert.HasCount(2, standings);
			Record? record1 = standings.FirstOrDefault();
			Record? record2 = standings.LastOrDefault();
			Assert.AreEqual("Team2", record1?.TeamName);
			Assert.AreEqual(1, record1?.W);
			Assert.AreEqual(0, record1?.L);
			Assert.AreEqual(0, record1?.BP);
			Assert.AreEqual(4, record1?.Pts);
			Assert.AreEqual("Team1", record2?.TeamName);
			Assert.AreEqual(0, record2?.W);
			Assert.AreEqual(1, record2?.L);
			Assert.AreEqual(1, record2?.BP);
			Assert.AreEqual(1, record2?.Pts);
		}
	}
}
