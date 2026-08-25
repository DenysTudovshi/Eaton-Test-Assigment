using FluentValidation;

using ItemFinder.Api.Endpoints;
using ItemFinder.Api.ExceptionHandling;
using ItemFinder.Application.Behaviors;
using ItemFinder.Application.Interfaces;
using ItemFinder.Application.Options;
using ItemFinder.Application.Services;
using ItemFinder.Infrastructure.Parsing;
using ItemFinder.Infrastructure.Storage;

using MediatR;

using Microsoft.Extensions.Options;
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
{
    configuration.RegisterServicesFromAssemblyContaining<ItemDirectoryLoader>();
    configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
});
builder.Services.AddValidatorsFromAssemblyContaining<ItemDirectoryLoader>();

builder.Services.Configure<DataFileOptions>(builder.Configuration.GetSection(DataFileOptions.SectionName));
builder.Services.PostConfigure<DataFileOptions>(options =>
{
    // Relative paths resolve against the binaries so the bundled seed works in publish output.
    options.StoragePath = Path.GetFullPath(options.StoragePath, AppContext.BaseDirectory);
    if (options.SeedPath is { Length: > 0 } seedPath)
    {
        options.SeedPath = Path.GetFullPath(seedPath, AppContext.BaseDirectory);
    }
});
builder.Services.AddSingleton<IDataFileParser, DataFileParser>();
builder.Services.AddSingleton<IManagedDataFileStore>(provider => new ManagedDataFileStore(
    provider.GetRequiredService<IOptions<DataFileOptions>>().Value,
    provider.GetRequiredService<IDataFileParser>()));

var app = builder.Build();

app.UseExceptionHandler();

app.UseSwagger();
app.UseSwaggerUI();

app.MapItemEndpoints();

app.Run();

/// <summary>Entry-point marker so integration tests can host the app via WebApplicationFactory.</summary>
public partial class Program
{
}