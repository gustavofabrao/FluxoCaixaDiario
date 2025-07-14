using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Linq;

namespace FluxoCaixaDiario.Lancamentos.Application.Common
{
    public class CustomExceptionFilter : IExceptionFilter
    {
        /// <summary>
        /// Método responsável para tratar os erros em um formato amigável para a API (FluentValidation)
        /// </summary>
        /// <param name="context"></param>
        public void OnException(ExceptionContext context)
        {
            if (context.Exception is ValidationException validationException)
            {
                var errors = validationException.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()
                    );

                context.Result = new BadRequestObjectResult(new
                {
                    Title = "Um ou mais erros de validação ocorreram.",
                    Errors = errors
                });
                context.HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.ExceptionHandled = true;
            }
        }
    }
}