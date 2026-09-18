using AethericForge.Runtime.Institutions.Abstractions.Builders;
using AethericForge.Runtime.Institutions.Abstractions.Models;
using AethericForge.Runtime.Institutions.Campus;
using TalentCampus.Core.Recruitment;
using TalentCampus.Institutions.Talent;

namespace TalentCampus.Web.Hosting;

public static class TalentCampusExtensions
{
    public static IServiceCollection AddTalentCampus(this IServiceCollection services, string rolesPath, IEnumerable<DecisionsDestination> destinations)
    {
        services.AddTalentInstitution(new TalentDeployment(rolesPath, destinations),
            sp => sp.GetRequiredService<ICampus>());
        services.AddSingleton<ICampus>(serviceProvider =>
        {
            var campusTemplate = InstitutionTemplateBuilder.Create()
                .WithDescriptor(
                    "TalentCampus",
                    new Version(0, 1, 0),
                    "A campus for defining roles and stewarding human and artificial talent.")
                .Build();
            var campus = new Campus(new CampusContext(campusTemplate, serviceProvider));

            var factory = serviceProvider.GetRequiredService<TalentInstitutionFactory>();
            campus.Register<ITalent>(factory.Create(campus, serviceProvider));

            return campus;
        });

        return services;
    }

    public static ITalent Talent(this ICampus campus) => campus.Resolve<ITalent>();
}
