namespace RaceDay.Api.Services;

public sealed class DuplicateEmailException : Exception
{
    public DuplicateEmailException()
        : base("An account with this email address already exists.")
    {
    }
}
