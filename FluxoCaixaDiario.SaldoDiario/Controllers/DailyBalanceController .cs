using FluxoCaixaDiario.SaldoDiario.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace FluxoCaixaDiario.SaldoDiario.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/saldo-diario")]
    public class DailyBalanceController : ControllerBase
    {
        private readonly IMediator _mediator;

        public DailyBalanceController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("{date}")]
        public async Task<IActionResult> GetDailyBalance(DateTime date)
        {
            var result = await _mediator.Send(new GetDailyBalanceQuery(date));

            if (result.Balance.HasValue)
            {
                return Ok(new { result.Date, Balance = result.Balance.Value });
            }

            return NotFound($"Não foi encontrado saldo diário  para a data: {date.ToShortDateString()}");
        }
    }
}