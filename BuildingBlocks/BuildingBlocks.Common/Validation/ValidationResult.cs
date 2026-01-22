namespace BuildingBlocks.Common.Validation;

public class ValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public Dictionary<string, List<string>> Errors { get; } = new();

    public void AddError(string field, string message)
    {
        if (!Errors.ContainsKey(field))
        {
            Errors[field] = new List<string>();
        }
        Errors[field].Add(message);
    }

    public void AddErrors(string field, IEnumerable<string> messages)
    {
        foreach (var message in messages)
        {
            AddError(field, message);
        }
    }

    public void Merge(ValidationResult other)
    {
        foreach (var (field, messages) in other.Errors)
        {
            AddErrors(field, messages);
        }
    }

    public IDictionary<string, string[]> ToDictionary()
    {
        return Errors.ToDictionary(x => x.Key, x => x.Value.ToArray());
    }
}
