using System;
using AuctionService.Consumers;
using AuctionService.Data;
using AuctionService.RequestHelpers;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using AuctionService.Services;
using Polly;
using Npgsql;

namespace AuctionService;

public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers();
        builder.Services.AddDbContext<AuctionDbContext>(options =>
        {
            options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
        });
        builder.Services.AddAutoMapper(typeof(MappingProfiles).Assembly);

        builder.Services.AddMassTransit(x =>
        {
            x.AddEntityFrameworkOutbox<AuctionDbContext>(o =>
            {
                o.QueryDelay = TimeSpan.FromSeconds(10);

                o.UsePostgres();
                o.UseBusOutbox();
            });

            x.AddConsumersFromNamespaceContaining<AuctionCreatedFaultConsumer>();

            x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("auction", false));

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.UseMessageRetry(r =>
                {
                    r.Handle<RabbitMqConnectionException>();
                    r.Interval(5, TimeSpan.FromSeconds(10));
                });

                // Specify the RabbitMQ host, username, and password
                cfg.Host(builder.Configuration["RabbitMq:Host"], "/", host =>
                {
                    host.Username(builder.Configuration.GetValue("RabbitMq:Username", "guest"));
                    host.Password(builder.Configuration.GetValue("RabbitMq:Password", "guest"));
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = builder.Configuration["IdentityServiceUrl"];
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters.ValidateAudience = false;
                options.TokenValidationParameters.NameClaimType = "username";
            });

        builder.Services.AddScoped<IAuctionRepository, AuctionRepository>();

        builder.Services.AddGrpc();

        var app = builder.Build();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapGrpcService<GrpcAuctionService>();

        var retryPolicy = Policy.Handle<NpgsqlException>()
            .WaitAndRetry(5, retryAttempt => TimeSpan.FromSeconds(10));

        retryPolicy.ExecuteAndCapture(() => DbInitializer.Initialize(app));

        app.Run();

    }
}

public partial class Program { }