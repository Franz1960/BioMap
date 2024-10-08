using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BioMap
{
  public class Program
  {
    public static void Main(string[] args) {
      IHostBuilder hostBuilder = CreateHostBuilder(args);
      hostBuilder.Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>               //webBuilder.UseUrls("http://localhost:5010");
              webBuilder.UseStartup<Startup>());
  }
}
