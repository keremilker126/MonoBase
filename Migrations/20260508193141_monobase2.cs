using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonoBase.Migrations
{
    /// <inheritdoc />
    public partial class monobase2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LoginVerificationToken",
                table: "Users",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LoginVerificationToken",
                table: "Users");
        }
    }
}
