using Dapper;
using MediatR;
using StargateAPI.Business.Data;
using StargateAPI.Business.Dtos;
using StargateAPI.Controllers;

namespace StargateAPI.Business.Queries
{
    public class GetAstronautDutiesByName : IRequest<GetAstronautDutiesByNameResult>
    {
        public string Name { get; set; } = string.Empty;
    }

    public class GetAstronautDutiesByNameHandler : IRequestHandler<GetAstronautDutiesByName, GetAstronautDutiesByNameResult>
    {
        private readonly StargateContext _context;

        public GetAstronautDutiesByNameHandler(StargateContext context)
        {
            _context = context;
        }

        public async Task<GetAstronautDutiesByNameResult> Handle(GetAstronautDutiesByName request, CancellationToken cancellationToken)
        {

            var result = new GetAstronautDutiesByNameResult();

            // use INNER JOIN to ensure only astronauts are returned
            var query = $"SELECT a.Id as PersonId, a.Name, b.CurrentRank, b.CurrentDutyTitle, b.CareerStartDate, b.CareerEndDate FROM [Person] a INNER JOIN [AstronautDetail] b on b.PersonId = a.Id WHERE a.Name = @Name COLLATE NOCASE";

            var person = await _context.Connection.QueryFirstOrDefaultAsync<PersonAstronaut>(query, new { request.Name });

            // if person not found, return with appropriate message
            if (person is null)
            {
                result.Success = false;
                result.Message = $"Person '{request.Name}' not found.";
                result.ResponseCode = 404;
                result.Person = new PersonAstronaut();
                result.AstronautDuties = new List<AstronautDuty>();
                return result;
            }

            result.Person = person;

            query = $"SELECT * FROM [AstronautDuty] WHERE PersonId = @PersonId Order By DutyStartDate Desc";

            var duties = await _context.Connection.QueryAsync<AstronautDuty>(query, new { PersonId = person.PersonId });

            result.AstronautDuties = duties.ToList();

            return result;

        }
    }

    public class GetAstronautDutiesByNameResult : BaseResponse
    {
        // let Person be null if not found
        public PersonAstronaut? Person { get; set; }
        public List<AstronautDuty> AstronautDuties { get; set; } = new List<AstronautDuty>();
    }
}
