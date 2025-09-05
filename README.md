# carols-movie-finder
Uses a barcode reader to fetch movie details and write to file

## Usage

### Interactive Mode

Run the script and enter barcodes one at a time:

```
python barcode.py
```

You will be prompted to enter barcodes manually. Type `exit` to quit.

### Batch Mode (CSV File)

To process a list of barcodes from a CSV file (one barcode per line, first column):

```
python barcode.py --file=movies.csv
```

Replace `movies.csv` with the path to your CSV file containing barcodes.

## API Keys

You must provide your own OMDb API key in the script for it to work. See comments in `barcode.py` for details.
