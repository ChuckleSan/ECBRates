using System.Globalization;
using System.Xml;

using ECBRates.Models;

namespace ECBRates.Services
{
    public class ExchangeRateService : IExchangeRateService
    {
        private readonly HttpClient _httpClient;
        private const string ECB_URL = "http://www.ecb.europa.eu/stats/eurofxref/eurofxref-hist.xml";

        public ExchangeRateService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<string>> GetCurrenciesAsync()
        {
            var xmlDoc = await LoadXmlDocumentAsync();
            var currencies = xmlDoc.SelectNodes("//*[@currency]")
                .Cast<XmlNode>()
                .Select(n => n.Attributes["currency"].Value)
                .Distinct()
                .ToList();

            // Add EUR as it's the base currency
            currencies.Add("EUR");
            return currencies.OrderBy(c => c).ToList();
        }

        public async Task<List<ExchangeRate>> GetRatesAsync(string baseCurrency, DateTime? startDate = null, DateTime? endDate = null)
        {
            var xmlDoc = await LoadXmlDocumentAsync();
            var rates = new List<ExchangeRate>();

            var cubeNodes = xmlDoc.SelectNodes("//*[@time]");
            foreach (XmlNode timeNode in cubeNodes)
            {
                var date = DateTime.Parse(timeNode.Attributes["time"].Value);

                // Apply date filters if provided
                if (startDate.HasValue && date < startDate.Value) continue;
                if (endDate.HasValue && date > endDate.Value) continue;

                var rateNodes = timeNode.SelectNodes("*[@currency]");
                var euroRates = rateNodes.Cast<XmlNode>()
                    .ToDictionary(
                        n => n.Attributes["currency"].Value,
                        n => decimal.Parse(n.Attributes["rate"].Value, CultureInfo.InvariantCulture));

                // Add EUR with rate 1.0
                euroRates["EUR"] = 1.0m;

                // Convert rates to the requested base currency
                if (!euroRates.ContainsKey(baseCurrency))
                    throw new ArgumentException($"Currency {baseCurrency} not found");

                var baseRate = euroRates[baseCurrency];
                foreach (var rate in euroRates)
                {
                    rates.Add(new ExchangeRate
                    {
                        CurrencyCode = rate.Key,
                        Rate = rate.Value / baseRate,
                        Date = date
                    });
                }
            }

            return rates.OrderBy(r => r.Date).ThenBy(r => r.CurrencyCode).ToList();
        }

        private async Task<XmlDocument> LoadXmlDocumentAsync()
        {
            var xmlString = await _httpClient.GetStringAsync(ECB_URL);
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlString);
            return xmlDoc;
        }
    }
}