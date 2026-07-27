using Microsoft.AspNetCore.Mvc;
using VehicleReId.Api.Models;
using VehicleReId.Api.Services;

namespace VehicleReId.Api.Controllers;

[ApiController]
[Route("api/reid")]
public class ReIdController : ControllerBase
{
    private readonly ReIdService _svc;

    public ReIdController(ReIdService svc) => _svc = svc;

    [HttpGet("health")]
    public IActionResult Health() => Ok(new { ok = true, service = "vehicle-reid" });

    [HttpPost("enroll")]
    public async Task<ActionResult<EnrollResponse>> Enroll([FromBody] EnrollRequest req)
        => Ok(await _svc.EnrollAsync(req));

    [HttpPost("match")]
    public async Task<ActionResult<MatchResponse>> Match([FromBody] MatchRequest req)
        => Ok(await _svc.MatchAsync(req));
}
