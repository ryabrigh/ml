using Amazon.DynamoDBv2.Model;
using Ml.Models;

namespace Ml.IData
{
    public interface IRepository
    {
		public Task<GetItemResponse> GetItemById(string tableName, int id);
		public Task<ScanResponse> GetAllItems(string tableName);
		public Task<PutItemResponse> WriteRecord<T>(string tableName, T record);
		public Task<BatchWriteItemResponse> WriteRecords<T>(string tableName, List<T> items);
		public Task<T?> GetRecordById<T>(int id, string tableName);
		public Task<T?> GetTJoinedToX<T, X>(int id, string joins);
		public Task<IEnumerable<T>?> GetAllTJoinedToX<T, X>(IEnumerable<T>? existingItems, string joinTable, string joins);
        public static abstract IEnumerable<T> FilterByProperty<T>(IEnumerable<T> items, string? propertyName, object? value);
		public Task<IEnumerable<T>> GetAllRecords<T>(string tableName);
		public object ReadFromRow<T>(Dictionary<string, AttributeValue> row);
		public IEnumerable<T> ReadFromRows<T>(ScanResponse allData);
		public Dictionary<string, AttributeValue> WriteToItem<T>(T record);
		public Task<T?> GetLatestRecord<T>(string tableName, string orderProperty);
		public Task<DeleteItemResponse> DeleteRecordById(string tableName, int id);
	}
}
