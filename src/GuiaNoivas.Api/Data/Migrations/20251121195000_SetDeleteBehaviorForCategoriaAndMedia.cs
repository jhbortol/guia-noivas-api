using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuiaNoivas.Api.Data.Migrations
{
    public partial class SetDeleteBehaviorForCategoriaAndMedia : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop existing FKs if present
            migrationBuilder.DropForeignKey(
                name: "FK_Fornecedores_Categorias_CategoriaId",
                table: "Fornecedores");

            migrationBuilder.DropForeignKey(
                name: "FK_Media_Fornecedores_FornecedorId",
                table: "Media");

            // Recreate with desired delete behaviors
            migrationBuilder.AddForeignKey(
                name: "FK_Fornecedores_Categorias_CategoriaId",
                table: "Fornecedores",
                column: "CategoriaId",
                principalTable: "Categorias",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Media_Fornecedores_FornecedorId",
                table: "Media",
                column: "FornecedorId",
                principalTable: "Fornecedores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Fornecedores_Categorias_CategoriaId",
                table: "Fornecedores");

            migrationBuilder.DropForeignKey(
                name: "FK_Media_Fornecedores_FornecedorId",
                table: "Media");

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
    }
}
