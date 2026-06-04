using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;

namespace SampleLegacyApp.Web;

public class MvcApplication : HttpApplication
{
    protected void Application_Start()
    {
        DependencyResolver.SetResolver(new SampleMvcDependencyResolver());
        ControllerBuilder.Current.SetControllerFactory(new SampleControllerFactory());

        AreaRegistration.RegisterAllAreas();
        GlobalConfiguration.Configure(WebApiConfig.Register);

        FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
        GlobalFilters.Filters.Add(new HandleErrorAttribute());

        RouteConfig.RegisterRoutes(RouteTable.Routes);
        BundleConfig.RegisterBundles(null);

        ModelBinders.Binders.Add(typeof(SampleModel), new SampleModelBinder());
        ValueProviderFactories.Factories.Add(new SampleValueProviderFactory());
    }
}

internal sealed class SampleMvcDependencyResolver : IDependencyResolver
{
    public object? GetService(Type serviceType)
    {
        return null;
    }

    public IEnumerable<object> GetServices(Type serviceType)
    {
        return Array.Empty<object>();
    }
}

internal sealed class SampleControllerFactory : DefaultControllerFactory
{
}

internal sealed class SampleModelBinder : IModelBinder
{
    public object? BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
    {
        return null;
    }
}

internal sealed class SampleValueProviderFactory : ValueProviderFactory
{
    public override IValueProvider GetValueProvider(ControllerContext controllerContext)
    {
        return new NameValueCollectionValueProvider(
            new System.Collections.Specialized.NameValueCollection(),
            System.Globalization.CultureInfo.InvariantCulture);
    }
}

internal sealed class SampleModel
{
    public string? Name { get; init; }
}