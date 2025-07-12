using FluxoCaixaDiario.Lancamentos.Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace FluxoCaixaDiario.Lancamentos.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/transacoes")]
    public class TransactionsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TransactionsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> RegisterTransaction([FromBody] RegisterTransactionCommand command)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var transactionId = await _mediator.Send(command);

            return CreatedAtAction(nameof(RegisterTransaction), new { id = transactionId }, new { TransactionId = transactionId });
        }
    }
}