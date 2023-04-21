using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MinitoriEx.Migrations
{
    public partial class bots : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "botMessage",
                table: "Messages",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "botMessage",
                table: "Messages");
        }
    }
}
