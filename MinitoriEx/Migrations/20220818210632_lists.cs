using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MinitoriEx.Migrations
{
    public partial class lists : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserAttachment");

            migrationBuilder.AddColumn<string>(
                name: "Attachments",
                table: "Messages",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Attachments",
                table: "Messages");

            migrationBuilder.CreateTable(
                name: "UserAttachment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Url = table.Column<string>(type: "TEXT", nullable: true),
                    UserMessageId = table.Column<ulong>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAttachment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAttachment_Messages_UserMessageId",
                        column: x => x.UserMessageId,
                        principalTable: "Messages",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserAttachment_UserMessageId",
                table: "UserAttachment",
                column: "UserMessageId");
        }
    }
}
