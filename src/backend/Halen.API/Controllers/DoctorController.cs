using Halen.Application.Attributes;
using Halen.Application.Doctor.Commands;
using Halen.Application.Doctor.Queries;
using Halen.Application.Interfaces;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Halen.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = "DoctorOnly")]
public class DoctorController(IMediator mediator, IFileStorage fileStorage) : HalenControllerBase
{
    [HttpPost("kyc/documents")]
    [RequireFeature("kyc")]
    [RequestSizeLimit(35_000_000)]
    public async Task<IActionResult> SubmitKycDocuments(
        IFormFile licensePhoto,
        IFormFile medicalCertificate,
        IFormFile identityProof,
        CancellationToken ct)
    {
        var files = new (IFormFile File, KycDocumentType Type)[]
        {
            (licensePhoto, KycDocumentType.LicensePhoto),
            (medicalCertificate, KycDocumentType.MedicalCertificate),
            (identityProof, KycDocumentType.IdentityProof),
        };

        var savedPaths = new List<string>();
        var uploads = new List<KycDocumentUpload>();

        try
        {
            foreach (var (file, type) in files)
            {
                await using var stream = file.OpenReadStream();
                var path = await fileStorage.SaveAsync("kyc", file.FileName, stream, ct);
                savedPaths.Add(path);

                uploads.Add(new KycDocumentUpload(
                    type,
                    file.FileName,
                    file.ContentType,
                    file.Length,
                    path));
            }

            var command = new SubmitKycDocumentsCommand(GetUserId(), uploads);
            var result = await mediator.Send(command, ct);

            if (!result.Success)
            {
                await CleanupFiles(savedPaths, ct);
                return MapError(result.Error, result.Kind);
            }

            return Ok(new { message = "KYC documents submitted successfully" });
        }
        catch
        {
            await CleanupFiles(savedPaths, ct);
            throw;
        }
    }

    [HttpGet("kyc/status")]
    [RequireFeature("kyc")]
    public async Task<IActionResult> GetKycStatus(CancellationToken ct)
    {
        var result = await mediator.Send(new GetKycStatusQuery(GetUserId()), ct);
        return Ok(result);
    }

    [HttpGet("patients")]
    public async Task<IActionResult> GetMyPatients(CancellationToken ct)
    {
        var result = await mediator.Send(new GetDoctorPatientsQuery(GetUserId()), ct);
        return Ok(new { patients = result });
    }

    private async Task CleanupFiles(List<string> paths, CancellationToken ct)
    {
        foreach (var path in paths)
        {
            try { await fileStorage.DeleteAsync(path, ct); }
            catch { /* best-effort cleanup */ }
        }
    }

}
