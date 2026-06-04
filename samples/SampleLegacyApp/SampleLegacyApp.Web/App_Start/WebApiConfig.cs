using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Dependencies;
using System.Web.Http.Filters;
using Newtonsoft.Json;

namespace SampleLegacyApp.Web;

public static class WebApiConfig
{
    public static void Register(HttpConfiguration config)
    {
        config.DependencyResolver = new SampleWebApiDependencyResolver();

        config.Formatters.JsonFormatter.SerializerSettings.Formatting = Formatting.Indented;
        config.MessageHandlers.Add(new SampleMessageHandler());
        config.Filters.Add(new SampleExceptionFilterAttribute());
        config.EnableCors();

        config.MapHttpAttributeRoutes();

        config.Routes.MapHttpRoute(
            name: "DefaultApi",
            routeTemplate: "api/{controller}/{id}",
            defaults: new
            {
                id = RouteParameter.Optional
            });
    }
}

internal sealed class SampleWebApiDependencyResolver : IDependencyResolver
{
    public IDependencyScope BeginScope()
    {
        return this;
    }

    public object? GetService(Type serviceType)
    {
        return null;
    }

    public IEnumerable<object> GetServices(Type serviceType)
    {
        return Array.Empty<object>();
    }

    public void Dispose()
    {
    }
}

internal sealed class SampleMessageHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return base.SendAsync(request, cancellationToken);
    }
}

internal sealed class SampleExceptionFilterAttribute : ExceptionFilterAttribute
{
}

// The SampleWebApiCorsExtensions class is there deliberately so config.EnableCors();
// compiles without needing to add the real Microsoft.AspNet.WebApi.Cors package just for the sample.
internal static class SampleWebApiCorsExtensions
{
    public static void EnableCors(this HttpConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);
    }
}