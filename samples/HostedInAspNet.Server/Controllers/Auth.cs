using HostedInAspNet.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace HostedInAspNet.Server.Controllers
{
    public class Auth : Controller
    {
        const string clientId = "fde5508a31ed099169cf";
        const string clientSecret = "put-it-here";

        public async Task<IActionResult> LogIn(string code)
        {
            var httpClient = CreateJsonClient();
            var response = await httpClient.PostAsync($"https://github.com/login/oauth/access_token?client_id={clientId}&client_secret={clientSecret}&code={code}", null);
            var token = await response.Content.ReadAsAsync<OAuthToken>();
            HttpContext.Session.SetString("githubauth", JsonConvert.SerializeObject(token));
            return Redirect("/");
        }

        public async Task<IActionResult> CurrentUserInfo()
        {
            var token = HttpContext.Session.GetString("githubauth");
            if (string.IsNullOrEmpty(token))
            {
                return NotFound();
            }
            var tokenParsed = JsonConvert.DeserializeObject<OAuthToken>(token);

            var httpClient = CreateJsonClient();
            var url = $"https://api.github.com/user?access_token={tokenParsed.access_token}";
            var userResponse = await httpClient.GetAsync(url);
            var userData = await userResponse.Content.ReadAsAsync<UserData>();

            return new ObjectResult(new UserInfo
            {
                UserName = userData.login,
                AvatarUrl = userData.avatar_url
            });
        }

        private static HttpClient CreateJsonClient()
        {
            // TODO: HttpClientFactory etc...
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("MyApp", "1.0"));
            return httpClient;
        }

        class OAuthToken
        {
            public string access_token { get; set; }
            public string scope { get; set; }
            public string token_type { get; set; }
        }

        class UserData
        {
            public string login { get; set; }
            public string avatar_url { get; set; }
        }
    }
}
