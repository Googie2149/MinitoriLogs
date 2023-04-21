using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MinitoriEx.Migrations
{
    public partial class guildid : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Deleted",
                table: "Messages",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedTime",
                table: "Messages",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<ulong>(
                name: "GuildId",
                table: "Messages",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0ul);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Deleted",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "DeletedTime",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "GuildId",
                table: "Messages");
        }
    }
}
