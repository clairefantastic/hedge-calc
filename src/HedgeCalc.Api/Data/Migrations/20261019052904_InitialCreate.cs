using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HedgeCalc.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HedgeCalculations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    BaseCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HedgeCalculations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HedgeCalculationLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    HedgeCalculationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Exposure = table.Column<decimal>(type: "TEXT", precision: 28, scale: 8, nullable: false),
                    HedgeRatio = table.Column<decimal>(type: "TEXT", precision: 9, scale: 8, nullable: false),
                    TargetNotional = table.Column<decimal>(type: "TEXT", precision: 28, scale: 8, nullable: false),
                    Direction = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HedgeCalculationLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HedgeCalculationLines_HedgeCalculations_HedgeCalculationId",
                        column: x => x.HedgeCalculationId,
                        principalTable: "HedgeCalculations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HedgeCalculationLines_HedgeCalculationId",
                table: "HedgeCalculationLines",
                column: "HedgeCalculationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HedgeCalculationLines");

            migrationBuilder.DropTable(
                name: "HedgeCalculations");
        }
    }
}
