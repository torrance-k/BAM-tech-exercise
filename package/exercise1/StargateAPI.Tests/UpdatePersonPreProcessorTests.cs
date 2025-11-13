using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Commands;
using StargateAPI.Business.Data;

namespace StargateAPI.Tests
{
    public class UpdatePersonPreProcessorTests
    {
        [Fact]
        public async Task Process_BlankNewName_ThrowsBadHttpRequestException()
        {
            using var context = DbContextFactory.CreateInMemoryContext();

            var preProcessor = new UpdatePersonPreProcessor(context);
            var request = new UpdatePerson
            {
                Name = "Jack O'Neill",
                NewName = "   "
            };

            async Task Act() => await preProcessor.Process(request, CancellationToken.None);

            var ex = await Assert.ThrowsAsync<BadHttpRequestException>(Act);
            Assert.Equal("New name cannot be blank.", ex.Message);
        }

        [Fact]
        public async Task Process_SameName_ThrowsBadHttpRequestException()
        {
            using var context = DbContextFactory.CreateInMemoryContext();

            var preProcessor = new UpdatePersonPreProcessor(context);
            var request = new UpdatePerson
            {
                Name = "Daniel Jackson",
                NewName = "Daniel Jackson"
            };

            async Task Act() => await preProcessor.Process(request, CancellationToken.None);

            var ex = await Assert.ThrowsAsync<BadHttpRequestException>(Act);
            Assert.Equal("New name cannot be the same as current name.", ex.Message);
        }

        [Fact]
        public async Task Process_PersonDoesNotExist_ThrowsBadHttpRequestException()
        {
            using var context = DbContextFactory.CreateInMemoryContext();

            var preProcessor = new UpdatePersonPreProcessor(context);
            var request = new UpdatePerson
            {
                Name = "Nonexistent",
                NewName = "New Name"
            };

            async Task Act() => await preProcessor.Process(request, CancellationToken.None);

            var ex = await Assert.ThrowsAsync<BadHttpRequestException>(Act);
            Assert.Equal("Person 'Nonexistent' not found.", ex.Message);
        }

        [Fact]
        public async Task Process_NewNameAlreadyExists_ThrowsBadHttpRequestException()
        {
            using var context = DbContextFactory.CreateInMemoryContext();

            context.People.Add(new Person { Name = "Existing" });
            context.People.Add(new Person { Name = "Original" });
            await context.SaveChangesAsync();

            var preProcessor = new UpdatePersonPreProcessor(context);
            var request = new UpdatePerson
            {
                Name = "Original",
                NewName = "Existing"
            };

            async Task Act() => await preProcessor.Process(request, CancellationToken.None);

            var ex = await Assert.ThrowsAsync<BadHttpRequestException>(Act);
            Assert.Equal("A person named 'Existing' already exists.", ex.Message);
        }

        [Fact]
        public async Task Process_ValidRename_SucceedsWithoutException()
        {
            using var context = DbContextFactory.CreateInMemoryContext();

            context.People.Add(new Person { Name = "Original Name" });
            await context.SaveChangesAsync();

            var preProcessor = new UpdatePersonPreProcessor(context);
            var request = new UpdatePerson
            {
                Name = "Original Name",
                NewName = "Updated Name"
            };

            await preProcessor.Process(request, CancellationToken.None);
        }
    }
}
