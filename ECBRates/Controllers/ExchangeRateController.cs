using ECBRates.Models;
using ECBRates.Services;

using Microsoft.AspNetCore.Mvc;

namespace ECBRates.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExchangeRateController : ControllerBase
    {
        private readonly IExchangeRateService _exchangeRateService;

        public ExchangeRateController(IExchangeRateService exchangeRateService)
        {
            _exchangeRateService = exchangeRateService;
        }

        /// <summary>
        /// Gets a list of all available currencies from ECB
        /// </summary>
        /// <returns>List of currency codes</returns>
        [HttpGet("currencies")]
        public async Task<ActionResult<List<string>>> GetCurrencies()
        {
            var currencies = await _exchangeRateService.GetCurrenciesAsync();
            return Ok(currencies);
        }

        /// <summary>
        /// Gets a list of all available currencies from ECB
        /// </summary>
        /// <returns>List of currency codes</returns>
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
        /// Gets a list of all available currencies from ECB
        /// </summary>
        /// <returns>List of currency codes</returns>
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
        /// Gets a list of all available currencies from ECB
        /// </summary>
        /// <returns>List of currency codes</returns>
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