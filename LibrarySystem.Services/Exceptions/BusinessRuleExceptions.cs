namespace LibrarySystem.Services.Exceptions;

public class LoanLimitExceededException : Exception
{
    public LoanLimitExceededException() : base("Member cannot borrow more than 3 books at the same time")
    {
    }
}

public class BookNotAvailableException : Exception
{
    public BookNotAvailableException() : base("No copies of this book are available for borrowing")
    {
    }
}

public class MembershipExpiredException : Exception
{
    public MembershipExpiredException() 
        : base("Membership has expired")
    {
    }
}

public class OutstandingFineException : Exception
{
    public OutstandingFineException(decimal fine)
        : base($"Member has outstanding fine of £{fine}")
    {
    }
}

public class AlreadyReturnedException : Exception
{
    public AlreadyReturnedException() 
        : base("This book has already been returned") { }
}