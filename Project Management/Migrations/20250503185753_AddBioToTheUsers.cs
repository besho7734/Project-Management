using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project_Management.Migrations
{
    /// <inheritdoc />
    public partial class AddBioToTheUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "bio",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "bio",
                table: "AspNetUsers");
        }
    }
}
