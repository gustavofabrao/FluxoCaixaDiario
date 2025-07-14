using FluxoCaixaDiario.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

[Authorize]
public class LancamentoController : Controller
{
    private readonly ILancamentoService _lancamentoService;

    public LancamentoController(ILancamentoService lancamentoService)
    {
        _lancamentoService = lancamentoService;
    }

    [HttpGet]
    public IActionResult Registrar()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Registrar(LancamentoInputModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var token = await HttpContext.GetTokenAsync("access_token");
        var sucesso = await _lancamentoService.RegistrarLancamentoAsync(model, token);

        ViewBag.Message = sucesso ? "Lançamento registrado com sucesso!" : "Erro ao registrar lançamento";
        return View();
    }
}