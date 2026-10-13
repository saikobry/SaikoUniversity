using Microsoft.AspNetCore.Mvc;
using Api.Contracts.v1;
using Application.ReferenceData;

namespace Api.Controllers.v1;

[ApiController]
[Route("api/v1/reference-data")]
[Produces("application/json")]
public sealed class ReferenceDataController(ReferenceDataService referenceData) : ControllerBase
{
    [HttpGet("academic-levels")]
    [ProducesResponseType<IReadOnlyList<ReferenceItemResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ReferenceItemResponse>>> GetAcademicLevels(
        CancellationToken cancellationToken = default)
    {
        var items = await referenceData.GetActiveAcademicLevelsAsync(cancellationToken);

        return Ok(items.Select(ReferenceItemResponse.From).ToList());
    }

    [HttpGet("departments")]
    [ProducesResponseType<IReadOnlyList<ReferenceItemResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ReferenceItemResponse>>> GetDepartments(
        CancellationToken cancellationToken = default)
    {
        var items = await referenceData.GetActiveDepartmentsAsync(cancellationToken);

        return Ok(items.Select(ReferenceItemResponse.From).ToList());
    }
}
