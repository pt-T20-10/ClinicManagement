using ClinicManagement.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicManagement.Configurations
{
    public class MonthlyStockConfiguration : IEntityTypeConfiguration<MonthlyStock>
    {
        public void Configure(EntityTypeBuilder<MonthlyStock> builder)
        {
            builder.ToTable("TonKhoTheoThang");

            builder.HasKey(e => e.MonthlyStockId).HasName("PK_TonKhoTheoThang");

            builder.Property(e => e.MonthlyStockId)
                .HasColumnName("MaTonThang");

            builder.Property(e => e.MedicineId)
                .HasColumnName("MaThuoc");

            builder.Property(e => e.Quantity)
                .HasColumnName("SoLuong")
                .HasDefaultValue(0);

               builder.Property(e => e.CanUsed)
                .HasColumnName("SuDungDuoc")
                .HasDefaultValue(0);

            builder.Property(e => e.MonthYear)
                .HasColumnName("ThangNam")
                .HasColumnType("CHAR(7)")
                .IsRequired();

            builder.Property(e => e.RecordedDate)
                .HasColumnName("NgayGhiNhan")
                .HasColumnType("datetime")
                .HasDefaultValueSql("GETDATE()");

            // Add the unique constraint
            builder.HasIndex(e => new { e.MedicineId, e.MonthYear })
                .HasDatabaseName("UQ_TonKhoTheoThang_ThangThuoc")
                .IsUnique();

            // Configure relationship with Medicine
            builder.HasOne(d => d.Medicine)
                .WithMany(p => p.MonthlyStocks)
                .HasForeignKey(d => d.MedicineId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TonKhoTheoThang_Thuoc");
        }
    }
}
