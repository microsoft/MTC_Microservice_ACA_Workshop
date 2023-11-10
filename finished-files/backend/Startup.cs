using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.ServiceBus;
using Microsoft.Identity.Web;

namespace backend
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddSwaggerGen();
            Console.WriteLine("Conn String: " + Configuration.GetValue<string>("SERVICE_BUS_CONN_STR"));
            Console.WriteLine("Topic Name: " + Configuration.GetValue<string>("topicName"));
            Console.WriteLine("Subscription Name: " + Configuration.GetValue<string>("subscriptionName"));
            services.AddHostedService<ConsumerService>();
            services.AddSingleton<ISubscriptionClient>(x => 
            new SubscriptionClient(Configuration.GetValue<string>("SERVICE_BUS_CONN_STR"),
                Configuration.GetValue<string>("topicName"), Configuration.GetValue<string>("subscriptionName")));
            services.AddControllers().AddDapr();
            // services.AddMicrosoftIdentityWebApiAuthentication(Configuration);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();

            }
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "backend v1"));

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.UseAuthentication();
            app.UseAuthorization();
        }
    }
}
