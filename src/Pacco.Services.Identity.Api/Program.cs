﻿using System;
using System.Threading.Tasks;
using Convey;
using Convey.Logging;
using Convey.WebApi;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Pacco.Services.Identity.Application;
using Pacco.Services.Identity.Application.Commands;
using Pacco.Services.Identity.Application.Queries;
using Pacco.Services.Identity.Application.Services;
using Pacco.Services.Identity.Infrastructure;

namespace Pacco.Services.Identity.Api
{
    public class Program
    {
        public static async Task Main(string[] args)
            => await WebHost.CreateDefaultBuilder(args)
                .ConfigureServices(services => services
                    .AddOpenTracing()
                    .AddConvey()
                    .AddWebApi()
                    .AddApplication()
                    .AddInfrastructure()
                    .Build())
                .Configure(app => app
                    .UseInfrastructure()
                    .UseEndpoints(endpoints => endpoints
                        .Get("", ctx => ctx.Response.WriteAsync("Welcome to Pacco Identity Service!"))
                        .Get<GetUser>("users/{id}", (query, ctx) => GetUserAsync(query.Id, ctx))
                        .Get("me", async ctx =>
                        {
                            var userId = await ctx.AuthenticateUsingJwtAsync();
                            await GetUserAsync(userId, ctx);
                        })
                        .Post<SignIn>("sign-in", async (cmd, ctx) =>
                        {
                            var token = await ctx.RequestServices.GetService<IIdentityService>().SignInAsync(cmd);
                            ctx.Response.WriteJson(token);
                        })
                        .Post<SignUp>("sign-up", async (cmd, ctx) =>
                        {
                            await ctx.RequestServices.GetService<IIdentityService>().SignUpAsync(cmd);
                            await ctx.Response.Created("identity/me");
                        })))
                .UseLogging()
                .Build()
                .RunAsync();

        private static async Task GetUserAsync(Guid id, HttpContext context)
        {
            var user = await context.RequestServices.GetService<IIdentityService>().GetAsync(id);
            if (user is null)
            {
                context.Response.StatusCode = 404;
                return;
            }

            context.Response.WriteJson(user);
        }
    }
}
