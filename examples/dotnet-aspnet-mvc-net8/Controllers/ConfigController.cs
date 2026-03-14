using DotnetAspNetMvcNet8;
using Microsoft.AspNetCore.Mvc;

namespace DotnetAspNetMvcNet8.Controllers;

[ApiController]
[Route("config")]
public sealed class ConfigController : ControllerBase
{
  private readonly IConfiguration _configuration;

  public ConfigController(IConfiguration configuration)
  {
    _configuration = configuration;
  }

  [HttpGet]
  public ActionResult<AppConfigSnapshot> Get()
  {
    return AppConfigSnapshot.From(_configuration);
  }
}