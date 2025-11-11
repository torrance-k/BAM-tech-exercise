using Dapper;
using MediatR;
using MediatR.Pipeline;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Data;
using StargateAPI.Controllers;
using System.Net;

namespace StargateAPI.Business.Commands
{
    public class CreateAstronautDuty : IRequest<CreateAstronautDutyResult>
    {
        public required string Name { get; set; }

        public required string Rank { get; set; }

        public required string DutyTitle { get; set; }

        public DateTime DutyStartDate { get; set; }
    }

    public class CreateAstronautDutyPreProcessor : IRequestPreProcessor<CreateAstronautDuty>
    {
        private readonly StargateContext _context;

        public CreateAstronautDutyPreProcessor(StargateContext context)
        {
            _context = context;
        }

        public Task Process(CreateAstronautDuty request, CancellationToken cancellationToken)
        {
            var person = _context.People.AsNoTracking().FirstOrDefault(z => z.Name == request.Name);

            if (person is null) throw new BadHttpRequestException($"Person '{request.Name}' not found.");

            // check if same-day start already exists for this person
            var sameDay = _context.AstronautDuties.AsNoTracking().Any(d => d.PersonId == person.Id && d.DutyStartDate == request.DutyStartDate.Date);
            if (sameDay) throw new BadHttpRequestException("A duty already starts on that day for this person.");

            // find current duty to enforce no overlapping duties
            // if current duty exists, creating another "current" duty with changed rank/title i snot allowed.
            // That must be modeled as a new duty with a later start date, or an update endpoint (not in scope).
            var currentDuty = _context.AstronautDuties.AsNoTracking().FirstOrDefault(d => d.PersonId == person.Id && d.DutyEndDate == null);
            if (currentDuty != null && request.DutyStartDate.Date <= currentDuty.DutyStartDate) throw new BadHttpRequestException("New duty must start after the current duty's start date.");

            return Task.CompletedTask;
        }
    }

    public class CreateAstronautDutyHandler : IRequestHandler<CreateAstronautDuty, CreateAstronautDutyResult>
    {
        private readonly StargateContext _context;

        public CreateAstronautDutyHandler(StargateContext context)
        {
            _context = context;
        }
        public async Task<CreateAstronautDutyResult> Handle(CreateAstronautDuty request, CancellationToken cancellationToken)
        {
            // use transaction for single atomic unit of work
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            // find person by name - add LIMIT 1 as a defensive safeguard
            var personSql = $"SELECT * FROM [Person] WHERE Name = @Name COLLATE NOCASE LIMIT 1";
            var person = await _context.Connection.QueryFirstOrDefaultAsync<Person>(personSql, new { request.Name });

            if (person is null)
            {
                return new CreateAstronautDutyResult
                {
                    Success = false,
                    ResponseCode = 404,
                    Message = $"Person '{request.Name}' not found."
                };
            }

            // check for existing AstronautDetail record - add LIMIT 1 as a defensive safeguard
            var detailSql = $"SELECT * FROM [AstronautDetail] WHERE PersonId = @PersonId LIMIT 1";
            var astronautDetail = await _context.Connection.QueryFirstOrDefaultAsync<AstronautDetail>(detailSql, new { PersonId = person.Id });

            // if none exists, create it now, but do not save yet - else update existing record
            if (astronautDetail is null)
            {
                astronautDetail = new AstronautDetail();
                astronautDetail.PersonId = person.Id;
                astronautDetail.CurrentDutyTitle = request.DutyTitle;
                astronautDetail.CurrentRank = request.Rank;
                astronautDetail.CareerStartDate = request.DutyStartDate.Date;

                if (request.DutyTitle == "RETIRED")
                {
                    astronautDetail.CareerEndDate = request.DutyStartDate.Date;
                }

                await _context.AstronautDetails.AddAsync(astronautDetail, cancellationToken);
            }
            else
            {
                astronautDetail.CurrentDutyTitle = request.DutyTitle;
                astronautDetail.CurrentRank = request.Rank;
                if (request.DutyTitle == "RETIRED")
                {
                    astronautDetail.CareerEndDate = request.DutyStartDate.AddDays(-1).Date;
                }
                _context.AstronautDetails.Update(astronautDetail);
            }

            // find the current duty by null end date
            var currentDutySql = "SELECT * FROM [AstronautDuty] WHERE PersonId = @PersonId AND DutyEndDate IS NULL LIMIT 1";
            var currentDuty = await _context.Connection.QueryFirstOrDefaultAsync<AstronautDuty>(currentDutySql, new { PersonId = person.Id });
            if (currentDuty != null)
            {
                currentDuty.DutyEndDate = request.DutyStartDate.AddDays(-1).Date;
                _context.AstronautDuties.Update(currentDuty);
            }

            // create new duty
            var newAstronautDuty = new AstronautDuty()
            {
                PersonId = person.Id,
                Rank = request.Rank,
                DutyTitle = request.DutyTitle,
                DutyStartDate = request.DutyStartDate.Date,
                DutyEndDate = null
            };

            await _context.AstronautDuties.AddAsync(newAstronautDuty, cancellationToken);

            try
            {
                // persist both inserts an updates together - there is no path that writes AstronautDetail without AstronautDuty - either fails, nothing is commited - satisfying rule 2
                await _context.SaveChangesAsync();
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException dbEx) when (dbEx.InnerException is SqliteException se && se.SqliteErrorCode == 19)
            {
                // 19 = constraint violation (unique index) in SQLite
                throw new BadHttpRequestException("Only one current duty is allowed per person.");
            }
            
            return new CreateAstronautDutyResult()
            {
                Id = newAstronautDuty.Id
            };
        }
    }

    public class CreateAstronautDutyResult : BaseResponse
    {
        public int? Id { get; set; }
    }
}
