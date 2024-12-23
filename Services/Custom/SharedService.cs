using Contracts.Custom;
using Newtonsoft.Json;
using RestSharp;
using System.Net;

namespace BecaWebService.Services.Custom
{
    public class SharedService : ISharedService
    {
        public SharedService() { }
        public T CallWS_JSON_mode<T>(string url, string token, Method method = Method.Get, object? body = null)
        {
            using (var client = new RestClient(url))
            {
                var request = new RestRequest(url, method);
                if (token != "") request.AddHeader("x-api-key", $"{token}");
                if (body != null)
                {
                    request.AddJsonBody(body, ContentType.Json);
                }

                var response = client.Execute(request);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return JsonConvert.DeserializeObject<T>(response.Content);
                }
                else
                {
                    throw new Exception($"Errore durante la chiamata all'endpoint: {url}. Codice di stato: {response.StatusCode}");
                }
            }
        }
    }
}
