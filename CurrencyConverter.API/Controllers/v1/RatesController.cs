using System.ComponentModel.DataAnnotations;
using CurrencyConverter.API.Application.Abstractions;
using CurrencyConverter.API.Domain.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyConverter.API.Controllers.v1
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [ApiVersion("1.0")]
    [Produces("application/json")]
    public class RatesController : ControllerBase
    {
        private readonly ICurrencyConversionService _conversionService;
        private readonly ILogger<RatesController> _logger;

        public RatesController(ICurrencyConversionService conversionService, ILogger<RatesController> logger)
        {
            _conversionService = conversionService;
            _logger = logger;
        }

        /// <summary>
        /// Get historical exchange rates for a currency pair within a date range
        /// </summary>
        /// <param name="baseCurrency">Base currency code</param>
        /// <param name="targetCurrency">Target currency code</param>
        /// <param name="startDate">Start date (YYYY-MM-DD)</param>
        /// <param name="endDate">End date (YYYY-MM-DD)</param>
        /// <returns>Historical exchange rates</returns>
        [HttpGet("historical")]
        [ProducesResponseType(typeof(HistoricalRatesResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<HistoricalRatesResponseDto>> GetHistoricalRates(
            [FromQuery, Required] string baseCurrency,
            [FromQuery, Required] string targetCurrency,
            [FromQuery, Required] DateTime startDate,
            [FromQuery, Required] DateTime endDate)
        {
            if (string.IsNullOrWhiteSpace(baseCurrency) || string.IsNullOrWhiteSpace(targetCurrency))
            {
                return BadRequest("Both baseCurrency and targetCurrency are required");
            }

            if (baseCurrency.Length != 3 || targetCurrency.Length != 3)
            {
                return BadRequest("Currency codes must be 3 characters long");
            }

            if (startDate > endDate)
            {
                return BadRequest("Start date cannot be after end date");
            }

            if (endDate > DateTime.UtcNow.Date)
            {
                return BadRequest("End date cannot be in the future");
            }

            // Limit the date range to prevent excessive data retrieval
            if ((endDate - startDate).TotalDays > 365)
            {
                return BadRequest("Date range cannot exceed 365 days");
            }

            try
            {
                var request = new HistoricalRatesRequestDto
                {
                    BaseCurrency = baseCurrency.ToUpper(),
                    TargetCurrency = targetCurrency.ToUpper(),
                    StartDate = startDate.Date,
                    EndDate = endDate.Date
                };

                var result = await _conversionService.GetHistoricalRatesAsync(request);

                if (result == null || !result.Rates.Any())
                {
                    return NotFound($"No historical rates found for {baseCurrency.ToUpper()}/{targetCurrency.ToUpper()} in the specified date range");
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving historical rates for {Base}/{Target}", baseCurrency, targetCurrency);
                return StatusCode(500, "An error occurred while retrieving historical rates");
            }
        }

        /// <summary>
        /// Get exchange rate for a specific historical date
        /// </summary>
        /// <param name="baseCurrency">Base currency code</param>
        /// <param name="targetCurrency">Target currency code</param>
        /// <param name="date">Specific date (YYYY-MM-DD)</param>
        /// <returns>Exchange rate for the specified date</returns>
        [HttpGet("historical/date")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> GetHistoricalRateForDate(
            [FromQuery, Required] string baseCurrency,
            [FromQuery, Required] string targetCurrency,
            [FromQuery, Required] DateTime date)
        {
            if (string.IsNullOrWhiteSpace(baseCurrency) || string.IsNullOrWhiteSpace(targetCurrency))
            {
                return BadRequest("Both baseCurrency and targetCurrency are required");
            }

            if (date > DateTime.UtcNow.Date)
            {
                return BadRequest("Date cannot be in the future");
            }

            try
            {
                var rate = await _conversionService.GetExchangeRateAsync(baseCurrency.ToUpper(), targetCurrency.ToUpper(), date.Date);

                if (!rate.HasValue)
                {
                    return NotFound($"No exchange rate found for {baseCurrency.ToUpper()}/{targetCurrency.ToUpper()} on {date:yyyy-MM-dd}");
                }

                return Ok(new
                {
                    baseCurrency = baseCurrency.ToUpper(),
                    targetCurrency = targetCurrency.ToUpper(),
                    date = date.Date,
                    exchangeRate = rate.Value
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting historical rate for {Base}/{Target} on {Date}", baseCurrency, targetCurrency, date);
                return StatusCode(500, "An error occurred while retrieving the historical rate");
            }
        }
    }
}
