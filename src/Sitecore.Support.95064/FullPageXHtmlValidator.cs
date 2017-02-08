using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Web;
using System.Web.Configuration;
using Sitecore.Data.Items;
using Sitecore.Data.Validators;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Layouts;
using Sitecore.Links;
using Sitecore.Sites;
using Sitecore.Text;
using Sitecore.Web;

namespace Sitecore.Support.Data.Validators.ItemValidators
{

    [Serializable]
    public class FullPageXHtmlValidator : StandardValidator
    {
        public FullPageXHtmlValidator()
        {
        }

        public FullPageXHtmlValidator(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        private static HttpWebRequest CreateRequest(string url)
        {
            Assert.ArgumentNotNull(url, "url");
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.UserAgent = HttpContext.Current.Request.UserAgent;
            string cookieName = ((SessionStateSection)ConfigurationManager.GetSection("system.web/sessionState")).CookieName;
            CookieContainer container = new CookieContainer();
            Uri uri = new Uri(url);
            HttpCookieCollection cookies = HttpContext.Current.Request.Cookies;
            for (int i = 0; i < cookies.Count; i++)
            {
                HttpCookie cookie = cookies[i];
                if (cookieName != cookie.Name)
                {
                    container.Add(new Cookie(cookie.Name, cookie.Value, cookie.Path, uri.Host));
                }
            }
            request.CookieContainer = container;
            return request;
        }

        protected override ValidatorResult Evaluate()
        {
            Item item = base.GetItem();
            if (item == null)
            {
                return ValidatorResult.Valid;
            }
            if (!item.Paths.IsContentItem)
            {
                return ValidatorResult.Valid;
            }
            if (item.Visualization.Layout == null)
            {
                return ValidatorResult.Valid;
            }
            string url = this.GetUrl(item).ToString();
            if (url.IndexOf("://", StringComparison.InvariantCulture) < 0)
            {
                url = WebUtil.GetServerUrl() + url;
            }
            HttpWebRequest request = CreateRequest(url);
            string xhtml = string.Empty;
            try
            {
                Stream responseStream = request.GetResponse().GetResponseStream();
                if (responseStream != null)
                {
                    xhtml = new StreamReader(responseStream).ReadToEnd();
                }
            }
            catch (WebException exception)
            {
                if (((HttpWebResponse)exception.Response).StatusCode == HttpStatusCode.NotFound)
                {
                    return ValidatorResult.Valid;
                }
                base.Text = base.GetText(Translate.Text("The page represented by the item '{0}' failed to render properly, The error was: {1}", new object[] { item.Paths.ContentPath, exception.Message }), new string[0]);
                return base.GetFailedResult(ValidatorResult.Error);
            }
            Collection<XHtmlValidatorError> collection = XHtml.Validate(XHtml.AddHtmlEntityConversionsInDoctype(xhtml));
            if (collection.Count == 0)
            {
                return ValidatorResult.Valid;
            }
            foreach (XHtmlValidatorError error in collection)
            {
                base.Errors.Add(String.Format("{0}:{1}, {2}, {3}", error.Severity, error.Message, error.LineNumber, error.LinePosition));
            }
            base.Text = base.GetText(Translate.Text("The page represented by the item '{0}' contains (or lacks) some formatting attributes which could cause in unexpected results in some browsers (such as Internet Explorer, Firefox, or Safari)", new object[] { item.Paths.ContentPath }), new string[0]);
            return base.GetFailedResult(ValidatorResult.Error);
        }

        protected override ValidatorResult GetMaxValidatorResult()
        {
            return base.GetFailedResult(ValidatorResult.Error);
        }

        private UrlString GetUrl(Item item)
        {
            Assert.ArgumentNotNull(item, "item");
            UrlOptions defaultOptions = UrlOptions.DefaultOptions;
            defaultOptions.Site = SiteContext.GetSite("shell");
            UrlString str = new UrlString(LinkManager.GetItemUrl(item, defaultOptions));
            if (!(str.ToString().EndsWith(".aspx") && (str.ToString() != "/sitecore/shell")))
            {
                str.Path = str.Path + "/" + item.Name + ".aspx";
            }
            str["sc_database"] = Client.ContentDatabase.Name;
            str["sc_duration"] = "temporary";
            str["sc_itemid"] = item.ID.ToString();
            str["sc_lang"] = item.Language.Name;
            str["sc_webedit"] = "0";
            if (base.Parameters.ContainsKey("device"))
            {
                str["sc_device"] = base.Parameters["device"];
            }
            return str;
        }

        public override string Name => "Full Page is XHtml";
    }

}