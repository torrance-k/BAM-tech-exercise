using StargateAPI.Business.Commands;
using StargateAPI.Business.Data;

namespace StargateAPI.Tests
{
    public class UpdatePersonHandlerTests
    {
        [Fact]
        public async Task Handle_PersonNotFound_ReturnsNotFoundResult()
        {
            using var context = DbContextFactory.CreateInMemoryContext();
            var handler = new UpdatePersonHandler(context);

            var request = new UpdatePerson
            {
                Name = "Missing Person",
                NewName = "New Name"
            };

            var result = await handler.Handle(request, CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal(404, result.ResponseCode);
            Assert.Equal("Person 'Missing Person' not found.", result.Message);
        }

        [Fact]
        public async Task Handle_ValidRename_UpdatesNameAndReturnsSuccess()
        {
            using var context = DbContextFactory.CreateInMemoryContext();

            var person = new Person { Name = "Original Name" };
            context.People.Add(person);
            await context.SaveChangesAsync();

            var handler = new UpdatePersonHandler(context);

            var request = new UpdatePerson
            {
                Name = "Original Name",
                NewName = "Updated Name"
            };

            var result = await handler.Handle(request, CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(200, result.ResponseCode);
            Assert.Equal($"Person renamed to 'Updated Name'", result.Message);

            var reloaded = await context.People.FindAsync(person.Id);
            Assert.NotNull(reloaded);
            Assert.Equal("Updated Name", reloaded!.Name);
        }
    }
}
