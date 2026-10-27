using Amazon.DynamoDBv2.Model;
using Ml.IData;
using Ml.Models;
using System.Globalization;
using System.Reflection;

namespace Rby.Data
{
    public class RepositoryMock : IRepository
    {
        private readonly IFormatProvider ukFormat = new CultureInfo("en-GB");

        public Task<GetItemResponse> GetItemById(string tableName, int id)
        {
            GetItemResponse response = new();
            switch (tableName)
            {
				case "RbyTm":
					Team? foundTeam = MockTeams().Where(t => t.Id == id).FirstOrDefault();
					if (foundTeam != null)
					{
						response.Item = WriteToItem<Team>(foundTeam);
					}
					break;
				case "RbySave":
					Save? foundSave = MockSaves().Where(t => t.Id == id).FirstOrDefault();
					if (foundSave != null)
					{
						response.Item = WriteToItem<Save>(foundSave);
					}
					break;
				case "RbyGm":
					Game? foundGame = MockGames().Where(t => t.Id == id).FirstOrDefault();
					if (foundGame != null)
					{
						response.Item = WriteToItem<Game>(foundGame);
					}
					break;
				case "RbyHealthCheck":
					HealthCheck? foundHealthCheck = MockHealthChecks().Where(t => t.Id == id).FirstOrDefault();
					if (foundHealthCheck != null)
					{
						response.Item = WriteToItem<HealthCheck>(foundHealthCheck);
					}
					break;
				default:
                    break;
            }
            return Task.FromResult(response);
        }

        public Task<ScanResponse> GetAllItems(string tableName)
        {
            ScanResponse response = new()
            {
                Items = []
            };

			switch (tableName)
			{
				case "RbyTm":
					List<Team> foundTeams = MockTeams();
					if (foundTeams != null)
					{
                        foreach(Team thisTeam in foundTeams)
                        {
                            response.Items.Add(WriteToItem<Team>(thisTeam));
                        }
					}
					break;
				case "RbySave":
					List<Save> foundSaves = MockSaves();
					if (foundSaves != null)
					{
						foreach (Save thisSave in foundSaves)
						{
							response.Items.Add(WriteToItem<Save>(thisSave));
						}
					}
					break;
				case "RbyGm":
					List<Game> foundGames = MockGames();
					if (foundGames != null)
					{
						foreach (Game thisGame in foundGames)
						{
							response.Items.Add(WriteToItem<Game>(thisGame));
						}
					}
					break;
				case "RbyHealthCheck":
					List<HealthCheck> foundHealthChecks = MockHealthChecks();
					if (foundHealthChecks != null)
					{
						foreach (HealthCheck thisHealthCheck in foundHealthChecks)
						{
							response.Items.Add(WriteToItem<HealthCheck>(thisHealthCheck));
						}
					}
					break;
				default:
					break;
			}
			return Task.FromResult(response);
		}

		public Task<PutItemResponse> WriteRecord<T>(string tableName, T record)
        {
			PutItemResponse response = new() { HttpStatusCode = System.Net.HttpStatusCode.OK };

			IEnumerable<PropertyInfo>? dbProperties = record?.GetType().GetProperties().Where(p => p.GetCustomAttributes(typeof(NoDbAttribute), true).Length == 0);
			int dbPropertiesCount = dbProperties == null ? 0 : dbProperties.Count();

			Dictionary<string, AttributeValue> item = WriteToItem<T>(record);
            if (item == null || item.Count != dbPropertiesCount)
            {
                response.HttpStatusCode = System.Net.HttpStatusCode.InternalServerError;
            }
            return Task.FromResult(response);
		}

		public Task<BatchWriteItemResponse> WriteRecords<T>(string tableName, List<T> items)
        {
			BatchWriteItemResponse response = new() { HttpStatusCode = System.Net.HttpStatusCode.OK};

            IEnumerable<PropertyInfo>? dbProperties = items[0]?.GetType().GetProperties().Where(p => p.GetCustomAttributes(typeof(NoDbAttribute), true).Length == 0);
            int dbPropertiesCount = dbProperties == null ? 0 : dbProperties.Count();
			foreach (T item in items)
			{
				Dictionary<string, AttributeValue> convertedItem = WriteToItem(item);
				if (convertedItem == null || convertedItem.Count != dbPropertiesCount)
				{
					response.HttpStatusCode = System.Net.HttpStatusCode.InternalServerError;
				}
			}

            return Task.FromResult(response);
		}

		public Task<T?> GetRecordById<T>(int id, string tableName)
        {
            var record = this.GetItemById(tableName, id);
            if (record.Result.Item != null)
            {
                return Task.FromResult((T?)this.ReadFromRow<T>(record.Result.Item));
            }
            else
            {
                return Task.FromResult((T?)default);
            }
        }

		public Task<IEnumerable<T>> GetAllRecords<T>(string tableName)
		{
			ScanResponse allData = GetAllItems(tableName).Result;
			return Task.FromResult(ReadFromRows<T>(allData));
		}

		public Task<T?> GetTJoinedToX<T, X>(int id, string joins)
        {
			string[] joinsSplit = joins.Split(':');
			string table1 = joinsSplit[0];
			string table2 = joinsSplit[1];
			T? thisItem = GetRecordById<T>(id, table1).Result;
			IEnumerable<X> allJoinItems = GetAllRecords<X>(table2).Result;

			if (thisItem != null)
			{
				for (int i = 2; i < joinsSplit.Length; i++)
				{
					// joinSplit needs to be .. "Table1IdField/Table1NameField/Table2IdField/Table2NameField"
					string[] thisJoin = joinsSplit[i].Split("/");
					X? thisJoinItem = FilterByProperty<X>(
						allJoinItems,                                               // find in items from Table2
						typeof(X)?.GetProperty(thisJoin[2])?.Name,                  // where Table2IdField
						typeof(T)?.GetProperty(thisJoin[0])?.GetValue(thisItem))    // equals Table1IdField of T
						.FirstOrDefault();

					if (thisJoinItem == null)
					{
						// could not find a row in X matching the join e.g. a game has a TmId of 11 but no Team exists with that id
						typeof(T).GetProperty(thisJoin[1])?.SetValue(thisItem, null);               // set the value of X.Table2NameField to be null
					}
					else
					{
						object? value = typeof(X)?.GetProperty(thisJoin[3])?.GetValue(thisJoinItem);    // Get X.Table2NameField
						typeof(T).GetProperty(thisJoin[1])?.SetValue(thisItem, value);                   // set the value of X.Table2NameField to be value
					}
				}
			}
			return Task.FromResult(thisItem);
		}

		public static IEnumerable<T> FilterByProperty<T>(IEnumerable<T> items, string? propertyName, object? value)
        {
			ArgumentNullException.ThrowIfNull(propertyName);
			ArgumentNullException.ThrowIfNull(value);
			var prop = typeof(T).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
			return prop == null
				? throw new ArgumentException($"Property '{propertyName}' not found on type '{typeof(T).Name}'.")
				: items.Where(item => Equals(prop.GetValue(item), value));
		}

		public object ReadFromRow<T>(Dictionary<string, AttributeValue> row)
        {
            Type thisType = typeof(T);
            T item = Activator.CreateInstance<T>();
            if (item == null)
            {
                throw new NullReferenceException("ReadFromRow could not create an instance of type " + thisType.Name);
            }
            else
            {
                foreach (PropertyInfo propertyInfo in thisType.GetProperties())
                {
                    if (!propertyInfo.CustomAttributes.Select(a => a.AttributeType).Contains(typeof(NoDbAttribute)))
                    {
                        switch (Type.GetTypeCode(propertyInfo.PropertyType))
                        {
                            case TypeCode.Int32:
                                thisType.GetProperty(propertyInfo.Name)?.SetValue(item, int.Parse(row[propertyInfo.Name].N));
                                break;
                            case TypeCode.DateTime:
                                thisType.GetProperty(propertyInfo.Name)?.SetValue(item, DateTime.Parse(row[propertyInfo.Name].S, ukFormat));
                                break;
                            case TypeCode.String:
                                thisType.GetProperty(propertyInfo.Name)?.SetValue(item, row[propertyInfo.Name].S);
                                break;
                            case TypeCode.Boolean:
                                thisType.GetProperty(propertyInfo.Name)?.SetValue(item, row[propertyInfo.Name].BOOL);
                                break;
                            default:
                                throw new Exception("ReadFromRow does not recognise property value type: " + Type.GetTypeCode(propertyInfo.PropertyType)
                                    + " for property: " + propertyInfo.Name);
                        }
                    }
                }
                return item;
            }
        }

        public IEnumerable<T> ReadFromRows<T>(ScanResponse allData)
        {
            List<T> list = [];
            foreach (var row in allData.Items)
            {
                list.Add((T)ReadFromRow<T>(row));
            }
            return list.AsEnumerable();
        }

        public Dictionary<string, AttributeValue> WriteToItem<T>(T record)
        {
            Type thisType = typeof(T);
            Dictionary<string, AttributeValue> item = [];

            foreach (PropertyInfo propertyInfo in thisType.GetProperties())
            {
                if (!propertyInfo.CustomAttributes.Select(a => a.AttributeType).Contains(typeof(NoDbAttribute)))
                {
                    switch (Type.GetTypeCode(propertyInfo.PropertyType))
                    {
                        case TypeCode.Int32:
                            item.Add(propertyInfo.Name, new AttributeValue { N = typeof(T).GetProperty(propertyInfo.Name)?.GetValue(record)?.ToString() });
                            break;
                        case TypeCode.DateTime:
                            string dateValue = typeof(T).GetProperty(propertyInfo.Name)?.GetValue(record)?.ToString() ?? ""; // get the date string
                            DateTime dateParsed = DateTime.Parse(dateValue); // convert the date to a datetime
                            item.Add(propertyInfo.Name, new AttributeValue { S = dateParsed.ToString(ukFormat) }); // set the value as the datetime in uk format
                            break;
                        case TypeCode.String:
                            item.Add(propertyInfo.Name, new AttributeValue { S = typeof(T).GetProperty(propertyInfo.Name)?.GetValue(record)?.ToString() });
                            break;
                        case TypeCode.Boolean:
                            item.Add(propertyInfo.Name, new AttributeValue { BOOL = (bool?)typeof(T).GetProperty(propertyInfo.Name)?.GetValue(record) });
                            break;
                        default:
                            throw new Exception("WriteToItem does not recognise property value type: " + Type.GetTypeCode(propertyInfo.PropertyType)
                                    + " for property: " + propertyInfo.Name);
                    }
                }
            }
            return item;
        }

        public async Task<T?> GetLatestRecord<T>(string tableName, string orderProperty)
        {
            var allRecords = await GetAllRecords<T>(tableName);
            if (allRecords.Any())
            {
                PropertyInfo? propertyInfo = typeof(T).GetProperty(orderProperty);
                if (propertyInfo != null)
                {
                    object keySelector(T x) =>
#pragma warning disable CS8603 // Possible null reference return.
                        propertyInfo.GetValue(x, null);
#pragma warning restore CS8603 // Possible null reference return.


                    return allRecords.OrderByDescending(keySelector).FirstOrDefault();
                }
                else
                {
                    return default;
                }
            }
            else
            {
                return default;
            }
        }

        public Task<DeleteItemResponse> DeleteRecordById(string tableName, int id)
        {
			throw new NotImplementedException();
		}

		private static List<Team> MockTeams()
		{
			return
			[
				new Team() { Name = "Team1", Id = 1, ActiveFrom = 1, HColour = "Black", HColourHex = "#555555", AColour = "White", AColourHex = "#FFFFFF" },
				new Team() { Name = "Team2", Id = 2, ActiveFrom = 1, HColour = "Green", HColourHex = "#115151", AColour = "Blue", AColourHex = "#98765" },
				new Team() { Name = "Team3", Id = 3, ActiveFrom = 2, HColour = "Black", HColourHex = "#555555", AColour = "White", AColourHex = "#FFFFFF" },
				new Team() { Name = "Team4", Id = 4, ActiveFrom = 2, HColour = "Green", HColourHex = "#115151", AColour = "Blue", AColourHex = "#98765" }
			];
		}

		private static List<Save> MockSaves()
		{
			return
			[
				new Save() { Id = 1, Name = "Save1", GmDate = new DateTime(2025,1,1), PlayerTm = 1, Season = 1 },
				new Save() { Id = 2, Name = "Save2", GmDate = new DateTime(2026,2,2), PlayerTm = 2, Season = 2 }
			];
		}

		private static List<Game> MockGames()
		{
			return
			[
				new Game() { Id = 1, Colours = ["Blue", "Yellow"], GmDate = new DateTime(2025,1,1),
                    Played = true, Round = 1, Season = 1, Sc1 = 1, Sc2 = 2, Tm1 = 1, Tm2 = 2 },
				new Game() { Id = 2, Colours = ["Red", "Green"], GmDate = new DateTime(2026,2,2),
					Played = false, Round = 2, Season = 2, Sc1 = 3, Sc2 = 4, Tm1 = 3, Tm2 = 4 },
				new Game() { Id = 3, Colours = ["Black", "White"], GmDate = new DateTime(2027,3,3),
					Played = false, Round = 3, Season = 3, Sc1 = 5, Sc2 = 6, Tm1 = 1, Tm2 = 2 }
			];
		}

		private static List<HealthCheck> MockHealthChecks()
		{
			return
			[
				new HealthCheck() { Id = 1, HCDate = new DateTime(2025,1,1) },
				new HealthCheck() { Id = 2, HCDate = new DateTime(2026,2,2) }
			];
		}
	}
}

