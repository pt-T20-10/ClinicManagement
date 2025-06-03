using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ClinicManagement.Models;

namespace ClinicManagement.Configurations
{
    public class AppointmentTypeConfiguration : IEntityTypeConfiguration<AppointmentType>
    {
        public void Configure(EntityTypeBuilder<AppointmentType> builder)
        {
            builder.ToTable("LoaiLichHen");

            builder.HasKey(e => e.AppointmentTypeId).HasName("PK_LoaiLichHen");

            builder.Property(e => e.AppointmentTypeId)
                .HasColumnName("MaLoaiLichHen");

            builder.Property(e => e.TypeName)
                .HasColumnName("TenLoai")
                .HasMaxLength(50);

            builder.Property(e => e.Description)
                .HasColumnName("MoTa")
                .HasMaxLength(255);

            builder.Property(e => e.IsDeleted)
                .HasColumnName("DaXoa")
                .HasDefaultValue(false);
        }
    }
}