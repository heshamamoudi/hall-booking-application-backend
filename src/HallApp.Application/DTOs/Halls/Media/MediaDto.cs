using HallApp.Application.DTOs.Halls.Hall;

namespace HallApp.Application.DTOs.Halls.Media;

public class MediaDto
{
    public int ID { get; set; }
    public string MediaType { get; set; } = string.Empty;  // e.g., image, video, etc.
    public string URL { get; set; } = string.Empty;
    public int HallID { get; set; }
    public int index { get; set; }
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
}
