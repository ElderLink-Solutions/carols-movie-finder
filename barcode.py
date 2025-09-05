import requests
import json
import os
import sys
import csv

# --- CONFIGURATION ---
# IMPORTANT: You need to get your own free API keys for this to work.
# 1. OMDb API Key: Go to http://www.omdbapi.com/apikey.aspx
# 2. UPCitemdb API Key: This is not strictly necessary for basic use as they have a free, keyless tier.
#    However, for more reliable results, sign up at https://www.upcitemdb.com/api/register
OMDB_API_KEY = 'YOUR_OMDB_API_KEY'  # <--- PASTE YOUR OMDb API KEY HERE
OUTPUT_FILE = 'movies_collection.txt'

def get_movie_info_from_upc(barcode):
    """
    Looks up a barcode using UPCitemdb to find the movie title or IMDb ID.
    """
    print(f"-> Searching for barcode: {barcode}...")
    # This API lets you make some requests without a key, but signing up is better.
    url = f"https://api.upcitemdb.com/prod/v1/lookup?upc={barcode}"

    try:
        response = requests.get(url)
        response.raise_for_status()  # Raises an exception for bad status codes (4xx or 5xx)
        data = response.json()

        if data.get('items') and len(data['items']) > 0:
            item = data['items'][0]
            # We prioritize getting an IMDb ID if it's available.
            if 'imdb_id' in item and item['imdb_id']:
                 return {'imdb_id': item['imdb_id']}
            # Otherwise, we fall back to the title.
            elif 'title' in item and item['title']:
                return {'title': item['title']}
    except requests.exceptions.RequestException as e:
        print(f"  [ERROR] Could not connect to UPCitemdb API: {e}")
    except json.JSONDecodeError:
        print("  [ERROR] Failed to parse response from UPCitemdb.")

    return None


def get_movie_details(movie_identifier):
    """
    Fetches detailed movie information from OMDb using either a title or IMDb ID.
    """
    if not OMDB_API_KEY or OMDB_API_KEY == 'YOUR_OMDB_API_KEY':
        print("  [FATAL ERROR] OMDb API key is missing. Please add it to the script.")
        return None

    base_url = f"http://www.omdbapi.com/?apikey={OMDB_API_KEY}"

    # Check if we have an IMDb ID or a title
    if 'imdb_id' in movie_identifier:
        print(f"-> Looking up details for IMDb ID: {movie_identifier['imdb_id']}...")
        params = {'i': movie_identifier['imdb_id']}
    elif 'title' in movie_identifier:
        print(f"-> Looking up details for title: {movie_identifier['title']}...")
        params = {'t': movie_identifier['title']}
    else:
        return None # No valid identifier

    try:
        response = requests.get(base_url, params=params)
        response.raise_for_status()
        data = response.json()

        if data.get('Response') == 'True':
            return data
        else:
            print(f"  [ERROR] OMDb API Error: {data.get('Error', 'Unknown error')}")

    except requests.exceptions.RequestException as e:
        print(f"  [ERROR] Could not connect to OMDb API: {e}")
    except json.JSONDecodeError:
        print("  [ERROR] Failed to parse response from OMDb.")

    return None

def format_movie_details(details):
    """
    Formats the movie details into a clean string for saving to a file.
    """
    # Using .get() is safer as it returns None if a key is missing, avoiding errors.
    title = details.get('Title', 'N/A')
    year = details.get('Year', 'N/A')
    rated = details.get('Rated', 'N/A')
    genre = details.get('Genre', 'N/A')
    director = details.get('Director', 'N/A')
    plot = details.get('Plot', 'N/A')
    imdb_rating = details.get('imdbRating', 'N/A')

    return (
        f"========================================\n"
        f"Title: {title} ({year})\n"
        f"Rated: {rated}\n"
        f"Genre: {genre}\n"
        f"Director: {director}\n"
        f"IMDb Rating: {imdb_rating}/10\n"
        f"Plot: {plot}\n"
        f"========================================\n\n"
    )

def process_barcode(barcode):
    """
    Process a single barcode: fetch movie info and write to file.
    """
    movie_identifier = get_movie_info_from_upc(barcode)

    if movie_identifier:
        details = get_movie_details(movie_identifier)
        if details:
            formatted_details = format_movie_details(details)

            print("\n--- Found Movie ---")
            print(formatted_details.strip())

            try:
                with open(OUTPUT_FILE, 'a', encoding='utf-8') as f:
                    f.write(formatted_details)
                print(f"Successfully saved to '{OUTPUT_FILE}'")
            except IOError as e:
                print(f"  [ERROR] Could not write to file: {e}")
    else:
        print("  [FAILURE] Could not find a movie for that barcode.")

def process_file(file_path):
    """
    Process barcodes from a CSV file.
    Assumes each row contains a barcode in the first column.
    """
    print(f"--- Batch Mode: Processing barcodes from '{file_path}' ---")
    if not os.path.exists(file_path):
        print(f"[ERROR] File '{file_path}' does not exist.")
        return

    # Create the output file if it doesn't exist
    if not os.path.exists(OUTPUT_FILE):
        open(OUTPUT_FILE, 'w').close()

    with open(file_path, newline='', encoding='utf-8') as csvfile:
        reader = csv.reader(csvfile)
        for row in reader:
            if not row:
                continue
            barcode = row[0].strip()
            if barcode.isdigit():
                print(f"\nProcessing barcode: {barcode}")
                process_barcode(barcode)
            else:
                print(f"[ERROR] Invalid barcode in file: {barcode}")

    print("\nBatch processing complete.")

def main():
    """
    Main application loop.
    Supports interactive mode and batch mode via --file argument.
    """
    print("--- Movie Barcode Scanner App ---")
    print(f"Movie details will be saved to '{OUTPUT_FILE}'")

    # Check for --file argument
    file_arg = None
    for arg in sys.argv[1:]:
        if arg.startswith("--file="):
            file_arg = arg.split("=", 1)[1]

    if file_arg:
        process_file(file_arg)
        return

    print("Type 'exit' to quit.")

    # Create the file if it doesn't exist
    if not os.path.exists(OUTPUT_FILE):
        open(OUTPUT_FILE, 'w').close()

    while True:
        barcode = input("\nEnter barcode (or 'exit'): ").strip()

        if barcode.lower() == 'exit':
            break

        if not barcode.isdigit():
            print("  [ERROR] Invalid input. Please enter numbers only.")
            continue

        process_barcode(barcode)

    print("\nApplication closed.")

if __name__ == '__main__':
    main()
