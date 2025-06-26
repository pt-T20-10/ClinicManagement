using ClinicManagement.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;


namespace ClinicManagement.Configurations
{
    public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
    {
        public void Configure(EntityTypeBuilder<Invoice> builder)
        {
            builder.ToTable("HoaDon");

            builder.HasKey(e => e.InvoiceId).HasName("PK_HoaDon");

            builder.Property(e => e.InvoiceId)
                .HasColumnName("MaHoaDon");


            builder.Property(e => e.StaffPrescriberId)
                .HasColumnName("NhanVienKeDon"); 

            builder.Property(e => e.StaffCashierId)
                .HasColumnName("NhanVienXacNhan");

            builder.Property(e => e.PatientId)
                .HasColumnName("MaBenhNhan");

            builder.Property(e => e.MedicalRecordId)
                .HasColumnName("MaHoSoBenhAn");

            builder.Property(e => e.InvoiceDate)
                .HasColumnName("NgayLapHoaDon")
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            builder.Property(e => e.TotalAmount)
                .HasColumnName("TongTien")
                .HasColumnType("decimal(18, 2)");

            builder.Property(e => e.Status)
                .HasColumnName("TrangThai")
                .HasMaxLength(20);

            builder.Property(e => e.Notes)
                .HasColumnName("GhiChu")
                .HasMaxLength(500);  
            
            builder.Property(e => e.InvoiceType)
                .HasColumnName("LoaiHoaDon")
                .HasMaxLength(20);

            builder.Property(e => e.Discount)
            .HasColumnName("GiamGia")
            .HasDefaultValue(0m)
            .HasColumnType("decimal(5, 2)");

            builder.Property(e => e.Tax)
            .HasColumnName("Thue")
            .HasDefaultValue(0m)
            .HasColumnType("decimal(5, 2)");

            builder.HasOne(d => d.Patient)
                .WithMany(p => p.Invoices)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_HoaDon_BenhNhan");

            builder.HasOne(d => d.StaffPrescriber)
               .WithMany(p => p.InvoicesPrescribed)
               .HasForeignKey(d => d.StaffPrescriberId)
               .OnDelete(DeleteBehavior.SetNull)
               .HasConstraintName("FK_HoaDon_NhanVienKeDon");


            builder.HasOne(d => d.StaffCashier)
               .WithMany(p => p.InvoicesConfirmed)
               .HasForeignKey(d => d.StaffCashierId)
               .OnDelete(DeleteBehavior.SetNull)
               .HasConstraintName("FK_HoaDon_NhanVienXacNhan");

            builder.HasOne(d => d.MedicalRecord)
                .WithMany(p => p.Invoices)
                .HasForeignKey(d => d.MedicalRecordId)
                .HasConstraintName("FK_HoaDon_HoSoBenhAn");
        }
    }
}
