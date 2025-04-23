using Contracts.Custom;
using Newtonsoft.Json;
using RestSharp;
using System.Net;

namespace BecaWebService.Services.Custom
{
    public class SharedService : ISharedService
    {
        private readonly RestClient _client;
        public SharedService() { _client = new RestClient(); }
        public T CallWS_JSON_mode<T>(string url, string token, Method method = Method.Get, object? body = null)
        {
            //using (var client = new RestClient(url))
            //{
            var request = new RestRequest(url, method);
            if (!string.IsNullOrWhiteSpace(token)) request.AddHeader("x-api-key", $"{token}");
            if (body != null)
            {
                request.AddJsonBody(body, ContentType.Json);
            }

            try
            {
                var response = _client.Execute(request);

                if (response.StatusCode == HttpStatusCode.OK && !string.IsNullOrWhiteSpace(response.Content))
                {
                    return JsonConvert.DeserializeObject<T>(response.Content)!;
                }
                else
                {
                    throw new Exception($"Errore durante la chiamata all'endpoint: {url}. Codice di stato: {response.StatusCode}, Messaggio: {response.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Errore di comunicazione con '{url}': {ex.Message}", ex);
            }
            //}
        }
    }
}
