
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using System;
using System.Text;
using Dapr.Client;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Web;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace backend
{
    public class ConsumerService : BackgroundService
    {
        private readonly ISubscriptionClient _subscriptionClient;
        private const string DAPR_STORE_NAME = "hallcos";
        private readonly DaprClient _daprClient;
        private readonly IConfiguration _configuration;

        public ConsumerService(ISubscriptionClient subscriptionClient, DaprClient daprClient, IConfiguration configuration)
        {
            _subscriptionClient = subscriptionClient;
            _daprClient = daprClient;
            _configuration = configuration;
        }
    
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _subscriptionClient.RegisterMessageHandler(async (message, token) =>
                {
                    var cardInfo = JsonConvert.DeserializeObject<CardInfo>(Encoding.UTF8.GetString(message.Body));
                    Console.WriteLine("Deserialized Message CardInfo: " + cardInfo.cardId);

                    Console.WriteLine("JSON Serialize: " + JsonConvert.SerializeObject(cardInfo));
                    Console.WriteLine("SERVICE_URL: " + _configuration.GetValue<string>("SERVICE_URL"));

                    string appToken = GetToken(_configuration.GetValue<string>("CLIENT_SECRET"), _configuration.GetValue<string>("CLIENT_ID"),
                                                _configuration.GetValue<string>("AUTHORITY"), _configuration.GetValue<string>("RESOURCE"));
                    Console.WriteLine("Token: " + appToken);
                    

                    // var httpClient = DaprClient.CreateInvokeHttpClient();
                    HttpClient httpClient = new HttpClient();
                    httpClient.BaseAddress = new System.Uri(_configuration.GetValue<string>("SERVICE_URL"));
                    httpClient.DefaultRequestHeaders.ExpectContinue = false;
                    httpClient.Timeout = TimeSpan.FromSeconds(10);
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/ls+json"));
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", appToken);

                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "cardinfo/cards");
                    request.Content = new StringContent(JsonConvert.SerializeObject(cardInfo), Encoding.UTF8, "application/ls+json");

                    await httpClient.SendAsync(request).ContinueWith(responseTask => {
                            Console.WriteLine("Response: {0}", responseTask.Result);
                    });


                    await _subscriptionClient.CompleteAsync(message.SystemProperties.LockToken);
                }, new MessageHandlerOptions(args => Task.CompletedTask)
                {
                    AutoComplete = false,
                    MaxConcurrentCalls = 10
            });
            } catch (Exception e)
            {
                Console.WriteLine("Error in Consume service :: " + e.StackTrace);

            }
            await Task.CompletedTask;
        }

        private string GetToken(string clientSecret, string clientId, string authority, string resource)
        {  
            AuthenticationContext authenticationContext = new AuthenticationContext(authority);
            ClientCredential clientCredential = new ClientCredential(clientId, clientSecret);

            return authenticationContext.AcquireTokenAsync(resource, clientCredential).Result.AccessToken;
        }
    }
}