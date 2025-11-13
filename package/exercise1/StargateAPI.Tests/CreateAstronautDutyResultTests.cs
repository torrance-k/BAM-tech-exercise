using StargateAPI.Business.Commands;
using StargateAPI.Controllers;

namespace StargateAPI.Tests
{
    public class CreateAstronautDutyResultTests
    {
        [Fact]
        public void DefaultResult_HasNullIdAndInheritsBaseResponse()
        {
            var result = new CreateAstronautDutyResult();

            Assert.Null(result.Id);

            // This compiles because CreateAstronautDutyResult inherits BaseResponse
            BaseResponse asBase = result;
            Assert.Same(result, asBase);
        }

        [Fact]
        public void CanSetIdAndMessageProperties()
        {
            var result = new CreateAstronautDutyResult
            {
                Id = 42,
                Success = true,
                ResponseCode = 201,
                Message = "Created"
            };

            Assert.Equal(42, result.Id);
            Assert.True(result.Success);
            Assert.Equal(201, result.ResponseCode);
            Assert.Equal("Created", result.Message);
        }
    }
}
