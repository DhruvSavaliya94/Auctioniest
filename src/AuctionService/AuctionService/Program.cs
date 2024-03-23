
using AuctionService.Data;
using AuctionService.RequestHelpers;
using Microsoft.EntityFrameworkCore;

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

            var app = builder.Build();


            // Configure the HTTP request pipeline.
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
