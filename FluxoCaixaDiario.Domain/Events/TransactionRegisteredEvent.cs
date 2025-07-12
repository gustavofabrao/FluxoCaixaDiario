using FluxoCaixaDiario.Domain.Enums;

namespace FluxoCaixaDiario.Domain.Events
{
    public class TransactionRegisteredEvent
    {
        public Guid TransactionId { get; set; }
        public DateTime TransactionDate { get; set; }
        public decimal Amount { get; set; }
        public TransactionTypeEnum Type { get; set; }
    }
}
