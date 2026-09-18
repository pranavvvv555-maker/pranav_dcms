using DCMSApp.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DCMSApp.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260826120000_AddSessionAttendance")]
public partial class AddSessionAttendance : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "SessionAttendances",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                SessionId = table.Column<int>(type: "INTEGER", nullable: false),
                StudentId = table.Column<int>(type: "INTEGER", nullable: false),
                IsPresent = table.Column<bool>(type: "INTEGER", nullable: false),
                RecordedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SessionAttendances", x => x.Id);
                table.ForeignKey(
                    name: "FK_SessionAttendances_Sessions_SessionId",
                    column: x => x.SessionId,
                    principalTable: "Sessions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_SessionAttendances_Students_StudentId",
                    column: x => x.StudentId,
                    principalTable: "Students",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_SessionAttendances_SessionId_StudentId",
            table: "SessionAttendances",
            columns: new[] { "SessionId", "StudentId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SessionAttendances_StudentId",
            table: "SessionAttendances",
            column: "StudentId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "SessionAttendances");
    }
}
