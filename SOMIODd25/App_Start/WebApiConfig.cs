using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace SOMIODd25
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "SOMIODApi",
                routeTemplate: "api/somiod/{appName}/{contName}/{dataType}/{dataName}",
                defaults: new
                {
                    controller = "somiod",
                    appName = RouteParameter.Optional,
                    contName = RouteParameter.Optional,
                    dataType = RouteParameter.Optional,
                    dataName = RouteParameter.Optional
                }
            );
        }
    }
}
