using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using GDrive;
using JsonFx.Json;


namespace GDrive
{
    partial class GoogleDrive
    {
        const int SERVER_PORT = 9271;

        string RedirectURI
        {
            get
            {
                return "http://localhost:" + SERVER_PORT; 
            }
        }

        static readonly string[] DefaultScopes = new string[]
        {
            "https://www.googleapis.com/auth/drive",
            "https://www.googleapis.com/auth/userinfo.email"
        };
        string[] scopes = DefaultScopes;

        public string[] Scopes
        {
            set { scopes = (string[])value.Clone(); }
            get { return scopes; }
        }

        Uri AuthorizationURL
        {
            get
            {
                return new Uri("https://accounts.google.com/o/oauth2/auth?" +
                    "scope=" + string.Join(" ", Scopes) +
                    "&response_type=code" +
                    "&redirect_uri=" + RedirectURI +
                    "&client_id=" + ClientID);
            }
        }

        bool isAuthorized = false;

        public bool IsAuthorized
        {
            get { return isAuthorized; }
            private set { isAuthorized = value; }
        }

        string accessToken = null;

        string AccessToken
        {
            get
            {
                if (accessToken == null)
                {
                    int key = ClientID.GetHashCode();
                    accessToken = PlayerPrefs.GetString("UnityGoogleDrive_Token_" + key, "");
                }

                return accessToken;
            }
            set
            {
                if (accessToken != value)
                {
                    accessToken = value;

                    int key = ClientID.GetHashCode();

                    if (accessToken != null)
                        PlayerPrefs.SetString("UnityGoogleDrive_Token_" + key, accessToken);
                    else
                        PlayerPrefs.DeleteKey("UnityGoogleDrive_Token_" + key);
                }
            }
        }

        string refreshToken = null;

        string RefreshToken
        {
            get
            {
                if (refreshToken == null)
                {
                    int key = ClientID.GetHashCode();
                    refreshToken = PlayerPrefs.GetString("UnityGoogleDrive_RefreshToken_" + key, "");
                }

                return refreshToken;
            }
            set
            {
                if (refreshToken != value)
                {
                    refreshToken = value;

                    int key = ClientID.GetHashCode();

                    if (refreshToken != null)
                        PlayerPrefs.SetString("UnityGoogleDrive_RefreshToken_" + key, refreshToken);
                    else
                        PlayerPrefs.DeleteKey("UnityGoogleDrive_RefreshToken_" + key);
                }
            }
        }

        string userAccount = null;

        public string UserAccount
        {
            get
            {
                if (userAccount == null)
                {
                    int key = ClientID.GetHashCode();
                    userAccount = PlayerPrefs.GetString("UnityGoogleDrive_UserAccount_" + key, "");
                }

                return userAccount;
            }
            private set
            {
                if (userAccount != value)
                {
                    userAccount = value;

                    int key = ClientID.GetHashCode();

                    if (userAccount != null)
                        PlayerPrefs.SetString("UnityGoogleDrive_UserAccount_" + key, userAccount);
                    else
                        PlayerPrefs.DeleteKey("UnityGoogleDrive_UserAccount_" + key);
                }
            }
        }

        DateTime expiresIn = DateTime.MaxValue;

        public IEnumerator Authorize()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                if (ClientID == null)
                {
                    yield return new Exception(-1, "ClientID is null.");
                    yield break;
                }
            }
            else
            {
                if (ClientID == null || ClientSecret == null)
                {
                    yield return new Exception(-1, "ClientID or ClientSecret is null.");
                    yield break;
                }
            }

            if (AccessToken == "")
            {
                // Open browser and authorization.
                var routine = GetAuthorizationCodeAndAccessToken();
                while (routine.MoveNext())
                    yield return null;

                if (routine.Current is Exception)
                {
                    yield return routine.Current;
                    yield break;
                }
                else if (AccessToken == "")
                {
                    yield return new Exception(-1, "Authorization failed.");
                    yield break;
                }
                else
                {
                    IsAuthorized = true;
                }
            }
            else
            {
                // Check the access token.
                var validate = ValidateToken(accessToken);
                {
                    while (validate.MoveNext())
                        yield return null;

                    if (validate.Current is Exception)
                    {
                        yield return validate.Current;
                        yield break;
                    }

                    var res = (TokenInfoResponse)validate.Current;

                    // Require re-authorization.
                    if (res.error != null)
                    {
                        // Remove saved access token.
                        AccessToken = null;

                        if (RefreshToken != "")
                        {
                            // Try refresh token.
                            var refresh = RefreshAccessToken();
                            while (refresh.MoveNext())
                                yield return null;
                        }

                        // No refresh token or refresh failed.
                        if (AccessToken == "")
                        {
                            // Open browser and authorization.
                            var routine = GetAuthorizationCodeAndAccessToken();
                            while (routine.MoveNext())
                                yield return null;

                            if (routine.Current is Exception)
                            {
                                yield return routine.Current;
                                yield break;
                            }
                        }

                        // If access token is available, authorization is succeeded.
                        if (AccessToken != "")
                            IsAuthorized = true;
                        else
                        {
                            yield return new Exception(-1, "Authorization failed.");
                            yield break;
                        }
                    }
                    else
                    {
                        // Validating succeeded.
                        IsAuthorized = true;
                        UserAccount = res.email;

                        expiresIn = DateTime.Now + new TimeSpan(0, 0, res.expiresIn);
                    }
                }
            }

            yield return new AsyncSuccess();
        }



        public IEnumerator Unauthorize()
        {
            IsAuthorized = false;

            var revoke = RevokeToken(AccessToken);
            while (revoke.MoveNext())
                yield return null;

            AccessToken = null;
            RefreshToken = null;

            if (revoke.Current is Exception)
            {
                yield return revoke.Current;
                yield break;
            }
            else
            {
                var res = (RevokeResponse)revoke.Current;

                if (res.error != null)
                {
                    yield return res.error;
                    yield break;
                }
            }

            yield return new AsyncSuccess();

        }

        IEnumerator GetAuthorizationCodeAndAccessToken()
        {
            // Google authorization URL
            Uri uri = AuthorizationURL;

            string authorizationCode = null;
            System.Diagnostics.Process browser;
            bool windows = false;

            // Open the browser.
            if (Application.platform == RuntimePlatform.WindowsPlayer ||
            Application.platform == RuntimePlatform.WindowsEditor)
            {
                windows = true;

                System.Diagnostics.ProcessStartInfo startInfo =
                    new System.Diagnostics.ProcessStartInfo("IExplore.exe");
                startInfo.Arguments = uri.ToString();

                browser = new System.Diagnostics.Process();
                browser.StartInfo = startInfo;
                browser.Start();
            }
            else
            {
                browser = System.Diagnostics.Process.Start(uri.ToString());
            }

            // Authorization code will redirect to this server.
            AuthRedirectionServer server = new AuthRedirectionServer();
            server.StartServer(SERVER_PORT);

            // Wait for authorization code.
            while (!windows || !browser.HasExited)
            {
                if (server.AuthorizationCode != null)
                {
                    browser.CloseMainWindow();
                    browser.Close();
                    break;
                }
                else
                    yield return null;
            }

            server.StopServer();

            // Authorization rejected.
            if (server.AuthorizationCode == null)
            {
                yield return new Exception(-1, "Authorization rejected.");
                yield break;
            }

            authorizationCode = server.AuthorizationCode;

            // Get the access token by the authroization code.
            var getAccessToken = GetAccessTokenByAuthorizationCode(authorizationCode);
            {
                while (getAccessToken.MoveNext())
                    yield return null;

                if (getAccessToken.Current is Exception)
                {
                    yield return getAccessToken.Current;
                    yield break;
                }

                var res = (TokenResponse)getAccessToken.Current;
                if (res.error != null)
                {
                    yield return res.error;
                    yield break;
                }

                AccessToken = res.accessToken;
                RefreshToken = res.refreshToken;
            }

            // And validate for email address.
            var validate = ValidateToken(accessToken);
            {
                while (validate.MoveNext())
                    yield return null;

                if (validate.Current is Exception)
                {
                    yield return validate.Current;
                    yield break;
                }

                var res = (TokenInfoResponse)validate.Current;
                if (res.error != null)
                {
                    yield return res.error;
                    yield break;
                }

                if (res.email != null)
                    UserAccount = res.email;
            }
        }

        IEnumerator RefreshAccessToken()
        {
            var refresh = GetAccessTokenByRefreshToken(RefreshToken);
            {
                while (refresh.MoveNext())
                    yield return null;

                if (refresh.Current is Exception)
                {
                    yield return refresh.Current;
                    yield break;
                }

                var res = (TokenResponse)refresh.Current;
                if (res.error != null)
                {
                    yield return res.error;
                    yield break;
                }

                AccessToken = res.accessToken;
            }

            // And validate for email address.
            var validate = ValidateToken(accessToken);
            {
                while (validate.MoveNext())
                    yield return null;

                if (validate.Current is Exception)
                {
                    yield return validate.Current;
                    yield break;
                }

                var res = (TokenInfoResponse)validate.Current;
                if (res.error != null)
                {
                    yield return res.error;
                    yield break;
                }

                if (res.email != null)
                    UserAccount = res.email;
            }
        }


        IEnumerator CheckExpiration()
        {
            if (DateTime.Now >= expiresIn)
            {
                var refresh = RefreshAccessToken();
                while (refresh.MoveNext())
                    yield return null;

                yield return refresh.Current;
            }
        }

        struct TokenResponse
        {
            public Exception error;
            public string accessToken;
            public string refreshToken;
            public int expiresIn;
            public string tokenType;

            public TokenResponse(Dictionary<string, object> json)
            {
                error = null;
                accessToken = null;
                refreshToken = null;
                expiresIn = 0;
                tokenType = null;

                if (json.ContainsKey("error"))
                {
                    error = GetError(json);
                }
                else
                {
                    if (json.ContainsKey("access_token"))
                        accessToken = json["access_token"] as string;
                    if (json.ContainsKey("refresh_token"))
                        refreshToken = json["refresh_token"] as string;
                    if (json.ContainsKey("expires_in"))
                        expiresIn = (int)json["expires_in"];
                    if (json.ContainsKey("token_type"))
                        tokenType = json["token_type"] as string;
                }
            }
        }


        IEnumerator GetAccessTokenByAuthorizationCode(string authorizationCode)
        {
            var request = new UnityWebRequest("https://accounts.google.com/o/oauth2/token");

            request.method = "POST";
            request.headers["Content-Type"] = "application/x-www-form-urlencoded";
            request.body = Encoding.UTF8.GetBytes(string.Format(
                    "code={0}&" +
                    "client_id={1}&" +
                    "client_secret={2}&" +
                    "redirect_uri={3}&" +
                    "grant_type=authorization_code",
                    authorizationCode, ClientID, ClientSecret, RedirectURI));

            var response = request.GetResponse();
            while (!response.isDone)
                yield return null;

            if (response.error != null)
            {
                yield return response.error;
                yield break;
            }

            JsonReader reader = new JsonReader(response.text);
Dictionary<string, object> json = reader.Deserialize<Dictionary<string, object>>();

            if (json == null)
            {
                yield return new Exception(-1, "GetAccessToken response parsing failed.");
                yield break;
            }

            yield return new TokenResponse(json);
        }

        IEnumerator GetAccessTokenByRefreshToken(string refreshToken)
        {
            var request = new UnityWebRequest("https://accounts.google.com/o/oauth2/token");

            request.method = "POST";
            request.headers["Content-Type"] = "application/x-www-form-urlencoded";
            request.body = Encoding.UTF8.GetBytes(string.Format(
                    "client_id={0}&" +
                    "client_secret={1}&" +
                    "refresh_token={2}&" +
                    "grant_type=refresh_token",
                    ClientID, ClientSecret, refreshToken));

            var response = request.GetResponse();
            while (!response.isDone)
                yield return null;

            if (response.error != null)
            {
                yield return response.error;
                yield break;
            }

           JsonReader reader = new JsonReader(response.text);
Dictionary<string, object> json = reader.Deserialize<Dictionary<string, object>>();;

            if (json == null)
            {
                yield return new Exception(-1, "RefreshToken response parsing failed.");
                yield break;
            }

            yield return new TokenResponse(json);
        }

        struct TokenInfoResponse
        {
            public Exception error;
            public string audience;
            public string scope;
            public string userId;
            public int expiresIn;
            public string email;

            public TokenInfoResponse(Dictionary<string, object> json)
            {
                error = null;
                audience = null;
                scope = null;
                userId = null;
                expiresIn = 0;
                email = null;

                if (json.ContainsKey("error"))
                {
                    error = GetError(json);
                }
                else
                {
                    if (json.ContainsKey("audience"))
                        audience = json["audience"] as string;
                    if (json.ContainsKey("scope"))
                        scope = json["scope"] as string;
                    if (json.ContainsKey("user_id"))
                        userId = json["user_id"] as string;
                    if (json.ContainsKey("expires_in"))
                        expiresIn = (int)json["expires_in"];
                    if (json.ContainsKey("email"))
                        email = json["email"] as string;
                }
            }
        }

        static IEnumerator ValidateToken(string accessToken)
        {
            var request = new UnityWebRequest(
                          "https://www.googleapis.com/oauth2/v1/tokeninfo?access_token=" + accessToken);

            var response = request.GetResponse();
            while (!response.isDone)
                yield return null;

            if (response.error != null)
            {
                yield return response.error;
                yield break;
            }

          JsonReader reader = new JsonReader(response.text);
            Dictionary<string, object> json = reader.Deserialize<Dictionary<string, object>>();

            if (json == null)
            {
                yield return new Exception(-1, "TokenInfo response parsing failed.");
                yield break;
            }

            yield return new TokenInfoResponse(json);
        }


        struct RevokeResponse
        {
            public Exception error;

            public RevokeResponse(Dictionary<string, object> json)
            {
                if (json.ContainsKey("error"))
                    error = GetError(json);
                else
                    error = null;
            }
        }

        static IEnumerator RevokeToken(string token)
        {
            var request = new UnityWebRequest(
                          "https://accounts.google.com/o/oauth2/revoke?token=" + token);

            var response = request.GetResponse();
            while (!response.isDone)
                yield return null;

            if (response.error != null)
            {
                yield return response.error;
                yield break;
            }

         JsonReader reader = new JsonReader(response.text);
        Dictionary<string, object> json = reader.Deserialize<Dictionary<string, object>>();

            if (json == null) // no response is success.
			yield return new RevokeResponse(); // error is null.
		else
                yield return new RevokeResponse(json);
        }
    }
}