using ClinicManagement.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;


namespace ClinicManagement.Configurations
{
    public class PatientConfiguration : IEntityTypeConfiguration<Patient>
    {
        public void Configure(EntityTypeBuilder<Patient> builder)
        {
            builder.ToTable("BenhNhan");

            builder.HasKey(e => e.PatientId).HasName("PK_BenhNhan");

            builder.Property(e => e.PatientId)
                .HasColumnName("MaBenhNhan");

            builder.Property(e => e.FullName)
                .HasColumnName("HoTen")
                .HasMaxLength(100);

            builder.Property(e => e.Gender)
                .HasColumnName("GioiTinh")
                .HasMaxLength(10);

            builder.Property(e => e.Phone)
                .HasColumnName("SoDienThoai")
                .HasMaxLength(20);

            builder.Property(e => e.Address)
                .HasColumnName("DiaChi")
                .HasMaxLength(255);

            builder.Property(e => e.DateOfBirth)
                .HasColumnName("NgaySinh");

            builder.Property(e => e.InsuranceCode)
                .HasColumnName("MaSoBaoHiem")
                .HasMaxLength(50);
                builder.Property(e => e.Email)
             .HasColumnName("Email")
             .HasMaxLength(255)
             .IsRequired(false); // nullable
            builder.Property(e => e.PatientTypeId)
                .HasColumnName("MaLoaiBenhNhan");

            builder.Property(e => e.CreatedAt)
                .HasColumnName("NgayTao")
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            builder.Property(e => e.IsDeleted)
                .HasColumnName("DaXoa")
                .HasDefaultValue(false);

            builder.HasOne(d => d.PatientType)
                .WithMany(p => p.Patients)
                .HasForeignKey(d => d.PatientTypeId)
                .HasConstraintName("FK_BenhNhan_LoaiBenhNhan");
        }
    }
}
