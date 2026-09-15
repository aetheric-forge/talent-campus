using System.Text.Json;
using TalentCampus.Core.Personas;
using TalentCampus.Core.Recruitment;
using TalentCampus.Core.Roles;

namespace TalentCampus.Application.Recruitment;

/// <summary>Single-process recruitment storage. Preparation does not dispatch or decide.</summary>
public sealed class FileRecruitmentOffice : IRecruitmentOffice
{
    private readonly string path;
    private readonly ITalentSteward roles;
    private readonly IPersonaDirectory personas;
    private readonly DecisionsDestination[] destinations;
    private readonly object gate = new();

    public FileRecruitmentOffice(string path, ITalentSteward roles, IPersonaDirectory personas,
        IEnumerable<DecisionsDestination> destinations)
    {
        this.path = Path.GetFullPath(path);
        this.roles = roles;
        this.personas = personas;
        this.destinations = destinations.ToArray();
        if (this.destinations.Any(d => d is null || string.IsNullOrWhiteSpace(d.Id) || string.IsNullOrWhiteSpace(d.Name)) ||
            this.destinations.Select(d => d.Id).Distinct(StringComparer.Ordinal).Count() != this.destinations.Length)
            throw new ArgumentException("Decisions offices require unique IDs and names.", nameof(destinations));
    }

    public HumanCandidate RegisterHuman(string name, string background)
    {
        var candidate = new HumanCandidate(Guid.NewGuid(), Required(name, "Candidate name"), Required(background, "Background"));
        lock (gate)
        {
            var state = Read();
            state.Humans.Add(candidate);
            Write(state);
        }
        return candidate;
    }

    public IReadOnlyCollection<HumanCandidate> ListHumans() { lock (gate) return Read().Humans.ToArray(); }
    public IReadOnlyCollection<Consideration> ListConsiderations() { lock (gate) return Read().Considerations.ToArray(); }
    public IReadOnlyCollection<DecisionsDestination> ListDestinations() => destinations.ToArray();

    public Consideration Prepare(Guid roleId, ParticipantKind kind, Guid participantId,
        string evidence, string recommendation, string destinationId)
    {
        evidence = Required(evidence, "Evidence");
        recommendation = Required(recommendation, "Recommendation");
        var destination = destinations.SingleOrDefault(d => d.Id == destinationId)
            ?? throw new ArgumentException("Select a configured Decisions office.");
        var role = roles.ListRoles().SingleOrDefault(r => r.Id.Value == roleId)
            ?? throw new ArgumentException("Select an existing role.");
        lock (gate)
        {
            var state = Read();
            ParticipantSnapshot participant;
            if (kind == ParticipantKind.Human)
            {
                var human = state.Humans.SingleOrDefault(h => h.Id == participantId)
                    ?? throw new ArgumentException("Select a registered human candidate.");
                participant = new(human.Id, kind, human.Name, human.Background, "", "", "", "");
            }
            else if (kind == ParticipantKind.AiPersona)
            {
                var persona = personas.List().SingleOrDefault(p => p.Id == participantId)
                    ?? throw new ArgumentException("Select an existing AI persona.");
                participant = new(persona.Id, kind, persona.Name, "", persona.Purpose,
                    persona.WorkingStyle, persona.Strengths, persona.Boundaries);
            }
            else throw new ArgumentException("Select a supported participant type.");

            var consideration = new Consideration(Guid.NewGuid(), DateTimeOffset.UtcNow,
                new(role.Id.Value, role.Name, role.Purpose, role.Responsibilities,
                    role.RequiredCapabilities.ToArray(), role.SuccessCriteria),
                participant, evidence, recommendation, destination);
            state.Considerations.Add(consideration);
            Write(state);
            return consideration;
        }
    }

    private Store Read()
    {
        if (!File.Exists(path)) return new([], []);
        try
        {
            var state = JsonSerializer.Deserialize<Store>(File.ReadAllText(path));
            if (state is null || state.Humans is null || state.Considerations is null ||
                state.Humans.Any(h => h is null || h.Id == Guid.Empty || Blank(h.Name) || Blank(h.Background)) ||
                state.Humans.Select(h => h.Id).Distinct().Count() != state.Humans.Count ||
                state.Considerations.Any(Invalid) ||
                state.Considerations.Select(c => c.Id).Distinct().Count() != state.Considerations.Count)
                throw new InvalidDataException("The recruitment store contains invalid records.");
            return state;
        }
        catch (JsonException ex) { throw new InvalidDataException("The recruitment store is invalid.", ex); }
    }

    private static bool Invalid(Consideration c) => c is null || c.Id == Guid.Empty || c.PreparedAt == default ||
        c.Role is null || c.Role.Id == Guid.Empty || Blank(c.Role.Name) || Blank(c.Role.Purpose) ||
        c.Role.RequiredCapabilities is null || c.Role.RequiredCapabilities.Length == 0 || c.Role.RequiredCapabilities.Any(Blank) ||
        c.Participant is null || c.Participant.Id == Guid.Empty || Blank(c.Participant.Name) ||
        !Enum.IsDefined(c.Participant.Kind) ||
        (c.Participant.Kind == ParticipantKind.Human ? Blank(c.Participant.Background) :
            Blank(c.Participant.Purpose) || Blank(c.Participant.WorkingStyle) || Blank(c.Participant.Strengths) || Blank(c.Participant.Boundaries)) ||
        Blank(c.Evidence) || Blank(c.Recommendation) || c.Destination is null || Blank(c.Destination.Id) || Blank(c.Destination.Name);

    private void Write(Store state)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(state));
        File.Move(temporary, path, overwrite: true);
    }

    private static bool Blank(string? value) => string.IsNullOrWhiteSpace(value);
    private static string Required(string value, string label) => !Blank(value) ? value.Trim() :
        throw new ArgumentException($"{label} is required.");
    private sealed record Store(List<HumanCandidate> Humans, List<Consideration> Considerations);
}
