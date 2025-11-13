using System.Net;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using StargateAPI.Controllers;

namespace StargateAPI.Tests
{
    public class AstronautDutyControllerTests
    {
        [Fact]
        public async Task GetAstronautDutiesByName_BlankName_ReturnsBadRequest()
        {
            IMediator mediator = new StubMediator();
            var controller = new AstronautDutyController(mediator);

            var result = await controller.GetAstronautDutiesByName("   ");

            var objectResult = Assert.IsType<ObjectResult>(result);
            var response = Assert.IsType<BaseResponse>(objectResult.Value);

            Assert.Equal((int)HttpStatusCode.BadRequest, response.ResponseCode);
            Assert.False(response.Success);
            Assert.Equal("Name route parameter cannot be blank.", response.Message);
        }

        [Fact]
        public async Task CreateAstronautDuty_NullBody_ReturnsBadRequest()
        {
            IMediator mediator = new StubMediator();
            var controller = new AstronautDutyController(mediator);

            var result = await controller.CreateAstronautDuty(null!);

            var objectResult = Assert.IsType<ObjectResult>(result);
            var response = Assert.IsType<BaseResponse>(objectResult.Value);

            Assert.Equal((int)HttpStatusCode.BadRequest, response.ResponseCode);
            Assert.False(response.Success);
            Assert.Equal("Request body cannot be null.", response.Message);
        }
    }
}
