using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ClinicManagement.Models;

namespace ClinicManagement.Configurations
{
    public class MedicineCategoryConfiguration : IEntityTypeConfiguration<MedicineCategory>
    {
        public void Configure(EntityTypeBuilder<MedicineCategory> builder)
        {
            builder.ToTable("DanhMucThuoc");

            builder.HasKey(e => e.CategoryId).HasName("PK_DanhMucThuoc");

            builder.Property(e => e.CategoryId)
                .HasColumnName("MaDanhMuc");

            builder.Property(e => e.CategoryName)
                .HasColumnName("TenDanhMuc")
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
