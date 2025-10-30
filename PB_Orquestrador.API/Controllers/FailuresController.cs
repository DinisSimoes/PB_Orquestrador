using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PB_Orquestrador.Infrastructure.Data;
using System.Text.Json;

namespace PB_Orquestrador.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FailuresController : ControllerBase
    {
        private readonly OrchestratorDbContext _db;
        private readonly IPublishEndpoint _publisher;

        public FailuresController(OrchestratorDbContext db, IPublishEndpoint publisher)
        {
            _db = db; _publisher = publisher;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] int page = 1) =>
            Ok(await _db.FailureRecords.OrderByDescending(f => f.CreatedAtUtc).Take(100).ToListAsync());

        [HttpPost("{id}/retry")]
        public async Task<IActionResult> Retry(Guid id)
        {
            var rec = await _db.FailureRecords.FindAsync(id);
            if (rec == null) return NotFound();

            var type = Type.GetType(rec.MessageType);
            if (type == null) return BadRequest("Message type not found");

            var msg = JsonSerializer.Deserialize(rec.PayloadJson, type);
            if (msg == null) return BadRequest("Unable to deserialize");

            await _publisher.Publish(msg, type);
            rec.AttemptCount++;
            rec.NextRetryAtUtc = DateTime.UtcNow.AddMinutes(1);
            rec.LastAttemptAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Accepted();
        }
    }
}
