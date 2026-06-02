using System.Web.Http;

namespace SampleLegacyApp.Web.Controllers;

[RoutePrefix("api/customers")]
public class CustomersApiController : ApiController
{
    [HttpGet]
    [Route("{id}")]
    public IHttpActionResult Get(int id)
    {
        return Ok(new
        {
            Id = id,
            Name = "Sample customer"
        });
    }

    [HttpPost]
    [Route("")]
    public IHttpActionResult Create(CustomerRequest request)
    {
        return Ok(new
        {
            request.Name
        });
    }
}

public sealed class CustomerRequest
{
    public string? Name { get; init; }
}