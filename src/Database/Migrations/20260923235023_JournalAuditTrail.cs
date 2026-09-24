using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FatouraDZ.Database.Migrations
{
    /// <inheritdoc />
    public partial class JournalAuditTrail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JournalAudit",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EntiteType = table.Column<string>(type: "TEXT", nullable: false),
                    EntiteId = table.Column<int>(type: "INTEGER", nullable: false),
                    Reference = table.Column<string>(type: "TEXT", nullable: false),
                    Action = table.Column<string>(type: "TEXT", nullable: false),
                    Details = table.Column<string>(type: "TEXT", nullable: true),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Utilisateur = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalAudit", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JournalAudit_EntiteType_EntiteId",
                table: "JournalAudit",
                columns: new[] { "EntiteType", "EntiteId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JournalAudit");
        }
    }
}
