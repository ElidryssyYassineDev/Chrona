using Chrona.Api.Endpoints;
using Chrona.Api.Infrastructure;
using Chrona.Shared.Application;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Workforce;
using Workforce.Application.Employees.GetEmployeeForPrincipal;
using Workforce.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpa", policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "http://keycloak:8080/realms/chrona";
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = "http://localhost:8080/realms/chrona",
            ValidAudience = "React-SPA"
        };
        options.MetadataAddress = "http://keycloak:8080/realms/chrona/.well-known/openid-configuration";
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AuthorizationPolicies.Manager, policy =>
        policy.RequireAssertion(ctx => ctx.User.HasRealmRole("Manager")))
    .AddPolicy(AuthorizationPolicies.Employee, policy =>
        policy.RequireAssertion(ctx => ctx.User.HasRealmRole("Employee") || ctx.User.HasRealmRole("Manager")));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();

builder.Services.AddWorkforceModule(
    builder.Configuration.GetConnectionString("Chrona")
    ?? throw new InvalidOperationException("ConnectionStrings:Chrona is not configured."));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("AllowSpa");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet(
    "/api/v1/employees/me",
    async (ICurrentUserContext currentUser, GetEmployeeForPrincipalHandler handler) =>
    {
        var employee = await handler.HandleAsync(new GetEmployeeForPrincipalQuery(currentUser.SubjectId));
        if (employee is null)
        {
            return Results.NotFound(new { message = "No employee record exists for this account" });
        }

        return Results.Ok(new
        {
            employee.Id,
            employee.FirstName,
            employee.LastName,
            employee.IsActive
        });
    })
    .RequireAuthorization();

app.MapDepartmentEndpoints();

app.MapGet("/health", () => "healthy");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WorkforceDbContext>();
    await db.Database.MigrateAsync();

    // Amorçage du premier compte : uniquement si Seed:AdminSubjectId est configuré.
    var seed = app.Configuration.GetSection("Seed");
    if (Guid.TryParse(seed["AdminSubjectId"], out var adminSubjectId))
    {
        await WorkforceSeeder.SeedAsync(db, adminSubjectId, seed["FirstName"] ?? "Admin", seed["LastName"] ?? "User");
    }
}

app.Run();