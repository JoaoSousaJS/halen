using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Halen.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicalRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reviews_ClinicId_DoctorProfileId_ModerationStatus",
                table: "Reviews");

            migrationBuilder.CreateTable(
                name: "MedicalDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentType = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FileName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FilePath = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LinkedAppointmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicalDocuments_Appointments_LinkedAppointmentId",
                        column: x => x.LinkedAppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MedicalDocuments_AspNetUsers_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MedicalDocuments_Clinics_ClinicId",
                        column: x => x.ClinicId,
                        principalTable: "Clinics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MedicalDocuments_PatientProfiles_PatientProfileId",
                        column: x => x.PatientProfileId,
                        principalTable: "PatientProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PatientAllergies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    AllergenName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Reaction = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Severity = table.Column<string>(type: "text", nullable: false),
                    DateIdentified = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    AddedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientAllergies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientAllergies_AspNetUsers_AddedByUserId",
                        column: x => x.AddedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PatientAllergies_Clinics_ClinicId",
                        column: x => x.ClinicId,
                        principalTable: "Clinics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PatientAllergies_PatientProfiles_PatientProfileId",
                        column: x => x.PatientProfileId,
                        principalTable: "PatientProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PatientConditions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    IcdCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IcdDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DateOfOnset = table.Column<DateOnly>(type: "date", nullable: true),
                    Severity = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ClinicalNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AddedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LinkedAppointmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientConditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientConditions_Appointments_LinkedAppointmentId",
                        column: x => x.LinkedAppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PatientConditions_AspNetUsers_AddedByUserId",
                        column: x => x.AddedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PatientConditions_Clinics_ClinicId",
                        column: x => x.ClinicId,
                        principalTable: "Clinics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PatientConditions_PatientProfiles_PatientProfileId",
                        column: x => x.PatientProfileId,
                        principalTable: "PatientProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PatientFamilyHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Relationship = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ConditionName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AgeAtOnset = table.Column<int>(type: "integer", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AddedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientFamilyHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientFamilyHistories_AspNetUsers_AddedByUserId",
                        column: x => x.AddedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PatientFamilyHistories_Clinics_ClinicId",
                        column: x => x.ClinicId,
                        principalTable: "Clinics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PatientFamilyHistories_PatientProfiles_PatientProfileId",
                        column: x => x.PatientProfileId,
                        principalTable: "PatientProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PatientMedications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    MedicationName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Dosage = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Frequency = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    PrescribedByName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LinkedPrescriptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    AddedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientMedications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientMedications_AspNetUsers_AddedByUserId",
                        column: x => x.AddedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PatientMedications_Clinics_ClinicId",
                        column: x => x.ClinicId,
                        principalTable: "Clinics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PatientMedications_PatientProfiles_PatientProfileId",
                        column: x => x.PatientProfileId,
                        principalTable: "PatientProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PatientMedications_Prescriptions_LinkedPrescriptionId",
                        column: x => x.LinkedPrescriptionId,
                        principalTable: "Prescriptions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PatientVitals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    VitalType = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    SecondaryValue = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MeasuredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AddedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientVitals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientVitals_AspNetUsers_AddedByUserId",
                        column: x => x.AddedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PatientVitals_Clinics_ClinicId",
                        column: x => x.ClinicId,
                        principalTable: "Clinics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PatientVitals_PatientProfiles_PatientProfileId",
                        column: x => x.PatientProfileId,
                        principalTable: "PatientProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecordAccesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    GrantedToUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccessLevel = table.Column<string>(type: "text", nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GrantedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecordAccesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecordAccesses_AspNetUsers_GrantedByUserId",
                        column: x => x.GrantedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecordAccesses_AspNetUsers_GrantedToUserId",
                        column: x => x.GrantedToUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecordAccesses_Clinics_ClinicId",
                        column: x => x.ClinicId,
                        principalTable: "Clinics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecordAccesses_PatientProfiles_PatientProfileId",
                        column: x => x.PatientProfileId,
                        principalTable: "PatientProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecordAccessLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccessedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ResourceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecordAccessLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecordAccessLogs_AspNetUsers_AccessedByUserId",
                        column: x => x.AccessedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecordAccessLogs_Clinics_ClinicId",
                        column: x => x.ClinicId,
                        principalTable: "Clinics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecordAccessLogs_PatientProfiles_PatientProfileId",
                        column: x => x.PatientProfileId,
                        principalTable: "PatientProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReviewHelpfulVotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewHelpfulVotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReviewHelpfulVotes_Clinics_ClinicId",
                        column: x => x.ClinicId,
                        principalTable: "Clinics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReviewHelpfulVotes_Reviews_ReviewId",
                        column: x => x.ReviewId,
                        principalTable: "Reviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ClinicId_DoctorProfileId_ModerationStatus_CreatedAt",
                table: "Reviews",
                columns: new[] { "ClinicId", "DoctorProfileId", "ModerationStatus", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicalDocuments_ClinicId_PatientProfileId",
                table: "MedicalDocuments",
                columns: new[] { "ClinicId", "PatientProfileId" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicalDocuments_LinkedAppointmentId",
                table: "MedicalDocuments",
                column: "LinkedAppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalDocuments_PatientProfileId",
                table: "MedicalDocuments",
                column: "PatientProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalDocuments_UploadedByUserId",
                table: "MedicalDocuments",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientAllergies_AddedByUserId",
                table: "PatientAllergies",
                column: "AddedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientAllergies_ClinicId_PatientProfileId",
                table: "PatientAllergies",
                columns: new[] { "ClinicId", "PatientProfileId" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientAllergies_PatientProfileId",
                table: "PatientAllergies",
                column: "PatientProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientConditions_AddedByUserId",
                table: "PatientConditions",
                column: "AddedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientConditions_ClinicId_PatientProfileId",
                table: "PatientConditions",
                columns: new[] { "ClinicId", "PatientProfileId" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientConditions_LinkedAppointmentId",
                table: "PatientConditions",
                column: "LinkedAppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientConditions_PatientProfileId",
                table: "PatientConditions",
                column: "PatientProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientFamilyHistories_AddedByUserId",
                table: "PatientFamilyHistories",
                column: "AddedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientFamilyHistories_ClinicId_PatientProfileId",
                table: "PatientFamilyHistories",
                columns: new[] { "ClinicId", "PatientProfileId" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientFamilyHistories_PatientProfileId",
                table: "PatientFamilyHistories",
                column: "PatientProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientMedications_AddedByUserId",
                table: "PatientMedications",
                column: "AddedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientMedications_ClinicId_PatientProfileId",
                table: "PatientMedications",
                columns: new[] { "ClinicId", "PatientProfileId" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientMedications_LinkedPrescriptionId",
                table: "PatientMedications",
                column: "LinkedPrescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientMedications_PatientProfileId",
                table: "PatientMedications",
                column: "PatientProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientVitals_AddedByUserId",
                table: "PatientVitals",
                column: "AddedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientVitals_ClinicId_PatientProfileId_VitalType_MeasuredAt",
                table: "PatientVitals",
                columns: new[] { "ClinicId", "PatientProfileId", "VitalType", "MeasuredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientVitals_PatientProfileId",
                table: "PatientVitals",
                column: "PatientProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_RecordAccesses_ClinicId_PatientProfileId_GrantedToUserId",
                table: "RecordAccesses",
                columns: new[] { "ClinicId", "PatientProfileId", "GrantedToUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecordAccesses_GrantedByUserId",
                table: "RecordAccesses",
                column: "GrantedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RecordAccesses_GrantedToUserId",
                table: "RecordAccesses",
                column: "GrantedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RecordAccesses_PatientProfileId",
                table: "RecordAccesses",
                column: "PatientProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_RecordAccessLogs_AccessedByUserId",
                table: "RecordAccessLogs",
                column: "AccessedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RecordAccessLogs_ClinicId_PatientProfileId_AccessedAt",
                table: "RecordAccessLogs",
                columns: new[] { "ClinicId", "PatientProfileId", "AccessedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RecordAccessLogs_PatientProfileId",
                table: "RecordAccessLogs",
                column: "PatientProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewHelpfulVotes_ClinicId",
                table: "ReviewHelpfulVotes",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewHelpfulVotes_ReviewId_UserId",
                table: "ReviewHelpfulVotes",
                columns: new[] { "ReviewId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MedicalDocuments");

            migrationBuilder.DropTable(
                name: "PatientAllergies");

            migrationBuilder.DropTable(
                name: "PatientConditions");

            migrationBuilder.DropTable(
                name: "PatientFamilyHistories");

            migrationBuilder.DropTable(
                name: "PatientMedications");

            migrationBuilder.DropTable(
                name: "PatientVitals");

            migrationBuilder.DropTable(
                name: "RecordAccesses");

            migrationBuilder.DropTable(
                name: "RecordAccessLogs");

            migrationBuilder.DropTable(
                name: "ReviewHelpfulVotes");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_ClinicId_DoctorProfileId_ModerationStatus_CreatedAt",
                table: "Reviews");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ClinicId_DoctorProfileId_ModerationStatus",
                table: "Reviews",
                columns: new[] { "ClinicId", "DoctorProfileId", "ModerationStatus" });
        }
    }
}
