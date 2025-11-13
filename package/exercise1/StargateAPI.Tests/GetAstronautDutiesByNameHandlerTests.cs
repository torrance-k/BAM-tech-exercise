using StargateAPI.Business.Data;
using StargateAPI.Business.Queries;

namespace StargateAPI.Tests
{
    public class GetAstronautDutiesByNameHandlerTests
    {
        [Fact]
        public async Task Handle_AstronautWithDuties_ReturnsPersonAndDuties()
        {
            using var context = DbContextFactory.CreateInMemoryContext();

            var person = new Person { Name = "Teal'c" };
            context.People.Add(person);
            await context.SaveChangesAsync();

            context.AstronautDetails.Add(new AstronautDetail
            {
                PersonId = person.Id,
                CurrentRank = "Jaffa Warrior",
                CurrentDutyTitle = "Advisor",
                CareerStartDate = new DateTime(2020, 1, 1)
            });

            var duty1Start = new DateTime(2020, 1, 1);
            var duty2Start = new DateTime(2021, 1, 1);

            context.AstronautDuties.Add(new AstronautDuty
            {
                PersonId = person.Id,
                Rank = "Jaffa Warrior",
                DutyTitle = "First Prime",
                DutyStartDate = duty1Start,
                DutyEndDate = duty2Start.AddDays(-1)
            });

            context.AstronautDuties.Add(new AstronautDuty
            {
                PersonId = person.Id,
                Rank = "Jaffa Warrior",
                DutyTitle = "Advisor",
                DutyStartDate = duty2Start,
                DutyEndDate = null
            });

            await context.SaveChangesAsync();

            var handler = new GetAstronautDutiesByNameHandler(context);

            var query = new GetAstronautDutiesByName
            {
                Name = "Teal'c"
            };

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.NotNull(result);
            Assert.True(result.Success); // default from BaseResponse, but handler does not set it to false
            Assert.NotNull(result.Person);
            Assert.Equal("Teal'c", result.Person!.Name);
            Assert.Equal("Jaffa Warrior", result.Person.CurrentRank);
            Assert.Equal("Advisor", result.Person.CurrentDutyTitle);

            Assert.NotNull(result.AstronautDuties);
            Assert.Equal(2, result.AstronautDuties.Count);

            // Ensure ordering by DutyStartDate Desc
            Assert.Equal("Advisor", result.AstronautDuties[0].DutyTitle);
            Assert.Equal(duty2Start, result.AstronautDuties[0].DutyStartDate);
            Assert.Equal("First Prime", result.AstronautDuties[1].DutyTitle);
            Assert.Equal(duty1Start, result.AstronautDuties[1].DutyStartDate);
        }

        [Fact]
        public async Task Handle_AstronautNotFound_ReturnsNotFoundResultWithEmptyCollections()
        {
            using var context = DbContextFactory.CreateInMemoryContext();

            var handler = new GetAstronautDutiesByNameHandler(context);

            var query = new GetAstronautDutiesByName
            {
                Name = "Missing Astronaut"
            };

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal(404, result.ResponseCode);
            Assert.Equal("Person 'Missing Astronaut' not found.", result.Message);

            // Implementation sets Person to a new PersonAstronaut, not null
            Assert.NotNull(result.Person);
            Assert.NotNull(result.AstronautDuties);
            Assert.Empty(result.AstronautDuties);
        }
    }
}
