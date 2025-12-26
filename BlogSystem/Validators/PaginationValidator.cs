namespace BlogSystem.Validators;

public class PaginationValidator
{
    private const int MIN_PAGE_NUMBER = 1;
    private const int MIN_PAGE_SIZE = 1;
    private const int MAX_PAGE_SIZE = 50;

    public int PageNumber { get; }

    public int PageSize { get; }

    public PaginationValidator(int pageNumber, int pageSize)
    {
        PageNumber = pageNumber < MIN_PAGE_NUMBER
            ? MIN_PAGE_NUMBER
            : pageNumber;

        PageSize = pageSize < MIN_PAGE_SIZE
            ? MAX_PAGE_SIZE
            : (pageSize > MAX_PAGE_SIZE ? MAX_PAGE_SIZE : pageSize);
    }
}
