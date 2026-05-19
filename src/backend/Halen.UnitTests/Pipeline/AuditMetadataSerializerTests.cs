using FluentAssertions;
using Halen.Application.Interfaces;
using Halen.Application.Pipeline;

namespace Halen.UnitTests.Pipeline;

[TestClass]
public class AuditMetadataSerializerTests
{
    private record SimpleCommand(string Name, int Value);

    private record CommandWithRedaction(
        string Name,
        [property: AuditRedact] string Password,
        int Value
    );

    private record NestedCommand(string Name, InnerData Inner);
    private record InnerData(string Field1, string Field2);

    private record NullableCommand(string? Name, int? Count);

    [TestMethod]
    public void Serialize_SimpleCommand_ReturnsCorrectJson()
    {
        var cmd = new SimpleCommand("test", 42);

        var result = AuditMetadataSerializer.Serialize(cmd);

        result.Should().Contain("\"Name\":\"test\"");
        result.Should().Contain("\"Value\":42");
    }

    [TestMethod]
    public void Serialize_RedactedProperty_ReplacesWithRedactedMarker()
    {
        var cmd = new CommandWithRedaction("admin", "secret123", 1);

        var result = AuditMetadataSerializer.Serialize(cmd);

        result.Should().Contain("\"Name\":\"admin\"");
        result.Should().Contain("\"Password\":\"[REDACTED]\"");
        result.Should().Contain("\"Value\":1");
        result.Should().NotContain("secret123");
    }

    [TestMethod]
    public void Serialize_MixedRedactedAndNormal_PreservesNonRedacted()
    {
        var cmd = new CommandWithRedaction("visible", "hidden", 99);

        var result = AuditMetadataSerializer.Serialize(cmd);

        result.Should().Contain("\"Name\":\"visible\"");
        result.Should().Contain("\"Value\":99");
        result.Should().NotContain("hidden");
    }

    [TestMethod]
    public void Serialize_LargePayload_ReturnsValidTruncationMarker()
    {
        var longString = new string('x', 8000);
        var cmd = new SimpleCommand(longString, 1);

        var result = AuditMetadataSerializer.Serialize(cmd);

        result.Should().Contain("_truncated");
        result.Should().Contain("SimpleCommand");
        var parsed = System.Text.Json.JsonDocument.Parse(result);
        parsed.RootElement.GetProperty("_truncated").GetBoolean().Should().BeTrue();
    }

    [TestMethod]
    public void Serialize_NonSerializableType_FallsBackToTypeName()
    {
        var cmd = new { Stream = new MemoryStream() };

        var result = AuditMetadataSerializer.Serialize(cmd);

        result.Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    public void Serialize_NestedObject_SerializesCorrectly()
    {
        var cmd = new NestedCommand("parent", new InnerData("a", "b"));

        var result = AuditMetadataSerializer.Serialize(cmd);

        result.Should().Contain("\"Name\":\"parent\"");
        result.Should().Contain("\"Field1\":\"a\"");
        result.Should().Contain("\"Field2\":\"b\"");
    }

    [TestMethod]
    public void Serialize_NullPropertyValues_HandlesGracefully()
    {
        var cmd = new NullableCommand(null, null);

        var result = AuditMetadataSerializer.Serialize(cmd);

        result.Should().Contain("\"Name\":null");
        result.Should().Contain("\"Count\":null");
    }
}
