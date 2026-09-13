using TalentCampus.Core.Roles;

namespace TalentCampus.Application.Roles;

public sealed class TalentSteward : ITalentSteward
{
    private readonly object _gate = new();
    private readonly List<RoleDefinition> _roles = [];

    public RoleDefinition DefineRole(
        string name,
        string purpose,
        IEnumerable<string> requiredCapabilities, string responsibilities = "", string successCriteria = "")
    {
        var role = new RoleDefinition(RoleId.New(), name, purpose, requiredCapabilities, responsibilities, successCriteria);

        lock (_gate)
        {
            _roles.Add(role);
        }

        return role;
    }

    public IReadOnlyCollection<RoleDefinition> ListRoles()
    {
        lock (_gate)
        {
            return _roles.ToArray();
        }
    }
}
