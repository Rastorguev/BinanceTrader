using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BinanceApi.Models
{
    public class ThrottlingMessageHandler : DelegatingHandler
    {
        private readonly TimeSpanSemaphore _timeSpanSemaphore;

        public ThrottlingMessageHandler(TimeSpanSemaphore timeSpanSemaphore)
            : this(timeSpanSemaphore, null)
        {
        }

        public ThrottlingMessageHandler(TimeSpanSemaphore timeSpanSemaphore, HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
            _timeSpanSemaphore = timeSpanSemaphore;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return _timeSpanSemaphore.RunAsync(base.SendAsync, request, cancellationToken);
        }
    }
}