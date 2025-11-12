using MediatR;
using Microsoft.AspNetCore.Mvc;
using StargateAPI.Business.Commands;
using StargateAPI.Business.Queries;
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
                var result = await _mediator.Send(new GetPeople()
                {

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

        [HttpGet("{name}")]
        public async Task<IActionResult> GetPersonByName(string name)
        {
            try
            {
                var result = await _mediator.Send(new GetPersonByName()
                {
                    Name = name
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
            [System.ComponentModel.DataAnnotations.Required]
            [System.ComponentModel.DataAnnotations.StringLength(200)]
            public string Name { get; set; } = string.Empty;
        }

        [HttpPost("")]
        public async Task<IActionResult> CreatePerson([FromBody] CreatePersonBody body)
        {
            try
            {
                if (!ModelState.IsValid) return this.GetResponse(new BaseResponse { Success = false, ResponseCode = 400, Message = "Invalid input." });

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
            [System.ComponentModel.DataAnnotations.Required]
            [System.ComponentModel.DataAnnotations.StringLength(200)]
            public string NewName { get; set; } = string.Empty;
        }

        [HttpPut("{name}")]
        public async Task<IActionResult> UpdatePerson(string name, [FromBody] UpdatePersonBody body)
        {
            try
            {
                if (!ModelState.IsValid) return this.GetResponse(new BaseResponse { Success = false, ResponseCode = 400, Message = "Invalid input." });
                var result = await _mediator.Send(new UpdatePerson
                {
                    Name = name,
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
    }
}