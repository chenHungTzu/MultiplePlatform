using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace PlatformCore.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly HttpClient httpClient;

        public WeatherForecastController()
        {
            httpClient = new HttpClient();
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public async Task<WeatherForecast[]> Get()
        {
            // get platform A api
            var responseOfB = await httpClient.GetAsync($"{Environment.GetEnvironmentVariable("platformB")}WeatherForecast");
            var contentOfB = await responseOfB.Content.ReadAsStringAsync();
            var resultOfB = JsonSerializer.Deserialize<List<WeatherForecast>>(contentOfB, new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true
            });

            // get platform B api
            var responseOfC = await httpClient.GetAsync($"{Environment.GetEnvironmentVariable("platformC")}WeatherForecast");
            var contentOfC = await responseOfC.Content.ReadAsStringAsync();
            var resultOfC = JsonSerializer.Deserialize<List<WeatherForecast>>(contentOfC, new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true
            });

            return resultOfB.Concat(resultOfC).ToArray();
        }
    }
}