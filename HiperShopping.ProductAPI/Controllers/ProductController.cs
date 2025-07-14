using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HiperShopping.ProductAPI.Controllers
{
    [Route("api/v1/saldo-diario")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        //private IProductRepository _repository;
        private readonly IMediator _mediator;

        public ProductController(IMediator mediator)
        {
            _mediator = mediator;
            //_repository = repository ?? throw new
            //    ArgumentNullException(nameof(repository));
        }

        //[HttpGet]
        //public async Task<ActionResult<IEnumerable<ProductVO>>> FindAll()
        //{
        //    var products = await _repository.FindAll();
        //    return Ok(products);
        //}

        //[HttpGet("{id}")]
        //[Authorize]
        //public async Task<ActionResult<ProductVO>> FindById(long id)
        //{
        //    var product = await _repository.FindById(id);
        //    if (product == null) return NotFound();
        //    return Ok(product);
        //}

        [HttpGet("{date}")]
        [Authorize]
        public async Task<IActionResult> GetDailyBalance(DateTime date)
        {
            //var result = await _mediator.Send(new GetDailyBalanceQuery(date));

            //if (result.Balance.HasValue)
            //{
            //    return Ok(result);
            //}

            return NotFound($"Não foi encontrado saldo diário  para a data: {date.ToShortDateString()}");
        }

        //[HttpPost]
        //[Authorize]
        //public async Task<ActionResult<ProductVO>> Create([FromBody] ProductVO vo)
        //{
        //    if (vo == null) return BadRequest();
        //    var product = await _repository.Create(vo);
        //    return Ok(product);
        //}

        //[HttpPut]
        //[Authorize]
        //public async Task<ActionResult<ProductVO>> Update([FromBody] ProductVO vo)
        //{
        //    if (vo == null) return BadRequest();
        //    var product = await _repository.Update(vo);
        //    return Ok(product);
        //}

        //[HttpDelete("{id}")]
        //[Authorize(Roles = Role.Admin)]
        //public async Task<ActionResult> Delete(long id)
        //{
        //    var status = await _repository.Delete(id);
        //    if (!status) return BadRequest();
        //    return Ok(status);
        //}
    }
}
