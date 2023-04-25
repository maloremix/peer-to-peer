using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp7
{
    public class WeatherData
    {
        public WeatherInfo[] Weather { get; set; }
        public MainInfo Main { get; set; }
    }

    public class WeatherInfo
    {
        public int Id { get; set; }
        public string Main { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
    }

    public class MainInfo
    {
        public double Temp { get; set; }
        public double FeelsLike { get; set; }
        public double TempMin { get; set; }
        public double TempMax { get; set; }
        public int Pressure { get; set; }
        public int Humidity { get; set; }
    }

    public class TemperatureAndWeather
    {
        public string MainWeather { get; set; }
        public double Temperature { get; set; }
    }

    public class WeatherClass
    {
        public static async Task<TemperatureAndWeather> GetWeatherAsync(string city)
        {
            var client = new HttpClient();
            string apiKey = "de5eb261e363ec3e95b7094dfe28d97b";
            var url = $"https://api.openweathermap.org/data/2.5/weather?q={city}&units=metric&appid={apiKey}";

            var response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<WeatherData>(json);

                var temperatureAndWeather = new TemperatureAndWeather
                {
                    MainWeather = data.Weather[0].Main,
                    Temperature = data.Main.Temp
                };

                return temperatureAndWeather;
            }
            else
            {
                return null;
            }
        }
    }
}
