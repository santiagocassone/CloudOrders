using System.Text;
using CloudOrders.Api.Contracts;
using CloudOrders.Api.Middleware;
using CloudOrders.Application.Abstractions;
using CloudOrders.Application.Auth;
using CloudOrders.Application.Orders;
using CloudOrders.Domain;
using CloudOrders.Infrastructure;
using CloudOrders.Infrastructure.Auth;
using CloudOrders.Infrastructure.Persistence;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<CloudOrdersDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CloudOrdersDb")));

builder.Services.AddScoped<IQuerySource, CloudOrdersDbContext>();
builder.Services.AddScoped<IOrderRepository, SqlOrderRepository>();
builder.Services.AddScoped<PlaceOrderHandler>();
builder.Services.AddScoped<GetOrderByIdHandler>();
builder.Services.AddScoped<ITokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IUserRepository, SqlUserRepository>();
builder.Services.AddScoped<LoginHandler>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddValidatorsFromAssemblyContaining<CreateOrderRequestValidator>();
builder.Services.AddFluentValidationAutoValidation();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

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
app.MapControllers();

app.Run();
