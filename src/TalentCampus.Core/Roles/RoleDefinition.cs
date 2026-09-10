namespace TalentCampus.Core.Roles;

public sealed class RoleDefinition
{
    public RoleDefinition(
        RoleId id,
        string name,
        string purpose,
        IEnumerable<string> requiredCapabilities)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = Required(name, nameof(name));
        Purpose = Required(purpose, nameof(purpose));
        ArgumentNullException.ThrowIfNull(requiredCapabilities);

        RequiredCapabilities = requiredCapabilities
            .Select(capability => Required(capability, nameof(requiredCapabilities)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (RequiredCapabilities.Count == 0)
        {
            throw new ArgumentException(
                "A role must require at least one capability.",
                nameof(requiredCapabilities));
        }
    }

    public RoleId Id { get; }
    public string Name { get; }
    public string Purpose { get; }
    public IReadOnlyList<string> RequiredCapabilities { get; }

    private static string Required(string value, string parameterName) =>
        !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : throw new ArgumentException("A value is required.", parameterName);
}
