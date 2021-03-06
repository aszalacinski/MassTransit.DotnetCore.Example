﻿namespace Client
{
    using MassTransit;
    using MassTransit.ExtensionsLoggingIntegration;
    using MassTransit.Util;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Sample.MessageTypes;
    using System;

    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }
        private IServiceProvider Services { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging();
            services.AddOptions();
            
            // rabbitmq configuration section
            services.Configure<RabbitMqConfiguration>(Configuration.GetSection("rabbitmq.settings"));

            // bus setup
            services.AddSingleton<IBusControl>(provider =>
            {
                string rabbitMqHost = provider.GetService<IOptions<RabbitMqConfiguration>>().Value.Host;
                string username = provider.GetService<IOptions<RabbitMqConfiguration>>().Value.Username;
                string password = provider.GetService<IOptions<RabbitMqConfiguration>>().Value.Password;

                return Bus.Factory.CreateUsingRabbitMq(cfg =>
                {
                    var host = cfg.Host(new Uri(rabbitMqHost), h =>
                    {
                        h.Username(username);
                        h.Password(password);
                    });
                });
            });

            services.AddSingleton<IBus>(provider =>
            {
                return provider.GetService<IBusControl>();
            });
            

            // Add framework services.
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IApplicationLifetime applicationLifetime)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            ExtensionsLogger.Use(loggerFactory);

            app.UseMvc();

            Services = app.ApplicationServices;

            // start micro services
            applicationLifetime.ApplicationStarted.Register(OnStart);
            applicationLifetime.ApplicationStopping.Register(OnStopping);
            applicationLifetime.ApplicationStopped.Register(OnStopped);
        }

        private void OnStart()
        {
            IBusControl busControl = Services.GetService<IBusControl>();

            // start mass transit
            TaskUtil.Await(() => busControl.StartAsync());
            
        }

        private void OnStopping()
        {
            // stop mass transit
            IBusControl busControl = Services.GetService<IBusControl>();
            busControl.Stop();

        }

        private void OnStopped()
        {
            // Stopped 
        }
    }

    public class RabbitMqConfiguration
    {
        public string Host { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class SimpleRequest : ISimpleRequest
    {
        readonly string _customerId;
        readonly DateTime _timestamp;

        public SimpleRequest(string customerId)
        {
            _customerId = customerId;
            _timestamp = DateTime.UtcNow;
        }

        public DateTime Timestamp
        {
            get { return _timestamp; }
        }

        public string CustomerId
        {
            get { return _customerId; }
        }
    }


}
