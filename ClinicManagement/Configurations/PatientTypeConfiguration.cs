using ClinicManagement.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;


namespace ClinicManagement.Configurations
{
    public class PatientTypeConfiguration : IEntityTypeConfiguration<PatientType>
    {
        public void Configure(EntityTypeBuilder<PatientType> builder)
        {
            builder.ToTable("LoaiBenhNhan");

            builder.HasKey(e => e.PatientTypeId).HasName("PK_LoaiBenhNhan");

            builder.Property(e => e.PatientTypeId)
                .HasColumnName("MaLoaiBenhNhan");

            builder.Property(e => e.TypeName)
                .HasColumnName("TenLoai")
                .HasMaxLength(50);

            builder.Property(e => e.Discount)
                .HasColumnName("GiamGia")
                .HasDefaultValue(0m)
                .HasColumnType("decimal(5, 2)");

            builder.Property(e => e.IsDeleted)
                .HasColumnName("DaXoa")
                .HasDefaultValue(false);
        }
    }
}
