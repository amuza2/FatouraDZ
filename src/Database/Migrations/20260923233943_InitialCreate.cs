using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FatouraDZ.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Businesses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nom = table.Column<string>(type: "TEXT", nullable: false),
                    TypeEntreprise = table.Column<int>(type: "INTEGER", nullable: false),
                    NomComplet = table.Column<string>(type: "TEXT", nullable: false),
                    RaisonSociale = table.Column<string>(type: "TEXT", nullable: true),
                    Activite = table.Column<string>(type: "TEXT", nullable: true),
                    Adresse = table.Column<string>(type: "TEXT", nullable: false),
                    Ville = table.Column<string>(type: "TEXT", nullable: false),
                    Wilaya = table.Column<string>(type: "TEXT", nullable: false),
                    CodePostal = table.Column<string>(type: "TEXT", nullable: true),
                    Telephone = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: true),
                    Fax = table.Column<string>(type: "TEXT", nullable: true),
                    RC = table.Column<string>(type: "TEXT", nullable: false),
                    NIS = table.Column<string>(type: "TEXT", nullable: false),
                    NIF = table.Column<string>(type: "TEXT", nullable: false),
                    AI = table.Column<string>(type: "TEXT", nullable: false),
                    NumeroImmatriculation = table.Column<string>(type: "TEXT", nullable: false),
                    CapitalSocial = table.Column<string>(type: "TEXT", nullable: true),
                    CheminLogo = table.Column<string>(type: "TEXT", nullable: true),
                    DateCreation = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateModification = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Businesses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Configurations",
                columns: table => new
                {
                    Cle = table.Column<string>(type: "TEXT", nullable: false),
                    Valeur = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Configurations", x => x.Cle);
                });

            migrationBuilder.CreateTable(
                name: "CategoriesTransaction",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BusinessId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nom = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoriesTransaction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CategoriesTransaction_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Clients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BusinessId = table.Column<int>(type: "INTEGER", nullable: false),
                    TypeClient = table.Column<int>(type: "INTEGER", nullable: false),
                    Nom = table.Column<string>(type: "TEXT", nullable: false),
                    Adresse = table.Column<string>(type: "TEXT", nullable: false),
                    Telephone = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: true),
                    Fax = table.Column<string>(type: "TEXT", nullable: true),
                    FormeJuridique = table.Column<string>(type: "TEXT", nullable: true),
                    RC = table.Column<string>(type: "TEXT", nullable: true),
                    NIS = table.Column<string>(type: "TEXT", nullable: true),
                    NIF = table.Column<string>(type: "TEXT", nullable: true),
                    AI = table.Column<string>(type: "TEXT", nullable: true),
                    NumeroImmatriculation = table.Column<string>(type: "TEXT", nullable: true),
                    Activite = table.Column<string>(type: "TEXT", nullable: true),
                    CapitalSocial = table.Column<string>(type: "TEXT", nullable: true),
                    DateCreation = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateModification = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Clients_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BusinessId = table.Column<int>(type: "INTEGER", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Montant = table.Column<decimal>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Categorie = table.Column<string>(type: "TEXT", nullable: false),
                    FactureId = table.Column<int>(type: "INTEGER", nullable: true),
                    NumeroFacture = table.Column<string>(type: "TEXT", nullable: true),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateModification = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transactions_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Factures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NumeroFacture = table.Column<string>(type: "TEXT", nullable: false),
                    DateFacture = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateEcheance = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateValidite = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TypeFacture = table.Column<int>(type: "INTEGER", nullable: false),
                    ModePaiement = table.Column<string>(type: "TEXT", nullable: false),
                    PaiementReference = table.Column<string>(type: "TEXT", nullable: true),
                    PaiementValeur = table.Column<decimal>(type: "TEXT", nullable: false),
                    PaiementNumeroPiece = table.Column<string>(type: "TEXT", nullable: true),
                    ClientBusinessType = table.Column<int>(type: "INTEGER", nullable: false),
                    ClientNom = table.Column<string>(type: "TEXT", nullable: false),
                    ClientAdresse = table.Column<string>(type: "TEXT", nullable: false),
                    ClientTelephone = table.Column<string>(type: "TEXT", nullable: false),
                    ClientEmail = table.Column<string>(type: "TEXT", nullable: true),
                    ClientFormeJuridique = table.Column<string>(type: "TEXT", nullable: true),
                    ClientRC = table.Column<string>(type: "TEXT", nullable: true),
                    ClientNIS = table.Column<string>(type: "TEXT", nullable: true),
                    ClientNIF = table.Column<string>(type: "TEXT", nullable: true),
                    ClientAI = table.Column<string>(type: "TEXT", nullable: true),
                    ClientNumeroImmatriculation = table.Column<string>(type: "TEXT", nullable: true),
                    ClientActivite = table.Column<string>(type: "TEXT", nullable: true),
                    ClientFax = table.Column<string>(type: "TEXT", nullable: true),
                    ClientCapitalSocial = table.Column<string>(type: "TEXT", nullable: true),
                    NumeroFactureOrigine = table.Column<string>(type: "TEXT", nullable: true),
                    TauxRetenueSource = table.Column<decimal>(type: "TEXT", nullable: true),
                    RetenueSource = table.Column<decimal>(type: "TEXT", nullable: false),
                    RemiseGlobale = table.Column<decimal>(type: "TEXT", nullable: false),
                    TypeRemiseGlobale = table.Column<int>(type: "INTEGER", nullable: false),
                    MontantRemiseGlobale = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalHT = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalTVA19 = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalTVA9 = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalTTC = table.Column<decimal>(type: "TEXT", nullable: false),
                    TimbreFiscal = table.Column<decimal>(type: "TEXT", nullable: false),
                    EstTimbreApplique = table.Column<bool>(type: "INTEGER", nullable: false),
                    MontantTotal = table.Column<decimal>(type: "TEXT", nullable: false),
                    MontantEnLettres = table.Column<string>(type: "TEXT", nullable: false),
                    CheminPDF = table.Column<string>(type: "TEXT", nullable: true),
                    Statut = table.Column<int>(type: "INTEGER", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateModification = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false),
                    BusinessId = table.Column<int>(type: "INTEGER", nullable: false),
                    ClientId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Factures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Factures_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Factures_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LignesFacture",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FactureId = table.Column<int>(type: "INTEGER", nullable: false),
                    NumeroLigne = table.Column<int>(type: "INTEGER", nullable: false),
                    Reference = table.Column<string>(type: "TEXT", nullable: true),
                    Designation = table.Column<string>(type: "TEXT", nullable: false),
                    Quantite = table.Column<decimal>(type: "TEXT", nullable: false),
                    Unite = table.Column<int>(type: "INTEGER", nullable: false),
                    PrixUnitaire = table.Column<decimal>(type: "TEXT", nullable: false),
                    TauxTVA = table.Column<int>(type: "INTEGER", nullable: false),
                    Remise = table.Column<decimal>(type: "TEXT", nullable: false),
                    TypeRemise = table.Column<int>(type: "INTEGER", nullable: false),
                    MontantRemise = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalHT = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LignesFacture", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LignesFacture_Factures_FactureId",
                        column: x => x.FactureId,
                        principalTable: "Factures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CategoriesTransaction_BusinessId",
                table: "CategoriesTransaction",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_BusinessId",
                table: "Clients",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_Factures_BusinessId",
                table: "Factures",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_Factures_ClientId",
                table: "Factures",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Factures_NumeroFacture",
                table: "Factures",
                column: "NumeroFacture",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LignesFacture_FactureId",
                table: "LignesFacture",
                column: "FactureId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_BusinessId",
                table: "Transactions",
                column: "BusinessId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CategoriesTransaction");

            migrationBuilder.DropTable(
                name: "Configurations");

            migrationBuilder.DropTable(
                name: "LignesFacture");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "Factures");

            migrationBuilder.DropTable(
                name: "Clients");

            migrationBuilder.DropTable(
                name: "Businesses");
        }
    }
}
