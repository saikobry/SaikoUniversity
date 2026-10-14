using FluentAssertions;
using Domain.Entities;
using Domain.Enums;

namespace Domain.Tests.Entities;

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

    [Theory]
    [InlineData(true, ProgramLifecycle.Active, true)]
    [InlineData(false, ProgramLifecycle.Active, false)]
    [InlineData(true, ProgramLifecycle.Retired, false)]
    [InlineData(false, ProgramLifecycle.Retired, false)]
    public void CanAcceptStudents_AllCombinations_MatchesExpected(
        bool recognised,
        ProgramLifecycle lifecycle,
        bool expected)
    {
        var program = CreateProgram(recognise: recognised, lifecycle: lifecycle);

        program.CanAcceptStudents.Should().Be(expected);
    }

    [Fact]
    public void Retire_WhenActive_SetsRetiredAndReturnsTrue_WhenAlreadyRetired_ReturnsFalse()
    {
        var program = CreateProgram(lifecycle: ProgramLifecycle.Active);

        var firstCall = program.Retire();
        firstCall.Should().BeTrue();
        program.Lifecycle.Should().Be(ProgramLifecycle.Retired);

        var secondCall = program.Retire();
        secondCall.Should().BeFalse();
        program.Lifecycle.Should().Be(ProgramLifecycle.Retired);
    }

    [Fact]
    public void Reinstate_WhenRetired_SetsActiveAndReturnsTrue_WhenAlreadyActive_ReturnsFalse()
    {
        var program = CreateProgram(lifecycle: ProgramLifecycle.Retired);

        var firstCall = program.Reinstate();
        firstCall.Should().BeTrue();
        program.Lifecycle.Should().Be(ProgramLifecycle.Active);

        var secondCall = program.Reinstate();
        secondCall.Should().BeFalse();
        program.Lifecycle.Should().Be(ProgramLifecycle.Active);
    }

    [Fact]
    public void Recognise_WhenUnrecognised_SetsRecognisedAndReturnsTrue_WhenAlreadyRecognised_ReturnsFalse()
    {
        var program = CreateProgram(recognise: false);

        var firstCall = program.Recognise();
        firstCall.Should().BeTrue();
        program.IsRegulatorRecognised.Should().BeTrue();

        var secondCall = program.Recognise();
        secondCall.Should().BeFalse();
        program.IsRegulatorRecognised.Should().BeTrue();
    }

    [Fact]
    public void WithdrawRecognition_WhenRecognised_SetsUnrecognisedAndReturnsTrue_WhenAlreadyUnrecognised_ReturnsFalse()
    {
        var program = CreateProgram(recognise: true);

        var firstCall = program.WithdrawRecognition();
        firstCall.Should().BeTrue();
        program.IsRegulatorRecognised.Should().BeFalse();

        var secondCall = program.WithdrawRecognition();
        secondCall.Should().BeFalse();
        program.IsRegulatorRecognised.Should().BeFalse();
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
    public void Retire_KeepsProgramVisible_ByLeavingLifecycleAsTheOnlyState()
    {
        // Retirement is the mechanism for stopping intake. There is no soft delete: a retired
        // program must stay visible to reports and transcripts, so hiding it behind a query filter
        // would be wrong. Rows are only removed when nothing references them.
        var program = CreateProgram();

        program.Retire();

        program.Lifecycle.Should().Be(ProgramLifecycle.Retired);
        program.CanAcceptStudents.Should().BeFalse();
    }
}
