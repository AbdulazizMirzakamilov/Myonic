namespace Myonic.Core.Models;

public class Exercise
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ExerciseType Type { get; set; }
    public string? Notes { get; set; }
    public bool IsCustom { get; set; }      // создано пользователем
    public bool IsArchived { get; set; }    // скрыто, но история сохранена
}