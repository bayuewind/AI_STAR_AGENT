namespace Core.Models;

public class ToolSchema
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<ToolParameter> Parameters { get; set; } = new();
    public List<ToolErrorSchema> PossibleErrors { get; set; } = new();
}

public class ToolParameter
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "string"; // string, int, bool, object
    public string Description { get; set; } = string.Empty;
    public bool Required { get; set; } = true;
}

public class ToolErrorSchema
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
