# Talent Campus

Talent Campus is a standalone Aetheric Forge Campus for defining roles and stewarding the talent that may fill them. Talent may be human or artificial; the institution owns suitability and assignment lifecycles without owning identity, payroll, access control, or work execution.

## First checkpoint

The initial vertical slice establishes:

- a root Campus composed with one application institution, Talent;
- a stable `ITalent` institutional contract and context;
- a Talent Steward authority;
- a small role-definition model shared by future human and agent workflows; and
- executable tests proving role validation and institutional resolution.

The initial checkpoint deferred recruiting-system adapters, persistence, candidacy, evaluation, assignment, and persona development. The v0.1 roadmap below records subsequent implementation progress.

## Run

```sh
dotnet restore
dotnet run --project src/TalentCampus.Web
```

## Test

```sh
dotnet test
```

## v0.1 development

See [the roadmap and acceptance criteria](docs/planning/v0.1-roadmap.md).

Role Designer is available at `/roles`. The standalone web host stores roles in
`src/TalentCampus.Web/App_Data/roles.json` (relative to its content root).
The directory is excluded from git. Stop the application before copying this file
for backup or restoring it. Run only one application process against this store.
A malformed store causes an error rather than silently replacing existing data.

AI Persona Development is available at `/personas`. Create and review a named
persona with purpose, working style, strengths, and boundaries. Personas are
independent of roles and do not provision or execute agents. Records are saved in
`src/TalentCampus.Web/App_Data/personas.json`, with the same single-process and
backup rules as roles. In Docker, both files use the existing data volume.
Incomplete submissions retain the entered values; invalid stored data is never
silently overwritten.

Recruitment Office is available at `/recruitment`. Register human candidates or
select existing AI personas, choose a role and a configured Decisions office,
and prepare a consideration with evidence and a recommendation. Each saved
consideration preserves the role and talent details as a snapshot, even if
the source records later change or disappear.

Candidates and considerations are stored in `App_Data/recruitment.json` beside
the role and persona stores, using the same single-process and backup rules.
Configure available destinations in `Recruitment:DecisionsOffices` in
`appsettings.json`; each requires a unique ID and a name. The supplied Local
Decisions Office is a destination label for preparation. Considerations are
saved locally; PostOffice delivery and Decisions processing remain upcoming
milestones.

## Docker

From the repository root:

```sh
git submodule update --init --recursive
cp .env.example .env.local
docker compose --env-file .env.local up --build -d
```

Edit `.env.local` for local settings. This file is ignored by git. Compose
automatically reads only `.env`, so our commands explicitly select `.env.local`
with `--env-file`. Copy the example only during initial setup to preserve your settings.

Open http://localhost:8081. Set `TALENT_CAMPUS_PORT` to change the host port.
The default host binding is loopback; `TALENT_CAMPUS_BIND_ADDRESS` can change it
when deliberately exposing the service to the existing platform.

This Compose file starts only Talent Campus. It does not provision PostgreSQL,
MongoDB, RabbitMQ, Redis, Keycloak, or other platform services. The current role
workflow does not depend on them. Platform networks and provider configuration
can be attached when those integrations are implemented; no network name or
credentials are assumed here.

The image runs as the .NET image's non-root `app` user. Named volumes preserve
role records and ASP.NET data-protection keys across container replacement.
`docker compose --env-file .env.local down` retains these volumes; `docker compose --env-file .env.local down -v` deletes them.
Existing role records from a non-container run are not automatically imported.
Stop the service before backing up or restoring the data volume, and run one
instance against it. The app currently has no sign-in protection.

The container serves HTTP on port 8080. TLS termination and trusted proxy
configuration belong to the existing platform and must be configured for that
specific deployment before publishing it externally.

```sh
docker compose --env-file .env.local logs -f talent-campus
docker compose --env-file .env.local down
```

## Institution package ownership

Talent now owns composition of its role, persona, and recruitment services. See [the ownership boundary](docs/architecture/institution-ownership.md).
