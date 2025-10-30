using MassTransit;
using Microsoft.EntityFrameworkCore;
using PB_Orquestrador.Infrastructure.Data;
using PB_Orquestrador.Worker;
using PB_Orquestrador.Worker.Events;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<OrchestratorDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("OrchestratorDb")));

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<PropostaFalhaConsumer>();
    x.AddConsumer<CartaoFalhaConsumer>();
    x.AddConsumer<ClienteFalhaConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", 5672, "/", h => {
            h.Username("rabbit");
            h.Password("rabbit");
        });

        cfg.ReceiveEndpoint("orchestrator-failures-queue", e =>
        {
            e.ConfigureConsumer<PropostaFalhaConsumer>(context);
            e.ConfigureConsumer<CartaoFalhaConsumer>(context);
            e.ConfigureConsumer<ClienteFalhaConsumer>(context);
        });
    });
});

builder.Services.AddHostedService<FailureRetryService>();

var host = builder.Build();
host.Run();
