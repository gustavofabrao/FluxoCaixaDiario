using FluentValidation;
using FluxoCaixaDiario.Domain.Repositories;
using FluxoCaixaDiario.Lancamentos.Application.Common;
using FluxoCaixaDiario.Lancamentos.Infra.Data.Context;
using FluxoCaixaDiario.Lancamentos.Infra.Data.Repositories;
using FluxoCaixaDiario.Lancamentos.Infra.MessageBroker;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MySQLContext>(options =>
    options.UseMySql(
        builder.Configuration["MySQLConnection:MySQLConnectionString"],
        ServerVersion.AutoDetect(builder.Configuration["MySQLConnection:MySQLConnectionString"]),
        b => b.MigrationsAssembly(typeof(MySQLContext).Assembly.FullName)));

builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();

builder.Services.AddSingleton<IConnectionFactory>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    return new ConnectionFactory
    {
        HostName = builder.Configuration["RabbitMQ:HostName"],
        UserName = builder.Configuration["RabbitMQ:UserName"],
        Password = builder.Configuration["RabbitMQ:Password"],
        Port = int.Parse(builder.Configuration["RabbitMQ:Port"])
    };
});
builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();
builder.Services.AddMediatR(cfg => cfg.AsScoped());

// Pipeline de validação
builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = builder.Configuration["Auth:Authority"];
        options.RequireHttpsMetadata = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ApiScope", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("scope", builder.Configuration["Auth:Audience"]!);
    });
});

builder.Services.AddControllers(options =>
{
    options.Filters.Add<CustomExceptionFilter>();
});
builder.Services.AddEndpointsApiExplorer();

// Configura o Swagger/OpenAPI
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = builder.Configuration["Auth:Name"], Version = "v1" });
    c.EnableAnnotations();
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = @"Enter 'Bearer' [space] and your token!",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In= ParameterLocation.Header
            },
            new List<string> ()
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Aplicando migrations aqui somente para ambiente de desenvolvimento, para produção, considerar usar um script de migração separado, por exemplo, via CI/CD
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<MySQLContext>();
            context.Database.Migrate();
            app.Logger.LogInformation("Migrations aplicadas com sucesso para FluxoCaixaDiario.Lancamentos");
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "Erro ao aplicar migrations para FluxoCaixaDiario.Lancamentos");
        }
    }

    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", builder.Configuration["Auth:Name"] + " v1"));
    var option = new RewriteOptions();
    option.AddRedirect("^$", "swagger");
    app.UseRewriter(option);
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.Run();
