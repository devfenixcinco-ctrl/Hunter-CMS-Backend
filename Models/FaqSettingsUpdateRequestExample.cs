using MreHunter.Api.Models;
using Swashbuckle.AspNetCore.Filters;

namespace MreHunter.Api.Models.Examples
{
    public class FaqSettingsUpdateRequestExample : IExamplesProvider<FaqSettingsUpdateRequest>
    {
        public FaqSettingsUpdateRequest GetExamples()
        {
            return new FaqSettingsUpdateRequest
            {
                HeroImageUrl = "text",
                HeroTitle = "text",
                HeroHighlightText = "text",
                HeroSubtitle = "text",
                Question1 = "text",
                Answer1 = "text",
                Question2 = "text",
                Answer2 = "text",
                Question3 = "text",
                Answer3 = "text",
                Question4 = "text",
                Answer4 = "text",
                Question5 = "text",
                Answer5 = "text",
                Question6 = "text",
                Answer6 = "text"
            };
        }
    }
}