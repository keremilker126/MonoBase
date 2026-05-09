using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonoBase.Migrations
{
    /// <inheritdoc />
    public partial class monobase6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CorsOrigins",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CorsOrigins",
                table: "Users",
                type: "TEXT",
                nullable: true);
        }
    }
}
