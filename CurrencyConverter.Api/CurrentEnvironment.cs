namespace CurrencyConverter.Api;

public sealed class CurrentEnvironment
{
    public string EnvironmentName { get; }

    public CurrentEnvironment()
    {
        const string environmentVariableName = "ASPNETCORE_ENVIRONMENT";
        
        var environmentVariable = Environment.GetEnvironmentVariable(environmentVariableName);
        
        EnvironmentName = environmentVariable ??
                          throw new ArgumentException("Environment name cannot be null", nameof(environmentVariableName));

        ThrowIfEnvironmentIsInvalid(EnvironmentName);
    }
    
    public bool IsDevelopmentOrTesting() => 
        Equals(nameof(SupportedEnvironments.Dev), EnvironmentName) || 
        Equals(nameof(SupportedEnvironments.Test), EnvironmentName);
    
    public bool IsProduction() => Equals(nameof(SupportedEnvironments.Prod), EnvironmentName);

    private static void ThrowIfEnvironmentIsInvalid(string environmentName)
    {
        if (Enum
            .GetNames<SupportedEnvironments>()
            .Any(supportedEnvironmentName => Equals(supportedEnvironmentName, environmentName)))
        {
            return;
        }

        throw new InvalidEnvironmentException(environmentName);
    }
    
    private enum SupportedEnvironments
    {
        Dev,
        Prod,
        Test
    }
    
    private static bool Equals(string expectedEnvName, string actualEnvName) => 
        string.Equals(expectedEnvName, actualEnvName, StringComparison.OrdinalIgnoreCase);

    private class InvalidEnvironmentException : Exception
    {
        public InvalidEnvironmentException(string environment) : base(
            $"Environment {environment} is invalid, Valid environments are " +
            $"{string.Join(" ", Enum.GetNames<SupportedEnvironments>())}")
        {
        }
    }
}


