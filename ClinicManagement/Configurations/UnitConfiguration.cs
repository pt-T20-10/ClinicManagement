using ClinicManagement.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;


namespace ClinicManagement.Configurations
{
    public class UnitConfiguration : IEntityTypeConfiguration<Unit>
    {
        public void Configure(EntityTypeBuilder<Unit> builder)
        {
            builder.ToTable("DonVi");

            builder.HasKey(e => e.UnitId).HasName("PK_DonVi");

            builder.Property(e => e.UnitId)
                .HasColumnName("MaDonVi");

            builder.Property(e => e.UnitName)
                .HasColumnName("TenDonVi")
                .HasMaxLength(50);

            builder.Property(e => e.Description)
                .HasColumnName("MoTa")
                .HasMaxLength(255);

            builder.Property(e => e.IsDeleted)
                .HasColumnName("DaXoa")
                .HasDefaultValue(false);
        }
    }
}

