namespace ECBRates.Models
{
    public class ExchangeRate
    {
        public string CurrencyCode { get; set; }
        public decimal Rate { get; set; }
        public DateTime Date { get; set; }
    }
}