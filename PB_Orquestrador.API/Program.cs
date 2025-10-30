using MassTransit;
using Microsoft.EntityFrameworkCore;
using PB_Orquestrador.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<OrchestratorDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("OrchestratorDb")));

builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", 5672, "/", h =>
        {
            h.Username("rabbit");
            h.Password("rabbit");
        });
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.Run();
