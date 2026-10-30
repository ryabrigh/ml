using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Ml.IData;
using Ml.Models;
using System.Globalization;
using System.Reflection;

namespace Ml.Data
{
    public class RepositoryDynamo : IRepository
    {
        private readonly AmazonDynamoDBConfig _clientConfig;
        private readonly AmazonDynamoDBClient _client;
        private readonly IFormatProvider ukFormat = new CultureInfo("en-GB");

        public RepositoryDynamo()
        {
            _clientConfig = new AmazonDynamoDBConfig() { RegionEndpoint = RegionEndpoint.EUWest2 };
            _client = new AmazonDynamoDBClient(_clientConfig);
        }

        public async Task<GetItemResponse> GetItemById(string tableName, int id)
        {
            var request = new GetItemRequest
            {
                TableName = tableName,
                Key = new Dictionary<string, AttributeValue>()
                {
                    { "id", new AttributeValue { N = id.ToString() } }
                }
            };

            GetItemResponse response;
            response = await _client.GetItemAsync(request);
            return response;
        }

        public async Task<ScanResponse> GetAllItems(string tableName)
        {
            ScanRequest request = new()
            {
                TableName = tableName
            };

            ScanResponse response = await _client.ScanAsync(request);
            return response;
        }

        public async Task<PutItemResponse> WriteRecord<T>(string tableName, T record)
        {
            Dictionary<string, AttributeValue> item = WriteToItem<T>(record);
            PutItemRequest request = new()
            {
                TableName = tableName,
                Item = item
            };

            PutItemResponse response = await _client.PutItemAsync(request);
            return response;
        }

        public async Task<BatchWriteItemResponse> WriteRecords<T>(string tableName, List<T> items)
        {
            BatchWriteItemResponse response = new();

            List <WriteRequest> writeRequests = [];
            foreach (T item in items)
            {
                Dictionary<string, AttributeValue> convertedItem = WriteToItem(item);
                writeRequests.Add(new WriteRequest
                {
                    PutRequest = new PutRequest
                    {
                        Item = convertedItem
                    }
                });
            }


            // loop through
            for (int i = 0; i < writeRequests.Count; i += 25)
            {
                // Take up to 'batchSize' items, but less if fewer remain
                List<WriteRequest> batch = [..writeRequests.Skip(i).Take(25)];


                BatchWriteItemRequest request = new()
                {
                    RequestItems = new Dictionary<string, List<WriteRequest>>
                    {
                        { 
                            tableName,
                            batch
                        }
                    }
                };
                bool unprocessedItems = true;
                while (unprocessedItems)
                {
                    response = await _client.BatchWriteItemAsync(request);
                    if (response.UnprocessedItems?.Count > 0)
                    {
                        unprocessedItems = true;
                        request = new()
                        {
                            RequestItems = response.UnprocessedItems
                        };
                    }
                    else
                    {
                        unprocessedItems = false;
                    }
                }
            }
            return response;
        }

        public async Task<T?> GetRecordById<T>(int id, string tableName)
        {
            var record = await this.GetItemById(tableName, id);
            if (record.Item != null)
            {
                return (T)this.ReadFromRow<T>(record.Item);
            }
            else
            {
                return default;
            }
        }

        public async Task<T?> GetTJoinedToX<T, X>(int id, string joins)
        {
            string[] joinsSplit = joins.Split(':');
            string table1 = joinsSplit[0];
            string table2 = joinsSplit[1];
            T? thisItem = await GetRecordById<T>(id, table1);
            IEnumerable<X> allJoinItems = await GetAllRecords<X>(table2);

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
            return thisItem;
        }

        public async Task<IEnumerable<T>?> GetAllTJoinedToX<T, X>(IEnumerable<T>? existingItems, string joinTable, string joins)
        {
            var allJoinItems = await GetAllRecords<X>(joinTable);

            if (existingItems != null)
            {
                foreach (T thisItem in existingItems)
                {
                    // joinSplit needs to be .. "Table1IdField/Table1NameField/Table2IdField/Table2NameField"
                    string[] thisJoin = joins.Split("/");
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
            return existingItems;
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

        public async Task<IEnumerable<T>> GetAllRecords<T>(string tableName)
        {
            ScanResponse allData = await GetAllItems(tableName);
            return ReadFromRows<T>(allData);
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
                            case TypeCode.Decimal:
                                thisType.GetProperty(propertyInfo.Name)?.SetValue(item, Decimal.Parse(row[propertyInfo.Name].N));
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
                        case TypeCode.Decimal:
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

        public async Task<DeleteItemResponse> DeleteRecordById(string tableName, int id)
        {
            var request = new DeleteItemRequest
            {
                TableName = tableName,
                Key = new Dictionary<string, AttributeValue>()
                {
                    { "id", new AttributeValue { N = id.ToString() } }
                }
            };

            DeleteItemResponse response;
            response = await _client.DeleteItemAsync(request);
            return response;
        }
	}
}

