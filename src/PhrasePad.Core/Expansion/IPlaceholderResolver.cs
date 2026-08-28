namespace PhrasePad.Core.Expansion;

public interface IPlaceholderResolver
{
    IReadOnlyList<PlaceholderField> GetFields(string expansion);

    string Fill(string expansion, IReadOnlyDictionary<string, string> values);
}
