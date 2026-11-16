using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PresensiJamaah.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Event",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nama = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Deskripsi = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    WaktuMulai = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    WaktuSelesai = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Recurrence = table.Column<int>(type: "integer", nullable: false),
                    RecurrenceEndDate = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Event", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Event_Nama",
                table: "Event",
                column: "Nama");

            migrationBuilder.CreateIndex(
                name: "IX_Event_WaktuMulai",
                table: "Event",
                column: "WaktuMulai");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Event");
        }
    }
}
