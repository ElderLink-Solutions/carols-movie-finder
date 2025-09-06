using SQLite;
using Newtonsoft.Json;

namespace MovieFinder.Models;

public class Movie
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [JsonProperty("Title")]
    public string? Title { get; set; }

    [JsonProperty("Year")]
    public string? Year { get; set; }

    [JsonProperty("Genre")]
    public string? Genre { get; set; }

    [JsonProperty("Director")]
    public string? Director { get; set; }

    [JsonProperty("Actors")]
    public string? Actors { get; set; }

    [JsonProperty("Plot")]
    public string? Plot { get; set; }

    [JsonProperty("imdbRating")]
    public string? ImdbRating { get; set; }

    [JsonProperty("Runtime")]
    public string? Runtime { get; set; }

    public string ShelfLocation { get; set; } = string.Empty;

    public bool IsBorrowed { get; set; }

    public string BorrowedBy { get; set; } = string.Empty;

    [JsonProperty("Poster")]
    public string? Poster { get; set; }

    [JsonProperty("imdbID")]
    public string? ImdbID { get; set; }
}
