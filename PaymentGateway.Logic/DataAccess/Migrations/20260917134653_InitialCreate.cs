using System;
using System.Diagnostics.CodeAnalysis;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PaymentGateway.Logic.DataAccess.Migrations;

[ExcludeFromCodeCoverage]
/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PaymentStatuses",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false),
                Name = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PaymentStatuses", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "PaymentHistories",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Status = table.Column<int>(type: "int", nullable: false),
                Payment = table.Column<string>(type: "nvarchar(max)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PaymentHistories", x => x.Id);
                table.ForeignKey(
                    name: "FK_PaymentHistories_PaymentStatuses_Status",
                    column: x => x.Status,
                    principalTable: "PaymentStatuses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.InsertData(
            table: "PaymentStatuses",
            columns: new[] { "Id", "Name" },
            values: new object[,]
            {
                { 0, "Authorized" },
                { 1, "Declined" },
                { 2, "Rejected" }
            });

        migrationBuilder.CreateIndex(
            name: "IX_PaymentHistories_Status",
            table: "PaymentHistories",
            column: "Status");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "PaymentHistories");

        migrationBuilder.DropTable(
            name: "PaymentStatuses");
    }
}