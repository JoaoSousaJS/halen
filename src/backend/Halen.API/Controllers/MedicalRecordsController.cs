using Halen.Application.Attributes;
using Halen.Application.MedicalRecords.Commands;
using Halen.Application.MedicalRecords.Queries;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Halen.API.Controllers;

[ApiController]
[Route("api/v1/medical-records")]
[Authorize]
[RequireFeature("medical_records")]
public class MedicalRecordsController(IMediator mediator) : HalenControllerBase
{
    // ── GET endpoints (queries) ──────────────────────────────────────────

    [HttpGet("{patientProfileId:guid}/timeline")]
    public async Task<IActionResult> GetTimeline(
        Guid patientProfileId,
        [FromQuery] string[]? filterTypes,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] Guid? filterDoctorId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var query = new GetPatientTimelineQuery(
            GetUserId(), GetUserRoleEnum(), patientProfileId,
            filterTypes, from, to, filterDoctorId, page, pageSize);

        var result = await mediator.Send(query, ct);
        if (!result.Success)
            return MapError(result.Error, result.Kind);

        return Ok(new { result.Entries, result.TotalCount });
    }

    [HttpGet("{patientProfileId:guid}/snapshot")]
    public async Task<IActionResult> GetSnapshot(Guid patientProfileId, CancellationToken ct)
    {
        var query = new GetPatientSnapshotQuery(GetUserId(), GetUserRoleEnum(), patientProfileId);
        var result = await mediator.Send(query, ct);
        if (!result.Success)
            return MapError(result.Error, result.Kind);

        return Ok(result.Snapshot);
    }

    [HttpGet("{patientProfileId:guid}/vitals/{vitalType}/history")]
    public async Task<IActionResult> GetVitalsHistory(
        Guid patientProfileId,
        VitalType vitalType,
        [FromQuery] int daysBack = 90,
        CancellationToken ct = default)
    {
        var query = new GetPatientVitalsHistoryQuery(
            GetUserId(), GetUserRoleEnum(), patientProfileId, vitalType, daysBack);

        var result = await mediator.Send(query, ct);
        if (!result.Success)
            return MapError(result.Error, result.Kind);

        return Ok(result.Readings);
    }

    [HttpGet("{patientProfileId:guid}/conditions")]
    public async Task<IActionResult> GetConditions(Guid patientProfileId, CancellationToken ct)
    {
        var query = new GetPatientConditionsQuery(GetUserId(), GetUserRoleEnum(), patientProfileId);
        var result = await mediator.Send(query, ct);
        if (!result.Success)
            return MapError(result.Error, result.Kind);

        return Ok(result.Conditions);
    }

    [HttpGet("{patientProfileId:guid}/medications")]
    public async Task<IActionResult> GetMedications(Guid patientProfileId, CancellationToken ct)
    {
        var query = new GetPatientMedicationsQuery(GetUserId(), GetUserRoleEnum(), patientProfileId);
        var result = await mediator.Send(query, ct);
        if (!result.Success)
            return MapError(result.Error, result.Kind);

        return Ok(result.Medications);
    }

    [HttpGet("{patientProfileId:guid}/allergies")]
    public async Task<IActionResult> GetAllergies(Guid patientProfileId, CancellationToken ct)
    {
        var query = new GetPatientAllergiesQuery(GetUserId(), GetUserRoleEnum(), patientProfileId);
        var result = await mediator.Send(query, ct);
        if (!result.Success)
            return MapError(result.Error, result.Kind);

        return Ok(result.Allergies);
    }

    [HttpGet("{patientProfileId:guid}/family-history")]
    public async Task<IActionResult> GetFamilyHistory(Guid patientProfileId, CancellationToken ct)
    {
        var query = new GetPatientFamilyHistoryQuery(GetUserId(), GetUserRoleEnum(), patientProfileId);
        var result = await mediator.Send(query, ct);
        if (!result.Success)
            return MapError(result.Error, result.Kind);

        return Ok(result.Entries);
    }

    [HttpGet("{patientProfileId:guid}/documents")]
    public async Task<IActionResult> GetDocuments(
        Guid patientProfileId,
        [FromQuery] MedicalDocumentType? filterType,
        CancellationToken ct = default)
    {
        var query = new GetPatientDocumentsQuery(
            GetUserId(), GetUserRoleEnum(), patientProfileId, filterType);

        var result = await mediator.Send(query, ct);
        if (!result.Success)
            return MapError(result.Error, result.Kind);

        return Ok(result.Documents);
    }

    [HttpGet("documents/{documentId:guid}/download")]
    public async Task<IActionResult> DownloadDocument(Guid documentId, CancellationToken ct)
    {
        var query = new DownloadMedicalDocumentQuery(GetUserId(), GetUserRoleEnum(), documentId);
        var result = await mediator.Send(query, ct);
        if (!result.Success)
            return MapError(result.Error, result.Kind);

        return File(result.FileStream!, result.ContentType!, result.FileName!);
    }

    [HttpGet("{patientProfileId:guid}/header")]
    public async Task<IActionResult> GetHeader(Guid patientProfileId, CancellationToken ct)
    {
        var query = new GetPatientHeaderQuery(GetUserId(), GetUserRoleEnum(), patientProfileId);
        var result = await mediator.Send(query, ct);
        if (!result.Success)
            return MapError(result.Error, result.Kind);

        return Ok(result.Header);
    }

    // ── POST/PUT/DELETE endpoints (commands) ─────────────────────────────

    [HttpPost("{patientProfileId:guid}/conditions")]
    public async Task<IActionResult> AddCondition(
        Guid patientProfileId, AddConditionRequest request, CancellationToken ct)
    {
        var command = new AddConditionCommand(
            GetUserId(), GetUserRoleEnum(), patientProfileId,
            request.IcdCode, request.IcdDescription, request.DateOfOnset,
            request.Severity, request.Status, request.ClinicalNotes,
            request.LinkedAppointmentId);

        var result = await mediator.Send(command, ct);
        if (!result.Success)
            return MapError(result.Error, result.Kind);

        return CreatedAtAction(nameof(GetConditions),
            new { patientProfileId }, new { result.ConditionId });
    }

    [HttpPut("conditions/{conditionId:guid}")]
    public async Task<IActionResult> UpdateCondition(
        Guid conditionId, UpdateConditionRequest request, CancellationToken ct)
    {
        var command = new UpdateConditionCommand(
            GetUserId(), GetUserRoleEnum(), conditionId,
            request.Severity, request.Status, request.ClinicalNotes);

        var result = await mediator.Send(command, ct);
        if (!result.Success)
            return MapError(result.Error, result.Kind);

        return Ok();
    }

    [HttpPost("{patientProfileId:guid}/allergies")]
    public async Task<IActionResult> AddAllergy(
        Guid patientProfileId, AddAllergyRequest request, CancellationToken ct)
    {
        var command = new AddAllergyCommand(
            GetUserId(), GetUserRoleEnum(), patientProfileId,
            request.AllergenName, request.Reaction, request.Severity,
            request.DateIdentified);

        var result = await mediator.Send(command, ct);
        if (!result.Success)
            return MapError(result.Error, result.Kind);

        return CreatedAtAction(nameof(GetAllergies),
            new { patientProfileId }, new { result.AllergyId });
    }

    [HttpPut("allergies/{allergyId:guid}")]
    public async Task<IActionResult> UpdateAllergy(
        Guid allergyId, UpdateAllergyRequest request, CancellationToken ct)
    {
        var command = new UpdateAllergyCommand(
            GetUserId(), GetUserRoleEnum(), allergyId,
            request.Reaction, request.Severity, request.IsActive);

        var result = await mediator.Send(command, ct);
        if (!result.Success)
            return MapError(result.Error, result.Kind);

        return Ok();
    }

    [HttpPost("{patientProfileId:guid}/vitals")]
    public async Task<IActionResult> AddVital(
        Guid patientProfileId, AddVitalRequest request, CancellationToken ct)
    {
        var command = new AddVitalCommand(
            GetUserId(), GetUserRoleEnum(), patientProfileId,
            request.VitalType, request.Value, request.SecondaryValue,
            request.Unit, request.MeasuredAt, request.Source, request.Notes);

        var result = await mediator.Send(command, ct);
        if (!result.Success)
            return MapError(result.Error, result.Kind);

        return CreatedAtAction(nameof(GetVitalsHistory),
            new { patientProfileId, vitalType = request.VitalType },
            new { result.VitalId });
    }

    [HttpPost("{patientProfileId:guid}/medications")]
    public async Task<IActionResult> AddMedication(
        Guid patientProfileId, AddMedicationRequest request, CancellationToken ct)
    {
        var command = new AddMedicationCommand(
            GetUserId(), GetUserRoleEnum(), patientProfileId,
            request.MedicationName, request.Dosage, request.Frequency,
            request.StartDate, request.EndDate, request.PrescribedByName,
            request.LinkedPrescriptionId);

        var result = await mediator.Send(command, ct);
        if (!result.Success)
            return MapError(result.Error, result.Kind);

        return CreatedAtAction(nameof(GetMedications),
            new { patientProfileId }, new { result.MedicationId });
    }

    [HttpPut("medications/{medicationId:guid}")]
    public async Task<IActionResult> UpdateMedication(
        Guid medicationId, UpdateMedicationRequest request, CancellationToken ct)
    {
        var command = new UpdateMedicationCommand(
            GetUserId(), GetUserRoleEnum(), medicationId,
            request.Dosage, request.Frequency, request.EndDate, request.IsActive);

        var result = await mediator.Send(command, ct);
        if (!result.Success)
            return MapError(result.Error, result.Kind);

        return Ok();
    }

    [HttpPost("{patientProfileId:guid}/family-history")]
    public async Task<IActionResult> AddFamilyHistory(
        Guid patientProfileId, AddFamilyHistoryRequest request, CancellationToken ct)
    {
        var command = new AddFamilyHistoryCommand(
            GetUserId(), GetUserRoleEnum(), patientProfileId,
            request.Relationship, request.ConditionName,
            request.AgeAtOnset, request.Notes);

        var result = await mediator.Send(command, ct);
        if (!result.Success)
            return MapError(result.Error, result.Kind);

        return CreatedAtAction(nameof(GetFamilyHistory),
            new { patientProfileId }, new { result.FamilyHistoryId });
    }

    [HttpPut("family-history/{familyHistoryId:guid}")]
    public async Task<IActionResult> UpdateFamilyHistory(
        Guid familyHistoryId, UpdateFamilyHistoryRequest request, CancellationToken ct)
    {
        var command = new UpdateFamilyHistoryCommand(
            GetUserId(), GetUserRoleEnum(), familyHistoryId,
            request.ConditionName, request.AgeAtOnset, request.Notes);

        var result = await mediator.Send(command, ct);
        if (!result.Success)
            return MapError(result.Error, result.Kind);

        return Ok();
    }

    [HttpPost("{patientProfileId:guid}/documents")]
    public async Task<IActionResult> UploadDocument(
        Guid patientProfileId, IFormFile file,
        [FromForm] MedicalDocumentType documentType,
        [FromForm] string title,
        [FromForm] string? description,
        [FromForm] Guid? linkedAppointmentId,
        CancellationToken ct = default)
    {
        var command = new UploadMedicalDocumentCommand(
            GetUserId(), GetUserRoleEnum(), patientProfileId,
            documentType, title, description,
            file.FileName, file.ContentType, file.Length,
            file.OpenReadStream(), linkedAppointmentId);

        var result = await mediator.Send(command, ct);
        if (!result.Success)
            return MapError(result.Error, result.Kind);

        return CreatedAtAction(nameof(GetDocuments),
            new { patientProfileId }, new { result.DocumentId });
    }

    [HttpDelete("documents/{documentId:guid}")]
    public async Task<IActionResult> DeleteDocument(Guid documentId, CancellationToken ct)
    {
        var command = new DeleteMedicalDocumentCommand(
            GetUserId(), GetUserRoleEnum(), documentId);

        var result = await mediator.Send(command, ct);
        if (!result.Success)
            return MapError(result.Error, result.Kind);

        return NoContent();
    }
}

// ── Request DTOs ─────────────────────────────────────────────────────────

public record AddConditionRequest(
    string IcdCode,
    string IcdDescription,
    DateOnly? DateOfOnset,
    ConditionSeverity Severity,
    ConditionStatus Status,
    string? ClinicalNotes,
    Guid? LinkedAppointmentId);

public record UpdateConditionRequest(
    ConditionSeverity Severity,
    ConditionStatus Status,
    string? ClinicalNotes);

public record AddAllergyRequest(
    string AllergenName,
    string? Reaction,
    ConditionSeverity Severity,
    DateOnly? DateIdentified);

public record UpdateAllergyRequest(
    string? Reaction,
    ConditionSeverity Severity,
    bool IsActive);

public record AddVitalRequest(
    VitalType VitalType,
    decimal Value,
    decimal? SecondaryValue,
    string Unit,
    DateTime MeasuredAt,
    VitalSource Source,
    string? Notes);

public record AddMedicationRequest(
    string MedicationName,
    string Dosage,
    string Frequency,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string? PrescribedByName,
    Guid? LinkedPrescriptionId);

public record UpdateMedicationRequest(
    string Dosage,
    string Frequency,
    DateOnly? EndDate,
    bool IsActive);

public record AddFamilyHistoryRequest(
    string Relationship,
    string ConditionName,
    int? AgeAtOnset,
    string? Notes);

public record UpdateFamilyHistoryRequest(
    string ConditionName,
    int? AgeAtOnset,
    string? Notes);
