using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows;
using Autofac;
using CactusPie.MapLocation.Minimap.Controls;
using CactusPie.MapLocation.Minimap.Data;
using CactusPie.MapLocation.Minimap.Services;
using CactusPie.MapLocation.Minimap.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using RestSharp;
using Serilog;
using Serilog.Core;

namespace CactusPie.MapLocation.Minimap;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private void ApplicationStartup(object sender, StartupEventArgs e)
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

        Logger logger = CreateLogger();
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            logger.Error(args.ExceptionObject as Exception, "An exception occured");
        };

        IComponentContext container = CreateContainer();

        var mainWindow = container.Resolve<MainWindow>();
        mainWindow.Show();
    }

    private static IComponentContext CreateContainer()
    {
        IConfiguration configuration = GetConfiguration();
        var mapConfiguration = configuration.Get<MapConfiguration>();

        if (mapConfiguration?.GameIpAddress == null)
        {
            throw new InvalidOperationException("Cannot retrieve the listen IP address from the configuration!");
        }

        var containerBuilder = new ContainerBuilder();
        containerBuilder.RegisterType<PlotWindow>().AsSelf().InstancePerDependency();
        containerBuilder.RegisterType<MainWindow>().AsSelf().InstancePerDependency();
        containerBuilder.RegisterType<MapControl>().AsSelf().InstancePerDependency();
        containerBuilder.RegisterType<BoundSettings>().AsSelf().InstancePerDependency();
        containerBuilder.RegisterType<AddNewMapDialog>().AsSelf().InstancePerDependency();
        containerBuilder.RegisterType<AddNewBoundDialog>().AsSelf().InstancePerDependency();
        containerBuilder.RegisterType<ThemeSelector>().AsSelf().InstancePerDependency();
        containerBuilder.RegisterType<MapDataRetriever>().As<IMapDataRetriever>().InstancePerDependency();
        containerBuilder.RegisterType<MapCreationDataManager>().As<IMapCreationDataManager>().SingleInstance();
        containerBuilder.RegisterType<CurrentMapData>().As<ICurrentMapData>().SingleInstance().AutoActivate();
        containerBuilder.RegisterInstance(mapConfiguration).AsSelf();

        var restClientOptions = new RestClientOptions($"http://{mapConfiguration.GameIpAddress}:{mapConfiguration.GamePort}");
        var restClient = new RestClient(restClientOptions);
        containerBuilder.RegisterInstance(restClient).AsSelf();

        return containerBuilder.Build();
    }

    private static IConfiguration GetConfiguration()
    {
        IConfigurationBuilder builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false);

        IConfiguration config = builder.Build();
        return config;
    }

    private static Logger CreateLogger()
    {
        Logger? logger = new LoggerConfiguration()
            .WriteTo.File("log.txt")
            .CreateLogger();

        return logger;
    }
}