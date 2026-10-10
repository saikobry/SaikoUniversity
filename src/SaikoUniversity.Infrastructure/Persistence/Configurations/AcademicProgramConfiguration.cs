using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaikoUniversity.Domain.Entities;

namespace SaikoUniversity.Infrastructure.Persistence.Configurations;

public sealed class AcademicProgramConfiguration : IEntityTypeConfiguration<AcademicProgram>
{
    public void Configure(EntityTypeBuilder<AcademicProgram> builder)
    {
        builder.ToTable("AcademicPrograms");

        builder.HasKey(x => x.Id);

        // Ids are UUIDv7 generated in the Domain layer, not by the database.
        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Code)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Regulator)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Lifecycle)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.CreditBasis)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.IsRegulatorRecognised)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.RequiredCredits)
            .IsRequired();

        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasDatabaseName("UX_AcademicPrograms_Code");

        builder.HasIndex(x => x.AcademicLevelId)
            .HasDatabaseName("IX_AcademicPrograms_AcademicLevelId");

        builder.HasIndex(x => x.DepartmentId)
            .HasDatabaseName("IX_AcademicPrograms_DepartmentId");

        builder.HasOne(x => x.AcademicLevel)
            .WithMany()
            .HasForeignKey(x => x.AcademicLevelId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne(x => x.Department)
            .WithMany()
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        // No query filter. A program is retired, not hidden, so it stays visible to reports and
        // transcripts. Rows are only removed when nothing references them, which the Restrict
        // foreign keys above enforce.
    }
}
