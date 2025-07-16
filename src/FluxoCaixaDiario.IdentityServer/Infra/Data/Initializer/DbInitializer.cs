using Duende.IdentityModel;
using FluxoCaixaDiario.IdentityServer.Domain.Entities;
using FluxoCaixaDiario.IdentityServer.Infra.Configuration;
using FluxoCaixaDiario.IdentityServer.Infra.Data.Context;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace FluxoCaixaDiario.IdentityServer.Infra.Data.Initializer
{
    public class DbInitializer : IDbInitializer
    {
        private readonly MySQLContext _context;
        private readonly UserManager<ApplicationUser> _user;
        private readonly RoleManager<IdentityRole> _role;

        public DbInitializer(MySQLContext context,
            UserManager<ApplicationUser> user,
            RoleManager<IdentityRole> role)
        {
            _context = context;
            _user = user;
            _role = role;
        }

        public void Initialize()
        {
            if (_role.FindByNameAsync(IdentityConfiguration.Admin).Result != null) return;
            _role.CreateAsync(new IdentityRole(
                IdentityConfiguration.Admin)).GetAwaiter().GetResult();
            _role.CreateAsync(new IdentityRole(
                IdentityConfiguration.Client)).GetAwaiter().GetResult();

            ApplicationUser admin = new ApplicationUser()
            {
                UserName = "opah-admin",
                Email = "opah-admin@opah.com.br",
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                TwoFactorEnabled = false,
                LockoutEnabled = false,
                AccessFailedCount = 0,
                PhoneNumber = "+55 (16) 99999-9999",
                FirstName = "Admin",
                LastName = "Opah"
            };

            _user.CreateAsync(admin, "Admin123!").GetAwaiter().GetResult(); // Necessita de senha segura (Maiusculas, minusculas e carac. Especiais)
            _user.AddToRoleAsync(admin,
                IdentityConfiguration.Admin).GetAwaiter().GetResult();
            var adminClaims = _user.AddClaimsAsync(admin, new Claim[]
            {
                new Claim(JwtClaimTypes.Name, $"{admin.FirstName} {admin.LastName}"),
                new Claim(JwtClaimTypes.GivenName, admin.FirstName),
                new Claim(JwtClaimTypes.FamilyName, admin.LastName),
                new Claim(JwtClaimTypes.Role, IdentityConfiguration.Admin)
            }).Result;

            ApplicationUser client = new ApplicationUser()
            {
                UserName = "opah-client",
                Email = "opah-client@opah.com.br",
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                TwoFactorEnabled = false,
                LockoutEnabled = false,
                AccessFailedCount = 0,
                PhoneNumber = "+55 (16) 99999-9999",
                FirstName = "Opah",
                LastName = "Client"
            };

            _user.CreateAsync(client, "Admin123!").GetAwaiter().GetResult(); // Necessita de senha segura (Maiusculas, minusculas e carac. Especiais)
            _user.AddToRoleAsync(client,
                IdentityConfiguration.Client).GetAwaiter().GetResult();
            var clientClaims = _user.AddClaimsAsync(client, new Claim[]
            {
                new Claim(JwtClaimTypes.Name, $"{client.FirstName} {client.LastName}"),
                new Claim(JwtClaimTypes.GivenName, client.FirstName),
                new Claim(JwtClaimTypes.FamilyName, client.LastName),
                new Claim(JwtClaimTypes.Role, IdentityConfiguration.Client)
            }).Result;
        }
    }
}
