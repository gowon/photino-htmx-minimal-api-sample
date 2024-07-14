namespace PhotinoApp;

using System.Drawing;
using System.Net.NetworkInformation;
using System.Text;
using Carter;
using Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using NLog.Layouts;
using NLog.Layouts.ClefJsonLayout;
using Photino.NET;

internal class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        // Create logger to support startup and exception logging, before host is built and executed
        var bootstrapLogger = LogManager.Setup().LoadConfiguration(builder =>
                builder.ForLogger().WriteToDebug(new CompactJsonLayout
                {
                    Attributes = { new JsonAttribute("@SourceContext", Layout.FromString("${logger}")) }
                }))
            .GetCurrentClassLogger();

        try
        {
            bootstrapLogger.Debug("Starting application host in the background.");
            var host = CreateHostBuilder(args).Build();
            host.StartAsync();

            bootstrapLogger.Debug("Starting Photino window.");
            var window = CreatePhotinoWindow(host);
            window.WaitForClose();
        }
        catch (Exception exception)
        {
            bootstrapLogger.Fatal(exception, "Application terminated unexpectedly.");
            Environment.Exit(1);
        }
        finally
        {
            // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
            LogManager.Shutdown();
        }
    }

    public static PhotinoWindow CreatePhotinoWindow(IHost host)
    {
        var internalHostPath = host.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!.Addresses.Single();

        var window = new PhotinoWindow()
            .SetTitle(nameof(PhotinoApp))
            .SetUseOsDefaultSize(false)
            .SetSize(new Size(800, 600))
            // Center window in the middle of the screen
            .Center()
            // Users can resize windows by default.
            // Let's make this one fixed instead.
            .SetResizable(true)
            .RegisterCustomSchemeHandler("app", (object sender, string scheme, string url, out string contentType) =>
            {
                contentType = "text/javascript";
                return new MemoryStream(Encoding.UTF8.GetBytes(@"
                        (() =>{
                            window.setTimeout(() => {
                                alert(`🎉 Dynamically inserted JavaScript.`);
                            }, 1000);
                        })();
                    "));
            })
            // Most event handlers can be registered after the
            // PhotinoWindow was instantiated by calling a registration 
            // method like the following RegisterWebMessageReceivedHandler.
            // This could be added in the PhotinoWindowOptions if preferred.
            .RegisterWebMessageReceivedHandler((sender, message) =>
            {
                var window = (PhotinoWindow)sender;

                // The message argument is coming in from sendMessage.
                // "window.external.sendMessage(message: string)"
                var response = $"Received message: \"{message}\"";

                // Send a message back the to JavaScript event handler.
                // "window.external.receiveMessage(callback: Function)"
                window.SendWebMessage(response);
            })
            //.Load(new Uri("http://google.com")); // Can be used with relative path strings or "new URI()" instance to load a website.
            //.Load("wwwroot/index.html"); // Can be used with relative path strings or "new URI()" instance to load a website.
            //.Load($"{internalHostPath}/index.html"); // Can be used with relative path strings or "new URI()" instance to load a website.
            .Load(internalHostPath); // Can be used with relative path strings or "new URI()" instance to load a website.

        

        window.WindowClosing += (sender, eventArgs) =>
        {
            host.StopAsync();
            return false;
        };

        return window;
    }

    public static IHostBuilder CreateHostBuilder(string[] args, int minPort = 49152, int maxPort = 65535)
    {
        var port = minPort;

        // Try ports until available port is found
        // ref: https://www.techtarget.com/searchnetworking/definition/dynamic-port-numbers
        while (IPGlobalProperties
               .GetIPGlobalProperties()
               .GetActiveTcpListeners()
               .Any(endPoint => endPoint.Port == port))
        {
            if (port > maxPort)
            {
                throw new SystemException($"Couldn't find open port within range {minPort} - {maxPort}.");
            }

            port++;
        }

        return Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(host =>
            {
                host.UseUrls($"http://localhost:{port}");
                host.ConfigureLogging(builder =>
                {
                    builder.Configure(options =>
                    {
                        options.ActivityTrackingOptions =
                            ActivityTrackingOptions.TraceId | ActivityTrackingOptions.SpanId |
                            ActivityTrackingOptions.ParentId;
                    });

                    builder.ClearProviders();
                    builder.AddNLog(provider =>
                    {
                        var factory = new LogFactory();
                        factory.Setup().LoadConfigurationFromSection(provider.GetRequiredService<IConfiguration>());
                        return factory;
                    });
                });

                host.ConfigureServices((context, services) =>
                {
                    services.AddRazorComponents();
                    services.AddCarter();
                    services.AddSingleton<WeatherForecastService>();
                });

                host.Configure((context, app) =>
                {
                    app.UseRouting();

                    var fileOptions = new DefaultFilesOptions();
                    fileOptions.DefaultFileNames.Clear();
                    fileOptions.DefaultFileNames.Add("index.html");
                    fileOptions.DefaultFileNames.Add("styles.css");
                    fileOptions.DefaultFileNames.Add("favicon.ico");
                    app.UseDefaultFiles(fileOptions);

                    app.UseStaticFiles(new StaticFileOptions
                    {
                        FileProvider =
                            new PhysicalFileProvider(
                                Path.Combine(context.HostingEnvironment.ContentRootPath, "wwwroot"))
                    });

                    app.UseEndpoints(builder => { builder.MapCarter(); });
                });
            });
    }
}