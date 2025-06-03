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
    public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
    {
        public void Configure(EntityTypeBuilder<Supplier> builder)
        {
            builder.ToTable("NhaCungCap");

            builder.HasKey(e => e.SupplierId).HasName("PK_NhaCungCap");

            builder.Property(e => e.SupplierId)
                .HasColumnName("MaNhaCungCap");

            builder.Property(e => e.SupplierName)
                .HasColumnName("TenNhaCungCap")
                .HasMaxLength(100);

            builder.Property(e => e.SupplierCode)
                .HasColumnName("MaSoNhaCungCap")
                .HasMaxLength(20);

            builder.Property(e => e.ContactPerson)
                .HasColumnName("NguoiLienHe")
                .HasMaxLength(50);

            builder.Property(e => e.Phone)
                .HasColumnName("SoDienThoai")
                .HasMaxLength(20);

            builder.Property(e => e.Email)
                .HasColumnName("Email")
                .HasMaxLength(100);

            builder.Property(e => e.Address)
                .HasColumnName("DiaChi")
                .HasMaxLength(255);

            builder.Property(e => e.TaxCode)
                .HasColumnName("MaSoThue")
                .HasMaxLength(20);

            builder.Property(e => e.IsActive)
                .HasColumnName("DangHoatDong")
                .HasDefaultValue(false);

            builder.Property(e => e.IsDeleted)
                .HasColumnName("DaXoa")
                .HasDefaultValue(false);
        }
    }
}
