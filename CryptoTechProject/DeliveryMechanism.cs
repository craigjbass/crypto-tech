using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Web;
using CryptoTechProject.Boundary;
using Frank.API.WebDevelopers.DTO;
using Newtonsoft.Json;
using static Frank.API.WebDevelopers.DTO.ResponseBuilders;


namespace CryptoTechProject
{
    public class DeliveryMechanism
    {
        HttpListener httpListener = new HttpListener();

        private IToggleWorkshopAttendance _toggleWorkshopAttendance;
        private IGetWorkshops _getWorkshops;
        private readonly string _port;

        public DeliveryMechanism(IToggleWorkshopAttendance toggleWorkshopAttendance, IGetWorkshops getWorkshops,
            string port)
        {
            _toggleWorkshopAttendance = toggleWorkshopAttendance;
            _getWorkshops = getWorkshops;
            _port = port;
        }

        public void Run(Action onStarted)
        {
            Frank.Server.Configure().OnRequest(route =>
            {
                route.Get("/attend").To(HandleInteractiveSlackButton)
                    .Post("/").To(ShowWorkshopsInSlack);
                
            }).StartListeningOn(int.Parse(_port));
            onStarted();
            SpinWait.SpinUntil(() => false);
        }

        private Response HandleInteractiveSlackButton(Request request)
        {
            var payload = request.Body;

            var firstString = HttpUtility.UrlDecode(payload);
            var payloadString = HttpUtility.ParseQueryString(firstString);

            Dictionary<string, string> dictionary = payloadString.Keys.Cast<string>()
                .ToDictionary(k => k, k => payloadString[k]);


            SlackButtonPayload deserialisedPayload = JsonConvert.DeserializeObject<SlackButtonPayload>(dictionary["payload"]);
            
            ToggleWorkshopAttendanceRequest toggleWorkshopAttendanceRequest = new ToggleWorkshopAttendanceRequest()
            {
                User = deserialisedPayload.User.Name, 
                WorkshopId = deserialisedPayload.Actions[0].Value
            };

            string response_url = deserialisedPayload.ResponseURL;

            _toggleWorkshopAttendance.Execute(toggleWorkshopAttendanceRequest);

            GetWorkshopsResponse workshops = _getWorkshops.Execute();
            var slackMessage = ToSlackMessage(workshops, toggleWorkshopAttendanceRequest.User);
            string jsonForSlack = JsonConvert.SerializeObject(slackMessage);


            WebClient webClient = new WebClient();
            webClient.Headers.Add("Content-type", "application/json");
            webClient.UploadString(response_url, "POST", jsonForSlack);

            return Ok();
        }

        private Response ShowWorkshopsInSlack(Request request)
        {
            var payload = request.Body;
            var payloadString = HttpUtility.ParseQueryString(payload);
            string user = payloadString.Get("user_name");

            GetWorkshopsResponse workshops = _getWorkshops.Execute();
            var slackMessage = ToSlackMessage(workshops, user);

            Console.WriteLine("no payload");

            return Ok().WithHeader("Content-type", "application/json").WithJsonBody(slackMessage);
        }

        private static SlackMessage ToSlackMessage(GetWorkshopsResponse workshops, string user)
        {
            SlackMessage slackMessage = new SlackMessage
            {
                Blocks = new SlackMessage.SlackMessageBlock[workshops.PresentableWorkshops.Length + 2]
            };

            slackMessage.Blocks[0] = new SlackMessage.TitleSectionBlock
            {
                Text = new SlackMessage.SectionBlockText
                {
                    Type = "mrkdwn",
                    Text = "*Workshops*"
                }
            };

            slackMessage.Blocks[1] = new SlackMessage.DividerBlock
            {
                Type = "divider"
            };

            for (int i = 0; i < workshops.PresentableWorkshops.Length; i++)
            {
                string attendanceStatus = "Attend";
                if (workshops.PresentableWorkshops[i].Attendees.Contains(user))
                {
                    attendanceStatus = "Unattend";
                }


                if (workshops.PresentableWorkshops[i].Type == "Showcase")
                {
                    string showcaseText = $"*{workshops.PresentableWorkshops[i].Name}*\n" +
                                          $"{workshops.PresentableWorkshops[i].Time.ToString("dd/MM/yyyy hh:mm tt")}\n" +
                                          $"{workshops.PresentableWorkshops[i].Host}\n";

                    if (i < workshops.PresentableWorkshops.Length - 1)
                        if (workshops.PresentableWorkshops[i].Time.Day !=
                            workshops.PresentableWorkshops[i + 1].Time.Day)
                            showcaseText = showcaseText +
                                           "---------------------------------------------------------------------------------------------------------\n";
                    slackMessage.Blocks[i + 2] = new SlackMessage.ShowcaseSectionBlock
                    {
                        Text = new SlackMessage.SectionBlockText
                        {
                            Type = "mrkdwn",
                            Text = showcaseText
                        }
                    };
                }
                else
                {
                    slackMessage.Blocks[i + 2] = new SlackMessage.SectionBlock
                    {
                        Text = new SlackMessage.SectionBlockText
                        {
                            Type = "mrkdwn",
                            Text = $"*{workshops.PresentableWorkshops[i].Name}*\n" +
                                   $"{workshops.PresentableWorkshops[i].Time.ToString("dd/MM/yyyy hh:mm tt")}\n" +
                                   $"{workshops.PresentableWorkshops[i].Host}\n" +
                                   $"Current number of attendees: {workshops.PresentableWorkshops[i].Attendees.Count}"
                        },
                        Accessory = new SlackMessage.SectionBlock.AccessoryBlock
                        {
                            Text = new SlackMessage.SectionBlockText
                            {
                                Type = "plain_text",
                                Text = attendanceStatus
                            },
                            Value = workshops.PresentableWorkshops[i].ID
                        }
                    };
                }
            }

            return slackMessage;
        }
    }
}