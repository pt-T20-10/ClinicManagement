using ClinicManagement.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;


namespace ClinicManagement.Configurations
{
    public class StockConfiguration : IEntityTypeConfiguration<Stock>
    {
        public void Configure(EntityTypeBuilder<Stock> builder)
        {
            builder.ToTable("KhoThuoc");

            builder.HasKey(e => e.StockId).HasName("PK_KhoThuoc");

            builder.Property(e => e.StockId)
                .HasColumnName("MaKho");

            builder.Property(e => e.MedicineId)
                .HasColumnName("MaThuoc");

            builder.Property(e => e.Quantity)
                .HasColumnName("TonKho");

            builder.Property(e => e.LastUpdated)
                .HasColumnName("NgayCapNhat")
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            builder.HasOne(d => d.Medicine)
                .WithMany(p => p.Stocks)
                .HasForeignKey(d => d.MedicineId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_KhoThuoc_Thuoc");
        }
    }
}
