using AethericForge.Runtime.Abstractions.Interfaces.Institutions;
using Microsoft.Extensions.DependencyInjection;
using TalentCampus.Core.Personas;
using TalentCampus.Core.Recruitment;
using TalentCampus.Core.Roles;

namespace TalentCampus.Institutions.Talent;

public static class TalentServices
{
    /// <summary>
    /// Registers one Talent package. The host mounts TalentInstitutionFactory on the supplied parent.
    /// Public service aliases resolve that mounted instance, so pages and institution callers share stores.
    /// </summary>
    public static IServiceCollection AddTalentInstitution(this IServiceCollection services,
        TalentDeployment deployment, Func<IServiceProvider, IInstitution> resolveParent)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(deployment);
        ArgumentNullException.ThrowIfNull(resolveParent);
        Type[] ownedContracts = [typeof(TalentDeployment), typeof(TalentInstitutionFactory), typeof(ITalent),
            typeof(ITalentSteward), typeof(IPersonaDirectory), typeof(IRecruitmentOffice)];
        if (services.Any(d => ownedContracts.Contains(d.ServiceType)))
            throw new InvalidOperationException("AddTalentInstitution owns Talent service registration and supports one institution per service provider. Remove individual Talent registrations before calling it.");

        services.AddSingleton(deployment);
        services.AddSingleton<TalentInstitutionFactory>();
        services.AddSingleton<ITalent>(sp =>
        {
            var parent = resolveParent(sp)
                ?? throw new InvalidOperationException("The Talent parent resolver returned no institution.");
            if (!parent.TryResolve<ITalent>(out var talent))
                throw new InvalidOperationException("Talent is not mounted. Register the TalentInstitutionFactory result on the parent before resolving Talent services.");
            if (!ReferenceEquals(talent, sp.GetRequiredService<TalentInstitutionFactory>().Create(parent, sp)))
                throw new InvalidOperationException("The mounted Talent must be created by this package's TalentInstitutionFactory.");
            return talent;
        });
        services.AddSingleton<ITalentSteward>(sp => sp.GetRequiredService<ITalent>().Steward);
        services.AddSingleton<IPersonaDirectory>(sp => sp.GetRequiredService<ITalent>().Personas);
        services.AddSingleton<IRecruitmentOffice>(sp => sp.GetRequiredService<ITalent>().Recruitment);
        return services;
    }
}
