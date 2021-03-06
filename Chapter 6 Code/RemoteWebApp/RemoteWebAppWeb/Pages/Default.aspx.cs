﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using System.Globalization;
using System.Web.Configuration;
using Microsoft.IdentityModel.S2S.Protocols.OAuth2;
using Microsoft.SharePoint.Client;

namespace RemoteWebAppWeb.Pages
{
    public partial class Default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // The following code gets the client context and Title property by using TokenHelper.
            // To access other properties, you may need to request permissions on the host web.

            //var contextToken = TokenHelper.GetContextTokenFromRequest(Page.Request);
            //var hostWeb = Page.Request["SPHostUrl"];

            //using (var clientContext = TokenHelper.GetClientContextWithContextToken(hostWeb, contextToken, Request.Url.Authority))
            //{
            //    clientContext.Load(clientContext.Web, web => web.Title);
            //    clientContext.ExecuteQuery();
            //    Response.Write(clientContext.Web.Title);
            //}

            // Get app info from web.config
            string clientID = string.IsNullOrEmpty(WebConfigurationManager.AppSettings.Get("ClientId"))
                                ? WebConfigurationManager.AppSettings.Get("HostedAppName")
                                : WebConfigurationManager.AppSettings.Get("ClientId");
            string clientSecret = string.IsNullOrEmpty(WebConfigurationManager.AppSettings.Get("ClientSecret"))
                                ? WebConfigurationManager.AppSettings.Get("HostedAppSigningKey")
                                : WebConfigurationManager.AppSettings.Get("ClientSecret");

            // Get values from Page.Request
            string reqAuthority = Request.Url.Authority;
            string hostWeb = Page.Request["SPHostUrl"];
            string hostWebAuthority = (new Uri(hostWeb)).Authority;

            // Get Context Token
            string contextTokenStr = TokenHelper.GetContextTokenFromRequest(Request);
            SharePointContextToken contextToken =
                TokenHelper.ReadAndValidateContextToken(contextTokenStr, reqAuthority);

            // Read data from the Context Token
            string targetPrincipalName = contextToken.TargetPrincipalName;
            string cacheKey = contextToken.CacheKey;
            string refreshTokenStr = contextToken.RefreshToken;
            string realm = contextToken.Realm;

            // Create principal and client strings
            string targetPrincipal = GetFormattedPrincipal(targetPrincipalName, hostWebAuthority, realm);
            string appPrincipal = GetFormattedPrincipal(clientID, null, realm);

            // Request an access token from ACS
            string stsUrl = TokenHelper.AcsMetadataParser.GetStsUrl(realm);
            OAuth2AccessTokenRequest oauth2Request =
                OAuth2MessageFactory.CreateAccessTokenRequestWithRefreshToken(
                    appPrincipal, clientSecret, refreshTokenStr, targetPrincipal);
            OAuth2S2SClient client = new OAuth2S2SClient();
            OAuth2AccessTokenResponse oauth2Response = client.Issue(stsUrl, oauth2Request) as OAuth2AccessTokenResponse;
            string accessTokenStr = oauth2Response.AccessToken;

            // Build the CSOM context with the access token
            ClientContext clientContext = TokenHelper.GetClientContextWithAccessToken(hostWeb, accessTokenStr);
            clientContext.Load(clientContext.Web, web => web.Title);
            clientContext.ExecuteQuery();

            // Dump values to the page
            DataTable dt = new DataTable();
            dt.Columns.Add("Name");
            dt.Columns.Add("Value");

            dt.Rows.Add("QueryString", Request.QueryString);
            dt.Rows.Add("clientID", clientID);
            dt.Rows.Add("clientSecret", clientSecret);
            dt.Rows.Add("hostWeb", hostWeb);
            dt.Rows.Add("contextTokenStr", contextTokenStr);
            dt.Rows.Add("contextToken", contextToken);
            dt.Rows.Add("targetPrincipalName", targetPrincipalName);
            dt.Rows.Add("cacheKey", cacheKey);
            dt.Rows.Add("refreshTokenStr", refreshTokenStr);
            dt.Rows.Add("realm", realm);
            dt.Rows.Add("targetPrincipal", targetPrincipal);
            dt.Rows.Add("appPrincipal", appPrincipal);
            dt.Rows.Add("stsUrl", stsUrl);
            dt.Rows.Add("oauth2Request", oauth2Request);
            dt.Rows.Add("client", client);
            dt.Rows.Add("oauth2Response", oauth2Response);
            dt.Rows.Add("accessTokenStr", accessTokenStr);
            dt.Rows.Add("Host Web Title", clientContext.Web.Title);

            grd.DataSource = dt;
            grd.DataBind();
        }

        private static string GetFormattedPrincipal(string principalName, string hostName, string realm)
        {
            if (!String.IsNullOrEmpty(hostName))
            {
                return String.Format(CultureInfo.InvariantCulture, "{0}/{1}@{2}", principalName, hostName, realm);
            }
            else
            {
                return String.Format(CultureInfo.InvariantCulture, "{0}@{1}", principalName, realm);
            }
        }
    }
}