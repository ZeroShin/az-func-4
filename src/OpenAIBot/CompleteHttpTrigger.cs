using System.Net;
using System.Text;
using Azure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace OpenAIBot
{
    public class CompleteHttpTrigger
    {
        private readonly ILogger _logger;

        public CompleteHttpTrigger(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<CompleteHttpTrigger>();
        }

        [Function("CompleteHttpTrigger")]
        
        [OpenApiOperation(operationId: nameof(CompleteHttpTrigger.Run), tags: new[] { "name" })]
        [OpenApiRequestBody(contentType: "text/plain", bodyType: typeof(string), Required = true, Description = "The request body")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]

        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "POST", Route = "completions")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            var prompt = req.ReadAsString();

            var endpoint = Environment.GetEnvironmentVariable("AI_EndPoint");
            var credential = Environment.GetEnvironmentVariable("AI_ApiKey");
            var modelName = Environment.GetEnvironmentVariable("AI_Model");

            using (var httpClient = new HttpClient())
            {
                // HTTP 요청 헤더 설정
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {credential}");
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Azure Function");

                // API 요청 데이터 생성
                var requestDataObject = new
                {
                    messages = new[]
                    {
                        new {role = "system", content =  "You are a helpful assistant. You are very good at summarizing the given text into 2-3 bullet points."},
                        new {role = "user", content = prompt}
                    },
                    model = modelName,
                    max_tokens = 800,
                    temperature = 0.7f,
                };

                // JSON으로 직렬화
                string jsonData = JsonConvert.SerializeObject(requestDataObject);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                // // API 호출 및 응답 처리
                var responseGPT = httpClient.PostAsync(endpoint, content).Result;
                string responseBody = responseGPT.Content.ReadAsStringAsync().Result;

                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

                response.WriteString(responseBody);

                return response;
            }
        }
    }
}
