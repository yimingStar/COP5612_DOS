namespace twitter_client

open System
open Microsoft.AspNetCore
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open WebSharper.AspNetCore

type Startup() =

    member this.ConfigureServices(services: IServiceCollection) =
        services.AddSitelet(Site.Main)
            .AddAuthentication("WebSharper")
            .AddCookie("WebSharper", fun options -> ())
        |> ignore

    member this.Configure(app: IApplicationBuilder, env: IWebHostEnvironment) =
        if env.IsDevelopment() then app.UseDeveloperExceptionPage() |> ignore

        app.UseAuthentication()
            .UseStaticFiles()
            .UseWebSharper()
            .Run(fun context ->
                context.Response.StatusCode <- 404
                context.Response.WriteAsync("Page not found"))

module Program =
    let BuildWebHost(args, port) =
        let hostURL = sprintf("http://localhost:%s") port
        WebHost
            .CreateDefaultBuilder(args)
            .UseUrls(hostURL) 
            .UseStartup<Startup>()
            .Build()

    [<EntryPoint>]
    let main args =
        printfn "Twitter Client started."
        let port = if args.Length > 1 then args.[1] else "5000"
        BuildWebHost(args, port).Run()
        0
