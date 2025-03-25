using System.Globalization;
using System.Xml;

using ECBRates.Models;

namespace ECBRates.Services
{
    /// <summary>
    /// Service to fetch and process exchange rates from the European Central Bank.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="ExchangeRateService"/> class.
    /// </remarks>
    /// <param name="httpClient">The HTTP client to use for fetching data.</param>
    public class ExchangeRateService(HttpClient httpClient) : IExchangeRateService
    {
        private readonly HttpClient _httpClient = httpClient;
        private const string ECB_URL = "http://www.ecb.europa.eu/stats/eurofxref/eurofxref-hist.xml";

        /// <summary>
        /// Gets the list of available currencies.
        /// </summary>
        /// <returns>A list of currency codes.</returns>
        public async Task<List<string>> GetCurrenciesAsync()
        {
            var xmlDoc = await LoadXmlDocumentAsync();
            var currencyNodes = xmlDoc.SelectNodes("//*[@currency]") ?? throw new InvalidOperationException("No currency nodes found in the XML document.");
            var currencies = currencyNodes
                .Cast<XmlNode>()
                .Select(selector: static n => n.Attributes?["currency"]?.Value ?? throw new InvalidOperationException("Currency attribute is missing"))
                .Distinct()
                .ToList();

            // Add EUR as it's the base currency
            currencies.Add("EUR");
            return currencies.OrderBy(c => c).ToList();
        }

        /// <summary>
        /// Gets the exchange rates for a specified base currency and date range.
        /// </summary>
        /// <param name="baseCurrency">The base currency code.</param>
        /// <param name="startDate">The start date for the rates (optional).</param>
        /// <param name="endDate">The end date for the rates (optional).</param>
        /// <returns>A list of exchange rates.</returns>
        public async Task<List<ExchangeRate>> GetRatesAsync(string baseCurrency, DateTime? startDate = null, DateTime? endDate = null)
        {
            var xmlDoc = await LoadXmlDocumentAsync();
            var rates = new List<ExchangeRate>();

            var cubeNodes = xmlDoc.SelectNodes("//*[@time]") ?? throw new InvalidOperationException("No time nodes found in the XML document.");
            IEnumerable<(XmlNode timeNode, DateTime date)> enumerable()
            {
                foreach (XmlNode timeNode in cubeNodes)
                {
                    var date = DateTime.Parse(timeNode.Attributes?["time"]?.Value ?? throw new InvalidOperationException("Time attribute is missing"));
                    yield return (timeNode, date);
                }
            }

            foreach (var (timeNode, date) in enumerable())
            {
                // Apply date filters if provided
                if (startDate.HasValue && date < startDate.Value) continue;
                if (endDate.HasValue && date > endDate.Value) continue;
                var rateNodes = timeNode.SelectNodes("*[@currency]");
                var euroRates = rateNodes.Cast<XmlNode>()
                                    .ToDictionary(
                                        n => n.Attributes?["currency"]?.Value ?? throw new InvalidOperationException("Currency attribute is missing"),
                                        n => decimal.Parse(n.Attributes?["rate"]?.Value ?? throw new InvalidOperationException("Rate attribute is missing"), CultureInfo.InvariantCulture));
                // Add EUR with rate 1.0
                euroRates["EUR"] = 1.0m;
                // Convert rates to the requested base currency
                if (!euroRates.TryGetValue(baseCurrency, out decimal baseRate))
                    throw new ArgumentException($"Currency {baseCurrency} not found");
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

        /// <summary>
        /// Loads the XML document from the ECB URL.
        /// </summary>
        /// <returns>The loaded XML document.</returns>
        private async Task<XmlDocument> LoadXmlDocumentAsync()
        {
            var xmlString = await _httpClient.GetStringAsync(ECB_URL);
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlString);
            return xmlDoc;
        }
    }
}