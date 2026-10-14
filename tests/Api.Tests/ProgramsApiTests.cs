using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Api.Tests.Support;
using Api.Contracts.v1;
using Infrastructure.Persistence;

namespace Api.Tests;

/// <summary>
/// Verifies the HTTP contract of the programs endpoints: status codes, the error body shape the
/// generated client depends on, and the <c>Location</c> header on create.
/// </summary>
/// <remarks>
/// These run against the real host graph over InMemory. They are not a substitute for
/// <c>Infrastructure.Tests</c> against SQL Server, and they do not cover the service's own rules —
/// those live in <c>ProgramServiceTests</c>.
/// </remarks>
public sealed class ProgramsApiTests(ApiFactory factory) : IClassFixture<ApiFactory>, IDisposable
{
    private readonly HttpClient _client = factory.CreateClient();

    public void Dispose() => _client.Dispose();

    [Fact]
    public async Task GetById_UnknownId_Returns404WithErrorBody()
    {
        var id = Guid.CreateVersion7();

        var response = await _client.GetAsync($"/api/v1/programs/{id}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // The generated client types this response as ErrorResponse, so a bare 404 advertises a
        // contract the server then does not honour. Asserting only the status misses that.
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body!.Code.Should().Be(nameof(Application.Common.ErrorCode.ProgramNotFound));
        body.Message.Should().Contain(id.ToString());
    }

    [Fact]
    public async Task Create_ValidRequest_Returns201WithLocationHeader()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/programs",
            ValidPayload(),
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<ProgramResponse>(TestContext.Current.CancellationToken);
        created.Should().NotBeNull();
        created!.Code.Should().StartWith("T");

        // CreatedAtAction promises a retrievable Location. Without it a client cannot follow up.
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain(created.Id.ToString());
    }

    [Fact]
    public async Task Create_CreatedProgram_IsRetrievableFromLocation()
    {
        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1/programs",
            ValidPayload(),
            TestContext.Current.CancellationToken);

        var created = await createResponse.Content.ReadFromJsonAsync<ProgramResponse>(TestContext.Current.CancellationToken);
        var location = createResponse.Headers.Location!;

        var fetched = await _client.GetAsync(location, TestContext.Current.CancellationToken);

        fetched.StatusCode.Should().Be(HttpStatusCode.OK);
        var roundTripped = await fetched.Content.ReadFromJsonAsync<ProgramResponse>(TestContext.Current.CancellationToken);
        roundTripped!.Id.Should().Be(created!.Id);
    }

    [Fact]
    public async Task Create_UnknownAcademicLevel_Returns400WithErrorCode()
    {
        var payload = ValidPayload() with { AcademicLevelId = Guid.CreateVersion7() };

        var response = await _client.PostAsJsonAsync("/api/v1/programs", payload, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>(TestContext.Current.CancellationToken);
        body!.Code.Should().Be(nameof(Application.Common.ErrorCode.ProgramInvalidAcademicLevel));
    }

    [Fact]
    public async Task Create_DuplicateCode_Returns409WithErrorCode()
    {
        var payload = ValidPayload("DUPLICATE");
        await _client.PostAsJsonAsync("/api/v1/programs", payload, TestContext.Current.CancellationToken);

        // Same code again. ProgramService checks uniqueness before the foreign keys, so this
        // exercises the conflict branch rather than re-reporting the references.
        var response = await _client.PostAsJsonAsync("/api/v1/programs", payload, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>(TestContext.Current.CancellationToken);
        body!.Code.Should().Be(nameof(Application.Common.ErrorCode.ProgramCodeAlreadyExists));
    }

    [Fact]
    public async Task Update_UnknownId_Returns404WithErrorBody()
    {
        var id = Guid.CreateVersion7();

        var response = await _client.PutAsJsonAsync(
            $"/api/v1/programs/{id}",
            new UpdateProgramRequest("Renamed", 144),
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>(TestContext.Current.CancellationToken);
        body!.Code.Should().Be(nameof(Application.Common.ErrorCode.ProgramNotFound));
    }

    [Fact]
    public async Task Update_ValidRequest_DoesNotMutateLifecycleOrRecognition()
    {
        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1/programs",
            ValidPayload(),
            TestContext.Current.CancellationToken);
        var created = await createResponse.Content.ReadFromJsonAsync<ProgramResponse>(TestContext.Current.CancellationToken);

        var response = await _client.PutAsJsonAsync(
            $"/api/v1/programs/{created!.Id}",
            new UpdateProgramRequest("Updated Name", 160),
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<ProgramResponse>(TestContext.Current.CancellationToken);
        updated!.Name.Should().Be("Updated Name");
        updated.RequiredCredits.Should().Be(160);
        updated.Lifecycle.Should().Be("Active");
        updated.IsRegulatorRecognised.Should().BeTrue();
        updated.CanAcceptStudents.Should().BeTrue();
    }

    [Fact]
    public async Task Retire_UnknownId_Returns404WithErrorBody()
    {
        var id = Guid.CreateVersion7();

        var response = await _client.PostAsync(
            $"/api/v1/programs/{id}/retire",
            content: null,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>(TestContext.Current.CancellationToken);
        body!.Code.Should().Be(nameof(Application.Common.ErrorCode.ProgramNotFound));
    }

    [Fact]
    public async Task Retire_ValidId_Returns200WithRetiredLifecycle()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/programs", ValidPayload(), TestContext.Current.CancellationToken);
        var created = await createResponse.Content.ReadFromJsonAsync<ProgramResponse>(TestContext.Current.CancellationToken);

        var response = await _client.PostAsync(
            $"/api/v1/programs/{created!.Id}/retire",
            content: null,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ProgramResponse>(TestContext.Current.CancellationToken);
        body!.Lifecycle.Should().Be("Retired");
        body.CanAcceptStudents.Should().BeFalse();
    }

    [Fact]
    public async Task Reinstate_UnknownId_Returns404WithErrorBody()
    {
        var id = Guid.CreateVersion7();

        var response = await _client.PostAsync(
            $"/api/v1/programs/{id}/reinstate",
            content: null,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>(TestContext.Current.CancellationToken);
        body!.Code.Should().Be(nameof(Application.Common.ErrorCode.ProgramNotFound));
    }

    [Fact]
    public async Task Reinstate_ValidId_Returns200WithActiveLifecycle()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/programs", ValidPayload(), TestContext.Current.CancellationToken);
        var created = await createResponse.Content.ReadFromJsonAsync<ProgramResponse>(TestContext.Current.CancellationToken);

        await _client.PostAsync($"/api/v1/programs/{created!.Id}/retire", content: null, TestContext.Current.CancellationToken);

        var response = await _client.PostAsync(
            $"/api/v1/programs/{created.Id}/reinstate",
            content: null,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ProgramResponse>(TestContext.Current.CancellationToken);
        body!.Lifecycle.Should().Be("Active");
        body.CanAcceptStudents.Should().BeTrue();
    }

    [Fact]
    public async Task Recognise_UnknownId_Returns404WithErrorBody()
    {
        var id = Guid.CreateVersion7();

        var response = await _client.PostAsync(
            $"/api/v1/programs/{id}/recognise",
            content: null,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>(TestContext.Current.CancellationToken);
        body!.Code.Should().Be(nameof(Application.Common.ErrorCode.ProgramNotFound));
    }

    [Fact]
    public async Task Recognise_ValidId_Returns200WithRecognisedTrue()
    {
        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1/programs",
            ValidPayload() with { IsRegulatorRecognised = false },
            TestContext.Current.CancellationToken);
        var created = await createResponse.Content.ReadFromJsonAsync<ProgramResponse>(TestContext.Current.CancellationToken);

        var response = await _client.PostAsync(
            $"/api/v1/programs/{created!.Id}/recognise",
            content: null,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ProgramResponse>(TestContext.Current.CancellationToken);
        body!.IsRegulatorRecognised.Should().BeTrue();
        body.CanAcceptStudents.Should().BeTrue();
    }

    [Fact]
    public async Task WithdrawRecognition_UnknownId_Returns404WithErrorBody()
    {
        var id = Guid.CreateVersion7();

        var response = await _client.PostAsync(
            $"/api/v1/programs/{id}/withdraw-recognition",
            content: null,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>(TestContext.Current.CancellationToken);
        body!.Code.Should().Be(nameof(Application.Common.ErrorCode.ProgramNotFound));
    }

    [Fact]
    public async Task WithdrawRecognition_ValidId_Returns200WithRecognisedFalse()
    {
        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1/programs",
            ValidPayload() with { IsRegulatorRecognised = true },
            TestContext.Current.CancellationToken);
        var created = await createResponse.Content.ReadFromJsonAsync<ProgramResponse>(TestContext.Current.CancellationToken);

        var response = await _client.PostAsync(
            $"/api/v1/programs/{created!.Id}/withdraw-recognition",
            content: null,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ProgramResponse>(TestContext.Current.CancellationToken);
        body!.IsRegulatorRecognised.Should().BeFalse();
        body.CanAcceptStudents.Should().BeFalse();
    }

    [Fact]
    public async Task GetAll_ReturnsPagedEnvelope()
    {
        await _client.PostAsJsonAsync("/api/v1/programs", ValidPayload(), TestContext.Current.CancellationToken);

        var response = await _client.GetAsync("/api/v1/programs", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Asserting the envelope, not just 200. A shape change here silently breaks the SPA's
        // pagination, and 200 on its own says nothing about that.
        var body = await response.Content.ReadFromJsonAsync<ProgramListResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body!.Items.Should().NotBeEmpty();
        body.Page.Should().Be(1);
        body.TotalCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetAll_CanAcceptStudentsFalse_Returns200WithNonAcceptingPrograms()
    {
        await _client.PostAsJsonAsync("/api/v1/programs", ValidPayload(), TestContext.Current.CancellationToken);

        var unrecognisedResponse = await _client.PostAsJsonAsync(
            "/api/v1/programs",
            ValidPayload() with { IsRegulatorRecognised = false },
            TestContext.Current.CancellationToken);
        var unrecognised = await unrecognisedResponse.Content.ReadFromJsonAsync<ProgramResponse>(TestContext.Current.CancellationToken);

        var response = await _client.GetAsync("/api/v1/programs?canAcceptStudents=false", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ProgramListResponse>(TestContext.Current.CancellationToken);
        body.Should().NotBeNull();
        body!.Items.Should().NotBeEmpty();
        body.Items.Should().OnlyContain(x => !x.CanAcceptStudents);
        body.Items.Should().Contain(x => x.Id == unrecognised!.Id);
    }

    [Fact]
    public async Task Factory_UsesInMemoryNotTheDevelopmentDatabase()
    {
        // Guards the seam itself. If the removal of the SqlServer provider stops working — an EF
        // upgrade changing the registration mechanism, say — the host would quietly fall back to the
        // real connection string and these tests would pass against the developer's database.
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        context.Database.ProviderName.Should().Be("Microsoft.EntityFrameworkCore.InMemory");
    }


    [Fact]
    public async Task CreatedProgram_CarriesAuditStamp()
    {
        // Read from the store rather than the response: ProgramResponse deliberately carries no audit
        // fields, and adding one to the public contract to serve a test would be the wrong trade.
        // CreatedAt is non-nullable, so an unwired interceptor surfaces as 0001-01-01 rather than as
        // an error, which is exactly what makes this worth asserting.
        var response = await _client.PostAsJsonAsync(
            "/api/v1/programs",
            ValidPayload(),
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<ProgramResponse>(TestContext.Current.CancellationToken);

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var stored = await context.AcademicPrograms
            .AsNoTracking()
            .FirstAsync(x => x.Id == created!.Id, TestContext.Current.CancellationToken);

        stored.CreatedAt.Should().NotBe(default);
    }

    /// <summary>
    /// A valid create request. <c>Code</c> defaults to a per-call value so tests that each create a
    /// program cannot collide on the uniqueness check, which ProgramService applies before the
    /// foreign keys. A test that means to collide passes the same code twice on purpose.
    /// </summary>
    /// <remarks>
    /// The random source is <see cref="Guid.NewGuid"/>, not <c>Guid.CreateVersion7</c>. A v7 GUID
    /// begins with a millisecond timestamp, so every code generated inside the same window would be
    /// identical — which reintroduces exactly the collision this is here to avoid.
    /// </remarks>
    private CreateProgramRequest ValidPayload(string? code = null) => new(
        Code: code ?? $"T{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}",
        Name: "BS Computer Science",
        AcademicLevelId: factory.AcademicLevelId,
        DepartmentId: factory.DepartmentId,
        Regulator: "Ched",
        RequiredCredits: 120,
        CreditBasis: "Hours",
        IsRegulatorRecognised: true);
}
