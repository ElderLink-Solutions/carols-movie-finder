using SQLite;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using MovieFinder.Models;

namespace MovieFinder.Services;

public class Database
{
    private readonly string _dbPath;
    private readonly IAppLogger _logger;

    public Database(string dbPath, IAppLogger logger)
    {
        _logger = logger;
        _dbPath = dbPath;
        _logger.Log("Database service created.");
        _logger.Log($"Database path: {dbPath}");
        // Create the table if it doesn't exist
        using (var db = new SQLiteConnection(dbPath))
        {
            db.CreateTable<Movie>();
        }
    }

    public async Task<List<Movie>> GetMoviesAsync()
    {
        var database = new SQLiteAsyncConnection(_dbPath);
        var movies = await database.Table<Movie>().ToListAsync();
        await database.CloseAsync();
        return movies;
    }

    public async Task<Movie> GetMovieAsync(int id)
    {
        var database = new SQLiteAsyncConnection(_dbPath);
        var movie = await database.Table<Movie>().Where(i => i.Id == id).FirstOrDefaultAsync();
        await database.CloseAsync();
        return movie;
    }

    public async Task<Movie> SaveMovieAsync(Movie movie)
    {
        try
        {
            var database = new SQLiteAsyncConnection(_dbPath);
            _logger.Log($"Saving movie. ID before save: {movie.Id}, ImdbID: {movie.ImdbID}");
            Movie? existingMovie = null;

            if (!string.IsNullOrEmpty(movie.ImdbID))
            {
                existingMovie = await database.Table<Movie>().Where(m => m.ImdbID == movie.ImdbID).FirstOrDefaultAsync();
            }
            else if (!string.IsNullOrEmpty(movie.Title) && !string.IsNullOrEmpty(movie.Year))
            {
                existingMovie = await database.Table<Movie>()
                    .Where(m => m.Title == movie.Title && m.Year == movie.Year)
                    .FirstOrDefaultAsync();
            }

            if (existingMovie != null)
            {
                if (existingMovie.Id == 0)
                {
                    _logger.Warn($"Existing movie '{existingMovie.Title}' has ID 0. This should not happen. Deleting and re-inserting.");
                    await database.DeleteAsync(existingMovie);
                    await database.InsertAsync(movie);
                }
                else
                {
                    _logger.Log($"Found existing movie with ID: {existingMovie.Id}. Updating.");
                    movie.Id = existingMovie.Id;
                    await database.UpdateAsync(movie);
                }
            }
            else
            {
                _logger.Log("No existing movie found. Inserting new movie.");
                await database.InsertAsync(movie);
            }
            _logger.Log($"Movie saved. ID after save: {movie.Id}");

            var savedMovie = await GetMovieAsync(movie.Id);
            _logger.Log($"Saved movie from DB: {Newtonsoft.Json.JsonConvert.SerializeObject(savedMovie)}");
            await database.CloseAsync();
            return movie;
        }
        catch (Exception ex)
        {
            _logger.Error($"Exception in SaveMovieAsync: {ex}");
            throw;
        }
    }

    public async Task<int> DeleteMovieAsync(Movie movie)
    {
        var database = new SQLiteAsyncConnection(_dbPath);
        var result = await database.DeleteAsync(movie);
        await database.CloseAsync();
        return result;
    }

    public async Task<List<Movie>> SearchMoviesAsync(string query)
    {
        var database = new SQLiteAsyncConnection(_dbPath);
        var movies = await database.Table<Movie>().Where(m => m.Title != null && m.Title.ToLower().Contains(query.ToLower())).ToListAsync();
        await database.CloseAsync();
        return movies;
    }
}
