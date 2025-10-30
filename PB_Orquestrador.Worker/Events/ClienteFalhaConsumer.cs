using MassTransit;
using PB_Common.Events;
using PB_Orquestrador.Infrastructure.Data;

namespace PB_Orquestrador.Worker.Events
{
    public class ClienteFalhaConsumer : IConsumer<ClienteFalhaEvent>
    {
        private readonly OrchestratorDbContext _db;
        private readonly ILogger<ClienteFalhaConsumer> _logger;

        public ClienteFalhaConsumer(OrchestratorDbContext db, ILogger<ClienteFalhaConsumer> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<ClienteFalhaEvent> context)
        {
            var evt = context.Message;

            var rec = new FailureRecord
            {
                Id = evt.FailureId,
                MessageType = evt.MessageType,
                PayloadJson = evt.PayloadJson,
                AttemptCount = evt.Attempt,
                MaxAttempts = 5,
                NextRetryAtUtc = DateTime.UtcNow.AddSeconds(10), // Retry rápido inicial
                Status = FailureStatus.Pending,
                LastError = evt.Reason,
                CreatedAtUtc = evt.OccurredUtc,
                OriginalClienteId = evt.ClienteId
            };

            _db.FailureRecords.Add(rec);
            await _db.SaveChangesAsync();

            _logger.LogInformation($"Registrada falha {evt.FailureId} para {evt.MessageType} (ClienteId: {evt.ClienteId})",
                rec.Id, rec.MessageType, rec.OriginalClienteId);
        
        }
    }
}
