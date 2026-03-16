using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Options;

namespace DotnetFunctionsIsolatedNet8;

public sealed class HealthFunction
{
  private readonly IOptionsSnapshot<VarlockFunctionsOptions> _options;

  public HealthFunction(IOptionsSnapshot<VarlockFunctionsOptions> options)
  {
    _options = options;
  }

  [Function("Health")]
  public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
  {
    var response = req.CreateResponse(HttpStatusCode.OK);
    response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
    response.WriteString($"OK - {_options.Value.AppName}");
    return response;
  }
}
