using FluentAssertions;
using SaikoUniversity.Domain.Entities;
using SaikoUniversity.Domain.Enums;

namespace SaikoUniversity.Domain.Tests.Entities;

public sealed class AcademicProgramTests
{
    private static AcademicProgram CreateProgram(
        bool recognise = true,
        ProgramLifecycle lifecycle = ProgramLifecycle.Active)
    {
        var level = new AcademicLevel("UG", "Undergraduate");
        var department = new Department("COMP", "Faculty of Computing");

        var program = new AcademicProgram(
            "BSCS",
            "Bachelor of Science in Computer Science",
            level.Id,
            department.Id,
            Regulator.Ched,
            144,
            CreditBasis.Hours);

        if (recognise)
        {
            program.Recognise();
        }

        if (lifecycle == ProgramLifecycle.Retired)
        {
            program.Retire();
        }

        return program;
    }

    [Fact]
    public void Constructor_WithRecognisedProgram_CanAcceptStudents()
    {
        var program = CreateProgram(recognise: true);

        program.CanAcceptStudents.Should().BeTrue();
    }

    [Theory]
    [InlineData(false, ProgramLifecycle.Active)]
    [InlineData(true, ProgramLifecycle.Retired)]
    [InlineData(false, ProgramLifecycle.Retired)]
    public void CanAcceptStudents_WhenEitherConditionFails_IsFalse(
        bool recognised,
        ProgramLifecycle lifecycle)
    {
        var program = CreateProgram(recognise: recognised, lifecycle: lifecycle);

        program.CanAcceptStudents.Should().BeFalse();
    }

    [Fact]
    public void Constructor_NewProgram_IsNotRecognised()
    {
        // A program must be explicitly recognized before it can accept students. Recognition is
        // never implied by construction.
        var program = CreateProgram(recognise: false);

        program.IsRegulatorRecognised.Should().BeFalse();
        program.Lifecycle.Should().Be(ProgramLifecycle.Active);
    }

    [Fact]
    public void WithdrawRecognition_WhileActive_CanNoLongerAcceptStudents()
    {
        var program = CreateProgram(recognise: true);

        program.WithdrawRecognition();

        program.CanAcceptStudents.Should().BeFalse();
    }

    [Fact]
    public void Recognise_AfterWithdrawal_RestoresEligibility()
    {
        var program = CreateProgram(recognise: false);

        program.Recognise();

        program.CanAcceptStudents.Should().BeTrue();
    }

    [Fact]
    public void Retire_ThenReinstate_ReturnsToActiveLifecycle()
    {
        var program = CreateProgram();

        program.Retire();
        program.Lifecycle.Should().Be(ProgramLifecycle.Retired);

        program.Reinstate();
        program.Lifecycle.Should().Be(ProgramLifecycle.Active);
        program.CanAcceptStudents.Should().BeTrue();
    }

    [Fact]
    public void SoftDelete_SetsIsDeleted()
    {
        var program = CreateProgram();

        program.SoftDelete();

        program.IsDeleted.Should().BeTrue();
    }
}
