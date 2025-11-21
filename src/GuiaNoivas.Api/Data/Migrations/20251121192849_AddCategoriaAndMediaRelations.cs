using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuiaNoivas.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoriaAndMediaRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CategoriaId",
                table: "Fornecedores",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Media_FornecedorId",
                table: "Media",
                column: "FornecedorId");

            migrationBuilder.CreateIndex(
                name: "IX_Fornecedores_CategoriaId",
                table: "Fornecedores",
                column: "CategoriaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Fornecedores_Categorias_CategoriaId",
                table: "Fornecedores",
                column: "CategoriaId",
                principalTable: "Categorias",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Media_Fornecedores_FornecedorId",
                table: "Media",
                column: "FornecedorId",
                principalTable: "Fornecedores",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Fornecedores_Categorias_CategoriaId",
                table: "Fornecedores");

            migrationBuilder.DropForeignKey(
                name: "FK_Media_Fornecedores_FornecedorId",
                table: "Media");

            migrationBuilder.DropIndex(
                name: "IX_Media_FornecedorId",
                table: "Media");

            migrationBuilder.DropIndex(
                name: "IX_Fornecedores_CategoriaId",
                table: "Fornecedores");

            migrationBuilder.DropColumn(
                name: "CategoriaId",
                table: "Fornecedores");
        }
    }
}
