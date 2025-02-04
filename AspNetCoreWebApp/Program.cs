using AspNetCoreWebApp.Services;
using Microsoft.EntityFrameworkCore;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure services
        builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseCosmos(
            builder.Configuration["CosmosDB:EndPoint"],
            builder.Configuration["CosmosDB:PrimaryKey"],
            builder.Configuration["CosmosDB:Database"]
        ));
        builder.Services.AddSingleton<BlobStorageService>();


        //// Add controllers
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddHttpClient(); // Register IHttpClientFactory
        builder.Services.AddSingleton<BookDownloadService>();
        builder.Services.AddHostedService<BookDownloadBackgroundService>();
        builder.Services.AddHttpClient<PythonApiService>();
        builder.Services.AddTransient<PythonApiService>(); 
        builder.Services.AddSingleton<IConfiguration>(builder.Configuration);


        var app = builder.Build();

        app.UseHttpsRedirection();
        app.UseDefaultFiles();
        app.UseStaticFiles();

        // Set default page to wwwroot/index.html

        // Configure middleware
        // app.UseRouting();
        // app.UseAuthorization();
        app.MapControllers();

        // Run the app

        app.Run();
    }
}
