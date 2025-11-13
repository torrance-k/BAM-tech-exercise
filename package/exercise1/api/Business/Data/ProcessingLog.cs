using System;

namespace StargateAPI.Business.Data
{
    public class ProcessingLog
    {
        public int Id { get; set; }
        public DateTime TimestampUtc { get; set; }
        public string Level { get; set; } = string.Empty;
        public string RequestName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? ResponseCode { get; set; }
        public string? ExceptionType { get; set; }
        public string? ExceptionMessage { get; set; }
        public string? ExceptionStackTrace { get; set; }
    }
}