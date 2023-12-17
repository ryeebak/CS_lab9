using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;


public class Weather
{
    public string Country { get; set; }
    public string Name { get; set; }
    public double Temp { get; set; }
    public string Description { get; set; }
}

public class WeatherInfo
{
    public MainInfo main { get; set; }
    public WeatherDescription[] weather { get; set; }
    public string name { get; set; }
    public SysInfo sys { get; set; }
}

public class MainInfo
{
    public double temp { get; set; }
}

public class WeatherDescription
{
    public string description { get; set; }
}

public class SysInfo
{
    public string country { get; set; }
}
partial class AppWeather
{
    private System.ComponentModel.IContainer components = null;
    private Label labelWeatherInfo;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }


    private void InitializeComponent()
    {
        listBoxCities = new ListBox();
        buttonLoadWeather = new Button();
        labelWeatherInfo = new Label();
        SuspendLayout();
        //окно с городами
        listBoxCities.BackColor = Color.FromArgb(60, 60, 60);
        listBoxCities.BorderStyle = BorderStyle.FixedSingle;
        listBoxCities.Font = new Font("Consolas", 11F, FontStyle.Regular, GraphicsUnit.Point);
        listBoxCities.ForeColor = Color.FromArgb(250, 250, 250);
        listBoxCities.FormattingEnabled = true;
        listBoxCities.ItemHeight = 18;
        listBoxCities.Location = new Point(10, 10);
        listBoxCities.Margin = new Padding(4, 3, 4, 3);
        listBoxCities.Name = "listBoxCities";
        listBoxCities.Size = new Size(300, 400);
        listBoxCities.TabIndex = 0;
        //кнопка загрузки
        buttonLoadWeather.BackColor = Color.FromArgb(0, 0, 0);
        buttonLoadWeather.FlatStyle = FlatStyle.System;
        buttonLoadWeather.Font = new Font("Consolas", 11F, FontStyle.Regular, GraphicsUnit.Point);
        buttonLoadWeather.ForeColor = Color.Gray;
        buttonLoadWeather.Location = new Point(10, 420);
        buttonLoadWeather.Margin = new Padding(4, 3, 4, 3);
        buttonLoadWeather.Name = "buttonLoadWeather";
        buttonLoadWeather.Size = new Size(300, 60);
        buttonLoadWeather.TabIndex = 1;
        buttonLoadWeather.Text = "Load Weather";
        buttonLoadWeather.UseVisualStyleBackColor = true;
        buttonLoadWeather.Click += buttonLoadWeather_Click;
        //информация из города
        labelWeatherInfo.AutoSize = true;
        labelWeatherInfo.Font = new Font("Consolas", 11F, FontStyle.Regular, GraphicsUnit.Point);
        labelWeatherInfo.ForeColor = Color.FromArgb(250, 250, 250);
        labelWeatherInfo.Location = new Point(330, 10);
        labelWeatherInfo.Name = "labelWeatherInfo";
        labelWeatherInfo.Size = new Size(0, 18);
        labelWeatherInfo.TabIndex = 0;
        //окно приложения
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(40, 40, 40);
        ClientSize = new Size(600, 500);
        Controls.Add(labelWeatherInfo);
        Controls.Add(buttonLoadWeather);
        Controls.Add(listBoxCities);
        Margin = new Padding(4, 3, 4, 3);
        Name = "Form1";
        Text = "Weather App";
        Load += Form1_Load;
        ResumeLayout(false);
        PerformLayout();
    }

    private ListBox listBoxCities;
    private Button buttonLoadWeather;
}
public partial class AppWeather : Form
{
    private const string apiKey = "1079432d32219ee76ef9ff5766bb3924";
    private const string apiUrl = "https://api.openweathermap.org/data/2.5/weather";

    public AppWeather()
    {
        InitializeComponent();
    }

    private async void Form1_Load(object sender, EventArgs e)
    {
        await LoadCityDataAsync();
    }

    private async Task LoadCityDataAsync()
    {
        try
        {
            var cities = await ReadCitiesFromFileAsync("city.txt");
            listBoxCities.Items.AddRange(cities.ToArray());
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading city data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void buttonLoadWeather_Click(object sender, EventArgs e)
    {
        if (listBoxCities.SelectedItem == null)
        {
            MessageBox.Show("Please select a city", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var selectedCity = listBoxCities.SelectedItem.ToString();
        var coordinates = selectedCity.Split(',');

        if (coordinates.Length != 3)
        {
            MessageBox.Show("Invalid city data", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        double latitude = Convert.ToDouble(coordinates[1], new System.Globalization.CultureInfo("en-US"));
        double longitude = Convert.ToDouble(coordinates[2], new System.Globalization.CultureInfo("en-US"));

        var weather = await GetWeatherDataAsync(latitude, longitude);
        if (weather != null)
        {
            DisplayWeatherInfo(weather);
        }
        else
        {
            labelWeatherInfo.Text = "Failed to fetch weather data";
        }

        Refresh();
    }

    private async Task<List<string>> ReadCitiesFromFileAsync(string filePath)
    {
        try
        {
            List<string> cities = new List<string>();

            using (StreamReader reader = new StreamReader(filePath))
            {
                while (!reader.EndOfStream)
                {
                    string line = await reader.ReadLineAsync();
                    string[] parts = line.Split('\t');

                    if (parts.Length == 2)
                    {
                        string cityName = parts[0].Trim();
                        string coordinates = parts[1].Trim();

                        if (IsValidCoordinates(coordinates))
                        {
                            cities.Add($"{cityName},{coordinates}");
                        }
                        else
                        {
                            MessageBox.Show($"Invalid coordinates format in line: {line}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show($"Invalid line format: {line}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }

            return cities;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error reading cities from file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return new List<string>();
        }
    }

    private bool IsValidCoordinates(string coordinates)
    {
        if (Regex.IsMatch(coordinates, @"^\s*-?\d+(\.\d+)?,\s*-?\d+(\.\d+)?\s*$"))
        {
            return true;
        }
        return false;
    }

    private async Task<Weather> GetWeatherDataAsync(double latitude, double longitude)
    {
        using (HttpClient client = new HttpClient())
        {
            int maxAttempts = 10;
            int attempt = 0;

            while (attempt < maxAttempts)
            {
                try
                {
                    var response = await client.GetStringAsync($"{apiUrl}?lat={latitude}&lon={longitude}&appid={apiKey}&units=metric");
                    var weatherInfo = JsonSerializer.Deserialize<WeatherInfo>(response);

                    if (weatherInfo != null && !string.IsNullOrEmpty(weatherInfo.sys.country))
                    {
                        string country = weatherInfo.sys.country;
                        string name = weatherInfo.name;
                        double temp = weatherInfo.main.temp;
                        string description = weatherInfo.weather[0].description;

                        return new Weather
                        {
                            Country = country,
                            Name = name,
                            Temp = temp,
                            Description = description
                        };
                    }
                }
                catch (HttpRequestException ex)
                {
                    MessageBox.Show($"HTTP request error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    attempt++;
                }
                catch (JsonException ex)
                {
                    MessageBox.Show($"JSON deserialization error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    attempt++;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    attempt++;
                }
            }

            return null;
        }
    }

    private void DisplayWeatherInfo(Weather weather)
    {
        if (weather != null)
        {
            string weatherInfoText = $"Country: {weather.Country}\nCity: {weather.Name}\nTemperature: {weather.Temp}°C\nDescription: {weather.Description}";
            labelWeatherInfo.Text = weatherInfoText;
        }
        else
        {
            labelWeatherInfo.Text = "Weather data not available.";
        }
    }
}
static class lab9task2
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new AppWeather());
    }
}
