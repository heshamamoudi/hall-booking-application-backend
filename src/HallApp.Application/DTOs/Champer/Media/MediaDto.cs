using HallApp.Application.DTOs.Champer.Hall;

namespace HallApp.Application.DTOs.Champer.Media;

public class MediaDto
{
    public int ID { get; set; }
    public string MediaType { get; set; }  // e.g., image, video, etc.
    public string URL { get; set; }
    public int HallID { get; set; }
    public int index { get; set; }
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
}
