using StargateAPI.Business.Data;
using StargateAPI.Business.Queries;

namespace StargateAPI.Tests
{
    public class GetPersonByNameHandlerTests
    {
        [Fact]
        public async Task Handle_PersonExistsWithoutAstronautDetail_ReturnsPerson()
        {
            using var context = DbContextFactory.CreateInMemoryContext();

            var person = new Person { Name = "Daniel Jackson" };
            context.People.Add(person);
            await context.SaveChangesAsync();

            var handler = new GetPersonByNameHandler(context);

            var query = new GetPersonByName
            {
                Name = "Daniel Jackson"
            };

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.NotNull(result);
            Assert.NotNull(result.Person);
            Assert.Equal("Daniel Jackson", result.Person!.Name);
            Assert.Equal(string.Empty, result.Person.CurrentRank);
            Assert.Equal(string.Empty, result.Person.CurrentDutyTitle);
        }

        [Fact]
        public async Task Handle_PersonDoesNotExist_ReturnsNullPerson()
        {
            using var context = DbContextFactory.CreateInMemoryContext();

            var handler = new GetPersonByNameHandler(context);

            var query = new GetPersonByName
            {
                Name = "Missing Person"
            };

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Null(result.Person);
        }
    }
}
