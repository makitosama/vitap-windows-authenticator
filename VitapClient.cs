using System;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace VitapAuthenticator
{
    public class VitapClient
    {
        private const string VITAP_HOST = "172.18.10.10";
        private const int VITAP_PORT = 1000;
        private static readonly string VITAP_BASE_URL = $"https://{VITAP_HOST}:{VITAP_PORT}";
        private static readonly HttpClientHandler handler = new HttpClientHandler()
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };
        private static readonly HttpClient client = new HttpClient(handler);

        public VitapClient()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11;
            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
        }

        public async Task<bool> AuthenticateAsync(string username, string password)
        {
            try
            {
                LoggingService.Log("=== VIT-AP Campus WiFi Authentication Started ===");
                LoggingService.LogInfo($"Username: {username}");
                LoggingService.LogInfo($"Target: {VITAP_BASE_URL}");
                LoggingService.LogInfo($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");

                // Step 1: Fetch magic token
                LoggingService.LogStep(1, "Fetching magic token from login page...");
                string magic = await FetchMagicTokenAsync();
                
                if (string.IsNullOrEmpty(magic))
                {
                    LoggingService.LogError("FAILED: Could not extract magic token");
                    LoggingService.LogWarning("Portal may be unreachable or token format changed");
                    return false;
                }

                LoggingService.LogSuccess($"Magic token extracted: {magic}");

                // Step 2: Submit login with magic token
                LoggingService.LogStep(2, "Submitting authentication with credentials and magic token...");
                bool success = await SubmitLoginAsync(username, password, magic);

                if (success)
                {
                    LoggingService.Log("");
                    LoggingService.LogSuccess("=== AUTHENTICATION SUCCESSFUL ===");
                    LoggingService.Log("");
                    return true;
                }
                else
                {
                    LoggingService.Log("");
                    LoggingService.LogError("=== AUTHENTICATION FAILED ===");
                    LoggingService.LogWarning("Check username and password");
                    LoggingService.Log("");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LoggingService.Log("");
                LoggingService.LogError($"=== AUTHENTICATION EXCEPTION ===");
                LoggingService.LogError($"Exception: {ex.Message}");
                LoggingService.LogWarning($"StackTrace: {ex.StackTrace}");
                LoggingService.Log("");
                return false;
            }
        }

        private async Task<string> FetchMagicTokenAsync()
        {
            try
            {
                LoggingService.LogHttpRequest("GET", "/login?");
                string url = $"{VITAP_BASE_URL}/login?";
                
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
                request.Headers.Add("Connection", "keep-alive");

                var response = await client.SendAsync(request);
                string content = await response.Content.ReadAsStringAsync();
                
                LoggingService.LogHttpResponse((int)response.StatusCode, content.Length);

                // Extract magic token using regex
                Match match = Regex.Match(content, @"<input type=\"hidden\" name=\"magic\" value=\"([^\"]+)\"");
                
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }

                LoggingService.LogWarning("Magic token regex pattern not found");
                return null;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"FetchMagicToken Exception: {ex.Message}");
                return null;
            }
        }

        private async Task<bool> SubmitLoginAsync(string username, string password, string magic)
        {
            try
            {
                LoggingService.LogHttpRequest("POST", "/login?");
                string url = $"{VITAP_BASE_URL}/login?";

                // Build POST data following Python implementation
                var postData = new Dictionary<string, string>
                {
                    { "4Tredir", "https://172.18.10.10:1000/login?" },
                    { "magic", magic },
                    { "username", username },
                    { "password", password }
                };

                var content = new FormUrlEncodedContent(postData);
                var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
                
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
                request.Headers.Add("Connection", "keep-alive");
                request.Headers.Add("Referer", url);
                request.Headers.Add("Origin", VITAP_BASE_URL);

                var response = await client.SendAsync(request);
                string responseContent = await response.Content.ReadAsStringAsync();
                
                LoggingService.LogHttpResponse((int)response.StatusCode, responseContent.Length);
                
                // Check for success indicator in response (Python code checks for this URL in response)
                if (responseContent.Contains("https://172.18.10.10:1000/keepalive?"))
                {
                    LoggingService.LogSuccess("Login successful - keepalive URL detected in response");
                    return true;
                }
                else if (responseContent.Contains("concurrent authentication is over limit"))
                {
                    LoggingService.LogError("Concurrent login limit reached");
                    return false;
                }
                else
                {
                    LoggingService.LogWarning($"Unexpected response content length: {responseContent.Length}");
                    LoggingService.LogWarning(responseContent.Length > 500 ? responseContent.Substring(0, 500) : responseContent);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"SubmitLogin Exception: {ex.Message}");
                return false;
            }
        }
    }
}
