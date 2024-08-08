# logfiler

A very simple library that reads a log file and stores it in a sqlite database

## Usage

```sh
logfiler --file|-f <file> --pattern|-p <pattern> [--silent|-s true|false] [ --commit-size|-c int]
```

* --file|-f: path to the log file, also the base name of the sqlite database if db not specified. Any existing database
  with the same name will be replaced.
* --db|-d: path to the sqlite database file. Any existing database with the same name will be replaced. (optional)
* --pattern|-p:  pattern to parse the log file, for example {timestamp:date:MM/dd/yyyy} {level:graphql-level} {source}
  {message}
* --silent|-s: Silent mode, with less logs (optional - default false)
* --verbose|-v: Verbose mode (even more logs) (optional - default false)
* --commit-size|-c: Number of entries to commit to the database at once (optional)

## Pattern Format

`{name:type:format}`

* name: name of the group to match
* type: type of the group (date, int, graphql-level, or text) (optional, default text)
* format: format of the group (optional), only supports date atm
    * for type `date`: format of the date, default yyyy-MM-ddThh:mm:ss.ffffffZ

If a pattern is not matched, the entire line will be ignored. Run with verbose mode to see the ignored lines.

## Examples

```sh
logfiler \
  /var/log/myapp.log \ 
  "{timestamp:date:yyyy-MM-ddThh:mm:ss.ffffffZ} {level:graphql-level} {source} {message}"
```

```sh
logfiler \
  /var/log/myapp.log \ 
  "{timestamp:date:yyyy-MM-dd} {level:graphql-level} {source} job {result} [table={table}, seqTxn={tx:int}, transactions={transactions:int}, rows={rows:int}, time={time}ms, {message}"
```
