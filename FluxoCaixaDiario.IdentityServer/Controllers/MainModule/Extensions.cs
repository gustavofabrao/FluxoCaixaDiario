using Duende.IdentityServer.Models;
using FluxoCaixaDiario.IdentityServer.Controllers.MainModule.Account;
using Microsoft.AspNetCore.Mvc;
using System;

namespace FluxoCaixaDiario.IdentityServer.Controllers.MainModule
{
    public static class Extensions
    {
        /// <summary>
        /// Verifica se o URI de redirecionamento é para um cliente nativo
        /// </summary>
        /// <returns></returns>
        public static bool IsNativeClient(this AuthorizationRequest context)
        {
            return !context.RedirectUri.StartsWith("https", StringComparison.Ordinal)
               && !context.RedirectUri.StartsWith("http", StringComparison.Ordinal);
        }

        public static IActionResult LoadingPage(this Controller controller, string viewName, string redirectUri)
        {
            controller.HttpContext.Response.StatusCode = 200;
            controller.HttpContext.Response.Headers["Location"] = "";

            return controller.View(viewName, new RedirectViewModel { RedirectUrl = redirectUri });
        }
    }
}
