using System;
using System.Configuration;
using System.Threading.Tasks;

namespace Clear_Google_Drive_Trash
{
    class Clear_Google_Drive_Trash
    {
        // Please make sure to update parameters in App.config BEFORE running this app!!!
        static void Main(string[] args)
        {
            String clientId = ConfigurationManager.AppSettings["clientId"];
            String secret = ConfigurationManager.AppSettings["secret"];
            if (String.IsNullOrEmpty(clientId) || String.IsNullOrEmpty(secret))
            {
                Console.WriteLine("Please make sure 'clientId' and 'secret' have been set in your app.config. You'll need to obtain them from your Google Developer Console!");
                return;
            }

            String smtpServer = ConfigurationManager.AppSettings["smtpServer"];
            String mailFrom = ConfigurationManager.AppSettings["mailFrom"];
            String mailTo = ConfigurationManager.AppSettings["mailTo"]; // this should be moved into some sort of DB since most likely, you'll have a list of these, which you want to keep with other account info
            if (String.IsNullOrEmpty(smtpServer) || String.IsNullOrEmpty(mailFrom) || String.IsNullOrEmpty(mailTo))
            {
                Console.WriteLine("Please make sure 'smtpServer', 'mailFrom' and 'mailTo' have been configured in your app.config.");
                return;
            }

            String scope = "https://docs.google.com/feeds ";
            scope += "https://docs.google.com/feeds ";
            scope += "https://www.googleapis.com/auth/userinfo.email ";
            scope += "https://www.googleapis.com/auth/userinfo.profile ";
            //scope += "https://www.googleapis.com/auth/drive"; // this scope will get you a "invalid_scope" response from the initial auth request, but "https://docs.google.com/feeds" just doesn't cut it.
            // http://stackoverflow.com/questions/24293523/im-trying-to-access-google-drive-through-the-cli-but-keep-getting-not-authori
            // https://developers.google.com/drive/web/scopes
            // https://accounts.google.com/o/oauth2/device/code


            int maxWaitSecs = 60;
            RequestAuthResponse authResponse;
            DeviceOAuth2 auth = new DeviceOAuth2(clientId, secret);
            using (Task<RequestAuthResponse> t = auth.RequestAuthFromDevice(scope))
            {
                authResponse = t.Result;
                int waitTime = 5;
                while (null == authResponse && !t.IsCompleted && maxWaitSecs >= 0)
                {
                    System.Threading.Thread.Sleep(waitTime * 1000);
                    maxWaitSecs -= waitTime;
                }
            }

            if (null == authResponse || !String.IsNullOrEmpty(authResponse.error))
            {
                Console.WriteLine("Sorry, got {0} response from Google!\n{1}", null == authResponse ? "no" : "error", null != authResponse ? authResponse.error + ":" + authResponse.error_description : "");
                return;
            }

            Console.WriteLine("Google response: URL: {0}, Device Code: {1}, User Code: {2}, Interval: {3}, Expires: {4}.", authResponse.verification_url, authResponse.device_code, authResponse.user_code, authResponse.interval, authResponse.expires_in);

            String strBody = String.Format("<p>Hello -<br /></p><p>We are sending you this email asking for permission accessing your Google Drive account in order to be able to manage the Trash folder.</p>");
            strBody += String.Format("<p>To do this in a secure way, please follow these steps:</p>");
            strBody += String.Format("<p>Use your internet browser and naviate to {0}.</p>", authResponse.verification_url);
            strBody += String.Format("<p>Upon request, please type in your personalized code '{0}'. (Do not enter the quotes. The code is case sensitive.)</p>", authResponse.user_code);
            strBody += String.Format("<p>Once you have completed these steps confirming access, we will be able to manage the trash folder for you.</p>");

            if (!auth.SendAuthRequestEmail(smtpServer, mailFrom, mailTo, "Google Drive authentication", strBody))
            {
                return;
            }

            maxWaitSecs = authResponse.expires_in; // default is 1800 (sec), which means 30 minutes
            UserAuthResponse userResponse;

            using (Task<UserAuthResponse> t = auth.PollUserAuth(authResponse.user_code))
            {
                userResponse = t.Result;
                while ((null == userResponse || userResponse.error == "authorization_pending") && maxWaitSecs >= 0)
                {
                    // t.IsCompleted
                    Console.WriteLine("Before wait of {0} secs.", authResponse.interval + 1);
                    System.Threading.Thread.Sleep((authResponse.interval + 1) * 1000);
                    Console.WriteLine("After wait. {0} secs. to go.", maxWaitSecs);
                    maxWaitSecs -= (authResponse.interval + 1);
                    Console.WriteLine("Still waiting for user auth. Max wait: {0}sec. Interval: {1}sec.{2}", maxWaitSecs, authResponse.interval, null != userResponse && !string.IsNullOrEmpty(userResponse.error) ? string.Format(" Error: {0}", userResponse.error) : "");
                }
            }


            if (null == userResponse || !String.IsNullOrEmpty(userResponse.error))
            {
                Console.WriteLine("Sorry, got {0} response from Google! {1}", null == userResponse ? "no" : "error", null != userResponse ? userResponse.error : "");
                return;
            }

            Console.WriteLine("Google response: Access Token: {0}, Token Type: {1}, Expires in (sec): {2}, Refresh Token: {3}.", userResponse.access_token, userResponse.token_type, userResponse.expires_in, userResponse.refresh_token);

        }
    }
}
