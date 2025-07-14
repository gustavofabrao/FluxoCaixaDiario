using FluxoCaixaDiario.Web.Models;
using FluxoCaixaDiario.Web.Services.IServices;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace FluxoCaixaDiario.Web.Controllers
{
    public class SaldoDiarioController : Controller
    {
        private readonly ISaldoDiarioService _saldoDiarioService;

        public SaldoDiarioController(ISaldoDiarioService saldoDiarioService)
        {
            _saldoDiarioService = saldoDiarioService;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Index(DateTime? date)
        {
            SaldoDiarioResult? saldo = null;
            var token = await HttpContext.GetTokenAsync("access_token");

            DateTime consulta = date ?? DateTime.Today;

            if (!string.IsNullOrEmpty(token))
            {
                saldo = await _saldoDiarioService.GetByDateAsync(consulta, token);
            }

            ViewBag.Consulta = consulta;
            return View(saldo);
        }
    }
}