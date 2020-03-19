using Microsoft.Azure.Relay;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ClientExample
{
    class Program
    {
        private const string RelayNamespace = "ag-relay.servicebus.windows.net";

        // replace {HybridConnectionName} with the name of your hybrid connection
        private const string ConnectionName = "ag-hybridconnectionboui";

        // replace {SAKKeyName} with the name of your Shared Access Policies key, which is RootManageSharedAccessKey by default
        private const string KeyName = "RootManageSharedAccessKey";

        // replace {SASKey} with the primary key of the namespace you saved earlier
        private const string Key = "sIjwfQAfumiFpER4WxhR95nH8uQLgMP8iC2Xer9geXM=";

        static void Main(string[] args)
        {
            RunAsync().GetAwaiter().GetResult();
        }

        private static async Task RunAsync()
        {
            var tokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider(
             KeyName, Key);
            var uri = new Uri(string.Format("https://{0}/{1}", RelayNamespace, ConnectionName));
            var token = (await tokenProvider.GetTokenAsync(uri.AbsoluteUri, TimeSpan.FromHours(1))).TokenString;
            var client = new HttpClient();
            var request = new HttpRequestMessage()
            {
                RequestUri = uri,
                Method = HttpMethod.Get,
            };
            request.Headers.Add("ServiceBusAuthorization", token);
            var response = await client.SendAsync(request);
            Console.WriteLine(await response.Content.ReadAsStringAsync());
        }
    }
}
