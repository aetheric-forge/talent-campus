using AethericForge.Runtime.Models.Institutions;
using TalentCampus.Core.Roles;
using TalentCampus.Core.Personas;
using TalentCampus.Core.Recruitment;

namespace TalentCampus.Institutions.Talent;

public sealed class Talent(ITalentContext context, ITalentSteward steward,
    IPersonaDirectory personas, IRecruitmentOffice recruitment)
    : InstitutionBase(context), ITalent
{
    public new ITalentContext Context => (ITalentContext)base.Context;

    public ITalentSteward Steward { get; } =
        steward ?? throw new ArgumentNullException(nameof(steward));

    public IPersonaDirectory Personas { get; } = personas ?? throw new ArgumentNullException(nameof(personas));
    public IRecruitmentOffice Recruitment { get; } = recruitment ?? throw new ArgumentNullException(nameof(recruitment));
}
