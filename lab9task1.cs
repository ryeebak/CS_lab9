using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;

class lab9task1
{
    static readonly Mutex mutex = new Mutex();

    static async Task Main()
    {
        List<string> tickers = new List<string>();

        using (StreamReader reader = new StreamReader("ticker.txt"))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                tickers.Add(line);
            }
        }

        using (HttpClient client = new HttpClient())
        {
            List<Task> tasks = new List<Task>();

            foreach (string ticker in tickers)
            {
                tasks.Add(GetDataForTicker(client, ticker));
            }

            await Task.WhenAll(tasks);
        }
    }

    static async Task GetDataForTicker(HttpClient client, string ticker)
    {
        try
        {
            DateTime startDate = DateTime.Now.AddYears(-1);
            DateTime endDate = DateTime.Now;

            long startUnixTime = ((DateTimeOffset)startDate).ToUnixTimeSeconds();
            long endUnixTime = ((DateTimeOffset)endDate).ToUnixTimeSeconds();

            string url = $"https://query1.finance.yahoo.com/v7/finance/download/{ticker}?period1={startUnixTime}&period2={endUnixTime}&interval=1d&events=history&includeAdjustedClose=true";

            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string csvData = await response.Content.ReadAsStringAsync();

            string[] lines = csvData.Split('\n');
            double totalAveragePrice = 0.0;
            int totalRowCount = 0;

            for (int i = 1; i < lines.Length - 1; i++)
            {
                try
                {
                    string[] values = lines[i].Split(',');

                    double high = Convert.ToDouble(values[2], new System.Globalization.CultureInfo("en-US"));
                    double low = Convert.ToDouble(values[3], new System.Globalization.CultureInfo("en-US"));

                    double averagePrice = (high + low) / 2;

                    totalAveragePrice += averagePrice;
                    totalRowCount++;

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при обработке строки для {ticker}: {ex.Message}");
                }

            }

            if (totalRowCount > 0)
            {
                double totalAverage = totalAveragePrice / totalRowCount;
                string result = $"{ticker}:{totalAverage}";

                mutex.WaitOne();
                try
                {
                    File.AppendAllText("results.txt", result + Environment.NewLine);
                }
                finally
                {
                    mutex.ReleaseMutex();
                }

                Console.WriteLine($"Средняя цена акции для {ticker} за год: {totalAverage}");
            }
            else
            {
                Console.WriteLine($"Для {ticker} нет данных за год.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при обработке {ticker}: {ex.Message}");
        }
    }
}
