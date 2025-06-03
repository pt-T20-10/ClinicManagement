using ClinicManagement.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;


namespace ClinicManagement.Configurations
{
    public class InvoiceDetailConfiguration : IEntityTypeConfiguration<InvoiceDetail>
    {
        public void Configure(EntityTypeBuilder<InvoiceDetail> builder)
        {
            builder.ToTable("ChiTietHoaDon");

            builder.HasKey(e => e.DetailsId).HasName("PK_ChiTietHoaDon");

            builder.Property(e => e.DetailsId)
                .HasColumnName("MaChiTiet");

            builder.Property(e => e.InvoiceId)
                .HasColumnName("MaHoaDon");

            builder.Property(e => e.MedicineId)
                .HasColumnName("MaThuoc");

            builder.Property(e => e.StockInId)
                .HasColumnName("MaNhapKho");

            builder.Property(e => e.Quantity)
                .HasColumnName("SoLuong");

            builder.Property(e => e.SalePrice)
                .HasColumnName("GiaBan")
                .HasColumnType("decimal(18, 2)");

            builder.Property(e => e.Discount)
                .HasColumnName("GiamGia")
                .HasDefaultValue(0m)
                .HasColumnType("decimal(5, 2)");

            builder.HasOne(d => d.Invoice)
                .WithMany(p => p.InvoiceDetails)
                .HasForeignKey(d => d.InvoiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ChiTietHoaDon_HoaDon");

            builder.HasOne(d => d.Medicine)
                .WithMany(p => p.InvoiceDetails)
                .HasForeignKey(d => d.MedicineId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ChiTietHoaDon_Thuoc");

            builder.HasOne(d => d.StockIn)
                .WithMany(p => p.InvoiceDetails)
                .HasForeignKey(d => d.StockInId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ChiTietHoaDon_NhapKho");
        }
    }

}
