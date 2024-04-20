using System;
using AuctionService.Consumers;
using AuctionService.Data;
using AuctionService.RequestHelpers;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace AuctionService
{
    public class Program
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
                    // Specify the RabbitMQ host, username, and password
                    cfg.Host("rabbitmq://localhost", h =>
                    {
                        h.Username("rabbitmq");
                        h.Password("rabbitmqpw");
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

            var app = builder.Build();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            try
            {
                DbInitializer.Initialize(app);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            app.Run();

        }
    }
}
