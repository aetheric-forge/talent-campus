using System.Text.Json;
using TalentCampus.Core.Personas;

namespace TalentCampus.Application.Personas;

/// <summary>Durable persona storage for one local application process.</summary>
public sealed class FilePersonaDirectory(string path) : IPersonaDirectory
{
    private readonly object gate = new();

    public PersonaDefinition Create(string name, string purpose, string workingStyle, string strengths, string boundaries)
    {
        var persona = new PersonaDefinition(Guid.NewGuid(), name, purpose, workingStyle, strengths, boundaries);
        lock (gate)
        {
            var personas = Read();
            personas.Add(persona);
            var fullPath = Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            var temporary = fullPath + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(personas));
            File.Move(temporary, fullPath, overwrite: true);
        }
        return persona;
    }

    public IReadOnlyCollection<PersonaDefinition> List()
    {
        lock (gate) return Read().ToArray();
    }

    private List<PersonaDefinition> Read()
    {
        if (!File.Exists(path)) return [];
        try
        {
            var personas = JsonSerializer.Deserialize<List<PersonaDefinition>>(File.ReadAllText(path))
                ?? throw new InvalidDataException("The persona store is empty or invalid.");
            if (personas.Any(p => p is null) || personas.Select(p => p.Id).Distinct().Count() != personas.Count)
                throw new InvalidDataException("The persona store contains invalid or duplicate records.");
            return personas;
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException)
        {
            throw new InvalidDataException("The persona store contains invalid records.", ex);
        }
    }
}
