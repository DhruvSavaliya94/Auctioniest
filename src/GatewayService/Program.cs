using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace GatewayService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddReverseProxy()
                .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

            // Add services to the container.
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = builder.Configuration["IdentityServiceUrl"];
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters.ValidateAudience = false;
                    options.TokenValidationParameters.NameClaimType = "username";
                });

            var app = builder.Build();

            app.MapReverseProxy();
            app.UseHttpsRedirection();
            app.UseAuthorization();

            app.Run();
        }
    }
}
