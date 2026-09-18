using AethericForge.Runtime.Abstractions.Interfaces.Institutions;
using AethericForge.Runtime.Institutions.Abstractions.Builders;
using AethericForge.Runtime.Models.Institutions;
using Microsoft.Extensions.DependencyInjection;
using TalentCampus.Core.Personas;
using TalentCampus.Core.Recruitment;
using TalentCampus.Core.Roles;

namespace TalentCampus.Institutions.Talent.Tests;

public sealed class TalentInstitutionTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "talent-boundary-" + Guid.NewGuid());
    private string RolesPath => Path.Combine(directory, "roles.json");

    [Fact]
    public void IndependentOwnerMountsAllOperationsAndAliasesShareInstances()
    {
        using var host = new Host(RolesPath);
        var talent = host.Services.GetRequiredService<ITalent>();
        Assert.Same(talent, host.Owner.Resolve<ITalent>());
        Assert.Same(host.Owner, talent.Context.Parent);
        Assert.Same(talent.Steward, host.Services.GetRequiredService<ITalentSteward>());
        Assert.Same(talent.Personas, host.Services.GetRequiredService<IPersonaDirectory>());
        Assert.Same(talent.Recruitment, host.Services.GetRequiredService<IRecruitmentOffice>());
        Assert.Same(talent, host.Services.GetRequiredService<TalentInstitutionFactory>().Create(host.Owner, host.Services));
    }

    [Theory]
    [InlineData(ParticipantKind.Human)]
    [InlineData(ParticipantKind.AiPersona)]
    public void MountedOperationsPrepareAndPersistConsiderationsAcrossHostRestart(ParticipantKind kind)
    {
        Guid roleId, participantId, considerationId;
        using (var host = new Host(RolesPath))
        {
            var talent = host.Services.GetRequiredService<ITalent>();
            var role = talent.Steward.DefineRole("Editor", "Review work", ["Writing"], "Check facts", "Accurate text");
            roleId = role.Id.Value;
            participantId = kind == ParticipantKind.Human
                ? talent.Recruitment.RegisterHuman("Taylor", "Editing experience").Id
                : talent.Personas.Create("Researcher", "Find evidence", "Careful", "Analysis", "Cite sources").Id;
            // Prepare through the legacy page alias using records created through the mounted institution.
            considerationId = host.Services.GetRequiredService<IRecruitmentOffice>().Prepare(roleId, kind,
                participantId, "Work samples", "Recommend review", "local").Id;
        }
        using var reopened = new Host(RolesPath);
        var restored = reopened.Services.GetRequiredService<ITalent>();
        Assert.Equal(roleId, Assert.Single(restored.Steward.ListRoles()).Id.Value);
        var consideration = Assert.Single(restored.Recruitment.ListConsiderations());
        Assert.Equal(considerationId, consideration.Id);
        Assert.Equal(participantId, consideration.Participant.Id);
        Assert.Equal("Editor", consideration.Role.Name);
        Assert.Equal("local", consideration.Destination.Id);
        if (kind == ParticipantKind.Human)
            Assert.Equal(participantId, Assert.Single(restored.Recruitment.ListHumans()).Id);
        else
            Assert.Equal(participantId, Assert.Single(restored.Personas.List()).Id);
    }

    [Fact]
    public void DeploymentSnapshotsDestinationConfigurationAndPreservesExistingPaths()
    {
        DecisionsDestination[] destinations = [new("original", "Original office")];
        var deployment = new TalentDeployment(RolesPath, destinations);
        destinations[0] = new("changed", "Changed office");
        var services = new ServiceCollection().BuildServiceProvider();
        using (services)
        {
            var owner = CreateOwner(services);
            var talent = new TalentInstitutionFactory(deployment).Create(owner, services);
            Assert.Equal("original", Assert.Single(talent.Recruitment.ListDestinations()).Id);
        }
        Assert.Equal(Path.GetFullPath(RolesPath), deployment.RolesPath);
        Assert.Equal(Path.Combine(directory, "personas.json"), deployment.PersonasPath);
        Assert.Equal(Path.Combine(directory, "recruitment.json"), deployment.RecruitmentPath);
    }

    [Fact]
    public void MissingMountReportsAnActionableDiagnostic()
    {
        using var host = new Host(RolesPath, mount: false);
        var error = Assert.Throws<InvalidOperationException>(() => host.Services.GetRequiredService<ITalentSteward>());
        Assert.Contains("Talent is not mounted", error.Message);
        Assert.Contains("TalentInstitutionFactory", error.Message);
    }

    [Fact]
    public void DuplicateRegistrationAndMountingAnotherOwnerAreRejected()
    {
        var services = new ServiceCollection();
        var deployment = new TalentDeployment(RolesPath, []);
        services.AddTalentInstitution(deployment, _ => throw new NotImplementedException());
        Assert.Throws<InvalidOperationException>(() => services.AddTalentInstitution(deployment, _ => throw new NotImplementedException()));
        using var host = new Host(RolesPath);
        _ = host.Owner;
        var factory = host.Services.GetRequiredService<TalentInstitutionFactory>();
        Assert.Contains("one mounted institution", Assert.Throws<InvalidOperationException>(() =>
            factory.Create(CreateOwner(host.Services), host.Services)).Message);
    }

    [Fact]
    public void UnknownDestinationStillRejectsPreparationWithoutSavingAConsideration()
    {
        using var host = new Host(RolesPath);
        var talent = host.Services.GetRequiredService<ITalent>();
        var role = talent.Steward.DefineRole("Editor", "Review", ["Writing"], "Check", "Accuracy");
        var human = talent.Recruitment.RegisterHuman("Taylor", "Experience");
        Assert.Throws<ArgumentException>(() => talent.Recruitment.Prepare(role.Id.Value,
            ParticipantKind.Human, human.Id, "Evidence", "Recommendation", "missing"));
        Assert.Empty(talent.Recruitment.ListConsiderations());
    }

    [Fact]
    public void InvalidDeploymentFailsBeforeRegistrationOrWritingFiles()
    {
        Assert.Throws<ArgumentException>(() => new TalentDeployment(" ", []));
        Assert.Throws<ArgumentException>(() => new TalentDeployment(RolesPath, [new("same", "One"), new("same", "Two")]));
        Assert.Throws<ArgumentException>(() => new TalentDeployment(RolesPath, [new("", "Blank ID")]));
        Assert.Throws<ArgumentException>(() => new TalentDeployment(Path.Combine(directory, "personas.json"), []));
        Assert.False(Directory.Exists(directory));
    }

    private static IInstitution CreateOwner(IServiceProvider services) => new Owner(new InstitutionContext(
        InstitutionTemplateBuilder.Create().WithDescriptor("Independent owner", new Version(1, 0), "Package test host").Build(), services));

    private sealed class Owner(IInstitutionContext context) : InstitutionBase(context);

    private sealed class Host : IDisposable
    {
        public ServiceProvider Services { get; }
        public IInstitution Owner => Services.GetRequiredService<IInstitution>();

        public Host(string rolesPath, bool mount = true)
        {
            var services = new ServiceCollection();
            services.AddTalentInstitution(new TalentDeployment(rolesPath, [new("local", "Local Decisions")]),
                sp => sp.GetRequiredService<IInstitution>());
            services.AddSingleton<IInstitution>(sp =>
            {
                var owner = CreateOwner(sp);
                if (mount) owner.Register<ITalent>(sp.GetRequiredService<TalentInstitutionFactory>().Create(owner, sp));
                return owner;
            });
            Services = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        }

        public void Dispose() => Services.Dispose();
    }

    public void Dispose()
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
    }
}
