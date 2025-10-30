using MassTransit;
using PB_Common.Events;
using PB_Orquestrador.Infrastructure.Data;

namespace PB_Orquestrador.Worker.Events
{
    public class PropostaFalhaConsumer : IConsumer<PropostaFalhaEvent>
    {
        private readonly OrchestratorDbContext _db;
        private readonly ILogger<PropostaFalhaConsumer> _logger;

        public PropostaFalhaConsumer(OrchestratorDbContext db, ILogger<PropostaFalhaConsumer> logger)
        {
            _db = db; _logger = logger;
        }

        public async Task Consume(ConsumeContext<PropostaFalhaEvent> context)
        {
            var evt = context.Message;
            var rec = new FailureRecord
            {
                Id = evt.FailureId,
                MessageType = evt.MessageType,
                PayloadJson = evt.PayloadJson,
                AttemptCount = evt.Attempt,
                MaxAttempts = 5,
                NextRetryAtUtc = DateTime.UtcNow.AddSeconds(10), // start retry window fast
                Status = FailureStatus.Pending,
                LastError = evt.Reason,
                CreatedAtUtc = evt.OccurredUtc,
                OriginalClienteId = evt.ClienteId
            };

            _db.FailureRecords.Add(rec);
            await _db.SaveChangesAsync();
            _logger.LogInformation($"Registered failure {evt.FailureId} for {evt.MessageType}", rec.Id, rec.MessageType);
        }
    }
}
