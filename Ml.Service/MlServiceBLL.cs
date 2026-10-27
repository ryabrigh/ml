using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Ml.Data;
using Ml.IData;
using Ml.Models;
using System.Globalization;
using System.Reflection;

namespace Ml.Service
{
    public class MlServiceBLL(IRepository repository)
	{
		private readonly IFormatProvider ukFormat = new CultureInfo("en-GB");

        public async Task<APIGatewayProxyResponse> Crud(JToken serviceData, ILambdaContext? context)
        {
            APIGatewayProxyResponse response = new();
            CrudRequest? crudRequest = serviceData.ToObject<CrudRequest>();
            string responseMessage;

            if (crudRequest == null)
            {
                responseMessage = "Request did not contain a proper crud request object";
                context?.Logger.LogInformation(responseMessage);
                response.StatusCode = 500;
                response.Body = responseMessage;
            }
            else
            {
                try
                {
                    string typeDescription = "Ml.Models." + crudRequest.Model + ", Ml.Models";
                    Type? crudType = Type.GetType(typeDescription);
                    if (crudType == null)
                    {
                        responseMessage = "Invalid type provided";
                        context?.Logger.LogInformation(responseMessage);
                        response.StatusCode = 500;
                        response.Body = responseMessage;
                        return response;
                    }
                    MethodInfo? crudMethod = typeof(MlServiceBLL).GetMethod("GenericCrud") ?? throw new NullReferenceException("Can't find GenericCrud method");
                    MethodInfo genericCrudMethod = crudMethod.MakeGenericMethod(crudType);
                    object? methodResponse = genericCrudMethod.Invoke(this, [crudRequest, context]);

                    if (methodResponse is Task task)
                    {
                        // Await the task dynamically and get the result
                        await task.ConfigureAwait(false);

                        // Use reflection to get the Result property of Task<T>
                        PropertyInfo? resultProperty = task.GetType().GetProperty("Result");
                        object? resultValue = resultProperty?.GetValue(task);

                        if (resultValue is null)
                        {
                            throw new NullReferenceException("Result of GenericCrud method is null");
                        }
                        else
                        {
                            response = (APIGatewayProxyResponse)resultValue;
                        }
                    }
                }
                catch (Exception ex)
                {
                    responseMessage = "Crud has errored: " + ex.Message;
                    context?.Logger.LogError(responseMessage);
                    response.StatusCode = 500;
                    response.Body = responseMessage;
                }
            }
            return response;
        }

        public async Task<APIGatewayProxyResponse> GenericCrud<T>(CrudRequest crudRequest, ILambdaContext? context)
        {
            APIGatewayProxyResponse response = new();
            string responseMessage = "";
            Type thisType = typeof(T);
            string tType = thisType.Name;

            try
            {
                switch (crudRequest.Crud)
                {
                    case "C":
                        responseMessage += "\nCreating new " + tType;
                        T? newItem = crudRequest.CrudData.ToObject<T>();
                        if (newItem == null)
                        {
                            responseMessage += "\nInvalid crud data " + crudRequest.CrudData;
                            response.StatusCode = 500;
                            break;
                        }
                        T? lastItem = await repository.GetLatestRecord<T>(crudRequest.TableName, "Id");
                        int newId = 1;
                        if (lastItem != null)
                        {
                            PropertyInfo? idProperty = thisType.GetProperty("Id");
                            if (idProperty != null)
                            {
                                object? idValue = idProperty.GetValue(lastItem);
                                if (idValue != null)
                                {
                                    bool parseInt = Int32.TryParse(idValue.ToString(), out int lastId);
                                    newId = lastId + 1;
                                }
                            }
                        }
                        thisType.GetProperty("Id")?.SetValue(newItem, newId);
                        PutItemResponse writeResponse = await repository.WriteRecord<T>(crudRequest.TableName, newItem);
                        responseMessage += "\nHttp response of write: " + writeResponse.HttpStatusCode.ToString();
                        response.StatusCode = writeResponse.HttpStatusCode.ToString() == "OK" ? 200 : 500;
                        break;
                    case "R":
                        responseMessage += "\nReading " + tType;
                        T? thisItem;
                        thisItem = await repository.GetRecordById<T>(crudRequest.Id, crudRequest.TableName);
                        if (thisItem == null)
                        {
                            responseMessage += "\nNo item found for Id: " + crudRequest.Id.ToString();
                        }
                        else
                        {
                            context?.Logger.LogInformation(responseMessage);
                            response.StatusCode = 200;
                            response.Body = JsonConvert.SerializeObject(thisItem);
                            return response;
                        }
                        break;
                    case "U":
                        responseMessage += "\nUpdating " + tType;
                        thisItem = await repository.GetRecordById<T>(crudRequest.Id, crudRequest.TableName);
                        newItem = crudRequest.CrudData.ToObject<T>();
                        if (newItem == null)
                        {
                            responseMessage += "\nInvalid crud data " + crudRequest.CrudData;
                            response.StatusCode = 500;
                            break;
                        }
                        writeResponse = await repository.WriteRecord<T>(crudRequest.TableName, newItem);
                        responseMessage += "\nHttp response of write: " + writeResponse.HttpStatusCode.ToString();
                        response.StatusCode = writeResponse.HttpStatusCode.ToString() == "OK" ? 200 : 500;
                        break;
                    case "D":
                        thisItem = await repository.GetRecordById<T>(crudRequest.Id, crudRequest.TableName);
                        if (thisItem == null)
                        {
                            responseMessage += "\nNo item found for Id: " + crudRequest.Id.ToString();
                        }
                        else
                        {
                            DeleteItemResponse deleteResponse = await repository.DeleteRecordById(crudRequest.TableName, crudRequest.Id);
                            responseMessage += "\nHttp response of delete: " + deleteResponse.HttpStatusCode.ToString();
                            response.StatusCode = deleteResponse.HttpStatusCode.ToString() == "OK" ? 200 : 500;
                        }
                        break;
                    case "RA":
                        // Read all
                        responseMessage += "\nReading all " + tType;
                        IEnumerable<T> items = await repository.GetAllRecords<T>(crudRequest.TableName);
                        if (items == null)
                        {
                            responseMessage += "\nNo items found for table: " + crudRequest.TableName;
                        }
                        else
                        {
                            IEnumerable<T> sortedItems = items;
                            if (crudRequest.SortField != "")
                            {
                                var sortField = crudRequest.SortField;
                                bool descending = false;
                                if (crudRequest.SortField[0] == '-')
                                {
                                    // descending
                                    descending = true;
                                    sortField = sortField.Substring(1);
                                }
                                PropertyInfo? propertyInfo = typeof(T).GetProperty(sortField);
                                if (propertyInfo != null)
                                {
                                    object keySelector(T x) =>
#pragma warning disable CS8603 // Possible null reference return.
                                        propertyInfo.GetValue(x, null);
#pragma warning restore CS8603 // Possible null reference return.

                                    sortedItems = descending ? items.OrderByDescending(keySelector) : items.OrderBy(keySelector);
                                }
                            }
                            context?.Logger.LogInformation(responseMessage);
                            response.StatusCode = 200;
                            response.Body = JsonConvert.SerializeObject(sortedItems);
                            return response;
                        }
                        break;
                    default:
                        responseMessage += "\nInvalid crud type " + crudRequest.Crud;
                        response.StatusCode = 500;
                        break;
                }
            }
            catch (Exception ex)
            {
                responseMessage += "\nGeneric Crud has errored: " + ex.Message;
                response.StatusCode = 500;
            }

            context?.Logger.LogInformation(responseMessage);
            response.Body = responseMessage;
            return response;
        }

        private static bool IsValXorYInCollectionAorB(IEnumerable<int> A, IEnumerable<int> B, int X, int Y)
        {
            return A.Contains(X) || A.Contains(Y) || B.Contains(X) || B.Contains(Y);
        }
    }
}

