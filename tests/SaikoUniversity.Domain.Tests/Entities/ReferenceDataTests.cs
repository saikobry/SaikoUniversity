using FluentAssertions;
using SaikoUniversity.Domain.Entities;

namespace SaikoUniversity.Domain.Tests.Entities;

public sealed class ReferenceDataTests
{
    [Fact]
    public void AcademicLevel_Constructor_IsActiveByDefault()
    {
        var level = new AcademicLevel("UG", "Undergraduate");

        level.IsActive.Should().BeTrue();
        level.Code.Should().Be("UG");
        level.Name.Should().Be("Undergraduate");
    }

    [Fact]
    public void AcademicLevel_DeactivateThenReactivate_RoundTrips()
    {
        var level = new AcademicLevel("UG", "Undergraduate");

        level.Deactivate();
        level.IsActive.Should().BeFalse();

        level.Reactivate();
        level.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Department_Constructor_IsActiveByDefault()
    {
        var department = new Department("COMP", "Faculty of Computing");

        department.IsActive.Should().BeTrue();
        department.Code.Should().Be("COMP");
    }

    [Fact]
    public void YearLevel_Constructor_StoresLevelAndSortOrder()
    {
        var levelId = Guid.CreateVersion7();

        var yearLevel = new YearLevel(levelId, "Y2", "Second Year", 2);

        yearLevel.AcademicLevelId.Should().Be(levelId);
        yearLevel.Code.Should().Be("Y2");
        yearLevel.DisplayName.Should().Be("Second Year");
        yearLevel.SortOrder.Should().Be(2);
        yearLevel.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Entity_Id_IsUuidVersion7()
    {
        var level = new AcademicLevel("UG", "Undergraduate");

        // Version nibble is the 13th hex digit of the first group.
        level.Id.ToString()[14].Should().Be('7');
    }

    [Fact]
    public void Entity_Id_IsUniquePerInstance()
    {
        var levels = Enumerable.Range(0, 500)
            .Select(_ => new AcademicLevel("UG", "Undergraduate"))
            .Select(x => x.Id)
            .ToList();

        levels.Distinct().Should().HaveCount(500);
    }
}
