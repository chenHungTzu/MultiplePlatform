using Microsoft.AspNetCore.Mvc;

namespace PlatformCore.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly HttpClient httpClient;

        public WeatherForecastController(IHttpClientFactory httpClientFactory)
        {
            httpClient = httpClientFactory.CreateClient();
        }

        /// <summary>
        /// 呼叫API並取得結果
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private async Task<List<WeatherForecast>?> GetWeatherForecasts(string url)
        {
            var resp = await httpClient.GetAsync(url);
            return await resp.Content.ReadFromJsonAsync<List<WeatherForecast>>();
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public async Task<List<WeatherForecast>> Get()
        {
            // get platform B api
            var resultOfB = await GetWeatherForecasts($"{Environment.GetEnvironmentVariable("platformB")}WeatherForecast");

            // get platform C api
            var resultOfC = await GetWeatherForecasts($"{Environment.GetEnvironmentVariable("platformC")}WeatherForecast");

            return Enumerable.Concat(resultOfB, resultOfC).ToList();
        }
    }
}