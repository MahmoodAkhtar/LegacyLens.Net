using SampleLegacyApp.Contracts;
using SampleLegacyApp.Data;

namespace SampleLegacyApp.Services;

public interface ICustomerService
{
    CustomerDto GetCustomer(int id);
}

public class CustomerService : ICustomerService, ICustomerContract
{
    private readonly CustomerRepository _repository = new();

    public CustomerDto GetCustomer(int id)
    {
        var customer = _repository.GetById(id);
        return new CustomerDto
        {
            Id = customer.Id,
            Name = customer.Name
        };
    }
}
