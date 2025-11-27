using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using RestSharp;
using Newtonsoft.Json;

namespace VitapAuthenticator
{
    public class VitapClient
    {
        private const string VITAP_HOST = "172.18.10.10";
        private const int VITAP_PORT = 8090;
        private static readonly string VITAP_BASE_URL = "http://" + VITAP_HOST + ":" + VITAP_PORT;
        private static readonly RestClientOptions options = new RestClientOptions(VITAP_BASE_URL)
        {
            MaxTimeout = TimeSpan.FromSeconds(10)
        };
        private static readonly RestClient client = new RestClient(options);
        private readonly System.Net.CookieContainer cookieContainer = new System.Net.CookieContainer();

        public async Task<bool> AuthenticateAsync(string username, string password)
        {
            try
            {
                // Step 1: Get login page to extract CSRF token
                System.Diagnostics.Debug.WriteLine("[VitapClient] Step 1: Fetching login page...");
                var loginPageContent = await GetLoginPageContentAsync();
                if (string.IsNullOrEmpty(loginPageContent))
                {
                    System.Diagnostics.Debug.WriteLine("[VitapClient] Failed to fetch login page");
                    return false;
                }

                // Step 2: Extract CSRF token from login page
                var csrfToken = ExtractCsrfToken(loginPageContent);
                if (string.IsNullOrEmpty(csrfToken))
                {
                    System.Diagnostics.Debug.WriteLine("[VitapClient] CSRF token not found");
                    return false;
                }
                System.Diagnostics.Debug.WriteLine($"[VitapClient] CSRF token extracted: {csrfToken}");

                // Step 3: Submit login with credentials and CSRF token
                System.Diagnostics.Debug.WriteLine($"[VitapClient] Step 3: Submitting login for user: {username}");
                var loginResponse = await SubmitLoginAsync(username, password, csrfToken);
                if (!loginResponse.IsSuccessful)
                {
                    string errorMsg = ExtractErrorMessage(loginResponse.Content ?? string.Empty);
                    System.Diagnostics.Debug.WriteLine($"[VitapClient] Login failed: {errorMsg}");
                    return false;
                }

                System.Diagnostics.Debug.WriteLine("[VitapClient] Login successful!");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VitapClient] Authentication error: {ex.Message}");
                return false;
            }
        }

        private async Task<string> GetLoginPageContentAsync()
        {
            try
            {
                var request = new RestRequest("/hotspot/login", Method.Get);
                request.AddHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                request.AddHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
                request.AddHeader("Accept-Language", "en-US,en;q=0.5");
                request.AddHeader("Cache-Control", "no-cache");
                request.AddHeader("Pragma", "no-cache");

                var response = await client.ExecuteAsync(request);
                System.Diagnostics.Debug.WriteLine($"[VitapClient] Login page response: {response.StatusCode}");
                return response.Content;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VitapClient] Error fetching login page: {ex.Message}");
                return null;
            }
        }

        private async Task<RestResponse> SubmitLoginAsync(string username, string password, string csrfToken)
        {
            try
            {
                var request = new RestRequest("/hotspot/login", Method.Post);
                request.AddHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                request.AddHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
                request.AddHeader("Accept-Language", "en-US,en;q=0.5");
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                request.AddHeader("Origin", VITAP_BASE_URL);
                request.AddHeader("Referer", VITAP_BASE_URL + "/hotspot/login");
                request.AddHeader("Cache-Control", "no-cache");
                request.AddHeader("Pragma", "no-cache");

                // Add form data
                request.AddParameter("username", username);
                request.AddParameter("password", password);
                request.AddParameter("csrf_token", csrfToken);

                var response = await client.ExecuteAsync(request);
                System.Diagnostics.Debug.WriteLine($"[VitapClient] Login submit response: {response.StatusCode}");
                if (!response.IsSuccessful)
                {
                    System.Diagnostics.Debug.WriteLine($"[VitapClient] Response content: {response.Content}");
                }
                return response;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VitapClient] Error submitting login: {ex.Message}");
                return new RestResponse { IsSuccessful = false };
            }
        }

        private string ExtractCsrfToken(string htmlContent)
        {
            if (string.IsNullOrEmpty(htmlContent))
                return null;

            try
            {
                var patterns = new string[]
                {
                    "name=\"csrf_token\"\\s+value=\"([a-zA-Z0-9]+)\"",
                    "csrf_token[\"'][\\s:=]+[\"']([a-zA-Z0-9]+)[\"']",
                    "<input[^>]*name=\"csrf_token\"[^>]*value=\"([^\"]*)\"",
                    "csrf[\"'][\\s:=]+[\"']([a-zA-Z0-9]+)[\"']"
                };

                foreach (var pattern in patterns)
                {
                    var match = Regex.Match(htmlContent, pattern, RegexOptions.IgnoreCase);
                    if (match.Success && match.Groups.Count > 1)
                    {
                        return match.Groups[1].Value;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VitapClient] Error extracting CSRF token: {ex.Message}");
                return null;
            }
        }

        private string ExtractErrorMessage(string htmlContent)
        {
            if (string.IsNullOrEmpty(htmlContent))
                return "Unknown error";

            try
            {
                var patterns = new string[]
                {
                    "<div[^>]*class=\"error\"[^>]*>([^<]*)</div>",
                    "<span[^>]*class=\"error\"[^>]*>([^<]*)</span>",
                    "error[\"'][\\s:=]+[\"']([^\"']*)[\"']",
                    "message[\"'][\\s:=]+[\"']([^\"']*)[\"']"
                };

                foreach (var pattern in patterns)
                {
                    var match = Regex.Match(htmlContent, pattern, RegexOptions.IgnoreCase);
                    if (match.Success && match.Groups.Count > 1)
                    {
                        return match.Groups[1].Value;
                    }
                }

                return "Login failed";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VitapClient] Error extracting error message: {ex.Message}");
                return "Could not extract error message";
            }
        }
    }
}
