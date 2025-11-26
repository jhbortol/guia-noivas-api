using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuiaNoivas.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInstagramToFornecedor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Instagram",
                table: "Fornecedores",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Instagram",
                table: "Fornecedores");
        }
    }
}
