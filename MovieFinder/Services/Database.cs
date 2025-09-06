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

    public Task<int> SaveMovieAsync(Movie movie)
    {
        // Upsert: Insert or replace based on unique ImdbID
        return _database.InsertOrReplaceAsync(movie);
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
