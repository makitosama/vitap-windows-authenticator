using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using RestSharp;
using Newtonsoft.Json;

namespace VitapAuthenticator
{
    public class VitapClient
    {
        private const string VITAP_HOST = "172.18.10.10";
        private const int VITAP_PORT = 8090;
        private static readonly string VITAP_BASE_URL = "http://" + VITAP_HOST + ":" + VITAP_PORT;
        private static readonly HttpClientHandler handler = new HttpClientHandler
        {
            UseCookies = true,
            CookieContainer = new System.Net.CookieContainer(),
            ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true
        };

        private HttpClient httpClient = new HttpClient(handler);

        public async Task<bool> AuthenticateAsync(string username, string password)
        {
            try
            {
                // Step 1: GET initial page to retrieve CSRF token
                var initialResponse = await httpClient.GetAsync($"{VITAP_BASE_URL}/login");
                var initialContent = await initialResponse.Content.ReadAsStringAsync();

                // Extract CSRF token (example - adjust based on actual response)
                string csrfToken = ExtractCsrfToken(initialContent);

                // Step 2: POST authentication request with CSRF token
                var postData = new Dictionary<string, string>
                {
                    { "username", username },
                    { "password", password },
                    { "csrf_token", csrfToken }
                };

                var content = new FormUrlEncodedContent(postData);
                var response = await httpClient.PostAsync($"{VITAP_BASE_URL}/login", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Authentication error: {ex.Message}");
                return false;
            }
        }

        private string ExtractCsrfToken(string htmlContent)
        {
            // Parse CSRF token from HTML (example implementation)
            int start = htmlContent.IndexOf("csrf_token\"") + 12;
            int end = htmlContent.IndexOf("\"", start);
            return htmlContent.Substring(start, end - start);
        }
    }
}
