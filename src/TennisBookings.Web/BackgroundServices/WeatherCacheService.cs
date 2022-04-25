using System;
using System.Threading;
using System.Threading.Tasks;
using TennisBookings.Web.Domain;
using TennisBookings.Web.External;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TennisBookings.Web.Core.Caching;
using TennisBookings.Web.Configuration;

namespace TennisBookings.Web.BackgroundServices
{
    public class WeatherCacheService : BackgroundService
    {
        private readonly IWeatherApiClient _weatherApiClient;
        private readonly IDistributedCache<CurrentWeatherResult> _cache;
        private readonly ILogger<WeatherCacheService> _logger;

        private readonly int _minutesToCache;
        private readonly int _refreshIntervalInSeconds;

        public WeatherCacheService(IWeatherApiClient weatherApiClient, IDistributedCache<CurrentWeatherResult> cache,
            ILogger<WeatherCacheService> logger, IOptionsMonitor<ExternalServicesConfig> options)
        {
            _weatherApiClient = weatherApiClient;
            _cache = cache;
            _logger = logger;
            _minutesToCache = options.Get(ExternalServicesConfig.WeatherApi).MinsToCache;
            _refreshIntervalInSeconds = _minutesToCache > 1 ? (_minutesToCache - 1) * 60 : 30;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var forecast = await _weatherApiClient.GetWeatherForecastAsync(stoppingToken);

                if(forecast is object)
                {
                    var currentWeather = new CurrentWeatherResult {  Description = forecast.Weather.Description };

                    var cacheKey = $"current_weather_{DateTime.UtcNow:yyyy_MM_dd}";

                    _logger.LogInformation("Updating weather in cache.");

                    await _cache.SetAsync(cacheKey, currentWeather, _minutesToCache);
                }

                await Task.Delay(TimeSpan.FromSeconds(_refreshIntervalInSeconds), stoppingToken);
            }
        }
    }
}
