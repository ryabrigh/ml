using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rby.Data;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Rby.Service;

public class Function
{

    /// <summary>
    /// Lambda function handler
    /// </summary>
    /// <param name="request">An APIGatewayProxyRequest for the Lambda function handler to process.</param>
    /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
    /// <returns>APIGatewayProxyResponse</returns>
    public static APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        context.Logger.LogInformation(request.Body);
        APIGatewayProxyResponse response = new();
        RbyServiceBLL _serviceBLL = new(new RepositoryDynamo());

        var serviceRequest = JsonConvert.DeserializeObject<ServiceRequest>(request.Body);

        if (serviceRequest == null || serviceRequest.ServiceData == null)
        {
            string message = "Request did not contain a proper service request";
            context.Logger.LogInformation(message);
            response.StatusCode = 500;
            response.Body = message;
            return response;
        }

        switch (serviceRequest.ServiceType)
        {
            case "HealthCheck":
                context.Logger.LogInformation("HealthCheck request received");
                response = _serviceBLL.GetHealthCheck(serviceRequest.ServiceData, context).GetAwaiter().GetResult();
                break;
            case "CRUD":
                context.Logger.LogInformation("CRUD request received");
                response = _serviceBLL.Crud(serviceRequest.ServiceData, context).GetAwaiter().GetResult();
                break;
            case "Schedule":
                context.Logger.LogInformation("Schedule request received");
                response = _serviceBLL.Schedule(serviceRequest.ServiceData, context).GetAwaiter().GetResult();
                break;
            case "TodaysGames":
                context.Logger.LogInformation("TodaysGames request received");
                response = _serviceBLL.TodaysGames(serviceRequest.ServiceData, context).GetAwaiter().GetResult();
                break;
            case "PlayGame":
                context.Logger.LogInformation("PlayGame request received");
                response = _serviceBLL.PlayGame(serviceRequest.ServiceData, context).GetAwaiter().GetResult();
                break;
            case "Standings":
                context.Logger.LogInformation("Standings request received");
                response = _serviceBLL.Standings(serviceRequest.ServiceData, context).GetAwaiter().GetResult();
                break;
            default:
                string message = "Invalid service type received";
                context.Logger.LogInformation(message);
                response.StatusCode = 500;
                response.Body = message;
                break;
        }

        return response;
    }

    public class ServiceRequest
    {
        public string ServiceType { get; set; } = string.Empty;
        public JToken ServiceData { get; set; } = string.Empty;
    }
}