namespace OrderManagement.Api.Models
{
    public class ApiErrorResponse
    {
        public bool Success { get; set; } = false;

        public string Message { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;

        public string CorrelationId { get; set; } = string.Empty;

        public Dictionary<string, string[]>? Errors { get; set; }
    }
}
