using Ml.Service;
using Ml.Data;
using Ml.Models;
using Newtonsoft.Json.Linq;
using Amazon.Lambda.APIGatewayEvents;
using Newtonsoft.Json;
using Amazon.DynamoDBv2.Model;
using System.Net;

namespace Ml.Test
{
	[TestClass]
	public class ScheduleTests
	{
		private readonly ScheduleBLL scheduleBLL = new(new RepositoryMock());

		[TestMethod]
		public async Task GetScheduleForSeason()
		{
			IEnumerable<Game> season = await scheduleBLL.GetScheduleForSeason(1);
			Assert.IsNotNull(season);
			Assert.AreEqual(1, season.Count());
			Assert.AreEqual("Team1", season.FirstOrDefault()?.TeamName1);
			Assert.AreEqual("Team2", season.FirstOrDefault()?.TeamName2);
		}

		[TestMethod]
		public async Task GetTodaysGames()
		{
			IEnumerable<Game> todaysGames = await scheduleBLL.GetTodaysGames(2, new DateTime(2026, 2, 2));
			IEnumerable<Game> noGames = await scheduleBLL.GetTodaysGames(2, new DateTime(2026, 2, 3));
			Assert.IsNotNull(todaysGames);
			Assert.AreEqual(1, todaysGames.Count());
			Assert.AreEqual("Team3", todaysGames.FirstOrDefault()?.TeamName1);
			Assert.AreEqual("Team4", todaysGames.FirstOrDefault()?.TeamName2);
			Assert.HasCount(0, noGames);
		}

		[TestMethod]
		public async Task GetNextGameDate()
		{
			DateTime? nextGameDate = await scheduleBLL.GetNextGameDate(2, new DateTime(2025, 12, 31));
			DateTime? noNextGameDate = await scheduleBLL.GetNextGameDate(1, new DateTime(2030, 12, 31));
			Assert.IsNotNull(nextGameDate);
			Assert.AreEqual(new DateTime(2026, 2, 2), nextGameDate);
			Assert.IsNull(noNextGameDate);
		}

		[TestMethod]
		public async Task GetStandings()
		{
			IEnumerable<Record> standings = await scheduleBLL.GetStandings(1);
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
