using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sidekick.ViewModels;

namespace Sidekick;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public IServiceProvider ServiceProvider { get; private set; }
    public IConfiguration Configuration { get; private set; }
    
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        Configuration = builder.Build();

        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);

        ServiceProvider = serviceCollection.BuildServiceProvider();

        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
        
        base.OnStartup(e);
    }
    
    private void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(Configuration);
        services.AddSingleton(Configuration.GetSection("HotKey").Get<HotKeySettings>() ?? new HotKeySettings());
        services.AddSingleton<ShellViewModel>();
        services.AddSingleton<GuidGeneratorViewModel>();
        
        services.AddTransient<MainWindow>();
    }
}

