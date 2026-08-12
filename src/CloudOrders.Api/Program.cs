using Azure.Identity;
using Azure.Messaging.ServiceBus;
using CloudOrders.Api.Contracts;
using CloudOrders.Api.Middleware;
using CloudOrders.Application.Abstractions;
using CloudOrders.Application.Auth;
using CloudOrders.Application.Orders;
using CloudOrders.Domain;
using CloudOrders.Infrastructure;
using CloudOrders.Infrastructure.Auth;
using CloudOrders.Infrastructure.Messaging.ServiceBus;
using CloudOrders.Infrastructure.Persistence;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddDbContext<CloudOrdersDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("CloudOrdersDb"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null)));

builder.Services.AddScoped<IQuerySource, CloudOrdersDbContext>();
builder.Services.AddScoped<IOrderRepository, SqlOrderRepository>();
builder.Services.AddScoped<PlaceOrderHandler>();
builder.Services.AddScoped<GetOrderByIdHandler>();
builder.Services.AddScoped<ITokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IUserRepository, SqlUserRepository>();
builder.Services.AddScoped<LoginHandler>();
builder.Services.AddScoped<IProcessedMessageRepository, SqlProcessedMessageRepository>();
builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<CloudOrdersDbContext>());
builder.Services.AddScoped<StockResultsHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddValidatorsFromAssemblyContaining<CreateOrderRequestValidator>();
builder.Services.AddFluentValidationAutoValidation();


// Configure JWT authentication
builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()!;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.Key))
        };
    });


// Configure Azure Service Bus
builder.Services.AddOptions<ServiceBusOptions>()
    .Bind(builder.Configuration.GetSection(ServiceBusOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.FullyQualifiedNamespace),
        "ServiceBus:FullyQualifiedNamespace is required.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.OrderPlacedQueue),
        "ServiceBus:OrderPlacedQueue is required.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.StockResultsQueue),
        "ServiceBus:StockResultsQueue is required.")
    .ValidateOnStart();

builder.Services.AddSingleton<ServiceBusClient>(sp =>
{
    var serviceBusOptions = sp.GetRequiredService<IOptions<ServiceBusOptions>>().Value;
    return new ServiceBusClient(serviceBusOptions.FullyQualifiedNamespace, builder.Environment.IsDevelopment() ? new AzureCliCredential() : new ManagedIdentityCredential(new ManagedIdentityCredentialOptions()));
});



builder.Services.AddSingleton<ServiceBusSender>(sp =>
{
    var serviceBusOptions = sp.GetRequiredService<IOptions<ServiceBusOptions>>().Value;
    var serviceBusClient = sp.GetRequiredService<ServiceBusClient>();
    return serviceBusClient.CreateSender(serviceBusOptions.OrderPlacedQueue);
});

builder.Services.AddSingleton<IOrderEventPublisher, AzureServiceBusOrderEventPublisher>();


// Add health checks for the SQL Server database
builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("CloudOrdersDb")!);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<CloudOrdersDbContext>();

    if (!dbContext.Users.Any())
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("Test123!");
        var user = User.Create("test@cloudorders.com", passwordHash);
        dbContext.Users.Add(user);
        dbContext.SaveChanges();
    }
}


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
