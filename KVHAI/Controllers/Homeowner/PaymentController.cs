using KVHAI.Models;
using KVHAI.Repository;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json.Nodes;

namespace KVHAI.Controllers.Homeowner
{
    public class PaymentController : Controller
    {
        private string PaypalClientId { get; set; }
        private string PaypalSecret { get; set; }
        private string PaypalUrl { get; set; }
        private readonly PaymentRepository _paymentRepo;

        public PaymentController(IConfiguration configuration, PaymentRepository paymentRepository)
        {
            PaypalClientId = configuration["PayPal:ClientId"]!;
            PaypalSecret = configuration["PayPal:ClientSecret"]!;
            PaypalUrl = configuration["PayPal:Url"]!;

            _paymentRepo = paymentRepository;

        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<string> Token()
        {
            return await GetPaypalAccessToken();
        }

        [HttpPost]
        public async Task<JsonResult> CreateOrder([FromBody] JsonObject data)
        {
            var totalAmount = data?["amount"]?.ToString();
            if (totalAmount == null)
            {
                return new JsonResult(new { Id = "" });
            }

            //create the request body
            JsonObject createOrderRequest = new JsonObject();
            createOrderRequest.Add("intent", "CAPTURE");

            JsonObject amount = new JsonObject();
            amount.Add("currency_code", "PHP");
            amount.Add("value", totalAmount);

            JsonObject purchaseUnit1 = new JsonObject();
            purchaseUnit1.Add("amount", amount);

            JsonArray purchaseUnits = new JsonArray();
            purchaseUnits.Add(purchaseUnit1);

            createOrderRequest.Add("purchase_units", purchaseUnits);

            //get access token
            string accessToken = await GetPaypalAccessToken();

            //send request 
            string url = PaypalUrl + "/v2/checkout/orders";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);

                var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
                requestMessage.Content = new StringContent(createOrderRequest.ToString(), null, "application/json");
                var httpResponse = await client.SendAsync(requestMessage);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var strResponse = await httpResponse.Content.ReadAsStringAsync();
                    var jsonResponse = JsonNode.Parse(strResponse);

                    if (jsonResponse != null)
                    {
                        string paypalOrderId = jsonResponse["id"]?.ToString() ?? "";

                        return new JsonResult(new { Id = paypalOrderId });

                    }
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    // Log error details here for easier debugging
                    Console.WriteLine($"CreateOrder failed: {errorContent}");
                    return new JsonResult(errorContent);

                }

            }

            return new JsonResult(new { Id = "" });
        }

        [HttpPost]
        public async Task<JsonResult> CompleteOrder([FromBody] JsonObject data)
        {
            var orderID = data?["orderID"]?.ToString();
            if (orderID == null)
            {
                return new JsonResult("error");
            }

            //get access token
            string accessToken = await GetPaypalAccessToken();

            //send request 
            string url = PaypalUrl + "/v2/checkout/orders/" + orderID + "/capture";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);

                var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
                requestMessage.Content = new StringContent("", null, "application/json");
                var httpResponse = await client.SendAsync(requestMessage);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var strResponse = await httpResponse.Content.ReadAsStringAsync();
                    var jsonResponse = JsonNode.Parse(strResponse);

                    if (jsonResponse != null)
                    {
                        string paypalOrderStatus = jsonResponse["status"]?.ToString() ?? "";
                        if (paypalOrderStatus == "COMPLETED")
                        {
                            // Payer Information
                            //save the order in database
                            // Create payment object from PayPal response
                            var payment = new Payment
                            {
                                Address_ID = Convert.ToInt32(data?["addressId"]?.ToString()), // You'll need to pass this from frontend
                                Resident_ID = Convert.ToInt32(data?["residentId"]?.ToString()), // You'll need to pass this from frontend
                                Bill = decimal.Parse(data?["bill_amount"]?.ToString()),
                                Paid_Amount = decimal.Parse(jsonResponse["purchase_units"]?[0]?["payments"]?["captures"]?[0]?["amount"]?["value"]?.ToString() ?? "0"),
                                Payment_Method = "online",
                                Paid_By = jsonResponse["payer"]?["name"]?["given_name"]?.ToString() + " " +
                                         jsonResponse["payer"]?["name"]?["surname"]?.ToString(),
                                // Additional PayPal specific fields
                                PayPal_OrderId = orderID,
                                PayPal_PayerId = jsonResponse["payer"]?["payer_id"]?.ToString(),
                                PayPal_PayerEmail = jsonResponse["payer"]?["email_address"]?.ToString(),
                                PayPal_TransactionId = jsonResponse["purchase_units"]?[0]?["payments"]?["captures"]?[0]?["id"]?.ToString()
                            };

                            // Use your existing InsertPayment method with modifications for PayPal
                            int paymentResult = await _paymentRepo.InsertPaymentOnline(payment);

                            if (paymentResult > 0)
                            {
                                return new JsonResult("success");
                            }

                            return new JsonResult(jsonResponse);
                        }
                    }
                }

            }

            return new JsonResult("error");

        }

        private async Task<string> GetPaypalAccessToken()
        {
            string accessToken = "";
            string url = PaypalUrl + "/v1/oauth2/token";

            using (var client = new HttpClient())
            {
                string credentials64 =
                    Convert.ToBase64String(Encoding.UTF8.GetBytes(PaypalClientId + ":" + PaypalSecret));

                client.DefaultRequestHeaders.Add("Authorization", "Basic " + credentials64);

                var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
                requestMessage.Content = new StringContent("grant_type=client_credentials", null, "application/x-www-form-urlencoded");

                var httpResponse = await client.SendAsync(requestMessage);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var strResponse = await httpResponse.Content.ReadAsStringAsync();

                    var jsonResponse = JsonNode.Parse(strResponse);
                    if (jsonResponse != null)
                    {
                        accessToken = jsonResponse["access_token"]?.ToString() ?? "";
                    }
                }
            }
            return accessToken;
        }

    }
}
