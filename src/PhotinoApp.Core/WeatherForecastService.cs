namespace PhotinoApp.Core;

using SimplePhotinoApp.Models;
using Soenneker.Utils.AutoBogus;
using Soenneker.Utils.AutoBogus.Config;

public class WeatherForecastService
{
    private readonly AutoFaker _faker;

    public WeatherForecastService()
    {
        var optionalConfig = new AutoFakerConfig();
        _faker = new AutoFaker(optionalConfig);
    }

    public List<WeatherForecast> GetWeatherForecasts(int amount)
    {
        var forecasts = _faker.Generate<WeatherForecast>(amount);
        return forecasts;
    }
}