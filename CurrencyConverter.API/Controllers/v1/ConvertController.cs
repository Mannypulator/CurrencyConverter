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
    public class ConvertController : ControllerBase
    {
        private readonly ICurrencyConversionService _conversionService;
        private readonly ILogger<ConvertController> _logger;

        public ConvertController(ICurrencyConversionService conversionService, ILogger<ConvertController> logger)
        {
            _conversionService = conversionService;
            _logger = logger;
        }

        /// <summary>
        /// Convert currency amount from one currency to another
        /// </summary>
        /// <param name="request">Currency conversion request</param>
        /// <returns>Conversion result with exchange rate and converted amount</returns>
        /// <response code="200">Conversion successful</response>
        /// <response code="400">Invalid request parameters</response>
        /// <response code="404">Currency not found</response>
        /// <response code="429">Rate limit exceeded</response>
        [HttpPost]
        [ProducesResponseType(typeof(ConversionResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(429)]
        public async Task<ActionResult<ConversionResponseDto>> Convert([FromBody] ConversionRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _conversionService.ConvertCurrencyAsync(request);

                if (result == null)
                {
                    return NotFound("Currency pair not found or conversion not possible");
                }

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during currency conversion");
                return StatusCode(500, "An error occurred during currency conversion");
            }
        }

        /// <summary>
        /// Convert currency amount for a specific historical date
        /// </summary>
        /// <param name="request">Currency conversion request with date</param>
        /// <returns>Historical conversion result</returns>
        [HttpPost("historical")]
        [ProducesResponseType(typeof(ConversionResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ConversionResponseDto>> ConvertHistorical([FromBody] ConversionRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!request.Date.HasValue)
            {
                return BadRequest("Date is required for historical conversion");
            }

            if (request.Date.Value > DateTime.UtcNow.Date)
            {
                return BadRequest("Cannot convert for future dates");
            }

            try
            {
                var result = await _conversionService.ConvertCurrencyAsync(request);

                if (result == null)
                {
                    return NotFound("Historical exchange rate not found for the specified date and currency pair");
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during historical currency conversion");
                return StatusCode(500, "An error occurred during historical currency conversion");
            }
        }

        /// <summary>
        /// Get current exchange rate between two currencies
        /// </summary>
        /// <param name="from">Source currency code</param>
        /// <param name="to">Target currency code</param>
        /// <returns>Current exchange rate</returns>
        [HttpGet("rate")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> GetExchangeRate(
            [FromQuery, Required] string from,
            [FromQuery, Required] string to)
        {
            if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
            {
                return BadRequest("Both 'from' and 'to' currency codes are required");
            }

            if (from.Length != 3 || to.Length != 3)
            {
                return BadRequest("Currency codes must be 3 characters long");
            }

            try
            {
                var rate = await _conversionService.GetExchangeRateAsync(from.ToUpper(), to.ToUpper());

                if (!rate.HasValue)
                {
                    return NotFound($"Exchange rate not found for {from.ToUpper()}/{to.ToUpper()}");
                }

                return Ok(new
                {
                    fromCurrency = from.ToUpper(),
                    toCurrency = to.ToUpper(),
                    exchangeRate = rate.Value,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting exchange rate for {From}/{To}", from, to);
                return StatusCode(500, "An error occurred while retrieving the exchange rate");
            }
        }
    }
}
