using ClinicManagement.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;


namespace ClinicManagement.Configurations
{
    public class MedicineConfiguration : IEntityTypeConfiguration<Medicine>
    {
        public void Configure(EntityTypeBuilder<Medicine> builder)
        {
            builder.ToTable("Thuoc");

            builder.HasKey(e => e.MedicineId).HasName("PK_Thuoc");

            builder.Property(e => e.MedicineId)
                .HasColumnName("MaThuoc");

            builder.Property(e => e.Name)
                .HasColumnName("TenThuoc")
                .HasMaxLength(100);

            builder.Property(e => e.CategoryId)
                .HasColumnName("MaDanhMuc");

            builder.Property(e => e.UnitId)
                .HasColumnName("MaDonVi");

            builder.Property(e => e.SupplierId)
                .HasColumnName("MaNhaCungCap");

            builder.Property(e => e.BarCode)
                .HasColumnName("MaVach")
                .IsUnicode(false);

            builder.Property(e => e.QrCode)
                .HasColumnName("MaQR")
                .IsUnicode(false);

            builder.Property(e => e.Mshnnb)
                .HasColumnName("MSHNNB")
                .HasMaxLength(50);

            builder.Property(e => e.IsDeleted)
                .HasColumnName("DaXoa")
                .HasDefaultValue(false);

            builder.HasOne(d => d.Category)
                .WithMany(p => p.Medicines)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK_Thuoc_DanhMucThuoc");

            builder.HasOne(d => d.Unit)
                .WithMany(p => p.Medicines)
                .HasForeignKey(d => d.UnitId)
                .HasConstraintName("FK_Thuoc_DonVi");

            builder.HasOne(d => d.Supplier)
                .WithMany(p => p.Medicines)
                .HasForeignKey(d => d.SupplierId)
                .HasConstraintName("FK_Thuoc_NhaCungCap");
        }
    }
}
