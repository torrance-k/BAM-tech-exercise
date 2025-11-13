using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Commands;
using StargateAPI.Business.Data;

namespace StargateAPI.Tests
{
    public class CreateAstronautDutyPreProcessorTests
    {
        [Fact]
        public async Task Process_RetiredAsFirstDuty_ThrowsBadHttpRequestException()
        {
            using var context = DbContextFactory.CreateInMemoryContext();

            var person = new Person { Name = "Samantha Carter" };
            context.People.Add(person);
            await context.SaveChangesAsync();

            var preProcessor = new CreateAstronautDutyPreProcessor(context);
            var request = new CreateAstronautDuty
            {
                Name = "Samantha Carter",
                Rank = "Colonel",
                DutyTitle = "RETIRED",
                DutyStartDate = new DateTime(2024, 1, 1)
            };

            async Task Act() => await preProcessor.Process(request, CancellationToken.None);

            var ex = await Assert.ThrowsAsync<BadHttpRequestException>(Act);
            Assert.Equal("RETIRED cannot be the first recorded duty for a person.", ex.Message);
        }

        [Fact]
        public async Task Process_SameDayDuty_ThrowsBadHttpRequestException()
        {
            using var context = DbContextFactory.CreateInMemoryContext();

            var person = new Person { Name = "Cameron Mitchell" };
            context.People.Add(person);
            await context.SaveChangesAsync();

            var duty = new AstronautDuty
            {
                PersonId = person.Id,
                Rank = "Colonel",
                DutyTitle = "Commanding Officer",
                DutyStartDate = new DateTime(2024, 1, 1),
                DutyEndDate = null
            };

            context.AstronautDuties.Add(duty);
            await context.SaveChangesAsync();

            var preProcessor = new CreateAstronautDutyPreProcessor(context);
            var request = new CreateAstronautDuty
            {
                Name = "Cameron Mitchell",
                Rank = "Colonel",
                DutyTitle = "Commander",
                DutyStartDate = new DateTime(2024, 1, 1) // same day
            };

            async Task Act() => await preProcessor.Process(request, CancellationToken.None);

            var ex = await Assert.ThrowsAsync<BadHttpRequestException>(Act);
            Assert.Equal("A duty already starts on that day for this person.", ex.Message);
        }

        [Fact]
        public async Task Process_BlankName_ThrowsBadHttpRequestException()
        {
            using var context = DbContextFactory.CreateInMemoryContext();
            var preProcessor = new CreateAstronautDutyPreProcessor(context);

            var request = new CreateAstronautDuty
            {
                Name = "   ",
                Rank = "Captain",
                DutyTitle = "Pilot",
                DutyStartDate = new DateTime(2024, 1, 1)
            };

            async Task Act() => await preProcessor.Process(request, CancellationToken.None);

            var ex = await Assert.ThrowsAsync<BadHttpRequestException>(Act);
            Assert.Equal("Name cannot be blank.", ex.Message);
        }

        [Fact]
        public async Task Process_DefaultDutyStartDate_ThrowsBadHttpRequestException()
        {
            using var context = DbContextFactory.CreateInMemoryContext();
            var preProcessor = new CreateAstronautDutyPreProcessor(context);

            var request = new CreateAstronautDuty
            {
                Name = "Jack O'Neill",
                Rank = "Colonel",
                DutyTitle = "CO",
                DutyStartDate = default // DateTime.MinValue
            };

            async Task Act() => await preProcessor.Process(request, CancellationToken.None);

            var ex = await Assert.ThrowsAsync<BadHttpRequestException>(Act);
            Assert.Equal("Duty start date must be provided.", ex.Message);
        }

        [Fact]
        public async Task Process_PersonDoesNotExist_ThrowsBadHttpRequestException()
        {
            using var context = DbContextFactory.CreateInMemoryContext();
            var preProcessor = new CreateAstronautDutyPreProcessor(context);

            var request = new CreateAstronautDuty
            {
                Name = "Missing Person",
                Rank = "Lieutenant",
                DutyTitle = "Science Officer",
                DutyStartDate = new DateTime(2024, 1, 1)
            };

            async Task Act() => await preProcessor.Process(request, CancellationToken.None);

            var ex = await Assert.ThrowsAsync<BadHttpRequestException>(Act);
            Assert.Equal("Person 'Missing Person' not found.", ex.Message);
        }

        [Fact]
        public async Task Process_NewDutyBeforeCurrentStart_ThrowsBadHttpRequestException()
        {
            using var context = DbContextFactory.CreateInMemoryContext();

            var person = new Person { Name = "Chronology Test" };
            context.People.Add(person);
            await context.SaveChangesAsync();

            context.AstronautDuties.Add(new AstronautDuty
            {
                PersonId = person.Id,
                Rank = "Major",
                DutyTitle = "Existing Duty",
                DutyStartDate = new DateTime(2024, 5, 10),
                DutyEndDate = null
            });
            await context.SaveChangesAsync();

            var preProcessor = new CreateAstronautDutyPreProcessor(context);

            var request = new CreateAstronautDuty
            {
                Name = "Chronology Test",
                Rank = "Major",
                DutyTitle = "New Duty",
                DutyStartDate = new DateTime(2024, 5, 10) // same or earlier than current
            };

            async Task Act() => await preProcessor.Process(request, CancellationToken.None);

            var ex = await Assert.ThrowsAsync<BadHttpRequestException>(Act);
            Assert.Equal("A duty already starts on that day for this person.", ex.Message);
        }

        [Fact]
        public async Task Process_CurrentDutyIsRetired_ThrowsBadHttpRequestException()
        {
            using var context = DbContextFactory.CreateInMemoryContext();

            var person = new Person { Name = "Retired Test" };
            context.People.Add(person);
            await context.SaveChangesAsync();

            context.AstronautDuties.Add(new AstronautDuty
            {
                PersonId = person.Id,
                Rank = "Colonel",
                DutyTitle = "RETIRED",
                DutyStartDate = new DateTime(2024, 1, 1),
                DutyEndDate = null
            });
            await context.SaveChangesAsync();

            var preProcessor = new CreateAstronautDutyPreProcessor(context);

            var request = new CreateAstronautDuty
            {
                Name = "Retired Test",
                Rank = "Colonel",
                DutyTitle = "Some New Duty",
                DutyStartDate = new DateTime(2024, 2, 1)
            };

            async Task Act() => await preProcessor.Process(request, CancellationToken.None);

            var ex = await Assert.ThrowsAsync<BadHttpRequestException>(Act);
            Assert.Equal("Cannot assign a new duty to a retired person.", ex.Message);
        }
    }
}
