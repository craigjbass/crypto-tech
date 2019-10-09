using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using CryptoTechProject.Domain;

namespace CryptoTechProject
{
    public class HardCodedWorkshopsGateway : IGetWorkshopsGateway
    {
        public List<Workshop> All()
        {
            DateTime sourceDate = new DateTime(2008, 5, 1, 8, 30, 0);
            DateTimeOffset time = new DateTimeOffset(sourceDate, 
                TimeZoneInfo.FindSystemTimeZoneById("Europe/London").GetUtcOffset(sourceDate));
            return new List<Workshop>()
            {
                new Workshop()
                {
                    name = "Coding Black Females - Code Dojo",
                    host = "Made Tech",
                    time = time,
                    location = "Made Tech O'Meara",
                    duration = 180,
                    type = "Code Dojo"
                }
            };
        }
    }
}