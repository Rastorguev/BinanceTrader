using System.Configuration;
using System.Reflection;

namespace BinanceTrader.Tools.KeyProviders;

public interface IConnectionStringsProvider
{
    string GetConnectionString(string name);
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
                Path.Combine(Path.GetDirectoryName(assembly.Location), "Configs", filename)
        };

        var config = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
        if (config.HasFile == false)
        {
            throw new FileNotFoundException("File not found", configMap.ExeConfigFilename);
        }

        var settings = config.AppSettings.Settings;
        if (settings[name] == null)
        {
            throw new ArgumentException($"Connection string '{name}' not found", nameof(name));
        }

        var connectionString = settings[name].Value;

        return connectionString;
    }
}