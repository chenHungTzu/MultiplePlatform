using Amazon.AppConfigData;
using Amazon.AppConfigData.Model;
using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Mvc.Testing;
using PlatformCore;
using System.Net.Http.Json;
using System.Text.Json;
using static PlatformCoreTest.MockHttpClientFactory<PlatformCore.Program>;

namespace PlatformCoreTest
{
    public class VersionMap
    {
        public class Version
        {
            public bool enabled { get; set; }
            public string image_version { get; set; }
        }

        public Version platformB { get; set; }
        public Version platformC { get; set; }
    }

    public class TestBase : IAsyncLifetime
    {
        protected VersionMap VersionMap;
        protected readonly MockHttpClient MockHttpClient;
        private AmazonAppConfigDataClient amazonAppConfigDataClient;

        public TestBase()
        {
            MockHttpClient = new MockHttpClientFactory<Program>().Create();
        }

        /// <summary>
        /// 取得容器版本資訊
        /// </summary>
        /// <returns></returns>
        private async Task getVersionInfo()
        {
            var applicationId = "90ugj76";
            var environmentId = "wv8s5ct";
            var configurationProfileId = "c3u90n8";

            var startSessionResponse =
                await amazonAppConfigDataClient.StartConfigurationSessionAsync(
                    new StartConfigurationSessionRequest
                    {
                        ApplicationIdentifier = applicationId,
                        EnvironmentIdentifier = environmentId,
                        ConfigurationProfileIdentifier = configurationProfileId
                    });

            string nextToken = startSessionResponse.InitialConfigurationToken;

            var request = new GetLatestConfigurationRequest
            {
                ConfigurationToken = nextToken
            };

            var response = await amazonAppConfigDataClient.GetLatestConfigurationAsync(request);

            using (var reader = new StreamReader(response.Configuration))
            {
                var configurationString = await reader.ReadToEndAsync();
                VersionMap = JsonSerializer.Deserialize<VersionMap>(configurationString);

                // 非webhost環境,不須要使用nextToken做polling
                // nextToken = response.NextPollConfigurationToken;
            }
        }

        /// <summary>
        /// 啟動容器
        /// </summary>
        /// <param name="imageWithTag"></param>
        /// <param name="containerPort"></param>
        /// <returns>uri of container</returns>
        public async Task<string> LaunchContainer(string imageWithTag, ushort containerPort = 80)
        {
            var container = new ContainerBuilder()
                    .WithImage(imageWithTag)
                    .WithPortBinding(containerPort, true)
                    .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request.ForPath("/App")))
                    .WithStartupCallback((c, t) => Task.CompletedTask)
                    .Build();

            await container.StartAsync()
                           .ConfigureAwait(false);

            var requestUri =
                new UriBuilder(Uri.UriSchemeHttp,
                container.Hostname,
                container.GetMappedPublicPort(containerPort)).Uri.ToString();

            return requestUri;
        }

        /// <summary>
        /// inherit from IAsyncLifetime
        /// </summary>
        /// <returns></returns>
        public async Task InitializeAsync()
        {
            amazonAppConfigDataClient = new AmazonAppConfigDataClient(region: Amazon.RegionEndpoint.APNortheast1);
            await getVersionInfo();
        }

        /// <summary>
        /// inherit from IAsyncLifetime
        /// </summary>
        /// <returns></returns>
        public Task DisposeAsync() => Task.CompletedTask;
    }

    public class MockHttpClientFactory<T> : WebApplicationFactory<T> where T : class
    {
        public MockHttpClient Create()
        {
            var client = CreateClient();

            return new MockHttpClient(client);
        }

        public class MockHttpClient
        {
            private readonly HttpClient _httpClient;

            public MockHttpClient(HttpClient httpClient)
            {
                _httpClient = httpClient;
            }

            public async Task<TResult?> GetAsync<TResult>(string uri)
            {
                HttpResponseMessage response = await _httpClient.GetAsync(uri);

                if (response.IsSuccessStatusCode == false)
                    throw new Exception(response.ReasonPhrase);

                return await response.Content.ReadFromJsonAsync<TResult>();
            }
        }
    }

    public class UnitTest1 : TestBase
    {
        /// <summary>
        /// 執行測試
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Test()
        {
            try
            {
                // 取得版本資訊後,得到對應平台的版本號
                // 接著透過testContainer啟動容器

                // 啟動PlatformB
                var containerBUrl = await LaunchContainer(VersionMap.platformB.image_version);

                // 啟動PlatformC
                var containerCUrl = await LaunchContainer(VersionMap.platformC.image_version);

                Environment.SetEnvironmentVariable("platformB", containerBUrl);
                Environment.SetEnvironmentVariable("platformC", containerCUrl);

                // 啟動Webhost進行驗證
                var result = await MockHttpClient.GetAsync<WeatherForecast[]>("WeatherForecast");

                Assert.Equal(5, result.Length);
                Assert.Equal(3, result.Count(x => x.Summary == "CV2"));
                Assert.Equal(2, result.Count(x => x.Summary == "BV2"));
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
        }
    }
}