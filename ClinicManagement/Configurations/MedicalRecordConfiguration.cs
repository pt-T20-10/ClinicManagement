using ClinicManagement.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;


namespace ClinicManagement.Configurations
{
    public class MedicalRecordConfiguration : IEntityTypeConfiguration<MedicalRecord>
    {
        public void Configure(EntityTypeBuilder<MedicalRecord> builder)
        {
            builder.ToTable("HoSoBenhAn");

            builder.HasKey(e => e.RecordId).HasName("PK_HoSoBenhAn");

            builder.Property(e => e.RecordId)
                .HasColumnName("MaHoSo");

            builder.Property(e => e.PatientId)
                .HasColumnName("MaBenhNhan");

            builder.Property(e => e.DoctorId)
                .HasColumnName("MaBacSi");

            builder.Property(e => e.RecordDate)
                .HasColumnName("NgayTao")
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            builder.Property(e => e.Diagnosis)
                .HasColumnName("ChanDoan");

            builder.Property(e => e.DoctorAdvice)
                .HasColumnName("LoiKhuyenBacSi");

            builder.Property(e => e.Prescription)
                .HasColumnName("DonThuoc");

            builder.Property(e => e.TestResults)
                .HasColumnName("KetQuaXetNghiem");

            builder.Property(e => e.IsDeleted)
                .HasColumnName("DaXoa")
                .HasDefaultValue(false);

            builder.HasOne(d => d.Patient)
                .WithMany(p => p.MedicalRecords)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HoSoBenhAn_BenhNhan");

            builder.HasOne(d => d.Doctor)
                .WithMany(p => p.MedicalRecords)
                .HasForeignKey(d => d.DoctorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HoSoBenhAn_BacSi");
        }
    }
}
