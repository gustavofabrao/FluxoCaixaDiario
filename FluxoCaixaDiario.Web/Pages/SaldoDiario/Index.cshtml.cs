using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace FluxoCaixaDiario.Web.Pages.SaldoDiario
{
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(IHttpClientFactory httpClientFactory, ILogger<IndexModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        [DataType(DataType.Date)]
        [Display(Name = "Data da Consulta")]
        public DateTime QueryDate { get; set; } = DateTime.Today;

        public DailyBalanceResult DailyBalance { get; set; }

        [TempData]
        public string Message { get; set; }

        public async Task OnGetAsync()
        {
            await GetDailyBalance();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await GetDailyBalance();
            return Page();
        }

        private async Task GetDailyBalance()
        {
            try
            {
                var accessToken = await HttpContext.GetTokenAsync("access_token");
                if (string.IsNullOrEmpty(accessToken))
                {
                    _logger.LogWarning("Token de acesso não encontrado para consulta de saldo diário");
                    Message = "Erro: Token de acesso não encontrado. Tente logar novamente!";
                    return;
                }

                var client = _httpClientFactory.CreateClient("SaldoDiarioApi");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var formattedDate = QueryDate.ToString("yyyy-MM-dd");
                var response = await client.GetAsync($"/api/saldo-diario/{formattedDate}");

                if (response.IsSuccessStatusCode)
                {
                    DailyBalance = await response.Content.ReadFromJsonAsync<DailyBalanceResult>();
                    if (DailyBalance == null || DailyBalance.Balance == null)
                    {
                        Message = $"Nenhum saldo consolidado encontrado para {formattedDate}.";
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    DailyBalance = null;
                    Message = $"Nenhum saldo consolidado encontrado para {formattedDate}.";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Falha ao obter o saldo diário. Status: {StatusCode}, Content: {ErrorContent}", response.StatusCode, errorContent);
                    Message = $"Erro ao consultar saldo: {response.StatusCode} - {errorContent}";
                }
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "A request HTTP falhou durante a consulta de saldo diário");
                Message = $"Erro de comunicação com o serviço: {httpEx.Message}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocorreu um erro inesperado durante a consulta de saldo diário");
                Message = $"Ocorreu um erro inesperado: {ex.Message}";
            }
        }
    }

    public class DailyBalanceResult
    {
        public DateTime Date { get; set; }
        public decimal? Balance { get; set; }
    }
}
