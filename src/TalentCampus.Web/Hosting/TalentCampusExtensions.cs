using AethericForge.Runtime.Institutions.Abstractions.Builders;
using AethericForge.Runtime.Institutions.Abstractions.Models;
using AethericForge.Runtime.Institutions.Campus;
using TalentCampus.Application.Roles;
using TalentCampus.Application.Recruitment;
using TalentCampus.Core.Recruitment;
using TalentCampus.Application.Personas;
using TalentCampus.Core.Personas;
using TalentCampus.Core.Roles;
using TalentCampus.Institutions.Talent;

namespace TalentCampus.Web.Hosting;

public static class TalentCampusExtensions
{
    public static IServiceCollection AddTalentCampus(this IServiceCollection services, string rolesPath, IEnumerable<DecisionsDestination> destinations)
    {
        services.AddSingleton<ITalentSteward>(new FileTalentSteward(rolesPath));
        services.AddSingleton<IPersonaDirectory>(new FilePersonaDirectory(
            Path.Combine(Path.GetDirectoryName(Path.GetFullPath(rolesPath))!, "personas.json")));
        services.AddSingleton<IRecruitmentOffice>(provider => new FileRecruitmentOffice(
            Path.Combine(Path.GetDirectoryName(Path.GetFullPath(rolesPath))!, "recruitment.json"),
            provider.GetRequiredService<ITalentSteward>(), provider.GetRequiredService<IPersonaDirectory>(), destinations));
        services.AddSingleton<ICampus>(serviceProvider =>
        {
            var campusTemplate = InstitutionTemplateBuilder.Create()
                .WithDescriptor(
                    "TalentCampus",
                    new Version(0, 1, 0),
                    "A campus for defining roles and stewarding human and artificial talent.")
                .Build();
            var campus = new Campus(new CampusContext(campusTemplate, serviceProvider));

            var talentTemplate = InstitutionTemplateBuilder.Create()
                .WithDescriptor(
                    "Talent",
                    new Version(0, 1, 0),
                    "Defines roles and stewards evidence-based talent lifecycles.")
                .Build();
            var talent = new TalentCampus.Institutions.Talent.Talent(
                new TalentContext(talentTemplate, serviceProvider, campus),
                serviceProvider.GetRequiredService<ITalentSteward>());
            campus.Register<ITalent>(talent);

            return campus;
        });

        return services;
    }

    public static ITalent Talent(this ICampus campus) => campus.Resolve<ITalent>();
}
