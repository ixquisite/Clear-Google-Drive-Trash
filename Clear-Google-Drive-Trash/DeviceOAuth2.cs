using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Runtime.Serialization.Json;

namespace Clear_Google_Drive_Trash
{
    /// <summary>
    /// BasicResponse with error details, which we'll be able to leverage with any response for error checking
    /// </summary>
    public abstract class BasicResponse
    {
        public String error { get; set; }
        public String error_description { get; set; }
        public String error_uri { get; set; }
    }

    /// <summary>
    /// OAuth2 Response for requesting device_code and user_code including verification_url
    /// </summary>
    public class RequestAuthResponse : BasicResponse
    {
        // https://developers.google.com/accounts/docs/OAuth2ForDevices
        public String device_code { get; set; }
        public String user_code { get; set; }
        public String verification_url { get; set; }
        public int expires_in { get; set; }
        public int interval { get; set; }
    }

    /// <summary>
    /// OAuth2 User Response for requesting access to the account for scopes provided returning access_token
    /// </summary>
    public class UserAuthResponse : BasicResponse
    {
        public String access_token { get; set; }
        public String token_type { get; set; }
        public int expires_in { get; set; }
        public String refresh_token { get; set; }
    }

    /// <summary>
    /// OAuth2 implementation for Google Drive 
    /// </summary>
    public class DeviceOAuth2
    {
        private String m_ClientId;
        private String m_Secret;

        public DeviceOAuth2(String clientId, String secret)
        {
            m_ClientId = clientId;
            m_Secret = secret;
        }

        public async Task<RequestAuthResponse> RequestAuthFromDevice(String scope)
        {
            // https://accounts.google.com/o/oauth2/device/code
            var client = new HttpClient();

            // http://stackoverflow.com/questions/7929013/making-a-curl-call-in-c-sharp
            // Create the HttpContent for the form to be posted.
            var requestContent = new FormUrlEncodedContent(new[] {
                new KeyValuePair<string, string>("client_id", m_ClientId),
                new KeyValuePair<string, string>("scope", scope)
            });

            // Post request and await the response.
            HttpResponseMessage response = await client.PostAsync("https://accounts.google.com/o/oauth2/device/code", requestContent);

            // Obtain the response content.
            HttpContent responseContent = response.Content;

            //String responseContentString;
            RequestAuthResponse authResponse;
            // Get the stream of the content.
            using (StreamReader reader = new StreamReader(await responseContent.ReadAsStreamAsync()))
            {
                DataContractJsonSerializer js = new DataContractJsonSerializer(typeof(RequestAuthResponse));
                authResponse = (RequestAuthResponse)js.ReadObject(reader.BaseStream);
            }
            return authResponse;
        }


        public async Task<UserAuthResponse> PollUserAuth(String user_code)
        {
            // https://accounts.google.com/o/oauth2/device/code
            var client = new HttpClient();

            // https://developers.google.com/accounts/docs/OAuth2ForDevices
            // Create the HttpContent for the form to be posted.
            var requestContent = new FormUrlEncodedContent(new[] {
                new KeyValuePair<string, string>("client_id", m_ClientId),
                new KeyValuePair<string, string>("client_secret", m_Secret),
                new KeyValuePair<string, string>("code", user_code),
                new KeyValuePair<string, string>("grant_type", "http://oauth.net/grant_type/device/1.0")
            });

            // Get the response.
            HttpResponseMessage response = await client.PostAsync("https://accounts.google.com/o/oauth2/token", requestContent);

            // Get the response content.
            HttpContent responseContent = response.Content;

            //String responseContentString;
            UserAuthResponse userAuthResponse;
            // Get the stream of the content.
            using (StreamReader reader = new StreamReader(await responseContent.ReadAsStreamAsync()))
            {
                DataContractJsonSerializer js = new DataContractJsonSerializer(typeof(UserAuthResponse));
                userAuthResponse = (UserAuthResponse)js.ReadObject(reader.BaseStream);
            }
            return userAuthResponse;
        }


        public async Task<UserAuthResponse> RefreshAuth(String refresh_token)
        {
            // https://accounts.google.com/o/oauth2/device/code
            var client = new HttpClient();

            // https://developers.google.com/accounts/docs/OAuth2ForDevices
            // Create the HttpContent for the form to be posted.
            var requestContent = new FormUrlEncodedContent(new[] {
                new KeyValuePair<string, string>("client_id", m_ClientId),
                new KeyValuePair<string, string>("client_secret", m_Secret),
                new KeyValuePair<string, string>("refresh_token", refresh_token),
                new KeyValuePair<string, string>("grant_type", "refresh_token")
            });

            // Get the response.
            HttpResponseMessage response = await client.PostAsync("https://accounts.google.com/o/oauth2/token", requestContent);

            // Get the response content.
            HttpContent responseContent = response.Content;

            //String responseContentString;
            UserAuthResponse userAuthResponse;
            // Get the stream of the content.
            using (StreamReader reader = new StreamReader(await responseContent.ReadAsStreamAsync()))
            {
                DataContractJsonSerializer js = new DataContractJsonSerializer(typeof(UserAuthResponse));
                userAuthResponse = (UserAuthResponse)js.ReadObject(reader.BaseStream);
            }
            return userAuthResponse;
        }


        public async Task<BasicResponse> EmptyTash()
        {
            // https://developers.google.com/drive/v2/reference/files/emptyTrash
            var client = new HttpClient();

            // Get the response.
            HttpResponseMessage response = await client.PostAsync("https://www.googleapis.com/drive/v2/files/trash", null);

            // Get the response content.
            HttpContent responseContent = response.Content;

            //String responseContentString;
            BasicResponse basicResponse;
            // Get the stream of the content.
            using (StreamReader reader = new StreamReader(await responseContent.ReadAsStreamAsync()))
            {
                DataContractJsonSerializer js = new DataContractJsonSerializer(typeof(BasicResponse));
                basicResponse = (BasicResponse)js.ReadObject(reader.BaseStream);
            }

            return basicResponse;
        }


        public bool SendAuthRequestEmail(String mailServer, String mailFrom, String mailTo, String mailSubject, String mailBody)
        {
            bool success = false;
            try
            {
                var email = new MailMessage();

                // setup Recipients
                email.To.Add(new MailAddress(mailTo));

                // setup From
                email.From = new MailAddress(mailFrom);

                // setup Reply-To
                email.ReplyToList.Add(new MailAddress(mailFrom));

                // setup Subject
                email.Subject = mailSubject;

                // setup Body
                email.IsBodyHtml = true;

                email.Body = mailBody;

                using (var smtpClient = new SmtpClient())
                {
                    smtpClient.Host = mailServer;
                    smtpClient.EnableSsl = false;

                    Console.WriteLine("Sending e-mail through {0} from {1} to {2} with subject {3} and reply set to {4}", smtpClient.Host, email.From.Address, email.To, email.Subject, email.ReplyToList);
                    smtpClient.Send(email);
                }
                success = true;
            }
            catch (Exception ex)
            {
                if (null != ex.InnerException)
                {
                    Console.WriteLine("DeviceOAuth2.SendEmail() Exception: {0} InnerException: {1}", ex.Message, ex.InnerException);
                }
            }
            return success;
        }
    }
}
