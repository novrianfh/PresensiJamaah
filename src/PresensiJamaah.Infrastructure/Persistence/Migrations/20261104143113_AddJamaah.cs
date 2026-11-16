using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PresensiJamaah.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddJamaah : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "nomor_jamaah_seq");

            migrationBuilder.CreateTable(
                name: "Jamaah",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nama = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    NomorHp = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Alamat = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    TanggalLahir = table.Column<DateOnly>(type: "date", nullable: false),
                    JenisKelamin = table.Column<int>(type: "integer", nullable: false),
                    NomorJamaah = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jamaah", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Jamaah_Nama",
                table: "Jamaah",
                column: "Nama");

            migrationBuilder.CreateIndex(
                name: "IX_Jamaah_NomorJamaah",
                table: "Jamaah",
                column: "NomorJamaah",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Jamaah");

            migrationBuilder.DropSequence(
                name: "nomor_jamaah_seq");
        }
    }
}
