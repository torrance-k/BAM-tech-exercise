using StargateAPI.Business.Data;
using StargateAPI.Business.Queries;

namespace StargateAPI.Tests
{
    public class GetPeopleHandlerTests
    {
        [Fact]
        public async Task Handle_ReturnsAllPeopleWithOptionalAstronautDetails()
        {
            using var context = DbContextFactory.CreateInMemoryContext();

            // Person 1: with astronaut detail
            var person1 = new Person { Name = "Jack O'Neill" };
            context.People.Add(person1);
            await context.SaveChangesAsync();

            context.AstronautDetails.Add(new AstronautDetail
            {
                PersonId = person1.Id,
                CurrentRank = "Colonel",
                CurrentDutyTitle = "CO",
                CareerStartDate = new System.DateTime(1997, 7, 27)
            });

            // Person 2: without astronaut detail
            var person2 = new Person { Name = "Samantha Carter" };
            context.People.Add(person2);

            await context.SaveChangesAsync();

            var handler = new GetPeopleHandler(context);

            var result = await handler.Handle(new GetPeople(), CancellationToken.None);

            Assert.NotNull(result);
            Assert.NotNull(result.People);
            Assert.Equal(2, result.People.Count);

            var jack = result.People.Single(p => p.Name == "Jack O'Neill");
            var sam = result.People.Single(p => p.Name == "Samantha Carter");

            // Jack should have astronaut details
            Assert.Equal("Colonel", jack.CurrentRank);
            Assert.Equal("CO", jack.CurrentDutyTitle);

            // Sam should have null astronaut details since there is no AstronautDetail row
            Assert.Equal(string.Empty, sam.CurrentRank);
            Assert.Equal(string.Empty, sam.CurrentDutyTitle);
        }
    }
}
