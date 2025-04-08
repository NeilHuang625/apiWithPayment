using cakeshop_api.Services;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;

namespace cakeshop_api.Controllers{
    [ApiController]
    [Route("api/[controller]")]
    public class WebhooksController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly OrderService _orderService;
        private readonly string _webhookSecret;
        private readonly ILogger<WebhooksController> _logger;
        
        public WebhooksController(
            IConfiguration configuration,
            OrderService orderService, ILogger<WebhooksController> logger)
        {
            _configuration = configuration;
            _orderService = orderService;
            _webhookSecret = _configuration["Stripe:WebhookSecret"];
            _logger = logger;
        }
        
        [HttpPost]
        public async Task<IActionResult> HandleWebhook()
        {
            // Testing webhook
            Console.WriteLine("Webhook received");
            _logger.LogInformation("Webhook secret: {WebhookSecret}", _webhookSecret);

            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            
            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    _webhookSecret,
                    throwOnApiVersionMismatch: false
                );

                _logger.LogInformation("Webhook event: {Event}", stripeEvent);
                
                // Handle the checkout.session.completed event
                if (stripeEvent.Type == "checkout.session.completed")
                {
                    var session = stripeEvent.Data.Object as Session;
                    _logger.LogInformation("Session ID: {SessionId}", session.Id);
                    _logger.LogInformation("Session metadata: {Metadata}", string.Join(", ", session.Metadata.Select(kv => $"{kv.Key}={kv.Value}")));

                    // Fulfill the purchase
                    await _orderService.FulfillOrder(
                        session.Metadata["PendingOrderId"],
                        session.PaymentIntentId,
                        session.CustomerId
                    );
                }
                
                return Ok();
            }
            catch (StripeException e)
            {
                _logger.LogError("❌ Stripe webhook exception: {Message}", e.Message);
                _logger.LogError("❌ Full exception: {Exception}", e.ToString());
                return BadRequest(e.Message);
            }
        }
    }
}