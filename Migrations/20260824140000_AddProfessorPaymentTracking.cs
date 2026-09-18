using DCMSApp.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DCMSApp.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260824140000_AddProfessorPaymentTracking")]
public partial class AddProfessorPaymentTracking : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsPaid",
            table: "PaymentLineItems",
            type: "INTEGER",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<DateTime>(
            name: "PaidAt",
            table: "PaymentLineItems",
            type: "TEXT",
            nullable: true);

        // Keep historical monthly payrolls consistent with their already-paid status.
        migrationBuilder.Sql("""
            UPDATE PaymentLineItems
            SET IsPaid = 1,
                PaidAt = CURRENT_TIMESTAMP
            WHERE PaymentPeriodId IN (
                SELECT Id FROM PaymentPeriods WHERE Status = 3
            );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "IsPaid",
            table: "PaymentLineItems");

        migrationBuilder.DropColumn(
            name: "PaidAt",
            table: "PaymentLineItems");
    }
}
