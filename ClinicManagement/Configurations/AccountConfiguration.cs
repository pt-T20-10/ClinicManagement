using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ClinicManagement.Models;

namespace ClinicManagement.Configurations
{
    public class AccountConfiguration : IEntityTypeConfiguration<Account>
    {
        public void Configure(EntityTypeBuilder<Account> builder)
        {
            builder.ToTable("TaiKhoan");

            builder.HasKey(e => e.Username).HasName("PK_TaiKhoan");

            builder.Property(e => e.Username)
                .HasColumnName("TenDangNhap")
                .HasMaxLength(50);

            builder.Property(e => e.Password)
                .HasColumnName("MatKhau")
                .HasMaxLength(256);

            builder.Property(e => e.Role)
                .HasColumnName("VaiTro")
                .HasMaxLength(50);

            builder.Property(e => e.DoctorId)
                .HasColumnName("MaBacSi");

            builder.Property(e => e.IsLogined)
                .HasColumnName("DaDangNhap")
                .HasDefaultValue(false);

            builder.Property(e => e.IsDeleted)
                .HasColumnName("DaXoa")
                .HasDefaultValue(false);

            builder.HasOne(d => d.Doctor)
                .WithMany(p => p.Accounts)
                .HasForeignKey(d => d.DoctorId)
                .HasConstraintName("FK_TaiKhoan_BacSi");
        }
    }
}