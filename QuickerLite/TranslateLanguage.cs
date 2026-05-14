namespace QuickerLite;

public sealed record TranslateLanguage(string Name, string Code)
{
    public override string ToString()
    {
        return Name;
    }
}
