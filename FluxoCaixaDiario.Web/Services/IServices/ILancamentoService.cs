using System.Threading.Tasks;
using FluxoCaixaDiario.Web.Models;

public interface ILancamentoService
{
    Task<bool> RegistrarLancamentoAsync(LancamentoInputModel model, string token);
}