using AethericForge.Runtime.Abstractions.Interfaces.Institutions;
using AethericForge.Runtime.Institutions.Abstractions.Builders;
using AethericForge.Runtime.Institutions.Abstractions.Primitives;
using TalentCampus.Application.Personas;
using TalentCampus.Application.Recruitment;
using TalentCampus.Application.Roles;

namespace TalentCampus.Institutions.Talent;

/// <summary>Owns composition for one mounted Talent institution, without constructing its parent.</summary>
public sealed class TalentInstitutionFactory
{
    private readonly TalentDeployment deployment;
    private readonly object gate = new();
    private ITalent? mounted;
    private IInstitution? parent;

    public TalentInstitutionFactory(TalentDeployment deployment)
    {
        this.deployment = deployment ?? throw new ArgumentNullException(nameof(deployment));
    }

    public IInstitutionTemplate Template { get; } = InstitutionTemplateBuilder.Create()
        .WithDescriptor("Talent", new Version(0, 1, 0), "Defines roles and stewards evidence-based talent lifecycles.")
        .Build();

    public ITalent Create(IInstitution owner, IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        lock (gate)
        {
            if (mounted is not null)
            {
                if (!ReferenceEquals(parent, owner))
                    throw new InvalidOperationException("A Talent factory supports one mounted institution. Use a separate deployment and service provider for another owner.");
                return mounted;
            }
            var steward = new FileTalentSteward(deployment.RolesPath);
            var personas = new FilePersonaDirectory(deployment.PersonasPath);
            var recruitment = new FileRecruitmentOffice(deployment.RecruitmentPath, steward, personas, deployment.Destinations);
            mounted = new Talent(new TalentContext(Template, serviceProvider, owner), steward, personas, recruitment);
            parent = owner;
            return mounted;
        }
    }
}
