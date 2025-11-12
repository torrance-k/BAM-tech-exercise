using MediatR;
using MediatR.Pipeline;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using StargateAPI.Business.Data;
using StargateAPI.Controllers;

namespace StargateAPI.Business.Commands
{
    public class UpdatePerson : IRequest<UpdatePersonResult>
    {
        public required string Name { get; set; } = string.Empty;
        public required string NewName { get; set; } = string.Empty;
    }

    public class UpdatePersonPreProcessor : IRequestPreProcessor<UpdatePerson>
    {
        private readonly StargateContext _context;
        public UpdatePersonPreProcessor(StargateContext context)
        {
            _context = context;
        }
        public async Task Process(UpdatePerson request, CancellationToken cancellationToken)
        {
            var currentName = request.Name.Trim() ?? string.Empty;
            var newName = request.NewName.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(currentName) || string.IsNullOrWhiteSpace(newName))
                throw new BadHttpRequestException("Both current name and new name are required.");

            if (string.Equals(currentName, newName, StringComparison.OrdinalIgnoreCase))
                throw new BadHttpRequestException("New name cannot be the same as current name.");

            var person = await _context.People.AsNoTracking().FirstOrDefaultAsync(p => p.Name == currentName, cancellationToken);

            if (person is null) throw new BadHttpRequestException($"Person '{currentName}' not found.");

            var nameExists = await _context.People.AsNoTracking().AnyAsync(p => p.Name == newName, cancellationToken);

            if (nameExists) throw new BadHttpRequestException($"A person named '{newName}' already exists.");

        }
    }

    public class UpdatePersonHandler : IRequestHandler<UpdatePerson, UpdatePersonResult>
    {
        private readonly StargateContext _context;

        public UpdatePersonHandler(StargateContext context)
        {
            _context = context;
        }
        public async Task<UpdatePersonResult> Handle(UpdatePerson request, CancellationToken cancellationToken)
        {

            var person = await _context.People.FirstOrDefaultAsync(p => p.Name == request.Name, cancellationToken);

            if (person is null)
            {
                return new UpdatePersonResult
                {
                    Success = false,
                    ResponseCode = 404,
                    Message = $"Person '{request.Name}' not found."
                };
            }

            person.Name = request.NewName.Trim();

            await _context.SaveChangesAsync(cancellationToken);

            return new UpdatePersonResult
            {
                Success = true,
                ResponseCode = 200,
                Message = $"Person renamed to '{person.Name}'",
                Id = person.Id
            };
          
        }
    }

    public class UpdatePersonResult : BaseResponse
    {
        public int Id { get; set; }
    }
}
