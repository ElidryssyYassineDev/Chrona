using System.ComponentModel;
using Chrona.Api.Infrastructure;
using Chrona.Shared.Application;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Workforce.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "http://localhost:8080/realms/chrona";
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidAudience = "React-SPA"
        };
    } );
builder.Services.AddAuthorization();

builder.Services.AddDbContext<WorkforceDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Chrona")));


builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/v1/employees/me", (ICurrentUserContext currentUserContext) =>
    new 
    {
        currentUserContext.SubjectId, 
        currentUserContext.Roles})
.RequireAuthorization();
app.MapGet("/health", () =>
{
    return "healthy";
});


app.Run();

