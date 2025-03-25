using ECBRates.Models;
using ECBRates.Services;

using Microsoft.AspNetCore.Mvc;

namespace ECBRates.Controllers
{
    /// <summary>
    /// Controller to handle exchange rate related requests.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="ExchangeRateController"/> class.
    /// </remarks>
    /// <param name="exchangeRateService">The exchange rate service.</param>
    [ApiController]
    [Route("api/[controller]")]
    public class ExchangeRateController(IExchangeRateService exchangeRateService) : ControllerBase
    {
        private readonly IExchangeRateService _exchangeRateService = exchangeRateService;

        /// <summary>
        /// Gets a list of all available currencies from ECB.
        /// </summary>
        /// <returns>List of currency codes.</returns>
        [HttpGet("currencies")]
        public async Task<ActionResult<List<string>>> GetCurrencies()
        {
            var currencies = await _exchangeRateService.GetCurrenciesAsync();
            return Ok(currencies);
        }

        /// <summary>
        /// Gets a list of exchange rates for a specific currency within a date range.
        /// </summary>
        /// <param name="currencyCode">The currency code.</param>
        /// <param name="dtStart">The start date.</param>
        /// <param name="dtEnd">The end date.</param>
        /// <returns>List of exchange rates.</returns>
        [HttpGet("rates")]
        public async Task<ActionResult<List<ExchangeRate>>> GetCurrencyRates(
            [FromQuery] string currencyCode,
            [FromQuery] DateTime? dtStart,
            [FromQuery] DateTime? dtEnd)
        {
            if (string.IsNullOrWhiteSpace(currencyCode))
                return BadRequest("Currency code is required");

            var rates = await _exchangeRateService.GetRatesAsync(currencyCode, dtStart, dtEnd);
            return Ok(rates);
        }

        /// <summary>
        /// Gets exchange rates for a specific currency on a specific date.
        /// </summary>
        /// <param name="currencyCode">The currency code.</param>
        /// <param name="dtEff">The effective date.</param>
        /// <returns>List of exchange rates.</returns>
        [HttpGet("rates/date")]
        public async Task<ActionResult<List<ExchangeRate>>> GetCurrencyRatesByDate(
            [FromQuery] string currencyCode,
            [FromQuery] DateTime dtEff)
        {
            if (string.IsNullOrWhiteSpace(currencyCode))
                return BadRequest("Currency code is required");

            var rates = await _exchangeRateService.GetRatesAsync(currencyCode, dtEff, dtEff);
            return Ok(rates);
        }

        /// <summary>
        /// Gets the latest exchange rates for a specific currency.
        /// </summary>
        /// <param name="currencyCode">The currency code.</param>
        /// <returns>List of latest exchange rates.</returns>
        [HttpGet("rates/latest")]
        public async Task<ActionResult<List<ExchangeRate>>> GetCurrencyLatestRates(
            [FromQuery] string currencyCode)
        {
            if (string.IsNullOrWhiteSpace(currencyCode))
                return BadRequest("Currency code is required");

            var allRates = await _exchangeRateService.GetRatesAsync(currencyCode);
            var latestDate = allRates.Max(r => r.Date);
            var latestRates = allRates.Where(r => r.Date == latestDate).ToList();
            return Ok(latestRates);
        }
    }
}