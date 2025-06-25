using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ClinicManagement.Models;

namespace ClinicManagement.Configurations
{
    public class StaffConfiguration : IEntityTypeConfiguration<Staff>
    {
        public void Configure(EntityTypeBuilder<Staff> builder)
        {
            builder.ToTable("NhanVien");

            builder.HasKey(e => e.StaffId).HasName("PK_NhanVien");

            builder.Property(e => e.StaffId)
                .HasColumnName("MaNhanVien");

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
            
            builder.Property(e => e.Email)
                .HasColumnName("Email")
                .HasMaxLength(255);

            builder.Property(e => e.IsDeleted)
                .HasColumnName("DaXoa")
                .HasDefaultValue(false);

            builder.Property(e => e.Address)
                .HasColumnName("DiaChi")
                .HasDefaultValue(false);

            builder.Property(e => e.RoleId)
           .HasColumnName("MaVaiTro");

            builder.HasOne(d => d.Role)
          .WithMany(p => p.Staffs)
          .HasForeignKey(d => d.RoleId)
          .HasConstraintName("FK_NhanVien_VaiTro");

            builder.HasOne(d => d.Specialty)
                .WithMany(p => p.Staffs)
                .HasForeignKey(d => d.SpecialtyId)
                .HasConstraintName("FK_BacSi_ChuyenKhoa");
        }
    }
}