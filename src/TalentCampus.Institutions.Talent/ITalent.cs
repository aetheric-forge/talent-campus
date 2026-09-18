using AethericForge.Runtime.Abstractions.Interfaces.Institutions;
using TalentCampus.Core.Roles;
using TalentCampus.Core.Personas;
using TalentCampus.Core.Recruitment;

namespace TalentCampus.Institutions.Talent;

public interface ITalent : IInstitution
{
    ITalentSteward Steward { get; }
    IPersonaDirectory Personas { get; }
    IRecruitmentOffice Recruitment { get; }
}
