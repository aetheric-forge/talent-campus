using AethericForge.Runtime.Institutions.Abstractions.Builders;
using AethericForge.Runtime.Institutions.Campus;
using TalentCampus.Application.Roles;
using TalentCampus.Core.Roles;

namespace TalentCampus.Institutions.Talent.Tests;

public sealed class TalentInstitutionTests
{
    [Fact]
    public void CampusResolvesTalentInstitutionAndItsSteward()
    {
        var services = new EmptyServiceProvider();
        var campusTemplate = InstitutionTemplateBuilder.Create()
            .WithDescriptor("TestCampus", new Version(1, 0), "Test campus")
            .Build();
        var campus = new Campus(new CampusContext(campusTemplate, services));
        var talentTemplate = InstitutionTemplateBuilder.Create()
            .WithDescriptor("Talent", new Version(0, 1), "Talent institution")
            .Build();
        var steward = new TalentSteward();
        ITalent talent = new Talent(
            new TalentContext(talentTemplate, services, campus),
            steward);

        campus.Register(talent);

        Assert.Same(talent, campus.Resolve<ITalent>());
        Assert.Same(steward, talent.Steward);
        Assert.Same(campus, talent.Context.Parent);
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
