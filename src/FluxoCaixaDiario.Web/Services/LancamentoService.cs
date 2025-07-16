using FluxoCaixaDiario.Web.Models;
using FluxoCaixaDiario.Web.Utils;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

public class LancamentoService : ILancamentoService
{
    private readonly HttpClient _client;
    public const string BasePath = "api/v1/transacoes";

    public LancamentoService(HttpClient client)
    {
        _client = client;
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public async Task<bool> RegistrarLancamentoAsync(LancamentoInputModel model, string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.PostAsJsonAsync(BasePath, model);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await response.ReadContentAs<ValidationErrorViewModel>();
            throw new HttpRequestException(errorMessage.GetFormattedErrors());
        }

        return response.IsSuccessStatusCode;
    }
}