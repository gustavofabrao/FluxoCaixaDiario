using System;

namespace FluxoCaixaDiario.SaldoDiario.Domain.Entities
{
    public class DailyBalance
    {
        public DateTime Date { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal Balance { get; set; }
    }
}