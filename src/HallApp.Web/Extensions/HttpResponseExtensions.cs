using System.Text.Json;

namespace HallApp.Web.Extensions;

public static class HttpResponseExtensions
{
    public static void AddPaginationHeader(this HttpResponse response, int currentPage, int itemsPerPage, int totalItems, int totalPages)
    {
        var paginationHeader = new
        {
            currentPage,
            itemsPerPage,
            totalItems,
            totalPages
        };

        response.Headers["Pagination"] = JsonSerializer.Serialize(paginationHeader);
        response.Headers["Access-Control-Expose-Headers"] = "Pagination";
    }
}
