using MongoDB.Driver;
using MongoDB.Entities;
using Polly;
using Polly.Extensions.Http;
using SearchService.Data;
using SearchService.Models;
using SearchService.Services;
using System.Net;

namespace SearchService
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            builder.Services.AddHttpClient<AuctionSvcHttpClient>()
                .AddPolicyHandler(GetPolicy());

            var app = builder.Build();

            // Configure the HTTP request pipeline.

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

            app.Lifetime.ApplicationStarted.Register(async () =>
            {
                try
                {
                    await DbInitializer.Initialize(app);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            });

            app.Run();
        }

        public static IAsyncPolicy<HttpResponseMessage> GetPolicy()
            => HttpPolicyExtensions.HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == HttpStatusCode.NotFound)
                .WaitAndRetryForeverAsync(_ => TimeSpan.FromSeconds(3));

    }
}
