using AulaPedidos.Application.Messaging;
using AulaPedidos.Domain.Entities;
using AulaPedidos.Api.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;

namespace AulaPedidos.IntegrationTests;
public sealed class ArchitectureTests
{
    [Fact]
    public void Domain_is_independent_and_application_does_not_reference_outer_layers()
    {
        var domainReferences = typeof(Product).Assembly.GetReferencedAssemblies().Select(assembly => assembly.Name!).ToArray();
        Assert.DoesNotContain(domainReferences, name => name.StartsWith("Microsoft.EntityFrameworkCore") || name.StartsWith("Microsoft.AspNetCore") || name.StartsWith("AulaPedidos."));
        var applicationReferences = typeof(IMediator).Assembly.GetReferencedAssemblies().Select(assembly => assembly.Name!).ToArray();
        Assert.DoesNotContain(applicationReferences, name => name is "AulaPedidos.Api" or "AulaPedidos.Infrastructure");
    }
    [Fact]
    public void Demo_authentication_is_refused_in_production()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:DemoMode"] = "true" }).Build();
        var services = new ServiceCollection();
        Assert.Throws<InvalidOperationException>(() => services.AddCourseAuthentication(config, new ProductionEnvironment()));
    }
    private sealed class ProductionEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "test";
        public string ContentRootPath { get; set; } = ".";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
