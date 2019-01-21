﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using API.Domain.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using API.Domain.Models.Options;
using API.Configurations.Filters.Swagger;
using API.Configurations.Factories;
using API.Configurations.Middlewares;
using API.Domain.Services;
using API.Domain.Validations;
using FluentValidation;

namespace API
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
            // Server
            services
                .AddMvcCore()
                .AddApiExplorer()
                .AddDataAnnotations()
                .AddJsonFormatters()
                .AddCors()
                .AddJsonOptions(options => 
                {
                    options.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
                })
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

#if (DEBUG)
            // Swagger
            services
                .AddSwaggerGen(options => 
                {
                    options.SwaggerDoc("v1", new OpenApiInfo
                    { 
                        Title = "API Bootstrap", 
                        Version = "v1",
                        Contact = new OpenApiContact
                        {
                            Name = "Gabriel Lucena",
                            Url = new Uri("https://www.github.com/gnllucena")
                        },
                        Description = @"API application with dynamic swagger documentation, endpoint for health checking, mysql container."
                    });
                    
                    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme 
                    {
                        In = ParameterLocation.Header,
                        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                        Name = "Authorization",
                        Type = SecuritySchemeType.ApiKey
                    });

                    options.DescribeAllEnumsAsStrings();

                    options.EnableAnnotations();
                    
                    options.DocumentFilter<KnownTypesResponseFilter>();
                    options.OperationFilter<ClientFaultResponseFilter>();
                    options.OperationFilter<ServerFaultResponseFilter>();
                    options.OperationFilter<HttpHeadersResponseFilter>();
                });
#endif
            // Healthcheck
            services.AddHealthChecks();

            // Appsettings configuration
            services.AddOptions();
            services.Configure<Database>(Configuration.GetSection("Database"));

            // Dependency injection
            // Factories
            services.AddSingleton<IDatabaseFactory, DatabaseFactory>();

            // Services
            services.AddSingleton<ISqlService, SqlService>();

            // Validators
            services.AddSingleton<IValidator<User>, UserValidator>();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseHsts();

            app.UseHealthChecks("/healthcheck");
            
#if (DEBUG)            
            app.UseSwagger();

            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Bootstrap");
            });
#endif

            app.UseHttpsRedirection();

            app.UseMiddleware<TransactionMiddleware>();

            app.UseMvc();
        }
    }
}
