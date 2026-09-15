using System.Text.Json;
using TalentCampus.Application.Personas;
using TalentCampus.Application.Recruitment;
using TalentCampus.Application.Roles;
using TalentCampus.Core.Recruitment;

namespace TalentCampus.Institutions.Talent.Tests;

public sealed class RecruitmentTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private string Store => Path.Combine(directory, "recruitment.json");
    private FileTalentSteward Roles => new(Path.Combine(directory, "roles.json"));
    private FilePersonaDirectory Personas => new(Path.Combine(directory, "personas.json"));
    private FileRecruitmentOffice Office => new(Store, Roles, Personas, [new("local", "Local Decisions")]);

    [Theory]
    [InlineData(ParticipantKind.Human)]
    [InlineData(ParticipantKind.AiPersona)]
    public void SnapshotSurvivesSourceRemovalAndRestart(ParticipantKind kind)
    {
        var role = Roles.DefineRole("Editor", "Review", ["Writing"], "Check claims", "Accurate text");
        var participantId = kind == ParticipantKind.Human
            ? Office.RegisterHuman(" Taylor ", " Editing experience ").Id
            : Personas.Create("Researcher", "Find evidence", "Careful", "Analysis", "Cite sources").Id;
        var original = Office.Prepare(role.Id.Value, kind, participantId, " Work samples ", " Consider for review ", "local");
        File.WriteAllText(Path.Combine(directory, "roles.json"), "[]");
        if (kind == ParticipantKind.AiPersona) File.WriteAllText(Path.Combine(directory, "personas.json"), "[]");
        var restored = Assert.Single(Office.ListConsiderations());
        Assert.Equal(JsonSerializer.Serialize(original), JsonSerializer.Serialize(restored));
        Assert.Equal("Editor", restored.Role.Name);
        Assert.Equal("Check claims", restored.Role.Responsibilities);
        Assert.Equal("Accurate text", restored.Role.SuccessCriteria);
        Assert.Equal("Writing", Assert.Single(restored.Role.RequiredCapabilities));
        Assert.Equal("Work samples", restored.Evidence);
        Assert.Equal("Consider for review", restored.Recommendation);
        Assert.Equal("local", restored.Destination.Id);
        if (kind == ParticipantKind.Human)
        {
            Assert.Equal("Taylor", Assert.Single(Office.ListHumans()).Name);
            Assert.Equal("Editing experience", restored.Participant.Background);
        }
        else
        {
            Assert.Equal("Find evidence", restored.Participant.Purpose);
            Assert.Equal("Careful", restored.Participant.WorkingStyle);
            Assert.Equal("Analysis", restored.Participant.Strengths);
            Assert.Equal("Cite sources", restored.Participant.Boundaries);
        }
    }

    [Theory]
    [InlineData("destination")]
    [InlineData("missingDestination")]
    [InlineData("role")]
    [InlineData("participant")]
    [InlineData("kind")]
    [InlineData("evidence")]
    [InlineData("recommendation")]
    public void InvalidPreparationDoesNotChangeStore(string invalid)
    {
        var role = Roles.DefineRole("Editor", "Review", ["Writing"], "Check", "Accuracy");
        var human = Office.RegisterHuman("Taylor", "Experience");
        var before = File.ReadAllText(Store);
        Assert.ThrowsAny<ArgumentException>(() => Office.Prepare(
            invalid == "role" ? Guid.NewGuid() : role.Id.Value,
            invalid == "kind" ? (ParticipantKind)99 : ParticipantKind.Human,
            invalid == "participant" ? Guid.NewGuid() : human.Id,
            invalid == "evidence" ? " " : "Samples",
            invalid == "recommendation" ? " " : "Review",
            invalid == "destination" ? "unknown" : invalid == "missingDestination" ? "" : "local"));
        Assert.Equal(before, File.ReadAllText(Store));
    }

    [Theory]
    [InlineData("", "Experience")]
    [InlineData("Taylor", " ")]
    public void InvalidCandidateDoesNotChangeStore(string name, string background)
    {
        Office.RegisterHuman("Existing", "Experience");
        var before = File.ReadAllText(Store);
        Assert.ThrowsAny<ArgumentException>(() => Office.RegisterHuman(name, background));
        Assert.Equal(before, File.ReadAllText(Store));
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("{\"Humans\":[null],\"Considerations\":[]}")]
    [InlineData("{\"Humans\":[],\"Considerations\":[{}]}")]
    public void CorruptStoreIsNotOverwritten(string content)
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(Store, content);
        Assert.Throws<InvalidDataException>(() => Office.ListConsiderations());
        Assert.Throws<InvalidDataException>(() => Office.RegisterHuman("Taylor", "Experience"));
        Assert.Equal(content, File.ReadAllText(Store));
    }

    [Fact]
    public void NoConfiguredOfficePreventsPreparation()
    {
        var office = new FileRecruitmentOffice(Store, Roles, Personas, []);
        Assert.Empty(office.ListDestinations());
        Assert.Throws<ArgumentException>(() => office.Prepare(Guid.NewGuid(), ParticipantKind.Human,
            Guid.NewGuid(), "Evidence", "Review", "local"));
        Assert.False(File.Exists(Store));
    }

    public void Dispose() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
}
