using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ClinicManagement.Models;

namespace ClinicManagement.Configurations
{
    public class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
    {
        public void Configure(EntityTypeBuilder<Doctor> builder)
        {
            builder.ToTable("BacSi");

            builder.HasKey(e => e.DoctorId).HasName("PK_BacSi");

            builder.Property(e => e.DoctorId)
                .HasColumnName("MaBacSi");

            builder.Property(e => e.FullName)
                .HasColumnName("HoTen")
                .HasMaxLength(100);

            builder.Property(e => e.Phone)
                .HasColumnName("SoDienThoai")
                .HasMaxLength(20);

            builder.Property(e => e.SpecialtyId)
                .HasColumnName("MaChuyenKhoa");

            builder.Property(e => e.Schedule)
                .HasColumnName("LichLamViec")
                .HasMaxLength(255);

            builder.Property(e => e.CertificateLink)
                .HasColumnName("LinkChungChi")
                .HasMaxLength(255);

            builder.Property(e => e.IsDeleted)
                .HasColumnName("DaXoa")
                .HasDefaultValue(false);

            builder.Property(e => e.Address)
                .HasColumnName("DiaChi")
                .HasDefaultValue(false);

            builder.HasOne(d => d.Specialty)
                .WithMany(p => p.Doctors)
                .HasForeignKey(d => d.SpecialtyId)
                .HasConstraintName("FK_BacSi_ChuyenKhoa");
        }
    }
}