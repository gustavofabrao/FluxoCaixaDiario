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

namespace FluxoCaixaDiario.Web.Pages.Transacoes
{
    public class RegisterModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<RegisterModel> _logger;

        public RegisterModel(IHttpClientFactory httpClientFactory, ILogger<RegisterModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [BindProperty]
        public TransactionInputModel Transaction { get; set; } = new TransactionInputModel();

        [TempData]
        public string Message { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var accessToken = await HttpContext.GetTokenAsync("access_token");
                if (string.IsNullOrEmpty(accessToken))
                {
                    _logger.LogWarning("Token de acesso não encontrado para registro de transação");
                    Message = "Erro: Token de acesso não encontrado. Tente logar novamente";
                    return RedirectToPage("/Login");
                }

                var client = _httpClientFactory.CreateClient("LancamentosApi");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await client.PostAsJsonAsync("/api/transacoes", Transaction);

                if (response.IsSuccessStatusCode)
                {
                    Message = "Transação registrada com sucesso!";
                    Transaction = new TransactionInputModel(); // Limpa o formulário
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Falha ao registrar a transação. Status: {StatusCode}, Content: {ErrorContent}", response.StatusCode, errorContent);
                    Message = $"Erro ao registrar transação: {response.StatusCode} - {errorContent}";
                }
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "Falha na requisição HTTP durante o registro da transação");
                Message = $"Erro de comunicação com o serviço: {httpEx.Message}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocorreu um erro inesperado durante o registro da transação");
                Message = $"Ocorreu um erro inesperado: {ex.Message}";
            }

            return Page();
        }
    }

    public class TransactionInputModel
    {
        [Required(ErrorMessage = "O valor é obrigatório.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor da transação deve ser positivo.")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "O tipo da transação é obrigatório.")]
        public string Type { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "A descrição não pode exceder 200 caracteres.")]
        public string Description { get; set; }
    }
}
