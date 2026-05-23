namespace SampleLegacyApp.Data;

public class CustomerRepository
{
    public CustomerRecord GetById(int id)
    {
        return new CustomerRecord
        {
            Id = id,
            Name = $"Customer {id}"
        };
    }
}

public class CustomerRecord
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
