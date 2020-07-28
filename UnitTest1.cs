using NBomber.CSharp;
using NBomber.Http.CSharp;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ExploreSolutionBomberTests
{
    public class UnitTest1
    {
        //[Fact]
        public void Test1()
        {
            var step = HttpStep.Create("http pull", context =>
                Http.CreateRequest("GET", "https://nbomber.com")
                    .WithHeader("Accept", "text/html")
            //.WithHeader("Cookie", "cookie1=value1; cookie2=value2")
            //.WithBody(new StringContent("{ some JSON }", Encoding.UTF8, "application/json"))
            //.WithCheck(response => Task.FromResult(response.IsSuccessStatusCode))
            );

            var scenario = ScenarioBuilder.CreateScenario("test_nbomber", new[] { step })
//                .WithConcurrentCopies(100)
.WithLoadSimulations()
                .WithWarmUpDuration(TimeSpan.FromSeconds(10));
  //              .WithDuration(TimeSpan.FromSeconds(20));

//            NBomberRunner.RegisterScenarios(scenario).RunInConsole();
        }

        [Fact]
        public async void ExploreSolution_GetAllTours_LoadTest()
        {
            string baseUrl = "https://exploresolutionapi.azurewebsites.net";
            string loginUrl = baseUrl + "/api/Authentication/request";
            string getAllToursUrl = baseUrl + "/api/tours/getAll";

            var loginStep = HttpStep.Create("login", context =>
            Http.CreateRequest("POST", loginUrl)
            //.WithBody(new StringContent(@"{ ""username"" : ""user"", ""password"": ""pass"" }"))
            //.WithHeader("Content-Type", "application/json")
            .WithBody(new StringContent(JsonConvert.SerializeObject(new LoginModel {Username = "user", Password = "pass" }), Encoding.UTF8, "application/json"))
            .WithCheck(response => Task.FromResult(response.IsSuccessStatusCode))
            .WithCheck(async (response) =>
            {
                string token = await response.Content.ReadAsStringAsync();
                return token != string.Empty;
            }));

            var getAllTours = HttpStep.Create("getAllTours", async (context) =>
            {
                var responseContent = context.Data as HttpResponseMessage;
                var authToken = await responseContent.Content.ReadAsStringAsync();
                return Http.CreateRequest("GET", getAllToursUrl)
                .WithHeader("Authorization", $"Bearer {authToken}")
                .WithCheck(response => Task.FromResult(response.IsSuccessStatusCode));
            });


            var scenario = ScenarioBuilder.CreateScenario("Get All Tours", new[] { loginStep, getAllTours });
//                .WithAssertions()
//                .WithConcurrentCopies(1)
//                .WithDuration(TimeSpan.FromMinutes(1));

//            NBomberRunner.RegisterScenarios(scenario).RunTest();
        }
    }

    public class LoginModel
    {
        public string Username { get; set; }

        public string Password { get; set; }
    }
}
