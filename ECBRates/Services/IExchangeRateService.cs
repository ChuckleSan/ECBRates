using ECBRates.Models;

namespace ECBRates.Services
{
    public interface IExchangeRateService
    {
        Task<List<string>> GetCurrenciesAsync();
        Task<List<ExchangeRate>> GetRatesAsync(string baseCurrency, DateTime? startDate = null, DateTime? endDate = null);
    }
}