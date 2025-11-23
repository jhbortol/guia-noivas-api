using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuiaNoivas.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoriaMedia_20251123 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CategoriaId",
                table: "Media",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Media_CategoriaId",
                table: "Media",
                column: "CategoriaId",
                unique: true,
                filter: "[CategoriaId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Media_Categorias_CategoriaId",
                table: "Media",
                column: "CategoriaId",
                principalTable: "Categorias",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Media_Categorias_CategoriaId",
                table: "Media");

            migrationBuilder.DropIndex(
                name: "IX_Media_CategoriaId",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "CategoriaId",
                table: "Media");
        }
    }
}
