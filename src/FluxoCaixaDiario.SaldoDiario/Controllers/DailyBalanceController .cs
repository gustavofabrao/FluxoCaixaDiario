using FluxoCaixaDiario.SaldoDiario.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace FluxoCaixaDiario.SaldoDiario.Controllers
{
    [ApiController]
    [Route("api/v1/saldo-diario")]
    public class DailyBalanceController : ControllerBase
    {
        private readonly IMediator _mediator;

        public DailyBalanceController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("{date}")]
        [Authorize]
        public async Task<IActionResult> GetDailyBalance(DateTime date)
        {
            var result = await _mediator.Send(new GetDailyBalanceQuery(date));

            if (result.Balance.HasValue)
            {
                return Ok(result);
            }

            return NotFound($"Não foi encontrado saldo diário para a data: {date.ToShortDateString()}");
        }
    }
}