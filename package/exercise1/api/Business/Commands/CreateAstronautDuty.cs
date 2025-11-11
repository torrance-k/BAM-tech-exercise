using Dapper;
using MediatR;
using MediatR.Pipeline;
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

            if (person is null) throw new BadHttpRequestException("Bad Request");

            var verifyNoPreviousDuty = _context.AstronautDuties.FirstOrDefault(z => z.DutyTitle == request.DutyTitle && z.DutyStartDate == request.DutyStartDate);

            if (verifyNoPreviousDuty is not null) throw new BadHttpRequestException("Bad Request");

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

            // close latest duty if exists
            var latestDutySql = $"SELECT * FROM [AstronautDuty] WHERE PersonId = @PersonId Order By DutyStartDate Desc LIMIT 1";
            var latestDuty = await _context.Connection.QueryFirstOrDefaultAsync<AstronautDuty>(latestDutySql, new { PersonId = person.Id });

            if (latestDuty != null)
            {
                latestDuty.DutyEndDate = request.DutyStartDate.AddDays(-1).Date;
                _context.AstronautDuties.Update(latestDuty);
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

            // persist both inserts an updates together - there is no path that writes AstronautDetail without AstronautDuty - either fails, nothing is commited - satisfying rule 2
            await _context.SaveChangesAsync();
            await transaction.CommitAsync(cancellationToken);

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
