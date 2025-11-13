using StargateAPI.Business.Commands;

namespace StargateAPI.Tests
{
    public class CreatePersonHandlerTests
    {
        [Fact]
        public async Task Handle_ValidRequest_InsertsPersonAndReturnsId()
        {
            using var context = DbContextFactory.CreateInMemoryContext();
            var handler = new CreatePersonHandler(context);

            var request = new CreatePerson
            {
                Name = "New Person"
            };

            var result = await handler.Handle(request, CancellationToken.None);

            // Verify person is persisted
            var person = await context.People.FindAsync(result.Id);
            Assert.NotNull(person);
            Assert.Equal("New Person", person!.Name);

            // Verify result
            Assert.Equal(person.Id, result.Id);
        }

        [Fact]
        public async Task Handle_TrimmedNameIsPersistedExactlyAsGiven()
        {
            using var context = DbContextFactory.CreateInMemoryContext();
            var handler = new CreatePersonHandler(context);

            var request = new CreatePerson
            {
                Name = "   Trim Me   "
            };

            var result = await handler.Handle(request, CancellationToken.None);

            var person = await context.People.FindAsync(result.Id);
            Assert.NotNull(person);

            // Handler does not trim, preprocessor is supposed to.
            // This test documents that behavior: the handler just uses whatever is on the request.
            Assert.Equal("   Trim Me   ", person!.Name);
        }
    }
}
