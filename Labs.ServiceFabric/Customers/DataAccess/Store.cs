using System.Collections.Concurrent;

namespace Customers.DataAccess;

public class Store
{
    public ConcurrentDictionary<string, Customer> Customers = new ConcurrentDictionary<string, Customer>();
}
