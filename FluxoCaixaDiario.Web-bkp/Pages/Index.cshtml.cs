using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace FluxoCaixaDiario.Web.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public async Task OnGetAsync()
        {
            //var accessToken = await HttpContext.GetTokenAsync("access_token"); //////////////
            //accessToken ??= string.Empty;
            //_logger.LogInformation("Página inicial acessada. Token de acesso: {AccessToken}", accessToken);
        }
    }
}