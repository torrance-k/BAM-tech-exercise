using MediatR;
using Microsoft.AspNetCore.Mvc;
using StargateAPI.Business.Commands;
using StargateAPI.Business.Queries;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace StargateAPI.Controllers
{
   
    [ApiController]
    [Route("[controller]")]
    public class PersonController : ControllerBase
    {
        private readonly IMediator _mediator;
        public PersonController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("")]
        public async Task<IActionResult> GetPeople()
        {
            try
            {
                var result = await _mediator.Send(new GetPeople());
                return this.GetResponse(result);
            }
            catch (Exception ex)
            {
                return this.GetResponse(new BaseResponse()
                {
                    Message = ex.Message,
                    Success = false,
                    ResponseCode = (int)HttpStatusCode.InternalServerError
                });
            }
        }

        [HttpGet("{name}")]
        public async Task<IActionResult> GetPersonByName(string name)
        {
            var cleanName = name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(cleanName))
            {
                var badRequest = new BaseResponse
                {
                    Success = false,
                    ResponseCode = (int)HttpStatusCode.BadRequest,
                    Message = "Name route parameter cannot be blank."
                };

                return this.GetResponse(badRequest);
            }

            try
            {
                var result = await _mediator.Send(new GetPersonByName()
                {
                    Name = cleanName
                });

                return this.GetResponse(result);
            }
            catch (Exception ex)
            {
                return this.GetResponse(new BaseResponse()
                {
                    Message = ex.Message,
                    Success = false,
                    ResponseCode = (int)HttpStatusCode.InternalServerError
                });
            }
        }

        public class CreatePersonBody
        {
            [Required]
            [StringLength(200)]
            public string Name { get; set; } = string.Empty;
        }

        [HttpPost("")]
        public async Task<IActionResult> CreatePerson([FromBody] CreatePersonBody body)
        {
            if (body is null)
            {
                var nullBodyResponse = new BaseResponse
                {
                    Success = false,
                    ResponseCode = (int)HttpStatusCode.BadRequest,
                    Message = "Request body cannot be null."
                };

                return this.GetResponse(nullBodyResponse);
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Invalid value." : e.ErrorMessage)
                    .ToArray();

                var modelErrorResponse = new BaseResponse
                {
                    Success = false,
                    ResponseCode = (int)HttpStatusCode.BadRequest,
                    Message = string.Join(" ", errors)
                };

                return this.GetResponse(modelErrorResponse);
            }
            
            try
            {
                var result = await _mediator.Send(new CreatePerson()
                {
                    Name = body.Name
                });

                return this.GetResponse(result);
            }
            catch (Exception ex)
            {
                return this.GetResponse(new BaseResponse()
                {
                    Message = ex.Message,
                    Success = false,
                    ResponseCode = (int)HttpStatusCode.InternalServerError
                });
            }

        }

        public class UpdatePersonBody
        {
            [Required]
            [StringLength(200)]
            public string NewName { get; set; } = string.Empty;
        }

        [HttpPut("{name}")]
        public async Task<IActionResult> UpdatePerson(string name, [FromBody] UpdatePersonBody body)
        {
            var cleanName = name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(cleanName))
            {
                return this.GetResponse(BuildBadRequest("Name route parameter cannot be blank."));
            }

            if (body is null)
            {
                return this.GetResponse(BuildBadRequest("Request body cannot be null."));
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Invalid value." : e.ErrorMessage)
                    .ToArray();

                return this.GetResponse(BuildBadRequest(string.Join(" ", errors)));
            }

            var bodyName = body.NewName?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(bodyName) &&
                !string.Equals(bodyName, cleanName, StringComparison.OrdinalIgnoreCase))
            {
                return this.GetResponse(BuildBadRequest("Route name and request body name must match."));
            }

            // Ensure the command uses the canonical route name
            body.NewName = cleanName;

            try
            {
                var result = await _mediator.Send(new UpdatePerson
                {
                    Name = cleanName,
                    NewName = body.NewName ?? string.Empty
                });

                return this.GetResponse(result);
            }
            catch (Exception ex)
            {
                return this.GetResponse(new BaseResponse()
                {
                    Message = ex.Message,
                    Success = false,
                    ResponseCode = (int)HttpStatusCode.InternalServerError
                });
            }
        }
        
        private static BaseResponse BuildBadRequest(string message)
        {
            return new BaseResponse
            {
                Success = false,
                ResponseCode = (int)HttpStatusCode.BadRequest,
                Message = message
            };
        }
    }
}