namespace LibrarySystem.Services.Exceptions;

public class LoanLimitExceededException : Exception
{
    public LoanLimitExceededException() : base("Member cannot borrow more than 3 books at the same time")
    {
    }

    public LoanLimitExceededException(string message) : base(message)
    {
    }
}

public class BookNotAvailableException : Exception
{
    public BookNotAvailableException() : base("No copies of this book are available for borrowing")
    {
    }

    public BookNotAvailableException(string message) : base(message)
    {
    }
}

public class MembershipExpiredException : Exception
{
    public MembershipExpiredException() 
        : base("Membership has expired")
    {
    }
    
    public MembershipExpiredException(string message) : base(message)
    {
    }
}

public class OutstandingFineException : Exception
{
    public OutstandingFineException(decimal fine)
        : base($"Member has outstanding fine of £{fine}")
    {
    }

    public OutstandingFineException(string message) : base(message)
    {
    }
}