using System;
using CurrencyConverter.API.Domain.Entity;

namespace CurrencyConverter.API.Application.Abstractions;

public interface ICurrencyRepository
{
    Task<Currency?> GetCurrencyAsync(string code, CancellationToken cancellationToken = default);
    Task<IEnumerable<Currency>> GetAllCurrenciesAsync(bool activeOnly = true, CancellationToken cancellationToken = default);
    Task<bool> CurrencyExistsAsync(string code, CancellationToken cancellationToken = default);
}
