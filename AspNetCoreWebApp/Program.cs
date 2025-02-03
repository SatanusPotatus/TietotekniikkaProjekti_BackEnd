using AspNetCoreWebApp.Services;
using Microsoft.EntityFrameworkCore;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure services
        // builder.Services.AddDbContext<AppDbContext>(options =>
        //     options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
        //
        builder.Services.AddSingleton<BlobStorageService>();
        // builder.Services.AddHttpClient<PythonApiService>();
        //
        //// Add controllers
        builder.Services.AddControllers();

        // Register HttpClient for BookDownloadService
        //builder.Services.AddHttpClient<BookDownloadService>();
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
