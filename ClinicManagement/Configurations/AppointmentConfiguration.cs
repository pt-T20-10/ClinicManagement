using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ClinicManagement.Models;

namespace ClinicManagement.Configurations
{
    public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
    {
        public void Configure(EntityTypeBuilder<Appointment> builder)
        {
            builder.ToTable("LichHen");

            builder.HasKey(e => e.AppointmentId).HasName("PK_LichHen");

            builder.Property(e => e.AppointmentId)
                .HasColumnName("MaLichHen");

            builder.Property(e => e.PatientId)
                .HasColumnName("MaBenhNhan");

            builder.Property(e => e.StaffId)
                .HasColumnName("MaBacSi");

            builder.Property(e => e.AppointmentTypeId)
                .HasColumnName("MaLoaiLichHen")
                .HasDefaultValue(1);

            builder.Property(e => e.AppointmentDate)
                .HasColumnName("NgayHen")
                .HasColumnType("datetime");

            builder.Property(e => e.Status)
                .HasColumnName("TrangThai")
                .HasMaxLength(20);

            builder.Property(e => e.Notes)
                .HasColumnName("GhiChu")
                .HasMaxLength(500);

            builder.Property(e => e.CreatedAt)
                .HasColumnName("NgayTao")
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            builder.Property(e => e.IsDeleted)
                .HasColumnName("DaXoa")
                .HasDefaultValue(false);

            builder.HasOne(d => d.Patient)
                .WithMany(p => p.Appointments)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LichHen_BenhNhan");

            builder.HasOne(d => d.Staff)
                .WithMany(p => p.Appointments)
                .HasForeignKey(d => d.StaffId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LichHen_BacSi");

            builder.HasOne(d => d.AppointmentType)
                .WithMany(p => p.Appointments)
                .HasForeignKey(d => d.AppointmentTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LichHen_LoaiLichHen");
        }
    }
}