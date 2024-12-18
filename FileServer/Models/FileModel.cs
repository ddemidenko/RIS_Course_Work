using System.ComponentModel;

namespace FileServer.Models;

public class FileModel
{
    public int Id { get; set; }

    [DisplayName("Наименование")]
    public string Name { get; set; }

    public int UserModelId { get; set; }

    public byte[]? Bytes { get; set; }

    [DisplayName("Уровень доступа")]
    public int ShareToAll { get; set; }

    public string? Path { get; set; }


    public UserModel UserModel { get; set; }

    public List<AccessModel> SharedUsers { get; set; }
}