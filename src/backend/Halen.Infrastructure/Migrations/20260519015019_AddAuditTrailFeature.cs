using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Halen.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditTrailFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ClinicId_TargetId",
                table: "AuditLogs",
                columns: new[] { "ClinicId", "TargetId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_ClinicId_TargetId",
                table: "AuditLogs");
        }
    }
}
