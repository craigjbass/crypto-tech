using System;
using System.Collections.Generic;
using CryptoTechProject.Boundary;
using CryptoTechProject.Domain;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TimeZoneConverter;

namespace CryptoTechProject.Tests
{
    [TestFixture]
    public class AcceptanceTest
    {
        private string AIRTABLE_API_KEY = "111";
        private string TABLE_ID = "2";
        private string AIRTABLE_URL = "http://localhost:8080/";

        AirtableSimulator airtableSimulator;

        [SetUp]
        public void SetUp()
        {
            airtableSimulator = new AirtableSimulator();
            airtableSimulator.Start();
        }

        [TearDown]
        public void TearDown()
        {
            airtableSimulator.Stop();
        }

        [Test]
        public void CanGetTwoAirtableWorkshops()
        {
            var expectedResponse = new AirtableResponseBuilder()
                .AddRecord(
                    "rec4rdaOkafgV1Bqm",
                    new DateTime(2019, 8, 22, 8, 25, 28)
                ).WithName("Team Performance: Team Agile-Lean maturity 'measures' in practice (at DfE and Hackney)")
                .WithHost("Barry")
                .WithCategories("Delivery")
                .WithTime(2019, 10, 18, 13, 0, 0)
                .WithDurationInSeconds(3600)
                .WithLocation("Everest, 2nd Foor")
                .WithSessionType("Seminar")
                .AddRecord(
                    "reca7W6WxWubIR7CK",
                    new DateTime(2019, 8, 27, 5, 24, 25)
                )
                .WithName("Account Leadership - Roles & Responsibilities")
                .WithHost("Rory")
                .WithCategories("Sales", "Workshop", "Life Skills", "Business")
                .WithTime(2019, 10, 18, 14, 30, 0)
                .WithDurationInSeconds(3600)
                .WithLocation("Everest")
                .WithSessionType("Workshop")
                .Build();

            airtableSimulator.SetUpAll(
                TABLE_ID,
                AIRTABLE_API_KEY,
                expectedResponse
            );


            AirtableGateway airtableGateway = new AirtableGateway(AIRTABLE_URL, AIRTABLE_API_KEY, TABLE_ID);
            GetWorkshops getWorkshops = new GetWorkshops(airtableGateway);
            GetWorkshopsResponse response = getWorkshops.Execute();

            DateTime sourceDate = new DateTime(2019, 10, 18, 14, 00, 0);
            DateTimeOffset time = new DateTimeOffset(sourceDate,
                TZConvert.GetTimeZoneInfo("Europe/London").GetUtcOffset(sourceDate));


            DateTime sourceDate2 = new DateTime(2019, 10, 18, 15, 30, 0);
            DateTimeOffset time2 = new DateTimeOffset(sourceDate2,
                TZConvert.GetTimeZoneInfo("Europe/London").GetUtcOffset(sourceDate2));

            PresentableWorkshop[] presentableWorkshops = response.PresentableWorkshops;

            Assert.AreEqual("Team Performance: Team Agile-Lean maturity 'measures' in practice (at DfE and Hackney)",
                presentableWorkshops[0].Name);
            Assert.AreEqual("Barry", presentableWorkshops[0].Host);
            Assert.AreEqual(time, presentableWorkshops[0].Time);
            Assert.AreEqual("Everest, 2nd Foor", presentableWorkshops[0].Location);
            Assert.AreEqual(60, presentableWorkshops[0].Duration);
            Assert.AreEqual("Seminar", presentableWorkshops[0].Type);

            Assert.AreEqual("Account Leadership - Roles & Responsibilities", response.PresentableWorkshops[1].Name);
            Assert.AreEqual("Rory", presentableWorkshops[1].Host);
            Assert.AreEqual(time2, presentableWorkshops[1].Time);
            Assert.AreEqual("Everest", presentableWorkshops[1].Location);
            Assert.AreEqual(60, presentableWorkshops[1].Duration);
            Assert.AreEqual("Workshop", presentableWorkshops[1].Type);
        }

        [Test]
        public void AddsUserToAirtableTable()
        {
            var expectedResponse = new AirtableResponseBuilder()
                .AddRecord(
                    "rec4rdaOkafgV1Bqm",
                    new DateTime(2019, 8, 22, 8, 25, 28)
                ).WithName("Team Performance: Team Agile-Lean maturity 'measures' in practice (at DfE and Hackney)")
                .WithHost("Barry")
                .WithCategories("Delivery")
                .WithTime(2019, 10, 18, 13, 0, 0)
                .WithDurationInSeconds(3600)
                .WithLocation("Everest, 2nd Foor")
                .WithSessionType("Seminar")
                .WithAttendees(new List<string>())
                .Build();

            airtableSimulator.SetUpFind(TABLE_ID, AIRTABLE_API_KEY, expectedResponse.Records[0], "ID000");
            airtableSimulator.SetUpSave(TABLE_ID, AIRTABLE_API_KEY);

            AirtableGateway gateway = new AirtableGateway(AIRTABLE_URL, AIRTABLE_API_KEY, TABLE_ID);
            ToggleWorkshopAttendance attend = new ToggleWorkshopAttendance(gateway, gateway);
            ToggleWorkshopAttendanceRequest payload = new ToggleWorkshopAttendanceRequest();
            payload.User = "Maria";
            payload.WorkshopId = "ID000";
            attend.Execute(payload);

            var requests = airtableSimulator.simulator.ReceivedRequests;
            Console.WriteLine(requests);
            var sentEmployee = requests[1].BodyAs<AirtableRequest>();

            Assert.AreEqual("Maria", sentEmployee.Records[0].Fields.Attendees[0]);
        }
        
        [Test]
        public void RemovesUserFromAirtableTable()
        {
            var expectedResponse = new AirtableResponseBuilder()
                .AddRecord(
                    "rec4rdaOkafgV1Bqm",
                    new DateTime(2019, 8, 22, 8, 25, 28)
                ).WithName("Team Performance: Team Agile-Lean maturity 'measures' in practice (at DfE and Hackney)")
                .WithHost("Barry")
                .WithCategories("Delivery")
                .WithTime(2019, 10, 18, 13, 0, 0)
                .WithDurationInSeconds(3600)
                .WithLocation("Everest, 2nd Foor")
                .WithSessionType("Seminar")
                .WithAttendees(new List<string>(){"Maria", "Kat"})
                .Build();

            airtableSimulator.SetUpFind(TABLE_ID, AIRTABLE_API_KEY, expectedResponse.Records[0], "ID000");
            airtableSimulator.SetUpSave(TABLE_ID, AIRTABLE_API_KEY);

            AirtableGateway gateway = new AirtableGateway(AIRTABLE_URL, AIRTABLE_API_KEY, TABLE_ID);
            ToggleWorkshopAttendance attend = new ToggleWorkshopAttendance(gateway, gateway);
            ToggleWorkshopAttendanceRequest payload = new ToggleWorkshopAttendanceRequest();
            payload.User = "Maria";
            payload.WorkshopId = "ID000";
            attend.Execute(payload);

            var requests = airtableSimulator.simulator.ReceivedRequests;
            Console.WriteLine(requests);
            var sentEmployee = requests[1].BodyAs<AirtableRequest>();

            Assert.IsFalse(sentEmployee.Records[0].Fields.Attendees.Contains("Maria"));
        }
    }
}