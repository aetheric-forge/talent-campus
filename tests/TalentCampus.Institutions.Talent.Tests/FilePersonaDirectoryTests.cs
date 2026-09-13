using TalentCampus.Application.Personas;
using TalentCampus.Application.Roles;

namespace TalentCampus.Institutions.Talent.Tests;

public sealed class FilePersonaDirectoryTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private string Store => Path.Combine(directory, "personas.json");

    [Fact]
    public void PersonasSurviveRestartIndependentlyOfRoles()
    {
        var personas = new FilePersonaDirectory(Store);
        var first = personas.Create(" Researcher ", " Find evidence ", " Ask questions ", " Analysis ", " Cite sources ");
        var second = personas.Create("Writer", "Explain", "Iterative", "Clarity", "No invented facts");
        var roles = new FileTalentSteward(Path.Combine(directory, "roles.json"));
        roles.DefineRole("Editor", "Review", ["Writing"], "Check claims", "Accurate text");

        var restored = new FilePersonaDirectory(Store).List().ToArray();
        Assert.Equal(2, restored.Length);
        Assert.Equal(first.Id, restored[0].Id);
        Assert.Equal(second.Id, restored[1].Id);
        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal("Researcher", restored[0].Name);
        Assert.Equal("Find evidence", restored[0].Purpose);
        Assert.Equal("Ask questions", restored[0].WorkingStyle);
        Assert.Equal("Analysis", restored[0].Strengths);
        Assert.Equal("Cite sources", restored[0].Boundaries);
        Assert.Single(roles.ListRoles());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void IncompletePersonaPreservesExistingStore(int missingField)
    {
        var personas = new FilePersonaDirectory(Store);
        personas.Create("Name", "Purpose", "Style", "Strengths", "Boundaries");
        var before = File.ReadAllText(Store);
        string[] fields = ["New", "Purpose", "Style", "Strengths", "Boundaries"];
        fields[missingField] = " ";
        Assert.ThrowsAny<ArgumentException>(() => personas.Create(fields[0], fields[1], fields[2], fields[3], fields[4]));
        Assert.Equal(before, File.ReadAllText(Store));
    }

    [Theory]
    [InlineData("invalid json")]
    [InlineData("null")]
    [InlineData("[null]")]
    [InlineData("[{}]")]
    public void InvalidStoreIsNeverOverwritten(string content)
    {
        System.IO.Directory.CreateDirectory(directory);
        File.WriteAllText(Store, content);
        var personas = new FilePersonaDirectory(Store);
        Assert.Throws<InvalidDataException>(() => personas.List());
        Assert.Throws<InvalidDataException>(() => personas.Create("Name", "Purpose", "Style", "Strengths", "Boundaries"));
        Assert.Equal(content, File.ReadAllText(Store));
    }

    public void Dispose()
    {
        if (System.IO.Directory.Exists(directory)) System.IO.Directory.Delete(directory, true);
    }
}
