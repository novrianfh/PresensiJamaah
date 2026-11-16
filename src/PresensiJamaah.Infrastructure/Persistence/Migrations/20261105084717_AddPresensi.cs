using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PresensiJamaah.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPresensi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Presensi",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JamaahId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    TanggalKejadian = table.Column<DateOnly>(type: "date", nullable: false),
                    WaktuScan = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Presensi", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Presensi_Event_EventId",
                        column: x => x.EventId,
                        principalTable: "Event",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Presensi_Jamaah_JamaahId",
                        column: x => x.JamaahId,
                        principalTable: "Jamaah",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Presensi_EventId_TanggalKejadian",
                table: "Presensi",
                columns: new[] { "EventId", "TanggalKejadian" });

            migrationBuilder.CreateIndex(
                name: "IX_Presensi_JamaahId_EventId_TanggalKejadian",
                table: "Presensi",
                columns: new[] { "JamaahId", "EventId", "TanggalKejadian" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Presensi");
        }
    }
}
