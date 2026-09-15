using TalentCampus.Web.Components;
using TalentCampus.Core.Recruitment;
using TalentCampus.Web.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddTalentCampus(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "roles.json"),
    builder.Configuration.GetSection("Recruitment:DecisionsOffices").Get<DecisionsDestination[]>() ?? []);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
