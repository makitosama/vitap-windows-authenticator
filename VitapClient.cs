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
        // Configuration constants
        private const string VITAP_HOST = "172.18.10.15";
        private const int VITAP_PORT = 8080;
        private static readonly string VITAP_BASE_URL = "http://" + VITAP_HOST + ":" + VITAP_PORT;
        private static readonly RestClientOptions options = new RestClientOptions(VITAP_BASE_URL)
        {
            FollowRedirects = true,
            MaxRedirects = 5,
            ThrowOnAnyError = false,
            ThrowOnDeserializationError = false
        };
        private static readonly RestClient client = new RestClient(options);
        private readonly System.Net.CookieContainer cookieContainer = new System.Net.CookieContainer();

        public async Task<bool> AuthenticateAsync(string username, string password)
        {
            try
            {
                LoggingService.Log("=== VIT-AP Authentication Workflow Started ===");
                LoggingService.LogInfo($"Username: {username}");
                LoggingService.LogInfo($"Target: {VITAP_BASE_URL}");
                LoggingService.LogInfo($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");

                LoggingService.LogStep(1, "Fetching login page to extract CSRF token...");
                var loginPageContent = await GetLoginPageContentAsync();
                
                if (string.IsNullOrEmpty(loginPageContent))
                {
                    LoggingService.LogError("FAILED: Login page content is empty!");
                    LoggingService.LogWarning("Possible causes: Portal unreachable, network timeout, or invalid response");
                    return false;
                }

                LoggingService.LogSuccess($"Login page fetched successfully ({loginPageContent.Length} bytes)");

                LoggingService.LogStep(2, "Extracting CSRF token from login page...");
                var csrfToken = ExtractCsrfToken(loginPageContent);
                
                if (string.IsNullOrEmpty(csrfToken))
                {
                    LoggingService.LogError("FAILED: Could not extract CSRF token from response!");
                    LoggingService.LogWarning($"Page snippet: {loginPageContent.Substring(0, Math.Min(300, loginPageContent.Length))}");
                    return false;
                }

                LoggingService.LogSuccess($"CSRF token extracted: {csrfToken}");

                LoggingService.LogStep(3, "Submitting login credentials...");
                LoggingService.LogInfo($"Parameters: username={username}, csrf_token={csrfToken}");
                
                var loginResponse = await SubmitLoginAsync(username, password, csrfToken);
                
                LoggingService.LogInfo($"HTTP Response Status: {loginResponse.StatusCode}");
                LoggingService.LogInfo($"Response Size: {(loginResponse.Content?.Length ?? 0)} bytes");
                
                bool isSuccessful = CheckAuthenticationSuccess(loginResponse);
                
                if (isSuccessful)
                {
                    LoggingService.Log("");
                    LoggingService.LogSuccess("=== AUTHENTICATION SUCCESSFUL ===");
                    LoggingService.Log("");
                    return true;
                }
                else
                {
                    string errorMsg = ExtractErrorMessage(loginResponse.Content ?? string.Empty);
                    LoggingService.Log("");
                    LoggingService.LogError($"=== AUTHENTICATION FAILED ===");
                    LoggingService.LogError($"Error: {errorMsg}");
                    LoggingService.LogWarning($"Response content: {loginResponse.Content}");
                    LoggingService.Log("");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LoggingService.Log("");
                LoggingService.LogError($"=== AUTHENTICATION EXCEPTION ===");
                LoggingService.LogError($"Exception Type: {ex.GetType().Name}");
                LoggingService.LogError($"Message: {ex.Message}");
                LoggingService.LogWarning($"StackTrace: {ex.StackTrace}");
                LoggingService.Log("");
                return false;
            }
        }

        private async Task<string> GetLoginPageContentAsync()
        {
            try
            {
                LoggingService.LogHttpRequest("GET", "/hotspot/login");
                var request = new RestRequest("/hotspot/login", Method.Get);
                request.Timeout = TimeSpan.FromSeconds(10);
                
                request.AddHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
                request.AddHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
                request.AddHeader("Accept-Language", "en-US,en;q=0.5");
                request.AddHeader("Cache-Control", "no-cache");
                request.AddHeader("Pragma", "no-cache");
                request.AddHeader("Connection", "keep-alive");
                
                var response = await client.ExecuteAsync(request);
                
                LoggingService.LogHttpResponse((int)response.StatusCode, response.Content?.Length ?? 0);
                
                if (!response.IsSuccessful)
                {
                    LoggingService.LogWarning($"HTTP Status: {response.StatusCode}, Error: {response.ErrorMessage}");
                }
                
                return response.Content ?? string.Empty;
            }
            catch (HttpRequestException hexc)
            {
                LoggingService.LogError($"Network Error: {hexc.Message}");
                return null;
            }
            catch (TaskCanceledException tcex)
            {
                LoggingService.LogError($"Request Timeout: {tcex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"GetLoginPage Exception: {ex.Message}");
                return null;
            }
        }

        private async Task<RestResponse> SubmitLoginAsync(string username, string password, string csrfToken)
        {
            try
            {
                LoggingService.LogHttpRequest("POST", "/hotspot/login", $"username={username}&csrf_token={csrfToken}");
                var request = new RestRequest("/hotspot/login", Method.Post);
                request.Timeout = TimeSpan.FromSeconds(10);
                
                request.AddHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
                request.AddHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
                request.AddHeader("Accept-Language", "en-US,en;q=0.5");
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                request.AddHeader("Origin", VITAP_BASE_URL);
                request.AddHeader("Referer", VITAP_BASE_URL + "/hotspot/login");
                request.AddHeader("Cache-Control", "no-cache");
                request.AddHeader("Pragma", "no-cache");
                request.AddHeader("Connection", "keep-alive");
                
                request.AddParameter("username", username);
                request.AddParameter("password", password);
                request.AddParameter("csrf_token", csrfToken);
                
                var response = await client.ExecuteAsync(request);
                
                LoggingService.LogHttpResponse((int)response.StatusCode, response.Content?.Length ?? 0, response.Content);
                
                if (!response.IsSuccessful)
                {
                    LoggingService.LogWarning($"HTTP Status: {response.StatusCode}, Error: {response.ErrorMessage}");
                }
                
                return response;
            }
            catch (HttpRequestException hexc)
            {
                LoggingService.LogError($"Network Error: {hexc.Message}");
                return new RestResponse { IsSuccessful = false, Content = hexc.Message };
            }
            catch (TaskCanceledException tcex)
            {
                LoggingService.LogError($"Request Timeout: {tcex.Message}");
                return new RestResponse { IsSuccessful = false, Content = tcex.Message };
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"SubmitLogin Exception: {ex.Message}");
                return new RestResponse { IsSuccessful = false, Content = ex.Message };
            }
        }

        private bool CheckAuthenticationSuccess(RestResponse response)
        {
            if (response == null || string.IsNullOrEmpty(response.Content))
            {
                LoggingService.LogWarning("Response is null or empty");
                return false;
            }

            string content = response.Content.ToLower();
            bool hasErrorIndicators = content.Contains("invalid") || content.Contains("incorrect") ||
                                      content.Contains("failed") || content.Contains("error") ||
                                      content.Contains("unauthorized");

            LoggingService.LogInfo($"Response has error indicators: {hasErrorIndicators}");
            LoggingService.LogInfo($"HTTP Status Code: {response.StatusCode}");

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
                    "\"csrf_token\"\\s*[:\\=]\\s*\"([a-zA-Z0-9_-]+)\""
                };

                for (int i = 0; i < patterns.Length; i++)
                {
                    var match = Regex.Match(htmlContent, patterns[i], RegexOptions.IgnoreCase);
                    if (match.Success && match.Groups.Count > 1)
                    {
                        LoggingService.LogInfo($"CSRF token found using pattern {i + 1}");
                        return match.Groups[1].Value;
                    }
                }

                LoggingService.LogWarning("No CSRF token pattern matched");
                return null;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"ExtractCSRFToken Exception: {ex.Message}");
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

                if (htmlContent.ToLower().Contains("invalid") || htmlContent.ToLower().Contains("incorrect"))
                    return "Invalid credentials";
                if (htmlContent.ToLower().Contains("error"))
                    return "Authentication error";
                return "Login failed";
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"ExtractError Exception: {ex.Message}");
                return "Could not extract error message";
            }
        }
    }
}
