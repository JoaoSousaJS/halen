namespace Halen.Domain.Constants;

public static class FeatureKeys
{
    public const string Prescriptions = "prescriptions";
    public const string Kyc = "kyc";
    public const string VideoCalls = "video_calls";

    public static readonly string[] All = [Prescriptions, Kyc, VideoCalls];
}
