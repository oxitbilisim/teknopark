
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Oxit.Core;
using Oxit.DataAccess.EntityFramework;
using Oxit.DataAccess.teknopark;
using Oxit.Scheduling.Core;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Oxit.Infrastructure;
namespace Oxit.Scheduling
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Altay.Jobs", Version = "v1" });
            });
            services.AddDbContext<appDbContext>(x =>
            {
                x.UseNpgsql(Settings.Database.ConnectionString);

            });
            services.AddDbContext<TeknoparkContext>(x =>
            {
                x.UseSqlServer(Settings.TeknoparkConnectionstring);

            });
            services.AddMemoryCache();
            services.Configure<QuartzOptions>(Configuration.GetSection("Quartz"));
            services.Configure<QuartzOptions>(options =>
            {
                options.Scheduling.IgnoreDuplicates = true; // default: false
                options.Scheduling.OverWriteExistingData = true; // default: true
            });

            services.AddScheduledJobs();
            services.AddAppDependencies();
        }


        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();

            }
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Altay.Jobs v1"));
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

        }
    }
}
