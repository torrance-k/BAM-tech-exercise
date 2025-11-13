using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Commands;
using StargateAPI.Business.Data;

namespace StargateAPI.Tests
{
    public class CreatePersonPreProcessorTests
    {
        [Fact]
        public async Task Process_BlankName_ThrowsBadHttpRequestException()
        {
            // Arrange
            using var context = DbContextFactory.CreateInMemoryContext();
            var preProcessor = new CreatePersonPreProcessor(context);
            var request = new CreatePerson { Name = "   " };

            // Act
            async Task Act() => await preProcessor.Process(request, CancellationToken.None);

            // Assert
            var ex = await Assert.ThrowsAsync<BadHttpRequestException>(Act);
            Assert.Equal("Name cannot be blank.", ex.Message);
        }

        [Fact]
        public async Task Process_DuplicateName_ThrowsBadHttpRequestException()
        {
            // Arrange
            using var context = DbContextFactory.CreateInMemoryContext();

            context.People.Add(new Person { Name = "Sam Carter" });
            await context.SaveChangesAsync();

            var preProcessor = new CreatePersonPreProcessor(context);
            var request = new CreatePerson { Name = "Sam Carter" };

            // Act
            async Task Act() => await preProcessor.Process(request, CancellationToken.None);

            // Assert
            var ex = await Assert.ThrowsAsync<BadHttpRequestException>(Act);
            Assert.Equal("A Person named 'Sam Carter' already exists", ex.Message);
        }

        [Fact]
        public async Task Process_ValidName_SucceedsWithoutException()
        {
            // Arrange
            using var context = DbContextFactory.CreateInMemoryContext();

            var preProcessor = new CreatePersonPreProcessor(context);
            var request = new CreatePerson { Name = "Teal'c" };

            // Act and Assert
            await preProcessor.Process(request, CancellationToken.None);
        }
    }
}
