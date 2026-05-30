<%@ WebHandler Language="C#" Class="DownloadHandler" %>
using System.Web;

public class DownloadHandler : IHttpHandler
{
    public void ProcessRequest(HttpContext context)
    {
        context.Response.ContentType = "text/plain";
        context.Response.Write("download handler");
    }

    public bool IsReusable
    {
        get { return true; }
    }
}
