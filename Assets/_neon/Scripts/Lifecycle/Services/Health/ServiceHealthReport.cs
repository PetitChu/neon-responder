namespace BrainlessLabs.Neon.Lifecycle
{
    /// <summary>
    /// Represents a health report from a service.
    /// </summary>
    public readonly struct ServiceHealthReport
    {
        public ServiceStatus Status { get; }
        public string Message { get; }

        public ServiceHealthReport(ServiceStatus status, string message = null)
        {
            Status = status;
            Message = message;
        }

        public static ServiceHealthReport Initializing(string message = null) =>
            new(ServiceStatus.Initializing, message);

        public static ServiceHealthReport Healthy(string message = null) =>
            new(ServiceStatus.Healthy, message);

        public static ServiceHealthReport Unhealthy(string message) =>
            new(ServiceStatus.Unhealthy, message);
    }
}
