namespace TalentCampus.Core.Roles;

public interface ITalentSteward
{
    RoleDefinition DefineRole(string name, string purpose, IEnumerable<string> requiredCapabilities);

    IReadOnlyCollection<RoleDefinition> ListRoles();
}
