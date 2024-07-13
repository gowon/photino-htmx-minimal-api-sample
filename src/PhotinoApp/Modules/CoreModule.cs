namespace PhotinoApp.Modules;

using Carter;
using Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Razor;

public class CoreModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/config", (IConfiguration configuration) =>
        {
            var root = configuration as IConfigurationRoot;
            return Results.Text(root!.GetDebugView());
        });

        app.MapGet("/weather/{amount:int}", (int amount, WeatherForecastService forecastService) =>
        {
            var forecasts = forecastService.GetWeatherForecasts(amount);
            return Results.Ok(forecasts);
        });

        app.MapPost("/weather-card/{amount:int}", (int amount, WeatherForecastService forecastService) =>
        {
            var forecasts = forecastService.GetWeatherForecasts(amount);
            var model = new WeatherForecastList.WeatherForecastListParameters
            {
                Forecasts = forecasts,
                TotalCount = forecasts.Count
            };

            return new RazorComponentResult<WeatherForecastList>(new { Model = model });
        });
    }
}