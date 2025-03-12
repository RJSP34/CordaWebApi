using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;
using System.Text;
using WebApiWithDocker.Data;
using Microsoft.AspNetCore.Cors;
using System.Net.Http.Headers;
using System.Security.Authentication;

namespace WebApiWithDocker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableCors("AllowAll")]
    public class Cordar : ControllerBase
    {
        private readonly ILogger<Cordar> _logger;
        private readonly IConfiguration _configuration;

        public Cordar(ILogger<Cordar> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        [HttpGet(Name = "Hello")]
        public IActionResult HandleGetRequest()
        {
            var response = new DecryptionResponse { decryptedText = "Hello" };
            return Ok(response);
        }

        [HttpPost(Name = "Document/Encript")]
        public async Task<IActionResult> HandlePostRequest([FromBody] DocumentEncriptHTTPBody documentEnc)
        {
            try
            {
                var httpClientHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true
                };

                using (var httpClient = new HttpClient(httpClientHandler))
                {
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var apiUrl = _configuration.GetValue(key: "KOTLAPI", defaultValue: "http://127.0.0.1");
                    var requestUrl = $"{apiUrl}decrypt";
                    var requestBody = System.Text.Json.JsonSerializer.Serialize(documentEnc);
                    var content = new StringContent(requestBody, System.Text.Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync(requestUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        var responseData = System.Text.Json.JsonSerializer.Deserialize<DecryptionResponse>(responseBody);
                        return Ok(new { data = responseData?.decryptedText });
                    }
                    else
                    {
                        return BadRequest($"HTTP request failed with status code {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions that occur during the request
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("flow/{x}")]
        public async Task<IActionResult> CreateFlowc(string x)
        {
            try
            {
                string authHeader = Request.Headers.Authorization;
                if (authHeader != null && authHeader.StartsWith("Basic ") && x != null && x != string.Empty && x != "undefined")
                {
                    string encodedCredentials = authHeader["Basic ".Length..].Trim();
                    string[] credentials = Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredentials)).Split(':');
                    string username = credentials[0];
                    string password = credentials[1];

                    string cordaApiUrl = _configuration.GetValue(key: "CORDAAPI", defaultValue: "http://127.0.0.1");
                    string apiUrl = $"{cordaApiUrl}flow/{x}";

                    string requestBody;
                    using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
                    {
                        requestBody = await reader.ReadToEndAsync();
                    }

                    ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;

                    WebRequest request = WebRequest.Create(apiUrl);
                    request.Method = "POST";
                    request.Headers["Authorization"] = $"Basic {Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"))}";
                    request.ContentType = "application/json"; // Specify content type as JSON

                    byte[] postData = Encoding.UTF8.GetBytes(requestBody);

                    using (Stream requestStream = request.GetRequestStream())
                    {
                        requestStream.Write(postData, 0, postData.Length);
                    }

                    try
                    {
                        WebResponse response = await request.GetResponseAsync();
                        using (Stream dataStream = response.GetResponseStream())
                        {
                            StreamReader reader = new StreamReader(dataStream);
                            string responseBody = reader.ReadToEnd();
                            var flowResultOBJ = System.Text.Json.JsonSerializer.Deserialize<FlowResultClass>(responseBody);

                            switch (flowResultOBJ.flowStatus)
                            {
                                case "START_REQUESTED":
                                    break;
                                default:
                                    return Ok(new { status = "error", error = "Error made in Flow Request.", flow_results = flowResultOBJ });
                            }

                            for (int i = 0; i < 3; i++)
                            {
                                int maxAttempts = 10;
                                int currentAttempt = 0;
                                int retryDelayMilliseconds = 500;
                                var httpClientHandler = new HttpClientHandler
                                {
                                    SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls,
                                    ServerCertificateCustomValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true
                                };

                                using (var httpClient = new HttpClient(httpClientHandler))
                                {
                                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}")));
                                    apiUrl = $"{cordaApiUrl}flow/{x}/{flowResultOBJ.clientRequestId}";
                                    await Task.Delay(retryDelayMilliseconds);

                                    while (currentAttempt < maxAttempts)
                                    {
                                        try
                                        {
                                            HttpResponseMessage response2 = await httpClient.GetAsync(apiUrl);

                                            if (response2.IsSuccessStatusCode)
                                            {
                                                string responseBody2 = await response2.Content.ReadAsStringAsync();
                                                var flowResultOBJ2 = System.Text.Json.JsonSerializer.Deserialize<FlowResultClass>(responseBody2);

                                                switch (flowResultOBJ2.flowStatus)
                                                {
                                                    case "COMPLETED":
                                                        return Ok(new { status = "completed", error = "", flow_results = flowResultOBJ2 });
                                                    case "RUNNING":
                                                    case "START_REQUESTED":
                                                        await Task.Delay(retryDelayMilliseconds);
                                                        currentAttempt++;
                                                        continue;
                                                    default:
                                                        return Ok(new { status = "error", error = "Error occurred in Flow Request.", flow_results = flowResultOBJ2.flowError?.message });
                                                }
                                            }
                                            else
                                            {
                                                _logger.LogError($"Request failed with status code: {response2.StatusCode}");
                                                return BadRequest(new { error = "Error occurred while making the request" });
                                            }
                                        }
                                        catch (HttpRequestException ex)
                                        {
                                            _logger.LogError($"Request error: {ex.Message}");
                                            return BadRequest(new { error = "Error occurred while making the request" });
                                        }
                                    }
                                }

                                _logger.LogError("Max retry attempts reached. Unable to make successful request.");
                                return Ok(new { status = "running", error = "Max retry attempts reached. Unable to make successful request.", flow_results = "" }); 
                            }
                            _logger.LogError("Max retry attempts reached. Unable to make successful request.");
                            return Ok(new { status = "running", error = "Max retry attempts reached. Unable to make successful request..", flow_results = "" });
                        }
                    }
                    catch (WebException ex)
                    {
                        _logger.LogError($"Request error: {ex.Message}");
                        return BadRequest(new { error = "Error occurred while making the request" });
                    }
                }
                else
                {
                    _logger.LogError("Authorization header is missing or not in the correct format");
                    return BadRequest(new { error = "Authorization header is missing or not in the correct format" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred: {ex.Message}");
                return StatusCode(500, new { error = "An error occurred while processing the request" });
            }
        }

        [HttpGet("getVirtualNodes")]
        public async Task<ActionResult<string>> GetVirtualNodesAsync()
        {
            try
            {
                string authHeader = Request.Headers.Authorization.ToString();
                if (authHeader != null && authHeader.StartsWith("Basic "))
                {
                    string encodedCredentials = authHeader["Basic ".Length..].Trim();
                    string[] credentials = Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredentials)).Split(':');
                    string username = credentials[0];
                    string password = credentials[1];

                    string cordaApiUrl = _configuration.GetValue<string>("CORDAAPI", "http://127.0.0.1");
                    string apiUrl = $"{cordaApiUrl}virtualnode"; // corrected endpoint URL
                    ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;

                    WebRequest request = WebRequest.Create(apiUrl);
                    request.Method = "GET";
                    request.Headers["Authorization"] = $"Basic {Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"))}";

                    try
                    {
                        WebResponse response = await request.GetResponseAsync();
                        using (Stream dataStream = response.GetResponseStream())
                        {
                            StreamReader reader = new StreamReader(dataStream);
                            string responseBody = reader.ReadToEnd();
                            JsonDocument jsonDocument = JsonDocument.Parse(responseBody);

                            JsonElement root = jsonDocument.RootElement;

                            string virtualNodesString = root.GetProperty("virtualNodes").ToString();
                            return virtualNodesString;
                        }
                    }
                    catch (WebException ex)
                    {
                        _logger.LogError($"Request error: {ex.Message}");
                        return BadRequest(new { error = "Error occurred while making the request" });
                    }
                }
                else
                {
                    _logger.LogError("Authorization header is missing or not in the correct format");
                    return BadRequest("Authorization header is missing or not in the correct format");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching virtual nodes: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
