using Duende.IdentityModel;
using Microsoft.AspNetCore.Authentication;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace FluxoCaixaDiario.IdentityServer.Controllers.MainModule.Diagnostics
{
    public class DiagnosticsViewModel
    {
        public DiagnosticsViewModel(AuthenticateResult result)
        {
            AuthenticateResult = result;

            if (result.Properties.Items.ContainsKey("client_list"))
            {
                var encoded = result.Properties.Items["client_list"];
                var bytes = Base64Url.Decode(encoded);
                var value = Encoding.UTF8.GetString(bytes);

                Clients = JsonSerializer.Deserialize<string[]>(value);
            }
        }

        public AuthenticateResult AuthenticateResult { get; }
        public IEnumerable<string> Clients { get; } = new List<string>();
    }
}