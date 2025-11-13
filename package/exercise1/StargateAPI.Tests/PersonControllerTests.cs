using System.Net;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using StargateAPI.Controllers;
using StargateAPI.Business.Commands;

namespace StargateAPI.Tests
{
    public class PersonControllerTests
    {
        [Fact]
        public async Task UpdatePerson_ValidRequest_ReturnsMediatorResult()
        {
            // Arrange
            var mediatorResponse = new UpdatePersonResult
            {
                Success = true,
                ResponseCode = 200,
                Message = "Person renamed to 'Jack ONeill'",
                Id = 42
            };

            IMediator mediator = new StubMediator(mediatorResponse);
            var controller = new PersonController(mediator);

            var body = new PersonController.UpdatePersonBody
            {
                NewName = "Jack ONeill"
            };

            // Act
            var result = await controller.UpdatePerson("Jack ONeill", body);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            var response = Assert.IsType<UpdatePersonResult>(objectResult.Value);

            Assert.True(response.Success);
            Assert.Equal(200, response.ResponseCode);
            Assert.Equal("Person renamed to 'Jack ONeill'", response.Message);
            Assert.Equal(42, response.Id);
        }

        [Fact]
        public async Task UpdatePerson_MediatorThrows_ReturnsInternalServerError()
        {
            // Arrange
            var exception = new InvalidOperationException("Something went wrong in handler.");
            IMediator mediator = new StubMediator(exception);
            var controller = new PersonController(mediator);

            var body = new PersonController.UpdatePersonBody
            {
                NewName = "Janet Fraiser"
            };

            // Act
            var result = await controller.UpdatePerson("Janet Fraiser", body);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            var response = Assert.IsType<BaseResponse>(objectResult.Value);

            Assert.False(response.Success);
            Assert.Equal((int)HttpStatusCode.InternalServerError, response.ResponseCode);
            Assert.Equal("Something went wrong in handler.", response.Message);
        }



        [Fact]
        public async Task GetPersonByName_BlankName_ReturnsBadRequest()
        {
            IMediator mediator = new StubMediator();
            var controller = new PersonController(mediator);

            var result = await controller.GetPersonByName("   ");

            var objectResult = Assert.IsType<ObjectResult>(result);
            var response = Assert.IsType<BaseResponse>(objectResult.Value);

            Assert.Equal((int)HttpStatusCode.BadRequest, response.ResponseCode);
            Assert.False(response.Success);
            Assert.Equal("Name route parameter cannot be blank.", response.Message);
        }

        [Fact]
        public async Task CreatePerson_NullBody_ReturnsBadRequest()
        {
            IMediator mediator = new StubMediator();
            var controller = new PersonController(mediator);

            var result = await controller.CreatePerson(null!);

            var objectResult = Assert.IsType<ObjectResult>(result);
            var response = Assert.IsType<BaseResponse>(objectResult.Value);

            Assert.Equal((int)HttpStatusCode.BadRequest, response.ResponseCode);
            Assert.False(response.Success);
            Assert.Equal("Request body cannot be null.", response.Message);
        }

        [Fact]
        public async Task UpdatePerson_BlankRouteName_ReturnsBadRequest()
        {
            IMediator mediator = new StubMediator();
            var controller = new PersonController(mediator);

            var result = await controller.UpdatePerson("   ", new PersonController.UpdatePersonBody { NewName = "New" });

            var objectResult = Assert.IsType<ObjectResult>(result);
            var response = Assert.IsType<BaseResponse>(objectResult.Value);

            Assert.Equal((int)HttpStatusCode.BadRequest, response.ResponseCode);
            Assert.False(response.Success);
            Assert.Equal("Name route parameter cannot be blank.", response.Message);
        }

        [Fact]
        public async Task UpdatePerson_NameMismatch_ReturnsBadRequest()
        {
            IMediator mediator = new StubMediator();
            var controller = new PersonController(mediator);

            var body = new PersonController.UpdatePersonBody
            {
                NewName = "Body Name"
            };

            var result = await controller.UpdatePerson("Route Name", body);

            var objectResult = Assert.IsType<ObjectResult>(result);
            var response = Assert.IsType<BaseResponse>(objectResult.Value);

            Assert.Equal((int)HttpStatusCode.BadRequest, response.ResponseCode);
            Assert.False(response.Success);
            Assert.Equal("Route name and request body name must match.", response.Message);
        }
    }
}
