using CurrencyConverter.API.Application.Abstractions;
using CurrencyConverter.API.Domain.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyConverter.API.Controllers.v2
{
    [ApiController]
    [Route("api/v2/[controller]")]
    [ApiVersion("2.0")]
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
        /// Convert currency with enhanced response including additional metadata
        /// </summary>
        /// <param name="request">Currency conversion request</param>
        /// <returns>Enhanced conversion result with metadata</returns>
        [HttpPost]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> Convert([FromBody] ConversionRequestDto request)
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

                // Enhanced response for V2
                var enhancedResponse = new
                {
                    conversion = result,
                    metadata = new
                    {
                        apiVersion = "2.0",
                        processingTime = DateTime.UtcNow,
                        rateSource = request.Date.HasValue ? "historical" : "real-time",
                        disclaimer = "Exchange rates are for informational purposes only and may not reflect actual trading rates"
                    }
                };

                return Ok(enhancedResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during currency conversion V2");
                return StatusCode(500, "An error occurred during currency conversion");
            }
        }

        /// <summary>
        /// Batch currency conversion
        /// </summary>
        /// <param name="requests">List of conversion requests</param>
        /// <returns>List of conversion results</returns>
        [HttpPost("batch")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult> BatchConvert([FromBody] List<ConversionRequestDto> requests)
        {
            if (requests == null || !requests.Any())
            {
                return BadRequest("At least one conversion request is required");
            }

            if (requests.Count > 10)
            {
                return BadRequest("Maximum 10 conversions allowed per batch");
            }

            var results = new List<object>();

            foreach (var request in requests)
            {
                try
                {
                    var result = await _conversionService.ConvertCurrencyAsync(request);
                    results.Add(new
                    {
                        success = result != null,
                        conversion = result,
                        error = result == null ? "Conversion failed" : null
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in batch conversion for {From} to {To}", request.FromCurrency, request.ToCurrency);
                    results.Add(new
                    {
                        success = false,
                        conversion = (ConversionResponseDto?)null,
                        error = "Conversion failed due to an error"
                    });
                }
            }

            return Ok(new
            {
                results = results,
                metadata = new
                {
                    apiVersion = "2.0",
                    totalRequests = requests.Count,
                    successfulConversions = results.Count(r => ((dynamic)r).success),
                    processingTime = DateTime.UtcNow
                }
            });
        }
    }
}
