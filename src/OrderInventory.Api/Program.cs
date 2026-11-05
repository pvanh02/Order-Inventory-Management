using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using OrderInventory.Api.Errors;
using OrderInventory.Api.Hosting;
using OrderInventory.Api.OpenApi;
using OrderInventory.Api.Security;
using OrderInventory.Domain.Identity;
using OrderInventory.Infrastructure;
using OrderInventory.Infrastructure.Configuration;
using OrderInventory.Infrastructure.Persistence;
using Serilog;

if (args.Contains("--healthcheck", StringComparer.Ordinal))
    return await HealthProbe.RunAsync();

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();
try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog((context, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration).Enrich.FromLogContext().WriteTo.Console());
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();
    builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState.Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(x => x.Key, _ => new[] { "The supplied value is invalid or required." });
            var result = new ObjectResult(ProblemResponses.Create(context.HttpContext, 400, "validation",
                "One or more validation errors occurred.", errors)) { StatusCode = 400 };
            result.ContentTypes.Add("application/problem+json");
            return result;
        };
    });
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
    builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
        .Configure<IOptions<JwtOptions>>((authentication, configured) =>
        {
            var jwt = configured.Value;
            authentication.MapInboundClaims = false;
            authentication.IncludeErrorDetails = false;
            authentication.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true, ValidIssuer = jwt.Issuer,
                ValidateAudience = true, ValidAudience = jwt.Audience,
                ValidateLifetime = true, RequireExpirationTime = true,
                RequireSignedTokens = true, ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(jwt.SigningKeyBase64)),
                ValidAlgorithms = [SecurityAlgorithms.HmacSha256], ClockSkew = TimeSpan.Zero,
                NameClaimType = "sub", RoleClaimType = "role"
            };
            authentication.Events = new JwtBearerEvents { OnTokenValidated = CurrentAccountTokenValidator.ValidateAsync };
        });
    builder.Services.AddAuthorization(options => options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser().Build());
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Order & Inventory API", Version = "v1",
            Description = "Quantities are integers. Product prices are positive whole VND; fractional prices are rejected. SKUs are normalized and immutable. Adjustments require a nonzero signed delta and reason. Writes are not automatically retried."
        });
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT",
            Description = "Use the access token returned by the login endpoint."
        });
        options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
        options.OperationFilter<AnonymousOperationFilter>();
        options.SchemaFilter<CatalogSchemaFilter>();
    });

    var app = builder.Build();
    _ = app.Services.GetRequiredService<IOptions<JwtOptions>>().Value;
    app.UseExceptionHandler();
    app.UseStatusCodePages(async context =>
    {
        var status = context.HttpContext.Response.StatusCode;
        var code = status switch { 401 => "unauthorized", 403 => "forbidden", 404 => "not_found", _ => "request_failed" };
        await ProblemResponses.WriteAsync(context.HttpContext,
            ProblemResponses.Create(context.HttpContext, status, code), context.HttpContext.RequestAborted);
    });
    app.UseSerilogRequestLogging();
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    await using (var scope = app.Services.CreateAsyncScope())
    {
        var database = scope.ServiceProvider.GetRequiredService<OrderInventoryDbContext>();
        if ((await database.Database.GetPendingMigrationsAsync()).Any() ||
            !await database.UserAccounts.AnyAsync(user => user.IsActive && user.Role == UserRole.Admin))
            throw new InvalidOperationException("Run the database migrator before starting the API.");
    }
    await app.RunAsync();
    return 0;
}
catch (Exception error)
{
    Log.Fatal("Application startup failed ({FailureType}). Configuration values were not logged.", error.GetType().Name);
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

public partial class Program { }
