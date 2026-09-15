using System.Text.Json;
using TalentCampus.Core.Roles;

namespace TalentCampus.Application.Roles;

/// <summary>Durable role storage for one local application process.</summary>
public sealed class FileTalentSteward(string path) : ITalentSteward
{
    private readonly object gate = new();

    public RoleDefinition DefineRole(string name, string purpose, IEnumerable<string> requiredCapabilities,
        string responsibilities = "", string successCriteria = "")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(responsibilities);
        ArgumentException.ThrowIfNullOrWhiteSpace(successCriteria);
        var role = new RoleDefinition(RoleId.New(), name, purpose, requiredCapabilities, responsibilities, successCriteria);
        lock (gate)
        {
            var roles = Read();
            roles.Add(role);
            var fullPath = Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            var temporary = fullPath + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(roles.Select(RoleRecord.From).ToArray()));
            File.Move(temporary, fullPath, overwrite: true);
        }
        return role;
    }

    public IReadOnlyCollection<RoleDefinition> ListRoles()
    {
        lock (gate) return Read().ToArray();
    }

    private List<RoleDefinition> Read() => !File.Exists(path) ? [] :
        (JsonSerializer.Deserialize<RoleRecord[]>(File.ReadAllText(path))
            ?? throw new InvalidDataException("The role store is empty or invalid."))
        .Select(r => new RoleDefinition(new RoleId(r.Id), r.Name, r.Purpose, r.Capabilities,
            r.Responsibilities, r.SuccessCriteria)).ToList();

    private sealed record RoleRecord(Guid Id, string Name, string Purpose, string[] Capabilities,
        string Responsibilities, string SuccessCriteria)
    {
        public static RoleRecord From(RoleDefinition role) => new(role.Id.Value, role.Name, role.Purpose,
            role.RequiredCapabilities.ToArray(), role.Responsibilities, role.SuccessCriteria);
    }
}
