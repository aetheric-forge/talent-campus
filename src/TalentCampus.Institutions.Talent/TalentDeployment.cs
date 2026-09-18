using TalentCampus.Core.Recruitment;

namespace TalentCampus.Institutions.Talent;

/// <summary>Deployment choices for the current single-process, file-backed Talent package.</summary>
public sealed class TalentDeployment
{
    public TalentDeployment(string rolesPath, IEnumerable<DecisionsDestination> destinations)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rolesPath);
        ArgumentNullException.ThrowIfNull(destinations);
        RolesPath = Path.GetFullPath(rolesPath);
        var directory = Path.GetDirectoryName(RolesPath)!;
        PersonasPath = Path.Combine(directory, "personas.json");
        RecruitmentPath = Path.Combine(directory, "recruitment.json");
        if (RolesPath == PersonasPath || RolesPath == RecruitmentPath)
            throw new ArgumentException("The role store must have a different path from the persona and recruitment stores.", nameof(rolesPath));
        var snapshot = destinations.ToArray();
        if (snapshot.Any(d => d is null || string.IsNullOrWhiteSpace(d.Id) || string.IsNullOrWhiteSpace(d.Name)) ||
            snapshot.Select(d => d.Id).Distinct(StringComparer.Ordinal).Count() != snapshot.Length)
            throw new ArgumentException("Decisions destinations require non-empty names and unique, non-empty IDs.", nameof(destinations));
        Destinations = Array.AsReadOnly(snapshot);
    }

    public string RolesPath { get; }
    public string PersonasPath { get; }
    public string RecruitmentPath { get; }
    public IReadOnlyList<DecisionsDestination> Destinations { get; }
}
