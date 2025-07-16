using FluxoCaixaDiario.IdentityServer.Domain.Entities;
using FluxoCaixaDiario.IdentityServer.Infra.Configuration;
using FluxoCaixaDiario.IdentityServer.Infra.Data.Context;
using FluxoCaixaDiario.IdentityServer.Infra.Data.Initializer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MySQLContext>(options =>
    options.UseMySql(builder.Configuration["MySQlConnection:MySQlConnectionString"],
                     ServerVersion.AutoDetect(builder.Configuration["MySQLConnection:MySQLConnectionString"]),
                     b => b.MigrationsAssembly(typeof(MySQLContext).Assembly.FullName)));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<MySQLContext>()
    .AddDefaultTokenProviders();

builder.Services.AddIdentityServer(options =>
{
    options.Events.RaiseErrorEvents = true;
    options.Events.RaiseInformationEvents = true;
    options.Events.RaiseFailureEvents = true;
    options.Events.RaiseSuccessEvents = true;
    options.EmitStaticAudienceClaim = true;
})
    .AddInMemoryIdentityResources(IdentityConfiguration.IdentityResources) // Define os recursos de identidade (claims)
    .AddInMemoryApiScopes(IdentityConfiguration.ApiScopes)         // Define as APIs protegidas
    .AddInMemoryClients(IdentityConfiguration.Clients)             // Define os Clients (aplicações)   
    .AddAspNetIdentity<ApplicationUser>()                         // Integração com ASP.NET Core Identity para gerenciar usuários
    .AddDeveloperSigningCredential();                            // Somente para desenvolvimento, em produção deve usar certificado real

builder.Services.AddScoped<IDbInitializer, DbInitializer>();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// Aplicando migrations aqui somente para ambiente de desenvolvimento, para produção, considerar usar um script de migração separado, por exemplo, via CI/CD
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<MySQLContext>();
        var dbInitializer = services.GetRequiredService<IDbInitializer>();

        context.Database.Migrate();
        dbInitializer.Initialize();

        app.Logger.LogInformation("Migrations e seeds aplicados para o IdentityServer.");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Erro ao aplicar migrations ou seed para o IdentityServer.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseIdentityServer();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
});

app.Run();