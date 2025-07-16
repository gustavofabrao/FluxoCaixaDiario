using System;
using System.ComponentModel.DataAnnotations;

namespace FluxoCaixaDiario.Web.Models
{
    public class LancamentoInputModel
    {
        [Required]
        public int Tipo { get; set; }
        [Required]
        public decimal Valor { get; set; }
        public string Descricao { get; set; }
        [Required]
        public DateTime Data { get; set; }
    }
}