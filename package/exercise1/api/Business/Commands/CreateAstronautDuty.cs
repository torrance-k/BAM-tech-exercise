using Dapper;
using MediatR;
using MediatR.Pipeline;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Data;
using StargateAPI.Controllers;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace StargateAPI.Business.Commands
{
    public class CreateAstronautDuty : IRequest<CreateAstronautDutyResult>
    {
        [Required]
        [StringLength(200)]
        public required string Name { get; set; }

        [Required]
        [StringLength(100)]
        public required string Rank { get; set; }

        [Required]
        [StringLength(100)]
        public required string DutyTitle { get; set; }

        [Required]
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
            if (request is null) throw new BadHttpRequestException("Request cannot be null.");

            var cleanName = request.Name?.Trim() ?? string.Empty;
            var cleanTitle = request.DutyTitle?.Trim() ?? string.Empty;
            var cleanRank = request.Rank?.Trim() ?? string.Empty;
            var startDate = request.DutyStartDate.Date;

            ValidateRequiredFields(cleanName, cleanTitle, cleanRank);

            if (request.DutyStartDate == default)
            {
                throw new BadHttpRequestException("Duty start date must be provided.");
            }

            var person = GetPersonOrThrow(cleanName);

            EnforceFirstDutyNotRetired(person.Id, cleanTitle);
            EnforceNoSameDayDuty(person.Id, startDate);
            EnforceChronologyAndRetirement(person.Id, startDate);

            return Task.CompletedTask;
        }
        
        private static void ValidateRequiredFields(string cleanName, string cleanTitle, string cleanRank)
        {
            if (string.IsNullOrWhiteSpace(cleanName))
            {
                throw new BadHttpRequestException("Name cannot be blank.");
            }

            if (string.IsNullOrWhiteSpace(cleanTitle))
            {
                throw new BadHttpRequestException("Duty title cannot be blank.");
            }

            if (string.IsNullOrWhiteSpace(cleanRank))
            {
                throw new BadHttpRequestException("Rank cannot be blank.");
            }
        }

        private Person GetPersonOrThrow(string cleanName)
        {
            var person = _context.People
                .AsNoTracking()
                .FirstOrDefault(p => p.Name == cleanName);

            if (person is null)
            {
                throw new BadHttpRequestException($"Person '{cleanName}' not found.");
            }

            return person;
        }

        private void EnforceFirstDutyNotRetired(int personId, string cleanTitle)
        {
            if (!string.Equals(cleanTitle, "RETIRED", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var hasAnyDuties = _context.AstronautDuties
                .AsNoTracking()
                .Any(d => d.PersonId == personId);

            if (!hasAnyDuties)
            {
                throw new BadHttpRequestException("RETIRED cannot be the first recorded duty for a person.");
            }
        }

        private void EnforceNoSameDayDuty(int personId, DateTime startDate)
        {
            var sameDayExists = _context.AstronautDuties
                .AsNoTracking()
                .Any(d => d.PersonId == personId && d.DutyStartDate == startDate);

            if (sameDayExists)
            {
                throw new BadHttpRequestException("A duty already starts on that day for this person.");
            }
        }

        private void EnforceChronologyAndRetirement(int personId, DateTime startDate)
        {
            var currentDuty = _context.AstronautDuties
                .AsNoTracking()
                .FirstOrDefault(d => d.PersonId == personId && d.DutyEndDate == null);

            if (currentDuty is null)
            {
                return;
            }

            if (startDate <= currentDuty.DutyStartDate)
            {
                throw new BadHttpRequestException("New duty must start after the current duty's start date.");
            }

            if (string.Equals(currentDuty.DutyTitle.Trim(), "RETIRED", StringComparison.OrdinalIgnoreCase))
            {
                throw new BadHttpRequestException("Cannot assign a new duty to a retired person.");
            }
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
            if (request is null)
            {
                return new CreateAstronautDutyResult
                {
                    Success = false,
                    ResponseCode = (int)HttpStatusCode.BadRequest,
                    Message = "Request cannot be null."
                };
            }

            // use transaction for single atomic unit of work
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            var normalizedName = request.Name?.Trim() ?? string.Empty;
            var normalizedTitle = request.DutyTitle?.Trim() ?? string.Empty;
            var normalizedRank = request.Rank?.Trim() ?? string.Empty;
            var startDate = request.DutyStartDate.Date;

            // find person by name - add LIMIT 1 as a defensive safeguard
            var personSql = $"SELECT * FROM [Person] WHERE Name = @Name COLLATE NOCASE LIMIT 1";
            var person = await _context.Connection.QueryFirstOrDefaultAsync<Person>(personSql, new { Name = normalizedName });

            if (person is null)
            {
                return new CreateAstronautDutyResult
                {
                    Success = false,
                    ResponseCode = (int)HttpStatusCode.NotFound,
                    Message = $"Person '{normalizedName}' not found."
                };
            }

            // check for existing AstronautDetail record - add LIMIT 1 as a defensive safeguard
            var detailSql = $"SELECT * FROM [AstronautDetail] WHERE PersonId = @PersonId LIMIT 1";
            var astronautDetail = await _context.Connection.QueryFirstOrDefaultAsync<AstronautDetail>(detailSql, new { PersonId = person.Id });
            var isRetired = string.Equals(normalizedTitle, "RETIRED", StringComparison.OrdinalIgnoreCase);

            // if none exists, create it now, but do not save yet - else update existing record
            if (astronautDetail is null)
            {
                astronautDetail = new AstronautDetail();
                astronautDetail.PersonId = person.Id;
                astronautDetail.CurrentDutyTitle = normalizedTitle;
                astronautDetail.CurrentRank = isRetired ? "Retired" : normalizedRank;
                astronautDetail.CareerStartDate = startDate;

                if (isRetired)
                {
                    // career end date is one day before the retired duty starts
                    astronautDetail.CareerEndDate = startDate.AddDays(-1);
                }

                await _context.AstronautDetails.AddAsync(astronautDetail, cancellationToken);
            }
            else
            {
                astronautDetail.CurrentDutyTitle = normalizedTitle;
                astronautDetail.CurrentRank = isRetired ? "Retired" : normalizedRank;

                if (isRetired)
                {
                    // career end date is one day before the retired duty starts
                    astronautDetail.CareerEndDate = startDate.AddDays(-1);
                }
                _context.AstronautDetails.Update(astronautDetail);
            }

            // find the current duty by null end date
            var currentDutySql = "SELECT * FROM [AstronautDuty] WHERE PersonId = @PersonId AND DutyEndDate IS NULL LIMIT 1";
            var currentDuty = await _context.Connection.QueryFirstOrDefaultAsync<AstronautDuty>(currentDutySql, new { PersonId = person.Id });

            // reject any new duty after retirement
            if (currentDuty != null && string.Equals(currentDuty.DutyTitle.Trim(), "RETIRED", StringComparison.OrdinalIgnoreCase))
            {
                return new CreateAstronautDutyResult
                {
                    Success = false,
                    ResponseCode = (int)HttpStatusCode.BadRequest,
                    Message = "Cannot add a new duty for a retired person."
                };
            }


            if (currentDuty != null)
            {
                // gaurd against bad chronology at the handler level
                if (request.DutyStartDate.Date <= currentDuty.DutyStartDate.Date)
                {
                    return new CreateAstronautDutyResult
                    {
                        Success = false,
                        ResponseCode = (int)HttpStatusCode.BadRequest,
                        Message = "New duty must start after the current duty's start date."
                    };
                }

                // before inserting new duty, set end date of current duty to day before new duty start date
                currentDuty.DutyEndDate = request.DutyStartDate.AddDays(-1).Date;
                _context.AstronautDuties.Update(currentDuty);
            }

            // create new duty with DutyEndDate = null, setting it as current duty
            var newAstronautDuty = new AstronautDuty()
            {
                PersonId = person.Id,
                Rank = normalizedRank,
                DutyTitle = normalizedTitle,
                DutyStartDate = startDate,
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
                // this is expected for the "one current duty per person" rule
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
