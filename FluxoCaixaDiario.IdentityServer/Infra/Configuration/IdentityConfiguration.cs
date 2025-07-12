using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using System.Collections.Generic;

namespace FluxoCaixaDiario.IdentityServer.Infra.Configuration
{
    public static class IdentityConfiguration
    {
        public const string Admin = "Admin";
        public const string Client = "Client";

        /// <summary>
        /// Define as APIs (Recursos da API) que o Duende IdentityServer controlará o acesso
        /// </summary>
        public static IEnumerable<ApiScope> ApiScopes =>
            new List<ApiScope>
            {
                new ApiScope("fluxocaixadiario.web", "Fluxo Caixa Diário Web"),
                new ApiScope("fluxocaixadiario.lancamentos.api", "Fluxo Caixa Diário API de Lançamentos"),
                new ApiScope("fluxocaixadiario.saldodiario.api", "Fluxo Caixa Diário API de Saldo Diário")
            };

        /// <summary>
        /// Define os Recursos de Identidade. Representam informações sobre o usuário autenticado (claims)       
        /// </summary>
        public static IEnumerable<IdentityResource> IdentityResources =>
            new List<IdentityResource>
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile()
            };

        /// <summary>
        /// Define os Clients (aplicações) que podem se conectar ao Duende IdentityServer 
        /// </summary>
        public static IEnumerable<Client> Clients =>
            new List<Client>
            {
                new Client
                {
                    ClientId = "fluxocaixadiario.web",
                    //ClientSecrets = { new Secret("my_super_secret".Sha256())},
                    ClientName = "Fluxo de Caixa Diário - Web",
                    AllowedGrantTypes = GrantTypes.Code,
                    RequireClientSecret = false,
                    RedirectUris = {"https://localhost:4430/signin-oidc"},
                    PostLogoutRedirectUris = {"https://localhost:4430/signout-callback-oidc"},
                    AllowedScopes = new List<string>
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        "fluxocaixadiario.web",
                        "fluxocaixadiario.lancamentos.api",
                        "fluxocaixadiario.saldodiario.api"
                    },
                    RequirePkce = true,
                    AllowOfflineAccess = true, 
                    AccessTokenLifetime = 3600 // 1 hora de validade do token de acesso
                },
                new Client
                {
                    ClientId = "fluxocaixadiario.client",
                    //ClientSecrets = { new Secret("my_super_secret".Sha256())},
                    ClientName = "Fluxo de Caixa Diário - Client",
                    AllowedGrantTypes = GrantTypes.ClientCredentials,
                    AllowedScopes = {"read", "write", "profile",
                                    "fluxocaixadiario.web",
                                    "fluxocaixadiario.lancamentos.api",
                                    "fluxocaixadiario.saldodiario.api"}
                }
            };
    }
}
