<%@ WebService Language="C#" Class="CustomerService" %>
using System.Web.Services;

[WebService(Namespace = "http://samplelegacylens.local/")]
[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
public class CustomerService : WebService
{
    [WebMethod]
    public string Ping()
    {
        return "ok";
    }
}
