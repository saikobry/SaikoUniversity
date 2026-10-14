using Microsoft.AspNetCore.Mvc;
using Api.Contracts.v1;
using Application.Common;
using Application.Programs;
using Application.Programs.Dtos;

namespace Api.Controllers.v1;

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
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProgramResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var program = await programs.GetByIdAsync(id, cancellationToken);

        if (program is null)
        {
            // The generated API client types this response as ErrorResponse, so it must carry a
            // body. A bare NotFound() advertises a contract the server then does not honour.
            return NotFound(new ErrorResponse(
                ErrorCode.ProgramNotFound.ToString(),
                $"Program '{id}' was not found."));
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
        var command = new Application.Programs.Dtos.CreateProgramRequest(
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
    /// Updates a program's name and required credits.
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
        var command = new Application.Programs.Dtos.UpdateProgramRequest(
            request.Name,
            request.RequiredCredits);

        var result = await programs.UpdateAsync(id, command, cancellationToken);

        if (!result.Succeeded)
        {
            return ToActionResult(result.Error!);
        }

        return Ok(ProgramResponse.From(result.Value!));
    }

    /// <summary>
    /// Retires an academic program.
    /// </summary>
    [HttpPost("{id:guid}/retire")]
    [ProducesResponseType<ProgramResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProgramResponse>> Retire(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await programs.RetireAsync(id, cancellationToken);

        if (!result.Succeeded)
        {
            return ToActionResult(result.Error!);
        }

        return Ok(ProgramResponse.From(result.Value!));
    }

    /// <summary>
    /// Reinstates a retired academic program.
    /// </summary>
    [HttpPost("{id:guid}/reinstate")]
    [ProducesResponseType<ProgramResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProgramResponse>> Reinstate(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await programs.ReinstateAsync(id, cancellationToken);

        if (!result.Succeeded)
        {
            return ToActionResult(result.Error!);
        }

        return Ok(ProgramResponse.From(result.Value!));
    }

    /// <summary>
    /// Marks an academic program as recognized by its regulator.
    /// </summary>
    [HttpPost("{id:guid}/recognise")]
    [ProducesResponseType<ProgramResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProgramResponse>> Recognise(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await programs.RecogniseAsync(id, cancellationToken);

        if (!result.Succeeded)
        {
            return ToActionResult(result.Error!);
        }

        return Ok(ProgramResponse.From(result.Value!));
    }

    /// <summary>
    /// Withdraws regulator recognition from an academic program.
    /// </summary>
    [HttpPost("{id:guid}/withdraw-recognition")]
    [ProducesResponseType<ProgramResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProgramResponse>> WithdrawRecognition(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await programs.WithdrawRecognitionAsync(id, cancellationToken);

        if (!result.Succeeded)
        {
            return ToActionResult(result.Error!);
        }

        return Ok(ProgramResponse.From(result.Value!));
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
