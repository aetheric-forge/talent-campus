# Talent institution ownership

This slice moves Talent service composition into `TalentCampus.Institutions.Talent`. The mounted `ITalent` owns the role steward, persona directory, and recruitment office. The website supplies the parent institution, storage location, and configured Decisions destinations.

## Boundary

- The Talent package owns its template, factory, deployment validation, and service lifetimes.
- The host constructs the parent Campus, mounts the factory result, and supplies routes and layout.
- Deployment chooses the role store path and destination configuration.

`ITalent.Steward`, `ITalent.Personas`, and `ITalent.Recruitment` expose operations through the mounted institution. Public DI aliases resolve those same instances, so existing pages do not create a second set of stores.

`TalentDeployment` normalizes paths, snapshots destination configuration, and rejects empty or duplicate destination IDs and colliding store paths. Existing JSON formats, paths beside the role store, single-process locking, and runtime revision are preserved.

## Host integration

```csharp
services.AddTalentInstitution(
    new TalentDeployment(rolesPath, destinations),
    sp => sp.GetRequiredService<ICampus>());

var factory = serviceProvider.GetRequiredService<TalentInstitutionFactory>();
campus.Register<ITalent>(factory.Create(campus, serviceProvider));
```

The package supports one mounted Talent institution per service provider. Duplicate registrations, missing mounts, and attempts to mount under another owner fail with actionable diagnostics.

This is an operational composition boundary, not an authentication boundary. Presentation migration and PostOffice exchange remain later slices.

## Verification

All 41 automated tests pass and the website builds successfully. A disposable Chromium walkthrough passed role validation, role and persona creation, human and AI consideration preparation, and persistence after app restart.

Storage and browser checks use temporary data; existing development records are not touched.
