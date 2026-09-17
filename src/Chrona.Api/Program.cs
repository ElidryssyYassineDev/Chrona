using Chrona.Api.Infrastructure;
using Chrona.Shared.Application;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Workforce.Application.Employees.GetEmployeeForPrincipal;
using Workforce.Domain;
using Workforce.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpa", policy =>

        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
    );
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
    } );
builder.Services.AddAuthorization();

builder.Services.AddDbContext<WorkforceDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Chrona")));


builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();
builder.Services.AddScoped<GetEmployeeForPrincipalHandler>();
var app = builder.Build();

// Configure the HTTP request pipeline.
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
    async (
        ICurrentUserContext currentUser, 
        GetEmployeeForPrincipalHandler handler) =>
    {
        var query = new GetEmployeeForPrincipalQuery(currentUser.SubjectId);

        var employee = await handler.HandleAsync(query);
        if (employee is null)
        {
            return Results.NotFound(new {message = "No employee record exists for this account"});
        }
        return Results.Ok(new
        {
            employee.Id,
            employee.FirstName,
            employee.LastName,
            employee.IsActive
        });
    }
        )
.RequireAuthorization();
app.MapGet("/health", () =>
{
    return "healthy";
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WorkforceDbContext>();
    var mySubjectId = Guid.Parse("0c2d071b-8303-4819-abd1-22ea9c763e89"
);
    if (!await db.Employees.AnyAsync(e => e.KeycloakSubjectId == mySubjectId))
    {
        var me = new Employee(mySubjectId, "yassine", "elidryssy", Guid.NewGuid());
        db.Employees.Add(me);
        await db.SaveChangesAsync();
    }
}


app.Run();

