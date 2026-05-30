using Application.Interfaces;
using Application.Services;
using Microsoft.Extensions.DependencyInjection;
using ReMeal.Infrastructure.DependencyInjection;

namespace Tests.AnalyticsModule.Infrastructure;

[TestClass]
public sealed class AnalyticsDependencyInjectionTests
{
    [TestMethod]
    public void AddApplicationServices_RegistersPartnerAnalyticsService()
    {
        var services = new ServiceCollection();

        services.AddApplicationServices();

        var descriptor =
            services.Single(x => x.ServiceType == typeof(IPartnerAnalyticsService));

        Assert.AreEqual(typeof(PartnerAnalyticsService), descriptor.ImplementationType);
        Assert.AreEqual(ServiceLifetime.Scoped, descriptor.Lifetime);
    }
}
