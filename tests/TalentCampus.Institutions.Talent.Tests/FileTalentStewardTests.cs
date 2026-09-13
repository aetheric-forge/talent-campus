using TalentCampus.Application.Roles;

namespace TalentCampus.Institutions.Talent.Tests;

public sealed class FileTalentStewardTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private string Store => Path.Combine(directory, "roles.json");

    [Fact]
    public void RoleSurvivesReopeningStoreWithNormalizedCapabilities()
    {
        var original = new FileTalentSteward(Store).DefineRole(" Designer ", " Build roles ",
            ["Writing", " writing ", "Analysis"], "Interview stakeholders", "Approved role brief");
        var restored = Assert.Single(new FileTalentSteward(Store).ListRoles());
        Assert.Equal(original.Id, restored.Id);
        Assert.Equal("Designer", restored.Name);
        Assert.Equal(["Analysis", "Writing"], restored.RequiredCapabilities);
        Assert.Equal("Interview stakeholders", restored.Responsibilities);
        Assert.Equal("Approved role brief", restored.SuccessCriteria);
    }

    [Theory]
    [InlineData("", "Success")]
    [InlineData("Responsibilities", " ")]
    public void IncompleteRoleDoesNotChangeExistingStore(string responsibilities, string success)
    {
        var steward = new FileTalentSteward(Store);
        steward.DefineRole("Existing", "Purpose", ["Capability"], "Responsibilities", "Success");
        var before = File.ReadAllText(Store);
        Assert.ThrowsAny<ArgumentException>(() => steward.DefineRole("New", "Purpose", ["Capability"], responsibilities, success));
        Assert.Equal(before, File.ReadAllText(Store));
    }

    [Fact]
    public void CorruptStoreIsNotOverwritten()
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(Store, "invalid json");
        Assert.Throws<System.Text.Json.JsonException>(() => new FileTalentSteward(Store)
            .DefineRole("Role", "Purpose", ["Capability"], "Responsibilities", "Success"));
        Assert.Equal("invalid json", File.ReadAllText(Store));
    }

    public void Dispose() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
}
