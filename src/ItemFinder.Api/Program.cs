using FluentValidation;

using ItemFinder.Api.ExceptionHandling;
using ItemFinder.Application.Services;

using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Item Finder API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        In = ParameterLocation.Header,
        Description = "Paste the accessToken returned by the identity login endpoint.",
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
            },
            Array.Empty<string>()
        },
    });
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddMediatR(configuration =>
    configuration.RegisterServicesFromAssemblyContaining<ItemDirectoryLoader>());
builder.Services.AddValidatorsFromAssemblyContaining<ItemDirectoryLoader>();

var app = builder.Build();

app.UseExceptionHandler();

app.UseSwagger();
app.UseSwaggerUI();

app.Run();

/// <summary>Entry-point marker so integration tests can host the app via WebApplicationFactory.</summary>
public partial class Program
{
}