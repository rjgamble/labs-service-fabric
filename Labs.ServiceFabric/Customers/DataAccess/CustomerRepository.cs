namespace Customers.DataAccess;

public interface ICustomerRepository
{
    Customer? Create(CreateCustomer request);
    Customer? Delete(string id);
    IEnumerable<Customer> GetAll();
    Customer? GetById(string id);
    Customer? Update(string id, string? name, Level? level);
}

public class CustomerRepository : ICustomerRepository
{
    private readonly Store _store;

    public CustomerRepository(Store store)
    {
        _store = store;
    }

    public Customer? Create(CreateCustomer request)
    {
        string id = Nanoid.Nanoid.Generate(size: 6);
        var customer = new Customer(id, request.Name, DateTimeOffset.UtcNow, request.Level);

        if (!_store.Customers.TryAdd(id, customer))
        {
            return null;
        }

        return customer;
    }

    public Customer? GetById(string id)
    {
        return _store.Customers.GetValueOrDefault(id);
    }

    public Customer? Update(string id, string? name, Level? level)
    {
        var existingCustomer = _store.Customers.GetValueOrDefault(id);
        if (existingCustomer == null)
        {
            return null;
        }

        var newCustomer = new Customer(id,
                                       name ?? existingCustomer.Name,
                                       existingCustomer.CreatedAt,
                                       level ?? existingCustomer.Level);

        if (!_store.Customers.TryUpdate(id, newCustomer, existingCustomer))
        {
            return null;
        }

        return newCustomer;
    }

    public Customer? Delete(string id)
    {
        var customer = _store.Customers.GetValueOrDefault(id);
        if (customer == null)
        {
            return null;
        }

        _store.Customers.Remove(id, out var removedCustomer);
        return removedCustomer;
    }

    public IEnumerable<Customer> GetAll()
    {
        return _store.Customers.Values;
    }
}
