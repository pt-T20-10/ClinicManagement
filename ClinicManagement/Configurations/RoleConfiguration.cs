using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClinicManagement.Models;

namespace ClinicManagement.Configurations
{
    public class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.ToTable("VaiTro");

            builder.HasKey(e => e.RoleId).HasName("PK_VaiTro");

            builder.Property(e => e.RoleId)
                .HasColumnName("MaVaiTro");

            builder.Property(e => e.RoleName)
                .HasColumnName("TenVaiTro")
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
