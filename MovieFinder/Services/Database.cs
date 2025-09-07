using SQLite;
using System.Collections.Generic;
using System.Threading.Tasks;
using MovieFinder.Models;

namespace MovieFinder.Services;

public class Database
{
    private readonly SQLiteAsyncConnection _database;
    private readonly IAppLogger _logger;

    public Database(string dbPath, IAppLogger logger)
    {
        _database = new SQLiteAsyncConnection(dbPath);
        _logger = logger;
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
        _logger.Log($"Saving movie. ID before save: {movie.Id}, ImdbID: {movie.ImdbID}");
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
            if (existingMovie.Id == 0)
            {
                _logger.Warn($"Existing movie '{existingMovie.Title}' has ID 0. This should not happen. Deleting and re-inserting.");
                await _database.DeleteAsync(existingMovie);
                await _database.InsertAsync(movie);
            }
            else
            {
                _logger.Log($"Found existing movie with ID: {existingMovie.Id}. Updating.");
                movie.Id = existingMovie.Id;
                await _database.UpdateAsync(movie);
            }
        }
        else
        {
            _logger.Log("No existing movie found. Inserting new movie.");
            await _database.InsertAsync(movie);
        }
        _logger.Log($"Movie saved. ID after save: {movie.Id}");
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
