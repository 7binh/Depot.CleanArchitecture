using System.Text.Json.Serialization;
using Depot.Api.Endpoints;
using Depot.Api.Extensions;
using Depot.Api.Identity;
using Depot.Application;
using Depot.Application.Abstractions;
using Depot.Infrastructure;
using Depot.Infrastructure.Identity;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Missing connection string 'DefaultConnection'.");

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString, builder.Configuration);
builder.Services.AddJwtAuthentication();
builder.Services.AddOpenApi();

// Enums travel as text ("A", "Damaged", "Truck"), the same way they are stored.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference().AllowAnonymous();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapLineOperatorEndpoints();
app.MapContainerTypeEndpoints();
app.MapCustomerEndpoints();
app.MapBlockEndpoints();
app.MapBayEndpoints();
app.MapYardRowEndpoints();
app.MapContainerEndpoints();
app.MapReleaseOrderEndpoints();
app.MapContainerStayEndpoints();

await app.Services.SeedAdminAsync();

await app.RunAsync();
