using SampleLegacyApp.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ICustomerService, CustomerService>();

var app = builder.Build();

app.MapGet("/", () => "Sample legacy web app");
app.MapGet("/customers/{id:int}", (int id, ICustomerService customerService) => customerService.GetCustomer(id));

app.Run();
