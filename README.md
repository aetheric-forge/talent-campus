# Talent Campus

Talent Campus is a standalone Aetheric Forge Campus for defining roles and stewarding the talent that may fill them. Talent may be human or artificial; the institution owns suitability and assignment lifecycles without owning identity, payroll, access control, or work execution.

## First checkpoint

The initial vertical slice establishes:

- a root Campus composed with one application institution, Talent;
- a stable `ITalent` institutional contract and context;
- a Talent Steward authority;
- a small role-definition model shared by future human and agent workflows; and
- executable tests proving role validation and institutional resolution.

Recruiting-system adapters, persistence, candidacy, evaluation, assignment, and persona development are intentionally deferred until their stories and boundaries are explicit.

## Run

```sh
dotnet restore
dotnet run --project src/TalentCampus.Web
```

## Test

```sh
dotnet test
```
