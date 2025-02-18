using Microsoft.AspNetCore.OData;
using NLog.Web;

namespace UrlShortener;

public static class Program
{
    internal static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        builder.Logging.AddNLog("nlog.config");

        Startup startup = new(builder.Configuration);
        startup.ConfigureServices(builder.Services);
        startup.ConfigureDatabase(builder.Services);

        // Add services to the container.
        builder.Services.AddControllersWithViews().AddOData(static opts => opts.Select().SetMaxTop(100).Filter());

        WebApplication app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.Run();
    }
}
