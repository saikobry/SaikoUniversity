using Microsoft.EntityFrameworkCore;
using Application.Abstractions;
using Application.Common;
using Application.Programs.Dtos;
using Domain.Entities;
using Domain.Enums;

// EF.Functions is used for case-insensitive LIKE. This project references the shared
// Microsoft.EntityFrameworkCore package, not the SQL Server provider, so the Application layer
// compiles and tests without a database-specific dependency.
#pragma warning disable CA2101

namespace Application.Programs;

public sealed class ProgramService(IAppDbContext context)
{
    /// <summary>
    /// Returns a page of programs. A read, so it returns a value and never throws for a filter
    /// that matches nothing.
    /// </summary>
    public async Task<PagedResult<ProgramDto>> GetAllAsync(
        ProgramFilter filter,
        CancellationToken cancellationToken = default)
    {
        var normalised = filter.Normalised();

        // Include returns IIncludableQueryable, which is narrower than the variable. Cast once here so
        // the filtering steps below read as plain IQueryable.
        IQueryable<AcademicProgram> query = context.AcademicPrograms
            .AsNoTracking()
            .Include(x => x.AcademicLevel!)
            .Include(x => x.Department!);

        if (!string.IsNullOrWhiteSpace(normalised.Search))
        {
            var term = normalised.Search.Trim();
            query = query.Where(x =>
                EF.Functions.Like(x.Code, $"%{term}%") ||
                EF.Functions.Like(x.Name, $"%{term}%"));
        }

        if (normalised.AcademicLevelId is { } levelId)
        {
            query = query.Where(x => x.AcademicLevelId == levelId);
        }

        if (normalised.DepartmentId is { } departmentId)
        {
            query = query.Where(x => x.DepartmentId == departmentId);
        }

        if (normalised.CanAcceptStudents is { } canAcceptStudents)
        {
            query = canAcceptStudents
                ? query.Where(x => x.IsRegulatorRecognised && x.Lifecycle == ProgramLifecycle.Active)
                : query.Where(x => !x.IsRegulatorRecognised || x.Lifecycle != ProgramLifecycle.Active);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(x => x.Code)
            .Skip((normalised.Page - 1) * normalised.PageSize)
            .Take(normalised.PageSize)
            .Select(x => new ProgramDto(
                x.Id,
                x.Code,
                x.Name,
                x.AcademicLevelId,
                x.AcademicLevel!.Code,
                x.DepartmentId,
                x.Department!.Name,
                x.Regulator.ToString(),
                x.IsRegulatorRecognised,
                x.Lifecycle.ToString(),
                x.RequiredCredits,
                x.CreditBasis.ToString(),
                x.Lifecycle == ProgramLifecycle.Active && x.IsRegulatorRecognised))
            .ToListAsync(cancellationToken);

        return new PagedResult<ProgramDto>(items, totalCount, normalised.Page, normalised.PageSize);
    }

    /// <summary>
    /// Updates a program's mutable fields. A write, so an unknown id comes back as a
    /// <see cref="Result{T}"/> failure rather than a null.
    /// </summary>
    public async Task<Result<ProgramDto>> UpdateAsync(
        Guid id,
        UpdateProgramRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Result<ProgramDto>.Failure(ErrorCode.Unknown, "Name is required.");
        }

        if (request.RequiredCredits <= 0)
        {
            return Result<ProgramDto>.Failure(ErrorCode.Unknown, "RequiredCredits must be greater than zero.");
        }

        // Navigations are loaded here so the returned DTO is complete. Returning a DTO with placeholder
        // values would push the burden onto every caller.
        var program = await context.AcademicPrograms
            .Include(x => x.AcademicLevel!)
            .Include(x => x.Department!)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (program is null)
        {
            return Result<ProgramDto>.Failure(
                ErrorCode.ProgramNotFound,
                $"Program '{id}' does not exist.");
        }

        program.Rename(request.Name.Trim());
        program.SetRequiredCredits(request.RequiredCredits);

        await context.SaveChangesAsync(cancellationToken);

        return Result<ProgramDto>.Success(ToDto(program));
    }

    /// <summary>
    /// Retires an academic program.
    /// </summary>
    public async Task<Result<ProgramDto>> RetireAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var program = await context.AcademicPrograms
            .Include(x => x.AcademicLevel!)
            .Include(x => x.Department!)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (program is null)
        {
            return Result<ProgramDto>.Failure(
                ErrorCode.ProgramNotFound,
                $"Program '{id}' does not exist.");
        }

        if (program.Retire())
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        return Result<ProgramDto>.Success(ToDto(program));
    }

    /// <summary>
    /// Reinstates a retired academic program.
    /// </summary>
    public async Task<Result<ProgramDto>> ReinstateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var program = await context.AcademicPrograms
            .Include(x => x.AcademicLevel!)
            .Include(x => x.Department!)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (program is null)
        {
            return Result<ProgramDto>.Failure(
                ErrorCode.ProgramNotFound,
                $"Program '{id}' does not exist.");
        }

        if (program.Reinstate())
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        return Result<ProgramDto>.Success(ToDto(program));
    }

    /// <summary>
    /// Marks an academic program as recognized by its regulator.
    /// </summary>
    public async Task<Result<ProgramDto>> RecogniseAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var program = await context.AcademicPrograms
            .Include(x => x.AcademicLevel!)
            .Include(x => x.Department!)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (program is null)
        {
            return Result<ProgramDto>.Failure(
                ErrorCode.ProgramNotFound,
                $"Program '{id}' does not exist.");
        }

        if (program.Recognise())
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        return Result<ProgramDto>.Success(ToDto(program));
    }

    /// <summary>
    /// Withdraws regulator recognition from an academic program.
    /// </summary>
    public async Task<Result<ProgramDto>> WithdrawRecognitionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var program = await context.AcademicPrograms
            .Include(x => x.AcademicLevel!)
            .Include(x => x.Department!)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (program is null)
        {
            return Result<ProgramDto>.Failure(
                ErrorCode.ProgramNotFound,
                $"Program '{id}' does not exist.");
        }

        if (program.WithdrawRecognition())
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        return Result<ProgramDto>.Success(ToDto(program));
    }

    /// <summary>
    /// Returns one program, or <c>null</c> when it does not exist. Absence is an answer, not a
    /// failure, so this is a query and returns a nullable type rather than a
    /// <see cref="Result{T}"/>.
    /// </summary>
    public async Task<ProgramDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var program = await context.AcademicPrograms
            .AsNoTracking()
            .Include(x => x.AcademicLevel)
            .Include(x => x.Department)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return program is null ? null : ToDto(program);
    }

    /// <summary>
    /// Creates a program. A write, so every expected failure comes back as a
    /// <see cref="Result{T}"/> instead of an exception.
    /// </summary>
    public async Task<Result<ProgramDto>> CreateAsync(
        CreateProgramRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<Regulator>(request.Regulator, ignoreCase: true, out var regulator))
        {
            return Result<ProgramDto>.Failure(
                ErrorCode.Unknown,
                $"'{request.Regulator}' is not a valid regulator. Expected Ched or Tesda.");
        }

        if (!Enum.TryParse<CreditBasis>(request.CreditBasis, ignoreCase: true, out var creditBasis))
        {
            return Result<ProgramDto>.Failure(
                ErrorCode.Unknown,
                $"'{request.CreditBasis}' is not a valid credit basis. Expected Hours or Units.");
        }

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return Result<ProgramDto>.Failure(ErrorCode.Unknown, "Code is required.");
        }

        if (request.RequiredCredits <= 0)
        {
            return Result<ProgramDto>.Failure(ErrorCode.Unknown, "RequiredCredits must be greater than zero.");
        }

        var code = request.Code.Trim();

        if (await context.AcademicPrograms.AnyAsync(x => x.Code == code, cancellationToken))
        {
            return Result<ProgramDto>.Failure(
                ErrorCode.ProgramCodeAlreadyExists,
                $"A program with code '{code}' already exists.");
        }

        if (!await context.AcademicLevels.AnyAsync(x => x.Id == request.AcademicLevelId, cancellationToken))
        {
            return Result<ProgramDto>.Failure(
                ErrorCode.ProgramInvalidAcademicLevel,
                $"Academic level '{request.AcademicLevelId}' does not exist.");
        }

        if (!await context.Departments.AnyAsync(x => x.Id == request.DepartmentId, cancellationToken))
        {
            return Result<ProgramDto>.Failure(
                ErrorCode.ProgramInvalidDepartment,
                $"Department '{request.DepartmentId}' does not exist.");
        }

        var program = new AcademicProgram(
            code,
            request.Name.Trim(),
            request.AcademicLevelId,
            request.DepartmentId,
            regulator,
            request.RequiredCredits,
            creditBasis);

        if (request.IsRegulatorRecognised)
        {
            program.Recognise();
        }

        context.AddProgram(program);
        await context.SaveChangesAsync(cancellationToken);

        // Navigation properties have private setters by design, so the related level and department
        // are read back rather than assigned.
        var level = await context.AcademicLevels
            .AsNoTracking()
            .FirstAsync(x => x.Id == program.AcademicLevelId, cancellationToken);

        var department = await context.Departments
            .AsNoTracking()
            .FirstAsync(x => x.Id == program.DepartmentId, cancellationToken);

        return Result<ProgramDto>.Success(new ProgramDto(
            program.Id,
            program.Code,
            program.Name,
            program.AcademicLevelId,
            level.Code,
            program.DepartmentId,
            department.Name,
            program.Regulator.ToString(),
            program.IsRegulatorRecognised,
            program.Lifecycle.ToString(),
            program.RequiredCredits,
            program.CreditBasis.ToString(),
            program.CanAcceptStudents));
    }

    private static ProgramDto ToDto(AcademicProgram program) => new(
        program.Id,
        program.Code,
        program.Name,
        program.AcademicLevelId,
        program.AcademicLevel?.Code ?? string.Empty,
        program.DepartmentId,
        program.Department?.Name ?? string.Empty,
        program.Regulator.ToString(),
        program.IsRegulatorRecognised,
        program.Lifecycle.ToString(),
        program.RequiredCredits,
        program.CreditBasis.ToString(),
        program.CanAcceptStudents);
}
