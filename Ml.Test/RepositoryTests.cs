using Rby.Service;
using Rby.Data;
using Rby.Models;
using Newtonsoft.Json.Linq;
using Amazon.Lambda.APIGatewayEvents;
using Newtonsoft.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Amazon.DynamoDBv2.Model;
using System.Reflection;

namespace Rby.Test
{
	[TestClass]
	public class RepositoryTests
	{
		private readonly RepositoryMock repository = new();

		[TestMethod]
		[DataRow("RbyTm", 2, "Name", "Team2")]
		[DataRow("RbySave", 1, "Name", "Save1")]
		public async Task GetItemById(string tableName, int id, string property, string value)
		{
			GetItemResponse response = await repository.GetItemById(tableName, id);

			Assert.IsNotNull(response.Item);
			Assert.AreEqual(response.Item[property].S, value);
		}

		[TestMethod]
		[DataRow("RbyTm", 4, "Name", "Team1")]
		[DataRow("RbySave", 2, "Name", "Save1")]
		public async Task GetAllItems(string tableName, int count, string property, string value)
		{
			ScanResponse response = await repository.GetAllItems(tableName);

			Assert.IsNotNull(response.Items);
			Assert.AreEqual(response.Items.Count, count);
			Assert.AreEqual(response.Items[0][property].S, value);
		}

		[TestMethod]
		public async Task WriteRecord()
		{
			Team teamToWrite = new() 
				{ Id = 3, Name = "Team3", ActiveFrom = 3, HColour = "Grey", AColour = "Brown", HColourHex = "#33333", AColourHex = "#44444" };

			PutItemResponse response = await repository.WriteRecord<Team>("RbyTm", teamToWrite);

			Assert.AreEqual(System.Net.HttpStatusCode.OK, response.HttpStatusCode);
		}

		[TestMethod]
		public async Task WriteRecords()
		{
			List<Game> gamesToWrite =
				[
					new() { Id = 4, Colours = ["Black", "White"], GmDate = new DateTime(2025,1,1), Played = true, Round = 1, Sc1 = 1, Sc2 = 2, Tm1 = 1, Tm2 = 2 },
					new() { Id = 5, Colours = ["Brown", "Green"], GmDate = new DateTime(2026,2,2), Played = false, Round = 2, Sc1 = 3, Sc2 = 4, Tm1 = 3, Tm2 = 4 }
				];

			BatchWriteItemResponse response = await repository.WriteRecords<Game>("RbyGm", gamesToWrite);

			Assert.AreEqual(System.Net.HttpStatusCode.OK, response.HttpStatusCode);
		}

		[TestMethod]
		public async Task GetRecordById()
		{
			Team? team = await repository.GetRecordById<Team>(1, "RbyTm");
			Game? game = await repository.GetRecordById<Game>(1, "RbyGm");

			Assert.IsNotNull(team);
			Assert.IsNotNull(game);
			Assert.AreEqual("Team1", team.Name);
			Assert.AreEqual(1, game.Tm1);
		}

		[TestMethod]
		public async Task GetAllRecords()
		{
			IEnumerable<Team> teams = await repository.GetAllRecords<Team>("RbyTm");
			IEnumerable<Save> saves = await repository.GetAllRecords<Save>("RbySave");
			IEnumerable<Game> games = await repository.GetAllRecords<Game>("RbyGm");

			Assert.HasCount(4, teams);
			Assert.HasCount(2, saves);
			Assert.HasCount(3, games);
			Assert.AreEqual("Team1", teams.Where(t => t.Id == 1).FirstOrDefault()?.Name);
			Assert.AreEqual("Save1", saves.Where(t => t.Id == 1).FirstOrDefault()?.Name);
			Assert.AreEqual(1, games.Where(t => t.Id == 1).FirstOrDefault()?.Tm1);
		}

		[TestMethod]
		public async Task GetTJoinedToX()
		{
			Game? game = await repository.GetTJoinedToX<Game, Team>(1, "RbyGm:RbyTm" +
								":Tm1/TeamName1/Id/Name:Tm2/TeamName2/Id/Name" +
								":Tm1/HColour1/Id/HColour:Tm1/HColourHex1/Id/HColourHex" +
								":Tm2/HColour2/Id/HColour:Tm2/HColourHex2/Id/HColourHex" +
								":Tm1/AColour1/Id/AColour:Tm1/AColourHex1/Id/AColourHex" +
								":Tm2/AColour2/Id/AColour:Tm2/AColourHex2/Id/AColourHex");

			Assert.IsNotNull(game);
			Assert.AreEqual("Team1", game.TeamName1);
			Assert.AreEqual("Team2", game.TeamName2);
			Assert.AreEqual("Black", game.HColour1);
			Assert.AreEqual("Green", game.HColour2);
			Assert.AreEqual("White", game.AColour1);
			Assert.AreEqual("Blue", game.AColour2);
			Assert.AreEqual("#555555", game.HColourHex1);
			Assert.AreEqual("#115151", game.HColourHex2);
			Assert.AreEqual("#FFFFFF", game.AColourHex1);
			Assert.AreEqual("#98765", game.AColourHex2);
		}

		[TestMethod]
		public async Task FilterByProperty()
		{
			IEnumerable<Game> games = await repository.GetAllRecords<Game>("RbyGm");
			IEnumerable<Game> filteredGames = RepositoryMock.FilterByProperty(games, "Tm1", 1);
			Assert.IsNotEmpty(filteredGames);
			Assert.HasCount(2, filteredGames);
			Assert.AreEqual(2, filteredGames.FirstOrDefault()?.Tm2);
		}

		[TestMethod]
		public void ReadFromRow()
		{
			Dictionary<string, AttributeValue> row = new() {
				{ "Id", new AttributeValue() { N = "1" } },
				{ "GmDate", new AttributeValue() { S = "2025-01-01" } },
				{ "Name", new AttributeValue() { S = "Save1" } },
				{ "PlayerTm", new AttributeValue() { N = "1" } },
				{ "Season", new AttributeValue() { N = "1" } },
			};
			Save save = (Save)repository.ReadFromRow<Save>(row);
			Assert.IsNotNull(save);
			Assert.AreEqual("Save1", save.Name);
		}

		[TestMethod]
		public void ReadFromRows()
		{
			ScanResponse scanResponse = new()
			{
				Items = [ new() {
				{ "Id", new AttributeValue() { N = "1" } },
				{ "ActiveFrom", new AttributeValue() { N = "1" } },
				{ "Name", new AttributeValue() { S = "Team1" } },
				{ "HColour", new AttributeValue() { S = "Black" } },
				{ "AColour", new AttributeValue() { S = "White" } },
				{ "HColourHex", new AttributeValue() { S = "#FFFFF" } },
				{ "AColourHex", new AttributeValue() { S = "#55555" } },
			},
			new() {
				{ "Id", new AttributeValue() { N = "2" } },
				{ "ActiveFrom", new AttributeValue() { N = "2" } },
				{ "Name", new AttributeValue() { S = "Team2" } },
				{ "HColour", new AttributeValue() { S = "Red" } },
				{ "AColour", new AttributeValue() { S = "Green" } },
				{ "HColourHex", new AttributeValue() { S = "#11111" } },
				{ "AColourHex", new AttributeValue() { S = "#22222" } },
			}]
			};
			IEnumerable<Team> teams = repository.ReadFromRows<Team>(scanResponse);
			Assert.IsNotNull(teams);
			Assert.HasCount(2, teams);
			Assert.AreEqual("Black", teams.First().HColour);
			Assert.AreEqual("#22222", teams.Last().AColourHex);
		}

		[TestMethod]
		public void WriteToItem()
		{
			Team team = new() { Id = 1, Name = "Team1", ActiveFrom = 1, HColour = "Black", AColour = "White", HColourHex = "#FFFFF", AColourHex = "#55555" };
			Save save = new() { Id = 2, Name = "Save2", GmDate = new DateTime(2026, 2, 2), PlayerTm = 2, Season = 2 };
			Dictionary<string, AttributeValue>? teamAsItem = repository.WriteToItem(team);
			Dictionary<string, AttributeValue>? saveAsItem = repository.WriteToItem(save);

			Assert.IsNotNull(teamAsItem);
			Assert.AreEqual("Black", teamAsItem["HColour"].S);
			Assert.AreEqual("1", teamAsItem["ActiveFrom"].N);
			Assert.IsNotNull(saveAsItem);
			Assert.AreEqual("Save2", saveAsItem["Name"].S);
			Assert.AreEqual("02/02/2026 00:00:00", saveAsItem["GmDate"].S);
		}

		[TestMethod]
		public async Task GetLatestRecord()
		{
			Game? latestGame = await repository.GetLatestRecord<Game>("RbyGm", "GmDate");

			Assert.IsNotNull(latestGame);
			Assert.AreEqual(3, latestGame.Id);
			Assert.AreEqual(1, latestGame.Tm1);
		}
	}
}