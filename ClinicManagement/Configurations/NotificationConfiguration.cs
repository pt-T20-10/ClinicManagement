using ClinicManagement.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClinicManagement.Configurations
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.ToTable("ThongBao");

            builder.HasKey(e => e.NotificationId).HasName("PK_ThongBao");

            builder.Property(e => e.NotificationId)
                .HasColumnName("MaThongBao");

            builder.Property(e => e.AppointmentId)
                .HasColumnName("MaLichHen");

            builder.Property(e => e.Message)
                .HasColumnName("NoiDung")
                .HasMaxLength(500);

            builder.Property(e => e.Method)
                .HasColumnName("PhuongThuc")
                .HasMaxLength(20);

            builder.Property(e => e.Status)
                .HasColumnName("TrangThai")
                .HasMaxLength(20);

            builder.Property(e => e.SentAt)
                .HasColumnName("NgayGui")
                .HasDefaultValueSql("(getdate())")  
                .HasColumnType("datetime");

            builder.Property(e => e.IsDeleted)
                .HasColumnName("DaXoa")
                .HasDefaultValue(false);

            builder.HasOne(d => d.Appointment)
                .WithMany(p => p.Notifications)
                .HasForeignKey(d => d.AppointmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ThongBao_LichHen");
        }
    }
}
