using System.Net;
using StargateAPI.Business.Commands;
using StargateAPI.Business.Data;

namespace StargateAPI.Tests
{
    public class CreateAstronautDutyHandlerTests
    {
        [Fact]
        public async Task Handle_PersonNotFound_ReturnsNotFoundResult()
        {
            using var context = DbContextFactory.CreateInMemoryContext();
            var handler = new CreateAstronautDutyHandler(context);

            var request = new CreateAstronautDuty
            {
                Name = "Unknown Person",
                Rank = "Captain",
                DutyTitle = "Pilot",
                DutyStartDate = new DateTime(2024, 1, 1)
            };

            var result = await handler.Handle(request, CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal(404, result.ResponseCode);
            Assert.Equal("Person 'Unknown Person' not found.", result.Message);
        }

        [Fact]
        public async Task Handle_NewDutyForRetiredPerson_ReturnsBadRequest()
        {
            using var context = DbContextFactory.CreateInMemoryContext();

            var person = new Person { Name = "Retired Person" };
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

            var handler = new CreateAstronautDutyHandler(context);
            var request = new CreateAstronautDuty
            {
                Name = "Retired Person",
                Rank = "Colonel",
                DutyTitle = "Some New Duty",
                DutyStartDate = new DateTime(2024, 2, 1)
            };

            var result = await handler.Handle(request, CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal(400, result.ResponseCode);
            Assert.Equal("Cannot add a new duty for a retired person.", result.Message);
        }

        [Fact]
        public async Task Handle_ValidNewDuty_EndsCurrentDutyAndCreatesNewOne()
        {
            using var context = DbContextFactory.CreateInMemoryContext();

            var person = new Person { Name = "Duty Person" };
            context.People.Add(person);
            await context.SaveChangesAsync();

            var currentDutyStart = new DateTime(2024, 1, 1);

            var currentDuty = new AstronautDuty
            {
                PersonId = person.Id,
                Rank = "Major",
                DutyTitle = "Existing Duty",
                DutyStartDate = currentDutyStart,
                DutyEndDate = null
            };
            context.AstronautDuties.Add(currentDuty);
            await context.SaveChangesAsync();

            context.ChangeTracker.Clear();

            var handler = new CreateAstronautDutyHandler(context);

            var newDutyStart = new DateTime(2024, 2, 1);

            var request = new CreateAstronautDuty
            {
                Name = "Duty Person",
                Rank = "Major",
                DutyTitle = "New Duty",
                DutyStartDate = newDutyStart
            };

            var result = await handler.Handle(request, CancellationToken.None);

            Assert.True(result.Success);
            Assert.NotNull(result.Id);

            var duties = context.AstronautDuties
                .Where(d => d.PersonId == person.Id)
                .OrderBy(d => d.DutyStartDate)
                .ToList();

            Assert.Equal(2, duties.Count);

            var updatedOldDuty = duties[0];
            var newDuty = duties[1];

            Assert.Equal(currentDutyStart, updatedOldDuty.DutyStartDate);
            Assert.Equal(newDutyStart.AddDays(-1), updatedOldDuty.DutyEndDate);

            Assert.Equal(newDutyStart, newDuty.DutyStartDate);
            Assert.Null(newDuty.DutyEndDate);

            var detail = context.AstronautDetails.SingleOrDefault(d => d.PersonId == person.Id);
            Assert.NotNull(detail);
            Assert.Equal("New Duty", detail.CurrentDutyTitle);
        }

        [Fact]
        public async Task Handle_NullRequest_ReturnsBadRequestResult()
        {
            using var context = DbContextFactory.CreateInMemoryContext();
            var handler = new CreateAstronautDutyHandler(context);

            var result = await handler.Handle(null!, CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal((int)HttpStatusCode.BadRequest, result.ResponseCode);
            Assert.Equal("Request cannot be null.", result.Message);
            Assert.Null(result.Id);
        }

        [Fact]
        public async Task Handle_FirstDutyRetired_SetsCareerEndDateAndCreatesRetiredDuty()
        {
            using var context = DbContextFactory.CreateInMemoryContext();

            var person = new Person { Name = "Retired First Duty" };
            context.People.Add(person);
            await context.SaveChangesAsync();

            var handler = new CreateAstronautDutyHandler(context);

            var retiredStart = new DateTime(2024, 3, 10);

            var request = new CreateAstronautDuty
            {
                Name = "Retired First Duty",
                Rank = "Colonel",
                DutyTitle = "RETIRED",
                DutyStartDate = retiredStart
            };

            // Note: this bypasses the preprocessor rule that forbids retired as first duty.
            var result = await handler.Handle(request, CancellationToken.None);

            Assert.True(result.Success);
            Assert.NotNull(result.Id);

            var detail = context.AstronautDetails.Single(d => d.PersonId == person.Id);
            Assert.Equal("RETIRED", detail.CurrentDutyTitle);
            Assert.Equal("Retired", detail.CurrentRank);
            Assert.Equal(retiredStart.Date, detail.CareerStartDate.Date);
            Assert.Equal(retiredStart.AddDays(-1).Date, detail.CareerEndDate!.Value.Date);

            var duty = context.AstronautDuties.Single(d => d.PersonId == person.Id);
            Assert.Equal("RETIRED", duty.DutyTitle);
            Assert.Equal(retiredStart.Date, duty.DutyStartDate.Date);
            Assert.Null(duty.DutyEndDate);
        }

        [Fact]
        public async Task Handle_ExistingAstronautDetail_NonRetiredDuty_UpdatesCurrentFieldsWithoutCareerEnd()
        {
            using var context = DbContextFactory.CreateInMemoryContext();

            var person = new Person { Name = "Detail Update Test" };
            context.People.Add(person);
            await context.SaveChangesAsync();

            var existingDetail = new AstronautDetail
            {
                PersonId = person.Id,
                CurrentRank = "Major",
                CurrentDutyTitle = "Old Duty",
                CareerStartDate = new DateTime(2020, 1, 1),
                CareerEndDate = null
            };
            context.AstronautDetails.Add(existingDetail);
            await context.SaveChangesAsync();

            context.ChangeTracker.Clear();

            var handler = new CreateAstronautDutyHandler(context);

            var newDutyStart = new DateTime(2024, 5, 1);

            // no current duty rows yet, handler will only insert new one
            var request = new CreateAstronautDuty
            {
                Name = "Detail Update Test",
                Rank = "Colonel",
                DutyTitle = "New CO Role",
                DutyStartDate = newDutyStart
            };

            var result = await handler.Handle(request, CancellationToken.None);

            Assert.True(result.Success);
            Assert.NotNull(result.Id);

            var updatedDetail = context.AstronautDetails.Single(d => d.PersonId == person.Id);
            Assert.Equal("New CO Role", updatedDetail.CurrentDutyTitle);
            Assert.Equal("Colonel", updatedDetail.CurrentRank);
            Assert.Null(updatedDetail.CareerEndDate);   // not retired

            var duty = context.AstronautDuties.Single(d => d.PersonId == person.Id);
            Assert.Equal("New CO Role", duty.DutyTitle);
            Assert.Equal("Colonel", duty.Rank);
            Assert.Equal(newDutyStart.Date, duty.DutyStartDate.Date);
            Assert.Null(duty.DutyEndDate);
        }

        [Fact]
        public async Task Handle_NewDutyWithBadChronology_ReturnsBadRequest()
        {
            using var context = DbContextFactory.CreateInMemoryContext();

            var person = new Person { Name = "Chronology Handler Test" };
            context.People.Add(person);
            await context.SaveChangesAsync();

            var currentStart = new DateTime(2024, 4, 1);

            context.AstronautDuties.Add(new AstronautDuty
            {
                PersonId = person.Id,
                Rank = "Major",
                DutyTitle = "Existing Duty",
                DutyStartDate = currentStart,
                DutyEndDate = null
            });
            await context.SaveChangesAsync();

            var handler = new CreateAstronautDutyHandler(context);

            // New duty starts on the same day, which is invalid
            var request = new CreateAstronautDuty
            {
                Name = "Chronology Handler Test",
                Rank = "Major",
                DutyTitle = "New Duty",
                DutyStartDate = currentStart
            };

            var result = await handler.Handle(request, CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal((int)HttpStatusCode.BadRequest, result.ResponseCode);
            Assert.Equal("New duty must start after the current duty's start date.", result.Message);

            // Ensure no new duty was created and old one remains open
            var duties = context.AstronautDuties.Where(d => d.PersonId == person.Id).ToList();
            Assert.Single(duties);
            Assert.Null(duties[0].DutyEndDate);
        }

        [Fact]
        public async Task Handle_ExistingDetailAndCurrentDuty_NonRetired_UpdatesDetailAndEndsCurrentDuty()
        {
            using var context = DbContextFactory.CreateInMemoryContext();

            var person = new Person { Name = "Detail And Duty Person" };
            context.People.Add(person);
            await context.SaveChangesAsync();

            // Existing astronaut detail
            var existingDetail = new AstronautDetail
            {
                PersonId = person.Id,
                CurrentRank = "Major",
                CurrentDutyTitle = "Old Duty",
                CareerStartDate = new DateTime(2020, 1, 1),
                CareerEndDate = null
            };
            context.AstronautDetails.Add(existingDetail);

            // Existing current duty
            var currentDutyStart = new DateTime(2024, 1, 1);
            var currentDuty = new AstronautDuty
            {
                PersonId = person.Id,
                Rank = "Major",
                DutyTitle = "Existing Duty",
                DutyStartDate = currentDutyStart,
                DutyEndDate = null
            };
            context.AstronautDuties.Add(currentDuty);

            await context.SaveChangesAsync();

            context.ChangeTracker.Clear();

            var handler = new CreateAstronautDutyHandler(context);

            var newDutyStart = new DateTime(2024, 2, 1);

            var request = new CreateAstronautDuty
            {
                Name = "Detail And Duty Person",
                Rank = "Colonel",
                DutyTitle = "New CO Role",
                DutyStartDate = newDutyStart
            };

            var result = await handler.Handle(request, CancellationToken.None);

            Assert.True(result.Success);
            Assert.NotNull(result.Id);

            // Detail should be updated
            var updatedDetail = context.AstronautDetails.Single(d => d.PersonId == person.Id);
            Assert.Equal("New CO Role", updatedDetail.CurrentDutyTitle);
            Assert.Equal("Colonel", updatedDetail.CurrentRank);
            Assert.Null(updatedDetail.CareerEndDate); // non retired

            // Duties
            var duties = context.AstronautDuties
                .Where(d => d.PersonId == person.Id)
                .OrderBy(d => d.DutyStartDate)
                .ToList();

            Assert.Equal(2, duties.Count);

            var updatedOldDuty = duties[0];
            var newDuty = duties[1];

            Assert.Equal(currentDutyStart, updatedOldDuty.DutyStartDate);
            Assert.Equal(newDutyStart.AddDays(-1), updatedOldDuty.DutyEndDate);

            Assert.Equal("New CO Role", newDuty.DutyTitle);
            Assert.Equal("Colonel", newDuty.Rank);
            Assert.Equal(newDutyStart, newDuty.DutyStartDate);
            Assert.Null(newDuty.DutyEndDate);
        }

        [Fact]
        public async Task Handle_ExistingDetailAndCurrentDuty_Retired_UpdatesDetailCareerEndAndEndsCurrentDuty()
        {
            using var context = DbContextFactory.CreateInMemoryContext();

            var person = new Person { Name = "Retired WithHistory" };
            context.People.Add(person);
            await context.SaveChangesAsync();

            context.ChangeTracker.Clear();

            // Existing astronaut detail
            var existingDetail = new AstronautDetail
            {
                PersonId = person.Id,
                CurrentRank = "Colonel",
                CurrentDutyTitle = "Active Duty",
                CareerStartDate = new DateTime(2010, 1, 1),
                CareerEndDate = null
            };
            context.AstronautDetails.Add(existingDetail);

            // Existing current duty
            var currentDutyStart = new DateTime(2024, 1, 1);
            context.AstronautDuties.Add(new AstronautDuty
            {
                PersonId = person.Id,
                Rank = "Colonel",
                DutyTitle = "CO",
                DutyStartDate = currentDutyStart,
                DutyEndDate = null
            });

            await context.SaveChangesAsync();

            context.ChangeTracker.Clear();

            var handler = new CreateAstronautDutyHandler(context);

            var retiredStart = new DateTime(2024, 6, 1);

            var request = new CreateAstronautDuty
            {
                Name = "Retired WithHistory",
                Rank = "Colonel",
                DutyTitle = "RETIRED",
                DutyStartDate = retiredStart
            };

            var result = await handler.Handle(request, CancellationToken.None);

            Assert.True(result.Success);
            Assert.NotNull(result.Id);

            var updatedDetail = context.AstronautDetails.Single(d => d.PersonId == person.Id);
            Assert.Equal("RETIRED", updatedDetail.CurrentDutyTitle);
            Assert.Equal("Retired", updatedDetail.CurrentRank);
            Assert.Equal(new DateTime(2010, 1, 1), updatedDetail.CareerStartDate);
            Assert.Equal(retiredStart.AddDays(-1).Date, updatedDetail.CareerEndDate!.Value.Date);

            var duties = context.AstronautDuties
                .Where(d => d.PersonId == person.Id)
                .OrderBy(d => d.DutyStartDate)
                .ToList();

            Assert.Equal(2, duties.Count);

            var endedDuty = duties[0];
            var retiredDuty = duties[1];

            Assert.Equal(currentDutyStart, endedDuty.DutyStartDate);
            Assert.Equal(retiredStart.AddDays(-1), endedDuty.DutyEndDate);

            Assert.Equal("RETIRED", retiredDuty.DutyTitle);
            Assert.Equal(retiredStart, retiredDuty.DutyStartDate);
            Assert.Null(retiredDuty.DutyEndDate);
        }

    }
}
