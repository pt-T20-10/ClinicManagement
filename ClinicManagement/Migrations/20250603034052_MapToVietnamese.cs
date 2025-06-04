    using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManagement.Migrations
{
    /// <inheritdoc />
    public partial class MapToVietnamese : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChuyenKhoaBacSi",
                columns: table => new
                {
                    MaChuyenKhoa = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenChuyenKhoa = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DaXoa = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    MoTa = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChuyenKhoaBacSi", x => x.MaChuyenKhoa);
                });

            migrationBuilder.CreateTable(
                name: "DanhMucThuoc",
                columns: table => new
                {
                    MaDanhMuc = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenDanhMuc = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DaXoa = table.Column<bool>(type: "bit", nullable: true, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DanhMucThuoc", x => x.MaDanhMuc);
                });

            migrationBuilder.CreateTable(
                name: "DonVi",
                columns: table => new
                {
                    MaDonVi = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenDonVi = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DaXoa = table.Column<bool>(type: "bit", nullable: true, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonVi", x => x.MaDonVi);
                });

            migrationBuilder.CreateTable(
                name: "LoaiBenhNhan",
                columns: table => new
                {
                    MaLoaiBenhNhan = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenLoai = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GiamGia = table.Column<decimal>(type: "decimal(5,2)", nullable: true, defaultValue: 0m),
                    DaXoa = table.Column<bool>(type: "bit", nullable: true, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoaiBenhNhan", x => x.MaLoaiBenhNhan);
                });

            migrationBuilder.CreateTable(
                name: "LoaiLichHen",
                columns: table => new
                {
                    MaLoaiLichHen = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenLoai = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DaXoa = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    MoTa = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoaiLichHen", x => x.MaLoaiLichHen);
                });

            migrationBuilder.CreateTable(
                name: "NhaCungCap",
                columns: table => new
                {
                    MaNhaCungCap = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaSoNhaCungCap = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TenNhaCungCap = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NguoiLienHe = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SoDienThoai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DiaChi = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    MaSoThue = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DangHoatDong = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    DaXoa = table.Column<bool>(type: "bit", nullable: true, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhaCungCap", x => x.MaNhaCungCap);
                });

            migrationBuilder.CreateTable(
                name: "BacSi",
                columns: table => new
                {
                    MaBacSi = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HoTen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MaChuyenKhoa = table.Column<int>(type: "int", nullable: true),
                    LinkChungChi = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    LichLamViec = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    SoDienThoai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DaXoa = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BacSi", x => x.MaBacSi);
                    table.ForeignKey(
                        name: "FK_BacSi_ChuyenKhoa",
                        column: x => x.MaChuyenKhoa,
                        principalTable: "ChuyenKhoaBacSi",
                        principalColumn: "MaChuyenKhoa");
                });

            migrationBuilder.CreateTable(
                name: "BenhNhan",
                columns: table => new
                {
                    MaBenhNhan = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaSoBaoHiem = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    HoTen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NgaySinh = table.Column<DateOnly>(type: "date", nullable: true),
                    GioiTinh = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    SoDienThoai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DiaChi = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    MaLoaiBenhNhan = table.Column<int>(type: "int", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DaXoa = table.Column<bool>(type: "bit", nullable: true, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenhNhan", x => x.MaBenhNhan);
                    table.ForeignKey(
                        name: "FK_BenhNhan_LoaiBenhNhan",
                        column: x => x.MaLoaiBenhNhan,
                        principalTable: "LoaiBenhNhan",
                        principalColumn: "MaLoaiBenhNhan");
                });

            migrationBuilder.CreateTable(
                name: "Thuoc",
                columns: table => new
                {
                    MaThuoc = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MSHNNB = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TenThuoc = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MaDanhMuc = table.Column<int>(type: "int", nullable: true),
                    MaDonVi = table.Column<int>(type: "int", nullable: true),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    DaXoa = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    MaNhaCungCap = table.Column<int>(type: "int", nullable: true),
                    MaVach = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true),
                    MaQR = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Thuoc", x => x.MaThuoc);
                    table.ForeignKey(
                        name: "FK_Thuoc_DanhMucThuoc",
                        column: x => x.MaDanhMuc,
                        principalTable: "DanhMucThuoc",
                        principalColumn: "MaDanhMuc");
                    table.ForeignKey(
                        name: "FK_Thuoc_DonVi",
                        column: x => x.MaDonVi,
                        principalTable: "DonVi",
                        principalColumn: "MaDonVi");
                    table.ForeignKey(
                        name: "FK_Thuoc_NhaCungCap",
                        column: x => x.MaNhaCungCap,
                        principalTable: "NhaCungCap",
                        principalColumn: "MaNhaCungCap");
                });

            migrationBuilder.CreateTable(
                name: "TaiKhoan",
                columns: table => new
                {
                    TenDangNhap = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MaBacSi = table.Column<int>(type: "int", nullable: true),
                    MatKhau = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    VaiTro = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DaDangNhap = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    DaXoa = table.Column<bool>(type: "bit", nullable: true, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaiKhoan", x => x.TenDangNhap);
                    table.ForeignKey(
                        name: "FK_TaiKhoan_BacSi",
                        column: x => x.MaBacSi,
                        principalTable: "BacSi",
                        principalColumn: "MaBacSi");
                });

            migrationBuilder.CreateTable(
                name: "HoSoBenhAn",
                columns: table => new
                {
                    MaHoSo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaBenhNhan = table.Column<int>(type: "int", nullable: false),
                    MaBacSi = table.Column<int>(type: "int", nullable: false),
                    ChanDoan = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DonThuoc = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KetQuaXetNghiem = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LoiKhuyenBacSi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    DaXoa = table.Column<bool>(type: "bit", nullable: true, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HoSoBenhAn", x => x.MaHoSo);
                    table.ForeignKey(
                        name: "FK_HoSoBenhAn_BacSi",
                        column: x => x.MaBacSi,
                        principalTable: "BacSi",
                        principalColumn: "MaBacSi");
                    table.ForeignKey(
                        name: "FK_HoSoBenhAn_BenhNhan",
                        column: x => x.MaBenhNhan,
                        principalTable: "BenhNhan",
                        principalColumn: "MaBenhNhan");
                });

            migrationBuilder.CreateTable(
                name: "LichHen",
                columns: table => new
                {
                    MaLichHen = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaBenhNhan = table.Column<int>(type: "int", nullable: false),
                    MaBacSi = table.Column<int>(type: "int", nullable: false),
                    NgayHen = table.Column<DateTime>(type: "datetime", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    MaLoaiLichHen = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    DaXoa = table.Column<bool>(type: "bit", nullable: true, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LichHen", x => x.MaLichHen);
                    table.ForeignKey(
                        name: "FK_LichHen_BacSi",
                        column: x => x.MaBacSi,
                        principalTable: "BacSi",
                        principalColumn: "MaBacSi");
                    table.ForeignKey(
                        name: "FK_LichHen_BenhNhan",
                        column: x => x.MaBenhNhan,
                        principalTable: "BenhNhan",
                        principalColumn: "MaBenhNhan");
                    table.ForeignKey(
                        name: "FK_LichHen_LoaiLichHen",
                        column: x => x.MaLoaiLichHen,
                        principalTable: "LoaiLichHen",
                        principalColumn: "MaLoaiLichHen");
                });

            migrationBuilder.CreateTable(
                name: "KhoThuoc",
                columns: table => new
                {
                    MaKho = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaThuoc = table.Column<int>(type: "int", nullable: false),
                    TonKho = table.Column<int>(type: "int", nullable: false),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KhoThuoc", x => x.MaKho);
                    table.ForeignKey(
                        name: "FK_KhoThuoc_Thuoc",
                        column: x => x.MaThuoc,
                        principalTable: "Thuoc",
                        principalColumn: "MaThuoc");
                });

            migrationBuilder.CreateTable(
                name: "NhapKho",
                columns: table => new
                {
                    MaNhapKho = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaThuoc = table.Column<int>(type: "int", nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false),
                    NgayNhap = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    DonGia = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GiaBan = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ThanhTien = table.Column<decimal>(type: "decimal(29,2)", nullable: true, computedColumnSql: "([SoLuong]*[DonGia])", stored: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhapKho", x => x.MaNhapKho);
                    table.ForeignKey(
                        name: "FK_NhapKho_Thuoc",
                        column: x => x.MaThuoc,
                        principalTable: "Thuoc",
                        principalColumn: "MaThuoc");
                });

            migrationBuilder.CreateTable(
                name: "HoaDon",
                columns: table => new
                {
                    MaHoaDon = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaBenhNhan = table.Column<int>(type: "int", nullable: true),
                    TongTien = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NgayLapHoaDon = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    TrangThai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsPharmacySale = table.Column<bool>(type: "bit", nullable: false),
                    MaHoSoBenhAn = table.Column<int>(type: "int", nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HoaDon", x => x.MaHoaDon);
                    table.ForeignKey(
                        name: "FK_HoaDon_BenhNhan",
                        column: x => x.MaBenhNhan,
                        principalTable: "BenhNhan",
                        principalColumn: "MaBenhNhan",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_HoaDon_HoSoBenhAn",
                        column: x => x.MaHoSoBenhAn,
                        principalTable: "HoSoBenhAn",
                        principalColumn: "MaHoSo");
                });

            migrationBuilder.CreateTable(
                name: "ThongBao",
                columns: table => new
                {
                    MaThongBao = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaLichHen = table.Column<int>(type: "int", nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    NgayGui = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    PhuongThuc = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DaXoa = table.Column<bool>(type: "bit", nullable: true, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThongBao", x => x.MaThongBao);
                    table.ForeignKey(
                        name: "FK_ThongBao_LichHen",
                        column: x => x.MaLichHen,
                        principalTable: "LichHen",
                        principalColumn: "MaLichHen");
                });

            migrationBuilder.CreateTable(
                name: "ChiTietHoaDon",
                columns: table => new
                {
                    MaChiTiet = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaHoaDon = table.Column<int>(type: "int", nullable: false),
                    MaThuoc = table.Column<int>(type: "int", nullable: false),
                    MaNhapKho = table.Column<int>(type: "int", nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false),
                    GiaBan = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GiamGia = table.Column<decimal>(type: "decimal(5,2)", nullable: true, defaultValue: 0m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietHoaDon", x => x.MaChiTiet);
                    table.ForeignKey(
                        name: "FK_ChiTietHoaDon_HoaDon",
                        column: x => x.MaHoaDon,
                        principalTable: "HoaDon",
                        principalColumn: "MaHoaDon");
                    table.ForeignKey(
                        name: "FK_ChiTietHoaDon_NhapKho",
                        column: x => x.MaNhapKho,
                        principalTable: "NhapKho",
                        principalColumn: "MaNhapKho");
                    table.ForeignKey(
                        name: "FK_ChiTietHoaDon_Thuoc",
                        column: x => x.MaThuoc,
                        principalTable: "Thuoc",
                        principalColumn: "MaThuoc");
                });

            migrationBuilder.CreateIndex(
                name: "IX_BacSi_MaChuyenKhoa",
                table: "BacSi",
                column: "MaChuyenKhoa");

            migrationBuilder.CreateIndex(
                name: "IX_BenhNhan_MaLoaiBenhNhan",
                table: "BenhNhan",
                column: "MaLoaiBenhNhan");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietHoaDon_MaHoaDon",
                table: "ChiTietHoaDon",
                column: "MaHoaDon");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietHoaDon_MaNhapKho",
                table: "ChiTietHoaDon",
                column: "MaNhapKho");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietHoaDon_MaThuoc",
                table: "ChiTietHoaDon",
                column: "MaThuoc");

            migrationBuilder.CreateIndex(
                name: "IX_HoaDon_MaBenhNhan",
                table: "HoaDon",
                column: "MaBenhNhan");

            migrationBuilder.CreateIndex(
                name: "IX_HoaDon_MaHoSoBenhAn",
                table: "HoaDon",
                column: "MaHoSoBenhAn");

            migrationBuilder.CreateIndex(
                name: "IX_HoSoBenhAn_MaBacSi",
                table: "HoSoBenhAn",
                column: "MaBacSi");

            migrationBuilder.CreateIndex(
                name: "IX_HoSoBenhAn_MaBenhNhan",
                table: "HoSoBenhAn",
                column: "MaBenhNhan");

            migrationBuilder.CreateIndex(
                name: "IX_KhoThuoc_MaThuoc",
                table: "KhoThuoc",
                column: "MaThuoc");

            migrationBuilder.CreateIndex(
                name: "IX_LichHen_MaBacSi",
                table: "LichHen",
                column: "MaBacSi");

            migrationBuilder.CreateIndex(
                name: "IX_LichHen_MaBenhNhan",
                table: "LichHen",
                column: "MaBenhNhan");

            migrationBuilder.CreateIndex(
                name: "IX_LichHen_MaLoaiLichHen",
                table: "LichHen",
                column: "MaLoaiLichHen");

            migrationBuilder.CreateIndex(
                name: "IX_NhapKho_MaThuoc",
                table: "NhapKho",
                column: "MaThuoc");

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoan_MaBacSi",
                table: "TaiKhoan",
                column: "MaBacSi");

            migrationBuilder.CreateIndex(
                name: "IX_ThongBao_MaLichHen",
                table: "ThongBao",
                column: "MaLichHen");

            migrationBuilder.CreateIndex(
                name: "IX_Thuoc_MaDanhMuc",
                table: "Thuoc",
                column: "MaDanhMuc");

            migrationBuilder.CreateIndex(
                name: "IX_Thuoc_MaDonVi",
                table: "Thuoc",
                column: "MaDonVi");

            migrationBuilder.CreateIndex(
                name: "IX_Thuoc_MaNhaCungCap",
                table: "Thuoc",
                column: "MaNhaCungCap");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChiTietHoaDon");

            migrationBuilder.DropTable(
                name: "KhoThuoc");

            migrationBuilder.DropTable(
                name: "TaiKhoan");

            migrationBuilder.DropTable(
                name: "ThongBao");

            migrationBuilder.DropTable(
                name: "HoaDon");

            migrationBuilder.DropTable(
                name: "NhapKho");

            migrationBuilder.DropTable(
                name: "LichHen");

            migrationBuilder.DropTable(
                name: "HoSoBenhAn");

            migrationBuilder.DropTable(
                name: "Thuoc");

            migrationBuilder.DropTable(
                name: "LoaiLichHen");

            migrationBuilder.DropTable(
                name: "BacSi");

            migrationBuilder.DropTable(
                name: "BenhNhan");

            migrationBuilder.DropTable(
                name: "DanhMucThuoc");

            migrationBuilder.DropTable(
                name: "DonVi");

            migrationBuilder.DropTable(
                name: "NhaCungCap");

            migrationBuilder.DropTable(
                name: "ChuyenKhoaBacSi");

            migrationBuilder.DropTable(
                name: "LoaiBenhNhan");
        }
    }
}
