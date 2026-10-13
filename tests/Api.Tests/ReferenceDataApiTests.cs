using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Api.Contracts.v1;
using Api.Tests.Support;

namespace Api.Tests;

public sealed class ReferenceDataApiTests(ApiFactory factory) : IClassFixture<ApiFactory>, IDisposable
{
    private readonly HttpClient _client = factory.CreateClient();

    public void Dispose() => _client.Dispose();

    [Fact]
    public async Task GetAcademicLevels_Returns200WithSeededLevel()
    {
        var response = await _client.GetAsync(
            "/api/v1/reference-data/academic-levels",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var items = await response.Content.ReadFromJsonAsync<IReadOnlyList<ReferenceItemResponse>>(
            TestContext.Current.CancellationToken);

        items.Should().NotBeNull();
        items.Should().NotBeEmpty();

        var seeded = items!.FirstOrDefault(x => x.Id == factory.AcademicLevelId);
        seeded.Should().NotBeNull();
        seeded!.Id.Should().Be(factory.AcademicLevelId);
        seeded.Code.Should().Be("UG");
        seeded.Name.Should().Be("Undergraduate");

        // Verify body contract has id, code, and name present
        foreach (var item in items!)
        {
            item.Id.Should().NotBeEmpty();
            item.Code.Should().NotBeNullOrWhiteSpace();
            item.Name.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public async Task GetDepartments_Returns200WithSeededDepartment()
    {
        var response = await _client.GetAsync(
            "/api/v1/reference-data/departments",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var items = await response.Content.ReadFromJsonAsync<IReadOnlyList<ReferenceItemResponse>>(
            TestContext.Current.CancellationToken);

        items.Should().NotBeNull();
        items.Should().NotBeEmpty();

        var seeded = items!.FirstOrDefault(x => x.Id == factory.DepartmentId);
        seeded.Should().NotBeNull();
        seeded!.Id.Should().Be(factory.DepartmentId);
        seeded.Code.Should().Be("COMP");
        seeded.Name.Should().Be("Faculty of Computing");

        // Verify body contract has id, code, and name present
        foreach (var item in items!)
        {
            item.Id.Should().NotBeEmpty();
            item.Code.Should().NotBeNullOrWhiteSpace();
            item.Name.Should().NotBeNullOrWhiteSpace();
        }
    }
}
