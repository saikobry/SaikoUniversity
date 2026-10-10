using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;

namespace Infrastructure.Persistence.Configurations;

public sealed class YearLevelConfiguration : IEntityTypeConfiguration<YearLevel>
{
    public void Configure(EntityTypeBuilder<YearLevel> builder)
    {
        builder.ToTable("YearLevels");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Code)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.DisplayName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.SortOrder)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        // Restrict, not Cascade. A level is reference data that a year level cannot take down with
        // it, and soft delete means neither will be removed while rows reference them.
        builder.HasOne(x => x.AcademicLevel)
            .WithMany()
            .HasForeignKey(x => x.AcademicLevelId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        // Unique per level, not globally. Different levels may both define a "1st" year.
        builder.HasIndex(x => new { x.AcademicLevelId, x.Code })
            .IsUnique()
            .HasDatabaseName("UX_YearLevels_AcademicLevelId_Code");

        builder.HasIndex(x => new { x.AcademicLevelId, x.SortOrder })
            .IsUnique()
            .HasDatabaseName("UX_YearLevels_AcademicLevelId_SortOrder");
    }
}
