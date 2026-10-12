//using Blazor.Web.Gateway.ExternalApis;
//using Microsoft.Extensions.Logging;
//using System.Net.Http.Json;

//namespace Blazor.Web.Services
//{
//    internal class ModulesService : IModulesService
//    {
//        private const string ApiBaseUrl = "http://api:8080";

//        private readonly ILogger<ModulesService> logger;
//        private readonly IGatewayClient gatewayClient;

//        public ModulesService(ILogger<ModulesService> logger, IGatewayClient gatewayClient)
//        {
//            this.logger = logger;
//            this.gatewayClient = gatewayClient;
//        }

//        public async Task<IReadOnlyList<StudentUpcomingEventModel>> GetUpcomingEventsAsync(Guid studentId)
//        {
//            logger.LogInformation("Getting upcoming module events for student {StudentId}.", studentId);

//            var route = $"Modules/students/{studentId}/upcoming-events";
//            var response = await gatewayClient.GetAsync(ApiBaseUrl, route);

//            return await ReadResponseAsync<IReadOnlyList<StudentUpcomingEventModel>>(
//                response,
//                "upcoming module events") ?? [];
//        }

//        public async Task<IReadOnlyList<StudentUpcomingEventModel>> GetUpcomingEventsAsync(
//            Guid studentId,
//            DateTimeOffset startsAt,
//            DateTimeOffset endsAt)
//        {
//            logger.LogInformation(
//                "Getting upcoming module events for student {StudentId} between {StartsAt} and {EndsAt}.",
//                studentId,
//                startsAt,
//                endsAt);

//            var route = $"Modules/students/{studentId}/upcoming-events/range?startsAt={Escape(startsAt)}&endsAt={Escape(endsAt)}";
//            var response = await gatewayClient.GetAsync(ApiBaseUrl, route);

//            return await ReadResponseAsync<IReadOnlyList<StudentUpcomingEventModel>>(
//                response,
//                "upcoming module events") ?? [];
//        }

//        public async Task<StudentMeetingDetailModel?> GetMeetingAsync(Guid studentId, Guid meetingId)
//        {
//            logger.LogInformation(
//                "Getting meeting {MeetingId} details for student {StudentId}.",
//                meetingId,
//                studentId);

//            var route = $"Modules/students/{studentId}/meetings/{meetingId}";
//            var response = await gatewayClient.GetAsync(ApiBaseUrl, route);

//            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
//            {
//                logger.LogInformation(
//                    "Meeting {MeetingId} was not found for student {StudentId}.",
//                    meetingId,
//                    studentId);

//                return null;
//            }

//            return await ReadResponseAsync<StudentMeetingDetailModel>(response, "meeting details");
//        }

//        private static string Escape(DateTimeOffset value)
//        {
//            return Uri.EscapeDataString(value.ToString("O"));
//        }

//        private async Task<T?> ReadResponseAsync<T>(HttpResponseMessage response, string resourceName)
//        {
//            if (response.IsSuccessStatusCode)
//            {
//                var result = await response.Content.ReadFromJsonAsync<T>();
//                logger.LogInformation("Successfully retrieved {ResourceName}.", resourceName);
//                return result;
//            }

//            var body = await response.Content.ReadAsStringAsync();
//            logger.LogError(
//                "Failed to retrieve {ResourceName}. Status code: {StatusCode}. Response: {ResponseBody}",
//                resourceName,
//                response.StatusCode,
//                body);

//            throw new Exception($"Failed to retrieve {resourceName}. Status code: {response.StatusCode}");
//        }
//    }
//}

