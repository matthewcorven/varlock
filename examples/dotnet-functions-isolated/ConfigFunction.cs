using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using System.Net;

namespace dotnet_functions_isolated;

// 👈 Varlock: HTTP-triggered function that displays configuration values loaded from .env.schema
public class ConfigFunction(IConfiguration configuration)
{
    // Limit the HTTP response to non-sensitive values.
    private static readonly string[] Keys = ["APP_NAME", "FUNCTIONS_ONLY_KEY"];

    [Function("Config")]
    public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

        response.WriteString("Functions non-sensitive configuration:\n");
        foreach (var key in Keys)
        {
            var value = configuration[key] ?? "(null)";
            response.WriteString($"  {key} = {value}\n");
        }

        return response;
    }
}
