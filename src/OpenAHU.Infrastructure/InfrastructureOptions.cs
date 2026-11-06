using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAHU.Application;

namespace OpenAHU.Infrastructure;

public static class InfrastructureOptions
{
    public static string DatabasePath(IConfiguration configuration)
    {
        var path=configuration["OpenAHU:DatabasePath"];
        if (string.IsNullOrWhiteSpace(path))
            path=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"OpenAHU","campus.db");
        // Relative paths depend on the process working directory and are not permitted.
        if (!Path.IsPathFullyQualified(path))
            throw new InvalidOperationException("OpenAHU:DatabasePath 必须是绝对路径。");
        return path;
    }
    public static IServiceCollection AddInfrastructure(this IServiceCollection services,IConfiguration configuration)
    {
        var path=DatabasePath(configuration);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        services.AddDbContext<CampusDbContext>(options => options.UseSqlite(
            new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder {
                DataSource=path,ForeignKeys=true,Pooling=true
            }.ToString()));
        services.AddScoped<ICampusRepository,CampusRepository>();
        return services;
    }
}
