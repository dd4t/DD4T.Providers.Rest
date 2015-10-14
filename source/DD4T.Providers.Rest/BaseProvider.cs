using DD4T.ContentModel.Contracts.Configuration;
using DD4T.ContentModel.Contracts.Logging;
using DD4T.ContentModel.Contracts.Providers;
using DD4T.ContentModel.Contracts.Resolvers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace DD4T.Providers.Rest
{
    public class BaseProvider : IProvider
    {
        private readonly IPublicationResolver _publicationResolver;
        protected readonly ILogger Logger;
        protected readonly IDD4TConfiguration Configuration;

        private readonly IHttpMessageHandlerFactory _httpMessageHandlerFactory;

        public BaseProvider(IProvidersCommonServices commonServices, IHttpMessageHandlerFactory httpMessageHandlerFactory)
        {
            if (commonServices == null)
                throw new ArgumentNullException("commonServices");

            if (httpMessageHandlerFactory == null)
                throw new ArgumentNullException("httpMessageHandlerFactory");

            Logger = commonServices.Logger;
            _httpMessageHandlerFactory = httpMessageHandlerFactory;
            _publicationResolver = commonServices.PublicationResolver;
            Configuration = commonServices.Configuration;

        }

        private int publicationId = 0;
        public int PublicationId
        {
            get
            {
                if (publicationId == 0)
                    return _publicationResolver.ResolvePublicationId();

                return publicationId;
            }
            set
            {
                publicationId = value;
            }
        }

        public T Execute<T>(string urlParameters)
        {  
            HttpClientHandler messageHandler = new HttpClientHandler() { UseCookies = false };
            var pipeline = this._httpMessageHandlerFactory.CreatePipeline(messageHandler);

            using (var client = HttpClientFactory.Create(pipeline))
            {
                client.BaseAddress = new Uri(Configuration.ContentProviderEndPoint);
                // Add an Accept header for JSON format.
                client.DefaultRequestHeaders.Accept.Add(
                     new MediaTypeWithQualityHeaderValue("application/json"));

                var message = new HttpRequestMessage(HttpMethod.Get, urlParameters);

                // read all http cookies and add it to the request. 
                // needed to enable session preview functionality
                try
                {
                    if(System.Web.HttpContext.Current != null)
                    {
                        var cookies = System.Web.HttpContext.Current.Request.Cookies;
                        var strBuilder = new StringBuilder();
                        foreach (var item in cookies.AllKeys)
                        {
                            strBuilder.Append(string.Format("{0}={1};", item, cookies[item].Value));
                        }
                        message.Headers.Add("Cookie", strBuilder.ToString());
                    }
                }
                catch
                {
                    Logger.Warning("HttpContext is not initialized yet..");
                }
                
                var result = client.SendAsync(message).Result;
                if(result.IsSuccessStatusCode)
                {
                    return result.Content.ReadAsAsync<T>().Result;
                }
            }
            return default(T);
        }

    }
}
