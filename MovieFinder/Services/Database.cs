using SQLite;
using System.Collections.Generic;
using System.Threading.Tasks;
using MovieFinder.Models;

namespace MovieFinder.Services;

public class Database
{
    private readonly SQLiteAsyncConnection _database;

    public Database(string dbPath)
    {
        _database = new SQLiteAsyncConnection(dbPath);
        _database.CreateTableAsync<Movie>().Wait();
    }

    public Task<List<Movie>> GetMoviesAsync()
    {
        return _database.Table<Movie>().ToListAsync();
    }

    public Task<Movie> GetMovieAsync(int id)
    {
        return _database.Table<Movie>().Where(i => i.Id == id).FirstOrDefaultAsync();
    }

    public async Task<Movie> SaveMovieAsync(Movie movie)
    {
        Movie? existingMovie = null;

        if (!string.IsNullOrEmpty(movie.ImdbID))
        {
            existingMovie = await _database.Table<Movie>().Where(m => m.ImdbID == movie.ImdbID).FirstOrDefaultAsync();
        }
        else if (!string.IsNullOrEmpty(movie.Title) && !string.IsNullOrEmpty(movie.Year))
        {
            existingMovie = await _database.Table<Movie>()
                .Where(m => m.Title == movie.Title && m.Year == movie.Year)
                .FirstOrDefaultAsync();
        }

        if (existingMovie != null)
        {
            // Preserve the ID of the existing record
            movie.Id = existingMovie.Id;
            await _database.UpdateAsync(movie);
        }
        else
        {
            // Insert new movie and let the database generate the ID
            await _database.InsertAsync(movie);
        }
        return movie;
    }

    public Task<int> DeleteMovieAsync(Movie movie)
    {
        return _database.DeleteAsync(movie);
    }

    public Task<List<Movie>> SearchMoviesAsync(string query)
    {
        return _database.Table<Movie>().Where(m => m.Title != null && m.Title.ToLower().Contains(query.ToLower())).ToListAsync();
    }
}
