using ClinicManagement.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;


namespace ClinicManagement.Configurations
{
    public class DoctorSpecialtyConfiguration : IEntityTypeConfiguration<DoctorSpecialty>
    {
        public void Configure(EntityTypeBuilder<DoctorSpecialty> builder)
        {
            builder.ToTable("ChuyenKhoaBacSi");

            builder.HasKey(e => e.SpecialtyId).HasName("PK_ChuyenKhoaBacSi");

            builder.Property(e => e.SpecialtyId)
                .HasColumnName("MaChuyenKhoa");

            builder.Property(e => e.SpecialtyName)
                .HasColumnName("TenChuyenKhoa")
                .HasMaxLength(100);

            builder.Property(e => e.Description)
                .HasColumnName("MoTa")
                .HasMaxLength(255);

            builder.Property(e => e.IsDeleted)
                .HasColumnName("DaXoa")
                .HasDefaultValue(false);
        }
    }
}
