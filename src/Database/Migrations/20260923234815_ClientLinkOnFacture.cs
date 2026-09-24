using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FatouraDZ.Database.Migrations
{
    /// <inheritdoc />
    public partial class ClientLinkOnFacture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Factures_Clients_ClientId",
                table: "Factures");

            migrationBuilder.AddForeignKey(
                name: "FK_Factures_Clients_ClientId",
                table: "Factures",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Factures_Clients_ClientId",
                table: "Factures");

            migrationBuilder.AddForeignKey(
                name: "FK_Factures_Clients_ClientId",
                table: "Factures",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id");
        }
    }
}
