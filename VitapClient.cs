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
        private static readonly RestClientOptions options = new RestClientOptions(VITAP_BASE_URL);
        private static readonly RestClient client = new RestClient(options);
        private readonly System.Net.CookieContainer cookieContainer = new System.Net.CookieContainer();

        public async Task<bool> AuthenticateAsync(string username, string password)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== VIT-AP Authentication Flow Started ===");
                System.Diagnostics.Debug.WriteLine($"[VitapClient] Username: {username}");
                System.Diagnostics.Debug.WriteLine($"[VitapClient] Target: {VITAP_BASE_URL}");

                // Step 1: Get login page to extract CSRF token
                System.Diagnostics.Debug.WriteLine("\n[Step 1] Fetching login page...");
                var loginPageContent = await GetLoginPageContentAsync();
                if (string.IsNullOrEmpty(loginPageContent))
                {
                    System.Diagnostics.Debug.WriteLine("[ERROR] Login page content is empty!");
                    return false;
                }

                System.Diagnostics.Debug.WriteLine($"[Step 1] Login page fetched. Length: {loginPageContent.Length} characters");

                // Step 2: Extract CSRF token
                System.Diagnostics.Debug.WriteLine("\n[Step 2] Extracting CSRF token...");
                var csrfToken = ExtractCsrfToken(loginPageContent);
                if (string.IsNullOrEmpty(csrfToken))
                {
                    System.Diagnostics.Debug.WriteLine("[ERROR] Failed to extract CSRF token!");
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Page content snippet: {loginPageContent.Substring(0, Math.Min(500, loginPageContent.Length))}");
                    return false;
                }
                System.Diagnostics.Debug.WriteLine($"[Step 2] CSRF token extracted: {csrfToken}");

                // Step 3: Submit login
                System.Diagnostics.Debug.WriteLine("\n[Step 3] Submitting login credentials...");
                var loginResponse = await SubmitLoginAsync(username, password, csrfToken);

                System.Diagnostics.Debug.WriteLine($"[Step 3] Login response status: {loginResponse.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"[Step 3] Response length: {(loginResponse.Content?.Length ?? 0)} characters");

                // Check if response contains success indicators
                bool isSuccessful = CheckAuthenticationSuccess(loginResponse);

                if (isSuccessful)
                {
                    System.Diagnostics.Debug.WriteLine("\n✓ Authentication successful!");
                    return true;
                }
                else
                {
                    string errorMsg = ExtractErrorMessage(loginResponse.Content ?? string.Empty);
                    System.Diagnostics.Debug.WriteLine($"\n✗ Authentication failed: {errorMsg}");
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Response content: {loginResponse.Content}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"\n[EXCEPTION] {ex.GetType().Name}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[EXCEPTION] StackTrace: {ex.StackTrace}");
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
                System.Diagnostics.Debug.WriteLine($"[GetLoginPage] HTTP Status: {response.StatusCode}");
                return response.Content;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetLoginPage] Error: {ex.Message}");
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

                // Add form parameters
                request.AddParameter("username", username);
                request.AddParameter("password", password);
                request.AddParameter("csrf_token", csrfToken);

                System.Diagnostics.Debug.WriteLine($"[SubmitLogin] Sending POST with username={username}, csrf_token={csrfToken}");

                var response = await client.ExecuteAsync(request);
                System.Diagnostics.Debug.WriteLine($"[SubmitLogin] HTTP Status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"[SubmitLogin] Is Success (HTTP): {response.IsSuccessful}");

                return response;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SubmitLogin] Error: {ex.Message}");
                return new RestResponse { IsSuccessful = false, Content = ex.Message };
            }
        }

        private bool CheckAuthenticationSuccess(RestResponse response)
        {
            if (response == null || string.IsNullOrEmpty(response.Content))
                return false;

            string content = response.Content.ToLower();

            // Check for success indicators
            bool hasSuccessIndicators = content.Contains("success") ||
                                       content.Contains("authenticated") ||
                                       content.Contains("welcome") ||
                                       content.Contains("redirect") ||
                                       (response.StatusCode == System.Net.HttpStatusCode.OK && !content.Contains("error"));

            // Check for error indicators
            bool hasErrorIndicators = content.Contains("invalid") ||
                                     content.Contains("incorrect") ||
                                     content.Contains("failed") ||
                                     content.Contains("error") ||
                                     content.Contains("unauthorized");

            System.Diagnostics.Debug.WriteLine($"[CheckSuccess] Has error indicators: {hasErrorIndicators}");
            System.Diagnostics.Debug.WriteLine($"[CheckSuccess] HTTP Status: {response.StatusCode}");

            return !hasErrorIndicators && response.StatusCode == System.Net.HttpStatusCode.OK;
        }

        private string ExtractCsrfToken(string htmlContent)
        {
            if (string.IsNullOrEmpty(htmlContent))
                return null;

            try
            {
                var patterns = new string[]
                {
                    "name=\"csrf_token\"\\s+value=\"([a-zA-Z0-9_-]+)\"",
                    "name=\"_token\"\\s+value=\"([a-zA-Z0-9_-]+)\"",
                    "csrf[\"'][\\s:=]+[\"']([a-zA-Z0-9_-]+)[\"']",
                    "\"csrf_token\"\\s*[:\=]\\s*\"([a-zA-Z0-9_-]+)\""
                };

                foreach (var pattern in patterns)
                {
                    var match = Regex.Match(htmlContent, pattern, RegexOptions.IgnoreCase);
                    if (match.Success && match.Groups.Count > 1)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ExtractCSRF] Token found with pattern: {pattern}");
                        return match.Groups[1].Value;
                    }
                }

                System.Diagnostics.Debug.WriteLine("[ExtractCSRF] No CSRF token found with any pattern");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ExtractCSRF] Exception: {ex.Message}");
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
                    "<div[^>]*class=\"error[^>]*>([^<]*)</div>",
                    "<span[^>]*class=\"error[^>]*>([^<]*)</span>",
                    "<p[^>]*class=\"error[^>]*>([^<]*)</p>",
                    "error[\"'][\\s:=]+[\"']([^\"']*)[\"']",
                    "message[\"'][\\s:=]+[\"']([^\"']*)[\"']"
                };

                foreach (var pattern in patterns)
                {
                    var match = Regex.Match(htmlContent, pattern, RegexOptions.IgnoreCase);
                    if (match.Success && match.Groups.Count > 1)
                    {
                        return match.Groups[1].Value.Trim();
                    }
                }

                // If no specific error pattern found, return generic message
                if (htmlContent.ToLower().Contains("invalid") || htmlContent.ToLower().Contains("incorrect"))
                    return "Invalid credentials";
                if (htmlContent.ToLower().Contains("error"))
                    return "Authentication error";

                return "Login failed";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ExtractError] Exception: {ex.Message}");
                return "Could not extract error message";
            }
        }
    }
}
