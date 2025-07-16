using FluxoCaixaDiario.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

[Authorize]
public class LancamentoController : Controller
{
    private readonly ILancamentoService _lancamentoService;
    private readonly ILogger<LancamentoController> _logger;

    public LancamentoController(ILancamentoService lancamentoService, ILogger<LancamentoController> logger) 
    {
        _lancamentoService = lancamentoService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Registrar()
    {
        ViewBag.DataRegistro = DateTime.Today;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Registrar(LancamentoInputModel model)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            var token = await HttpContext.GetTokenAsync("access_token");
            var sucesso = await _lancamentoService.RegistrarLancamentoAsync(model, token);
            ViewBag.Message = sucesso ? "Lançamento registrado com sucesso!" : "Erro ao registrar lançamento";
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            ViewBag.Message = (e.Message);
        }        
        
        ModelState.Clear();
        return View();
    }
}