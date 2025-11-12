using MediatR;
using Microsoft.AspNetCore.Mvc;
using StargateAPI.Business.Commands;
using StargateAPI.Business.Queries;
using System.Net;

namespace StargateAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AstronautDutyController : ControllerBase
    {
        private readonly IMediator _mediator;
        public AstronautDutyController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("{name}")]
        public async Task<IActionResult> GetAstronautDutiesByName(string name)
        {
            var cleanName = name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(cleanName))
            {
                var response = new BaseResponse
                {
                    Success = false,
                    ResponseCode = (int)HttpStatusCode.BadRequest,
                    Message = "Name route parameter cannot be blank."
                };

                return this.GetResponse(response);
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

        [HttpPost("")]
        public async Task<IActionResult> CreateAstronautDuty([FromBody] CreateAstronautDuty request)
        {
            if (request is null)
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
                var result = await _mediator.Send(request);
                return this.GetResponse(result);
            }
            catch (Exception ex)
            {
                var response = new BaseResponse
                {
                    Message = ex.Message,
                    Success = false,
                    ResponseCode = (int)HttpStatusCode.InternalServerError
                };

                return this.GetResponse(response);
            }
     
        }
    }
}