using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Blazorise;
using Blazorise.Bootstrap;
using Blazorise.Icons.FontAwesome;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace BioMap
{
  public class Startup
  {
    public Startup(IConfiguration configuration) {
      this.Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    public void ConfigureServices(IServiceCollection services) {
      services.AddRazorPages();
      services.AddServerSideBlazor();
      services.AddSingleton<DataService>();
      services.AddScoped<SessionData>();
      services.AddLocalization(options => options.ResourcesPath = "Resources");
      services.Configure<RequestLocalizationOptions>(options => {
        // Define the list of cultures your app will support
        var supportedCultures = new List<CultureInfo>()
        {
                new CultureInfo("en"),
                new CultureInfo("de")
                  };
        // Set the default culture
        options.DefaultRequestCulture = new RequestCulture("en");
        options.SupportedCultures = supportedCultures;
        options.SupportedUICultures = supportedCultures;
      });
      services.AddControllers();
      services.AddBlazorise(options => {
        options.ChangeTextOnKeyPress = false; // optional
        options.DelayTextOnKeyPress = true;
        options.DelayTextOnKeyPressInterval = 500;
      });
      services.AddBootstrapProviders();
      services.AddFontAwesomeIcons();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
      if (env.IsDevelopment()) {
        app.UseDeveloperExceptionPage();
      } else {
        app.UseExceptionHandler("/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
      }

      //app.UseHttpsRedirection();
      app.UseStaticFiles();

      app.UseRouting();

      app.UseRequestLocalization(app.ApplicationServices.GetService<IOptions<RequestLocalizationOptions>>().Value);

      app.UseEndpoints(endpoints => {
        endpoints.MapBlazorHub();
        endpoints.MapFallbackToPage("/_Host");
        //
        endpoints.MapControllers();
      });

      var ds = app.ApplicationServices.GetRequiredService<DataService>();
      ds.Init();
    }
  }
}
