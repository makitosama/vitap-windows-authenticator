using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;

namespace VitapAuthenticator
{
    public class SessionManager
    {
        private readonly HttpClient httpClient = new HttpClient();
        private const string VITAP_HOST = "172.18.10.10";
        private const int VITAP_PORT = 8090;
        private static readonly string VITAP_BASE_URL = "http://" + VITAP_HOST + ":" + VITAP_PORT;        private const int KEEP_ALIVE_INTERVAL_MS = 300000; // 5 minutes
        private const int SESSION_TIMEOUT_MS = 1800000; // 30 minutes

        private Timer keepAliveTimer;
        private DateTime lastActivityTime;

        public void StartKeepAlive()
        {
            lastActivityTime = DateTime.Now;
            keepAliveTimer = new Timer(KeepAliveCallback, null, KEEP_ALIVE_INTERVAL_MS, KEEP_ALIVE_INTERVAL_MS);
        }

        private void KeepAliveCallback(object state)
        {
            // Check if session has timed out
            if (DateTime.Now - lastActivityTime > TimeSpan.FromMilliseconds(SESSION_TIMEOUT_MS))
            {
                StopKeepAlive();
                return;
            }

            // Send keep-alive request
            Task.Run(async () =>
            {
                try
                {
                    await httpClient.GetAsync($"{VITAP_BASE_URL}/keep-alive");
                    lastActivityTime = DateTime.Now;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Keep-alive error: {ex.Message}");
                }
            });
        }

        public void UpdateActivity()
        {
            lastActivityTime = DateTime.Now;
        }

        public void StopKeepAlive()
        {
            keepAliveTimer?.Dispose();
            keepAliveTimer = null;
        }
    }
}
