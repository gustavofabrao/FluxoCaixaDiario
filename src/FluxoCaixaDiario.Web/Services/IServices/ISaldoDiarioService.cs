using System;
using System.Threading.Tasks;
using FluxoCaixaDiario.Web.Models;

namespace FluxoCaixaDiario.Web.Services.IServices
{
    public interface ISaldoDiarioService
    {
        Task<SaldoDiarioResult?> GetByDateAsync(DateTime date, string token);
    }
}