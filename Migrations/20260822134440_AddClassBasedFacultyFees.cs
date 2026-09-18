using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DCMSApp.Migrations
{
    /// <inheritdoc />
    public partial class AddClassBasedFacultyFees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LectureCount",
                table: "PaymentLineItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "LectureRate",
                table: "PaymentLineItems",
                type: "TEXT",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "PracticalCount",
                table: "PaymentLineItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "PracticalRate",
                table: "PaymentLineItems",
                type: "TEXT",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LectureRateINR",
                table: "FacultyRates",
                type: "TEXT",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PracticalRateINR",
                table: "FacultyRates",
                type: "TEXT",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            // Preserve existing configured rates as a sensible starting point for both class types.
            // Administrators can then set the two values independently from the Faculty page.
            migrationBuilder.Sql("""
                UPDATE FacultyRates
                SET LectureRateINR = HourlyRateINR,
                    PracticalRateINR = HourlyRateINR
                WHERE CAST(LectureRateINR AS REAL) = 0
                  AND CAST(PracticalRateINR AS REAL) = 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LectureCount",
                table: "PaymentLineItems");

            migrationBuilder.DropColumn(
                name: "LectureRate",
                table: "PaymentLineItems");

            migrationBuilder.DropColumn(
                name: "PracticalCount",
                table: "PaymentLineItems");

            migrationBuilder.DropColumn(
                name: "PracticalRate",
                table: "PaymentLineItems");

            migrationBuilder.DropColumn(
                name: "LectureRateINR",
                table: "FacultyRates");

            migrationBuilder.DropColumn(
                name: "PracticalRateINR",
                table: "FacultyRates");
        }
    }
}
