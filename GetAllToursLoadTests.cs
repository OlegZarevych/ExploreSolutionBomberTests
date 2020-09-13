using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Plugins.Http.CSharp;
using NBomber.Sinks.InfluxDB;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using Xunit;

namespace ExploreSolutionBomberTests
{
    public class GetAllToursLoadTests
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
                .WithWarmUpDuration(TimeSpan.FromSeconds(10))
                .WithLoadSimulations(new[]
                {
                    Simulation.InjectPerSec(10, TimeSpan.FromSeconds(10))
                });

            NBomberRunner.RegisterScenarios(scenario).Run();
        }

        [Fact]
        public async void ExploreSolution_GetAllTours_LoadTest()
        {
            string baseUrl = "https://exploresolutionapi.azurewebsites.net";
            string loginUrl = baseUrl + "/api/Authentication/request";
            string getAllToursUrl = baseUrl + "/api/tours/getAll";

            IStep loginStep = Auth(loginUrl);

            var getAllTours = HttpStep.Create("getAllTours", async (context) =>
            {
                var responseContent = context.Data as HttpResponseMessage;
                var authToken = await responseContent.Content.ReadAsStringAsync();
                return Http.CreateRequest("GET", getAllToursUrl)
                .WithHeader("Authorization", $"Bearer {authToken}")
                .WithCheck(async response =>
                {
                    return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
                });
           
            });


            var scenario = ScenarioBuilder.CreateScenario("Get All Tours", new[] { loginStep, getAllTours })
                .WithoutWarmUp()
                .WithLoadSimulations(new[]
                {
                    Simulation.KeepConstant(1, TimeSpan.FromMinutes(1))
                });

            var nodeStats = NBomberRunner.RegisterScenarios(scenario).Run();
        }

        [Fact]
        public async void ExploreSolution_GetAllReservations_LoadTest()
        {
            string baseUrl = "https://exploresolutionapi.azurewebsites.net";
            string loginUrl = baseUrl + "/api/Authentication/request";
            string getAllToursUrl = baseUrl + "/api/reservations/GetAllReservations";
            TimeSpan warmUp = TimeSpan.FromMinutes(1);

            IStep loginStep = Auth(loginUrl);
            
            var getAllReservations = HttpStep.Create("getReservations", async (context) =>
            {
                var responseContent = context.Data as HttpResponseMessage;
                var authToken = await responseContent.Content.ReadAsStringAsync();
                return Http.CreateRequest("GET", getAllToursUrl)
                .WithHeader("Authorization", $"Bearer {authToken}");
               //.WithCheck(async response =>
               //{
               //    return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
               //});
            });

            var influxConfig = InfluxDbSinkConfig.Create("https://westeurope-1.azure.cloud2.influxdata.com", dbName: "oleg.zarevych@gmail.com");
            var influxDb = new InfluxDBSink(influxConfig);

            var scenario = ScenarioBuilder.CreateScenario("Get All Reservations", new[] { loginStep, getAllReservations })
                .WithWarmUpDuration(warmUp)
                .WithLoadSimulations(new[]
                {
                    Simulation.RampPerSec(10, TimeSpan.FromMinutes(1))
                });

            var nodeStats = NBomberRunner.RegisterScenarios(scenario).Run();
        }

        private static IStep Auth(string loginUrl)
        {
            var loginStep = HttpStep.Create("login", context =>
            Http.CreateRequest("POST", loginUrl)
            //.WithBody(new StringContent(@"{ ""username"" : ""user"", ""password"": ""pass"" }"))
            //.WithHeader("Content-Type", "application/json")
            .WithBody(new StringContent(JsonConvert.SerializeObject(new LoginModel { Username = "user", Password = "pass" }), Encoding.UTF8, "application/json"))
            .WithCheck(async response =>
               {
                   return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
               })
            .WithCheck(async response =>
            {
                string token = await response.Content.ReadAsStringAsync();
                return token != string.Empty ? Response.Ok() : Response.Fail();
            }));
            return loginStep;
        }
    }


    public class LoginModel
    {
        public string Username { get; set; }

        public string Password { get; set; }
    }
}
