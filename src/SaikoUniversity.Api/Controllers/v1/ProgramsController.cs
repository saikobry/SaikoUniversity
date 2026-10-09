using Microsoft.AspNetCore.Mvc;
using SaikoUniversity.Api.Contracts.v1;
using SaikoUniversity.Application.Common;
using SaikoUniversity.Application.Programs;
using SaikoUniversity.Application.Programs.Dtos;

namespace SaikoUniversity.Api.Controllers.v1;

[ApiController]
[Route("api/v1/programs")]
[Produces("application/json")]
public sealed class ProgramsController(ProgramService programs) : ControllerBase
{
    /// <summary>
    /// Lists programs, filtered and paged.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<ProgramListResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProgramListResponse>> GetAll(
        [FromQuery] string? search,
        [FromQuery] Guid? academicLevelId,
        [FromQuery] Guid? departmentId,
        [FromQuery] bool? canAcceptStudentsOnly,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var filter = new ProgramFilter(search, academicLevelId, departmentId, canAcceptStudentsOnly, page, pageSize);

        var result = await programs.GetAllAsync(filter, cancellationToken);

        return Ok(ProgramListResponse.From(result));
    }

    /// <summary>
    /// Returns one program, or 404 when it does not exist.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<ProgramResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProgramResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var program = await programs.GetByIdAsync(id, cancellationToken);

        if (program is null)
        {
            return NotFound();
        }

        return Ok(ProgramResponse.From(program));
    }

    /// <summary>
    /// Creates a program.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<ProgramResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProgramResponse>> Create(
        [FromBody] Contracts.v1.CreateProgramRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new SaikoUniversity.Application.Programs.Dtos.CreateProgramRequest(
            request.Code,
            request.Name,
            request.AcademicLevelId,
            request.DepartmentId,
            request.Regulator,
            request.RequiredCredits,
            request.CreditBasis,
            request.IsRegulatorRecognised);

        var result = await programs.CreateAsync(command, cancellationToken);

        if (!result.Succeeded)
        {
            return ToActionResult(result.Error!);
        }

        var created = ProgramResponse.From(result.Value!);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Updates a program's name, required credits, recognition, and lifecycle.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<ProgramResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProgramResponse>> Update(
        Guid id,
        [FromBody] Contracts.v1.UpdateProgramRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new SaikoUniversity.Application.Programs.Dtos.UpdateProgramRequest(
            request.Name,
            request.RequiredCredits,
            request.IsRegulatorRecognised,
            request.IsRetired);

        var result = await programs.UpdateAsync(id, command, cancellationToken);

        if (!result.Succeeded)
        {
            return ToActionResult(result.Error!);
        }

        // Re-read so the response carries the level code and department name the update path does
        // not load.
        var updated = await programs.GetByIdAsync(id, cancellationToken);

        return Ok(ProgramResponse.From(updated!));
    }

    private ActionResult ToActionResult(Error error) => error.Code switch
    {
        ErrorCode.ProgramCodeAlreadyExists => Conflict(new ErrorResponse(error.Code.ToString(), error.Message)),
        ErrorCode.ProgramInvalidAcademicLevel or ErrorCode.ProgramInvalidDepartment
            => BadRequest(new ErrorResponse(error.Code.ToString(), error.Message)),
        ErrorCode.ProgramNotFound => NotFound(new ErrorResponse(error.Code.ToString(), error.Message)),
        _ => BadRequest(new ErrorResponse(error.Code.ToString(), error.Message))
    };
}
