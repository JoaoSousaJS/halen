using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Halen.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiTenancy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Prescriptions_DoctorId_Status",
                table: "Prescriptions");

            migrationBuilder.DropIndex(
                name: "IX_Prescriptions_PatientId_Status",
                table: "Prescriptions");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_ActorId",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_DoctorId_Status_ScheduledAt",
                table: "Appointments");

            migrationBuilder.AddColumn<Guid>(
                name: "ClinicId",
                table: "Prescriptions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ClinicId",
                table: "PatientProfiles",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ClinicId",
                table: "KycReviews",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ClinicId",
                table: "KycDocuments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ClinicId",
                table: "DoctorProfiles",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ClinicId",
                table: "AuditLogs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ClinicId",
                table: "AspNetUsers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ClinicId",
                table: "Appointments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "Clinics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clinics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClinicFeatureFlags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uuid", nullable: false),
                    FeatureKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicFeatureFlags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClinicFeatureFlags_Clinics_ClinicId",
                        column: x => x.ClinicId,
                        principalTable: "Clinics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_ClinicId_DoctorId_Status",
                table: "Prescriptions",
                columns: new[] { "ClinicId", "DoctorId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_ClinicId_PatientId_Status",
                table: "Prescriptions",
                columns: new[] { "ClinicId", "PatientId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_DoctorId",
                table: "Prescriptions",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_PatientId",
                table: "Prescriptions",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientProfiles_ClinicId",
                table: "PatientProfiles",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_KycReviews_ClinicId",
                table: "KycReviews",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_KycDocuments_ClinicId",
                table: "KycDocuments",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorProfiles_ClinicId",
                table: "DoctorProfiles",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ClinicId_ActorId",
                table: "AuditLogs",
                columns: new[] { "ClinicId", "ActorId" });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_ClinicId",
                table: "AspNetUsers",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ClinicId_DoctorId_Status_ScheduledAt",
                table: "Appointments",
                columns: new[] { "ClinicId", "DoctorId", "Status", "ScheduledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ClinicId_PatientId",
                table: "Appointments",
                columns: new[] { "ClinicId", "PatientId" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_DoctorId",
                table: "Appointments",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicFeatureFlags_ClinicId_FeatureKey",
                table: "ClinicFeatureFlags",
                columns: new[] { "ClinicId", "FeatureKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clinics_Slug",
                table: "Clinics",
                column: "Slug",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Clinics_ClinicId",
                table: "Appointments",
                column: "ClinicId",
                principalTable: "Clinics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Clinics_ClinicId",
                table: "AspNetUsers",
                column: "ClinicId",
                principalTable: "Clinics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_Clinics_ClinicId",
                table: "AuditLogs",
                column: "ClinicId",
                principalTable: "Clinics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DoctorProfiles_Clinics_ClinicId",
                table: "DoctorProfiles",
                column: "ClinicId",
                principalTable: "Clinics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_KycDocuments_Clinics_ClinicId",
                table: "KycDocuments",
                column: "ClinicId",
                principalTable: "Clinics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_KycReviews_Clinics_ClinicId",
                table: "KycReviews",
                column: "ClinicId",
                principalTable: "Clinics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PatientProfiles_Clinics_ClinicId",
                table: "PatientProfiles",
                column: "ClinicId",
                principalTable: "Clinics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Prescriptions_Clinics_ClinicId",
                table: "Prescriptions",
                column: "ClinicId",
                principalTable: "Clinics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Clinics_ClinicId",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Clinics_ClinicId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_Clinics_ClinicId",
                table: "AuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_DoctorProfiles_Clinics_ClinicId",
                table: "DoctorProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_KycDocuments_Clinics_ClinicId",
                table: "KycDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_KycReviews_Clinics_ClinicId",
                table: "KycReviews");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientProfiles_Clinics_ClinicId",
                table: "PatientProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_Prescriptions_Clinics_ClinicId",
                table: "Prescriptions");

            migrationBuilder.DropTable(
                name: "ClinicFeatureFlags");

            migrationBuilder.DropTable(
                name: "Clinics");

            migrationBuilder.DropIndex(
                name: "IX_Prescriptions_ClinicId_DoctorId_Status",
                table: "Prescriptions");

            migrationBuilder.DropIndex(
                name: "IX_Prescriptions_ClinicId_PatientId_Status",
                table: "Prescriptions");

            migrationBuilder.DropIndex(
                name: "IX_Prescriptions_DoctorId",
                table: "Prescriptions");

            migrationBuilder.DropIndex(
                name: "IX_Prescriptions_PatientId",
                table: "Prescriptions");

            migrationBuilder.DropIndex(
                name: "IX_PatientProfiles_ClinicId",
                table: "PatientProfiles");

            migrationBuilder.DropIndex(
                name: "IX_KycReviews_ClinicId",
                table: "KycReviews");

            migrationBuilder.DropIndex(
                name: "IX_KycDocuments_ClinicId",
                table: "KycDocuments");

            migrationBuilder.DropIndex(
                name: "IX_DoctorProfiles_ClinicId",
                table: "DoctorProfiles");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_ClinicId_ActorId",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_ClinicId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_ClinicId_DoctorId_Status_ScheduledAt",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_ClinicId_PatientId",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_DoctorId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "ClinicId",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "ClinicId",
                table: "PatientProfiles");

            migrationBuilder.DropColumn(
                name: "ClinicId",
                table: "KycReviews");

            migrationBuilder.DropColumn(
                name: "ClinicId",
                table: "KycDocuments");

            migrationBuilder.DropColumn(
                name: "ClinicId",
                table: "DoctorProfiles");

            migrationBuilder.DropColumn(
                name: "ClinicId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "ClinicId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ClinicId",
                table: "Appointments");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_DoctorId_Status",
                table: "Prescriptions",
                columns: new[] { "DoctorId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_PatientId_Status",
                table: "Prescriptions",
                columns: new[] { "PatientId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ActorId",
                table: "AuditLogs",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_DoctorId_Status_ScheduledAt",
                table: "Appointments",
                columns: new[] { "DoctorId", "Status", "ScheduledAt" });
        }
    }
}
