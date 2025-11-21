using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuiaNoivas.Api.Data.Migrations
{
    public partial class AddFornecedorAtivo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add column for SQL Server and other providers
            migrationBuilder.AddColumn<bool>(
                name: "Ativo",
                table: "Fornecedores",
                type: "bit",
                nullable: false,
                defaultValue: true);

            // For SQLite we need to ensure compatibility: AddColumn above works for SQLite too
            // but older SQLite providers may require table rebuilds. This migration uses AddColumn
            // which EF will translate appropriately.
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ativo",
                table: "Fornecedores");
        }
    }
}
