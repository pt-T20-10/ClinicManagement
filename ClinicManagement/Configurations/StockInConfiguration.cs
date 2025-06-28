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
            
            builder.Property(e => e.StaffId)
                .HasColumnName("NguoiNhap");

            builder.Property(e => e.RemainQuantity)
                .HasColumnName("ConLai");

            builder.Property(e => e.Quantity)
               .HasColumnName("SoLuong");

            builder.Property(e => e.UnitPrice)
                .HasColumnName("DonGia")
                .HasColumnType("decimal(18, 2)");

            builder.Property(e => e.ProfitMargin)
                .HasColumnName("TiLeLoiNhuan")
                .HasColumnType("decimal(5, 2)");

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

            builder.Property(e => e.ExpiryDate)
               .HasColumnName("HanSuDung")
            
               .HasColumnType("date");

            builder.HasOne(d => d.Medicine)
                .WithMany(p => p.StockIns)
                .HasForeignKey(d => d.MedicineId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_NhapKho_Thuoc"); 
            
            builder.HasOne(d => d.Staff)
                .WithMany(p => p.StockIns)
                .HasForeignKey(d => d.StaffId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_NhapKho_NhanVien");

            builder.Property(e => e.SupplierId)
         .HasColumnName("MaNhaCungCap");

            builder.HasOne(e => e.Supplier)
             .WithMany(p => p.StockIns)
             .HasForeignKey(e => e.SupplierId)
             .OnDelete(DeleteBehavior.ClientSetNull)
             .HasConstraintName("FK_NhapKho_NhaCungCap");
        }
    }
}
