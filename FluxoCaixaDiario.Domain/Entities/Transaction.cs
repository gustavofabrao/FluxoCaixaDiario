using FluxoCaixaDiario.Domain.Enums;

namespace FluxoCaixaDiario.Domain.Entities
{
    public class Transaction
    {
        public Guid Id { get; set; }
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public TransactionTypeEnum Type { get; set; }
        public string Description { get; set; }
    }
}
