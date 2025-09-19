using CurrencyConverter.API.Application.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyConverter.API.Controllers.v1
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [ApiVersion("1.0")]
    [Produces("application/json")]
    public class CurrenciesController : ControllerBase
    {
        private readonly ICurrencyRepository _currencyRepository;
        private readonly ILogger<CurrenciesController> _logger;

        public CurrenciesController(ICurrencyRepository currencyRepository, ILogger<CurrenciesController> logger)
        {
            _currencyRepository = currencyRepository;
            _logger = logger;
        }

        /// <summary>
        /// Get all supported currencies
        /// </summary>
        /// <returns>List of supported currencies</returns>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<object>), 200)]
        public async Task<ActionResult> GetCurrencies()
        {
            try
            {
                var currencies = await _currencyRepository.GetAllCurrenciesAsync();

                var result = currencies.Select(c => new
                {
                    code = c.Code,
                    name = c.Name,
                    symbol = c.Symbol
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving currencies");
                return StatusCode(500, "An error occurred while retrieving currencies");
            }
        }

        /// <summary>
        /// Get specific currency information
        /// </summary>
        /// <param name="code">Currency code</param>
        /// <returns>Currency information</returns>
        [HttpGet("{code}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> GetCurrency(string code)
        {
            if (string.IsNullOrWhiteSpace(code) || code.Length != 3)
            {
                return BadRequest("Currency code must be 3 characters long");
            }

            try
            {
                var currency = await _currencyRepository.GetCurrencyAsync(code.ToUpper());

                if (currency == null)
                {
                    return NotFound($"Currency {code.ToUpper()} not found");
                }

                return Ok(new
                {
                    code = currency.Code,
                    name = currency.Name,
                    symbol = currency.Symbol,
                    isActive = currency.IsActive
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving currency {Code}", code);
                return StatusCode(500, "An error occurred while retrieving the currency");
            }
        }
    }
}

