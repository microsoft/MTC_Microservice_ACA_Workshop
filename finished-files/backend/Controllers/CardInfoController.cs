using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Dapr.Client;
using Microsoft.AspNetCore.Authorization;

namespace backend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CardInfoController : ControllerBase
    {
        private static CardInfo[] TheMenu = new[]
        {
            new CardInfo { cardName = "Birthday Card", cardColor = "White", cardId = "12345", cardTheme = "Birthday"},
            new CardInfo { cardName = "Friendship Card", cardColor = "Yellow", cardId = "23456", cardTheme = "Friendship"},
            new CardInfo { cardName = "Father's Day Card", cardColor = "Orange", cardId = "34567", cardTheme = "Father's Day"},
            new CardInfo { cardName = "Mother's Day Card", cardColor = "Green", cardId = "45678", cardTheme = "Mother's Day"},
            new CardInfo { cardName = "Congratulations Card", cardColor = "Black", cardId = "56789", cardTheme = "Congratulations"}
        };

        private readonly ILogger<CardInfoController> _logger;
        private IConfiguration _configuration;
        private const string DAPR_STORE_NAME = "hallcos";
        private readonly DaprClient _daprClient;

        public CardInfoController(ILogger<CardInfoController> logger, IConfiguration Configuration, DaprClient daprClient)
        {
            _logger = logger;
            _configuration = Configuration;
            _daprClient = daprClient;
        }

        [HttpGet]
        [Authorize(Roles = "Api.ReadOnly,Api.ReadWrite")]
        public IEnumerable<CardInfo> Get()
        {
            return TheMenu;
        }
        
        [HttpPost("cards")]
        [Authorize(Roles = "Api.ReadWrite")]
        public async Task<IActionResult> OrderReceived([FromBody] CardInfo cardInfo)
        {
            _logger.LogInformation("Received new card at: '{0}' card Id: '{1}'" , DateTime.UtcNow, cardInfo.cardId);            
            await _daprClient.SaveStateAsync(DAPR_STORE_NAME, cardInfo.cardId, JsonConvert.SerializeObject(cardInfo));
            
            //Return 200 ok to acknowledge order is processed successfully          
            return Ok($"Order Processing completed successfully");
        }
    }
}