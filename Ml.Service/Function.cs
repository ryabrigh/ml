using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Ml.Data;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Ml.Service;

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
        MlServiceBLL _serviceBLL = new(new RepositoryDynamo());

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
            case "Exist":
                context.Logger.LogInformation("Exist request received");
                response.StatusCode = 200;
                response.Body = "Lambda function exists";
                break;
            case "CRUD":
                context.Logger.LogInformation("CRUD request received");
                response = _serviceBLL.Crud(serviceRequest.ServiceData, context).GetAwaiter().GetResult();
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