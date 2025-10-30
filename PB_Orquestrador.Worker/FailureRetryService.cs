using MassTransit;
using Microsoft.EntityFrameworkCore;
using PB_Orquestrador.Infrastructure.Data;
using System.Text.Json;

namespace PB_Orquestrador.Worker
{
    public class FailureRetryService : BackgroundService
    {
        private readonly IServiceProvider _sp;
        private readonly ILogger<FailureRetryService> _logger;

        public FailureRetryService(IServiceProvider sp, ILogger<FailureRetryService> logger)
        {
            _sp = sp; _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();
                    var publisher = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

                    var toRetry = await db.FailureRecords
                        .Where(f => f.Status == FailureStatus.Pending && f.NextRetryAtUtc <= DateTime.UtcNow)
                        .OrderBy(f => f.NextRetryAtUtc)
                        .Take(20)
                        .ToListAsync(stoppingToken);

                    foreach (var rec in toRetry)
                    {
                        rec.Status = FailureStatus.Retrying;
                        await db.SaveChangesAsync(stoppingToken);

                        try
                        {
                            // desserializar
                            var type = Type.GetType(rec.MessageType);
                            if (type == null) throw new InvalidOperationException("Tipo não encontrado: " + rec.MessageType);

                            var msg = JsonSerializer.Deserialize(rec.PayloadJson, type, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                            // republish
                            await publisher.Publish(msg!, type, stoppingToken);

                            rec.Status = FailureStatus.Resolved;
                            rec.LastAttemptAtUtc = DateTime.UtcNow;
                            await db.SaveChangesAsync(stoppingToken);
                            _logger.LogInformation("Republished failure {Id}", rec.Id);
                        }
                        catch (Exception ex)
                        {
                            rec.AttemptCount++;
                            rec.LastAttemptAtUtc = DateTime.UtcNow;
                            rec.LastError = ex.ToString();
                            // backoff exponencial simples: e.g. 2^attempt * 10s
                            var nextDelay = TimeSpan.FromSeconds(Math.Pow(2, rec.AttemptCount) * 10);
                            rec.NextRetryAtUtc = DateTime.UtcNow.Add(nextDelay);

                            if (rec.AttemptCount >= rec.MaxAttempts)
                            {
                                rec.Status = FailureStatus.Failed;
                                // opcional: publisher.Publish(new MovedToDlqEvent(...))
                            }
                            else
                            {
                                rec.Status = FailureStatus.Pending;
                            }
                            await db.SaveChangesAsync(stoppingToken);
                            _logger.LogWarning(ex, "Retry failed for {Id}, attempt {Attempt}", rec.Id, rec.AttemptCount);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "FailureRetryService general error");
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}
