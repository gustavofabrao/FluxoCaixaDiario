using FluxoCaixaDiario.Web.Models;
using FluxoCaixaDiario.Web.Services.IServices;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Configuration;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace FluxoCaixaDiario.Web.Services
{
    public class SaldoDiarioService : ISaldoDiarioService
    {
        private readonly HttpClient _client;
        public const string BasePath = "api/v1/saldo-diario";

        public SaldoDiarioService(HttpClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public async Task<SaldoDiarioResult?> GetByDateAsync(DateTime date, string token)
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await _client.GetAsync($"{BasePath}/{date:yyyy-MM-dd}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<SaldoDiarioResult>();
            }
            else if (response.StatusCode.Equals(HttpStatusCode.NotFound))
            {
                return new SaldoDiarioResult() { DailyBalanceMessage = $"N�o foi encontrado saldo di�rio para a data: {date.ToShortDateString()}" };
            }
            else if (response.StatusCode.Equals(HttpStatusCode.Unauthorized))
            {
                throw new UnauthorizedAccessException("Acesso n�o autorizado. Verifique suas credenciais.");
            }

            return null;
        }
    }
}