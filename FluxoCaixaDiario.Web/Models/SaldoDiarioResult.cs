using System;

namespace FluxoCaixaDiario.Web.Models
{
    public class SaldoDiarioResult
    {
        public DateTime Date { get; set; }
        public decimal? TotalCredit { get; set; }
        public decimal? TotalDebit { get; set; }
        public decimal? Balance { get; set; }
    }
}