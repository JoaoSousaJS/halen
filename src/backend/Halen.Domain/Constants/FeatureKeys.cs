namespace Halen.Domain.Constants;

public static class FeatureKeys
{
    public const string Prescriptions = "prescriptions";
    public const string Kyc = "kyc";
    public const string VideoCalls = "video_calls";
    public const string DoctorReviews = "doctor_reviews";
    public const string MedicalRecords = "medical_records";
    public const string Messaging = "messaging";
    public const string AuditTrail = "audit_trail";

    public static readonly string[] All = [Prescriptions, Kyc, VideoCalls, DoctorReviews, MedicalRecords, Messaging, AuditTrail];
}
