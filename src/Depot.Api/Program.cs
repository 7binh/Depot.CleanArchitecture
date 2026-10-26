using Depot.Api.Endpoints;
using Depot.Api.Identity;
using Depot.Application;
using Depot.Application.Abstractions;
using Depot.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Missing connection string 'DefaultConnection'.");

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapLineOperatorEndpoints();
app.MapContainerTypeEndpoints();
app.MapCustomerEndpoints();
app.MapBlockEndpoints();
app.MapBayEndpoints();
app.MapYardRowEndpoints();

await app.RunAsync();
