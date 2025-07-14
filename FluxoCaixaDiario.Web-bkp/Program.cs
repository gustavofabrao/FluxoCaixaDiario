using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("LancamentosApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:Lancamentos"]);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient("SaldoDiarioApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:SaldoDiario"]!);
    client.Timeout = TimeSpan.FromSeconds(30);
});

JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60); 
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true; 
    options.Cookie.IsEssential = true; 
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
})
.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.Authority = builder.Configuration["IdentityServer:Authority"];
    options.MetadataAddress = $"{builder.Configuration["IdentityServer:Authority"]}/.well-known/openid-configuration";
    options.ClientId = builder.Configuration["IdentityServer:ClientId"];
    options.ClientSecret = Convert.ToBase64String(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes("my_super_secret")));
    options.ResponseType = "code";
    options.ClaimActions.MapJsonKey("role", "role", "role");
    options.ClaimActions.MapJsonKey("sub", "sub", "sub");
    options.SaveTokens = true;
    options.GetClaimsFromUserInfoEndpoint = true;

    options.Scope.Clear();
    foreach (var scope in builder.Configuration["IdentityServer:Scope"]!.Split(' ', StringSplitOptions.RemoveEmptyEntries))
    {
        options.Scope.Add(scope);
    }
    
    options.TokenValidationParameters = new TokenValidationParameters
    {
        NameClaimType = "name",
        RoleClaimType = "role"
    };

    options.UsePkce = true;
    options.RequireHttpsMetadata = builder.Environment.IsProduction();
    options.CallbackPath = "/signin-oidc";
    options.SignedOutCallbackPath = "/signout-callback-oidc";

    //options.Events.OnRemoteFailure = context =>
    //{
    //    context.Response.Redirect("/Error");
    //    context.HandleResponse();
    //    return Task.CompletedTask;
    //};
});

builder.Services.AddRazorPages();

var app = builder.Build();

//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Error");
//    app.UseHsts();
//}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();