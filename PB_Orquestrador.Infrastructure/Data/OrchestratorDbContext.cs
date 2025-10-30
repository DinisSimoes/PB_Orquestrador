using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PB_Orquestrador.Infrastructure.Data
{
    public enum FailureStatus { Pending = 0, Retrying = 1, Failed = 2, Resolved = 3 }

    public class FailureRecord
    {
        public Guid Id { get; set; }
        public string MessageType { get; set; } = null!;
        public string PayloadJson { get; set; } = null!;
        public int AttemptCount { get; set; }
        public int MaxAttempts { get; set; } = 5;
        public DateTime NextRetryAtUtc { get; set; } = DateTime.UtcNow;
        public FailureStatus Status { get; set; } = FailureStatus.Pending;
        public string? LastError { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? LastAttemptAtUtc { get; set; }
        public Guid? OriginalClienteId { get; set; }
    }

    public class OrchestratorDbContext : DbContext
    {
        public OrchestratorDbContext(DbContextOptions<OrchestratorDbContext> options) : base(options) { }
        public DbSet<FailureRecord> FailureRecords { get; set; } = null!;
    }
}
