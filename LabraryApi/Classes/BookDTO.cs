namespace LabraryApi.Classes {
    public record BookDTO (
        string id,
        string name,
        string description,
        string author,
        int year,
        string publisher
    );
}
