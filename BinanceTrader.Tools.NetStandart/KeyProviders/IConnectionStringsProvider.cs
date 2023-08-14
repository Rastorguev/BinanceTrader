using System.Configuration;
using System.Reflection;
using JetBrains.Annotations;

namespace BinanceTrader.Tools.KeyProviders;

public interface IConnectionStringsProvider
{
    [NotNull]
    string GetConnectionString([NotNull] string name);
}

public class ConnectionStringsProvider : IConnectionStringsProvider
{
    public string GetConnectionString(string name)
    {
        const string filename = "ConnectionStrings.config";

        var assembly = Assembly.GetExecutingAssembly();
        var configMap = new ExeConfigurationFileMap
        {
            ExeConfigFilename =
                Path.Combine(Path.GetDirectoryName(assembly.Location).NotNull(), "Configs", filename)
        };

        var config = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
        if (config.HasFile == false)
        {
            throw new FileNotFoundException("File not found", configMap.ExeConfigFilename);
        }

        var settings = config.AppSettings.NotNull().Settings.NotNull();
        if (settings[name] == null)
        {
            throw new ArgumentException($"Connection string '{name}' not found", nameof(name));
        }

        var connectionString = settings[name].NotNull().Value.NotNull();

        return connectionString;
    }
}