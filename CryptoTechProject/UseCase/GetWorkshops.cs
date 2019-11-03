using System;
using System.Linq;
using CryptoTechProject.Boundary;
using Newtonsoft.Json.Linq;
using TimeZoneConverter;

namespace CryptoTechProject
{
    public class GetWorkshops : IGetWorkshops
    {
        private IWorkshopsGateway gateway;

        // Constructor below:
        public GetWorkshops(IWorkshopsGateway gateway)
        {
            this.gateway = gateway;
        }

        public GetWorkshopsResponse Execute()
        {
            var list = gateway.All();
            if (list.Count == 0)
                return new GetWorkshopsResponse()
                {
                    PresentableWorkshops = new PresentableWorkshop[] { }
                };

            var getWorkshopsResponse = new GetWorkshopsResponse()
            {
                PresentableWorkshops = new PresentableWorkshop[list.Count]
            };
            for (int i = 0; i < list.Count; i++)
            {
                getWorkshopsResponse.PresentableWorkshops[i] = new PresentableWorkshop()
                {
                    ID = list[i].id,
                    Name = list[i].name,
                    Host = list[i].host,
                    // Time = list[i].time.ToLocalTime(),
                    Time = new DateTimeOffset(list[i].time, TimeSpan.Zero).ToOffset(
                        TZConvert.GetTimeZoneInfo("Europe/London").GetUtcOffset(list[i].time)
                    ),
                    Location = list[i].location,
                    Duration = list[i].duration,
                    Type = list[i].type,
                    Attendees = list[i].attendees
                };
            }

            return getWorkshopsResponse;
        }
    }
}