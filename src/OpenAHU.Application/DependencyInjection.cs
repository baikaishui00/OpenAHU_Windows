using Microsoft.Extensions.DependencyInjection;
namespace OpenAHU.Application;
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services) =>
        services.AddScoped<ICampusService, CampusService>();
}
