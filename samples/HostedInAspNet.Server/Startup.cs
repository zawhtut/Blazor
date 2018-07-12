// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace HostedInAspNet.Server
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddServerSideBlazor();

            services.AddHttpClient();
            services.AddScoped<HttpClient>(s => s.GetRequiredService<IHttpClientFactory>().CreateClient());
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseServerSideBlazor(renderer =>
            {
                renderer.AddComponent<StandaloneApp.App>("app");
            });

            app.UseBlazor<StandaloneApp.App>();
        }
    }
}
