using ClinicManagement.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;


namespace ClinicManagement.Configurations
{
    public class StockInConfiguration : IEntityTypeConfiguration<StockIn>
    {
        public void Configure(EntityTypeBuilder<StockIn> builder)
        {
            builder.ToTable("NhapKho");

            builder.HasKey(e => e.StockInId).HasName("PK_NhapKho");

            builder.Property(e => e.StockInId)
                .HasColumnName("MaNhapKho");

            builder.Property(e => e.MedicineId)
                .HasColumnName("MaThuoc");

            builder.Property(e => e.Quantity)
                .HasColumnName("SoLuong");

            builder.Property(e => e.UnitPrice)
                .HasColumnName("DonGia")
                .HasColumnType("decimal(18, 2)");

            builder.Property(e => e.SellPrice)
                .HasColumnName("GiaBan")
                .HasColumnType("decimal(18, 2)");

            builder.Property(e => e.TotalCost)
                .HasColumnName("ThanhTien")
                .HasComputedColumnSql("([SoLuong]*[DonGia])", false)
                .HasColumnType("decimal(29, 2)");


            builder.Property(e => e.ImportDate)
                .HasColumnName("NgayNhap")
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            builder.HasOne(d => d.Medicine)
                .WithMany(p => p.StockIns)
                .HasForeignKey(d => d.MedicineId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_NhapKho_Thuoc");
        }
    }
}
