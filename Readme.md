# logfiler

A very simple library that reads a log file and stores it in a sqlite database

## Usage

```sh
logfiler <file> <pattern> [--silent] [--peek]
```

* file: path to the log file, also the base name of the sqlite database. Any existing database with the same name will be replaced.
* pattern: pattern to parse the log file, for example {timestamp:date:MM/dd/yyyy} {level:graphql-level} {source} {message}

## Pattern Format

`{name:type:format}`

* name: name of the group to match
* type: type of the group (date, int, graphql-level, or text) (optional, default text)
* format: format of the group (optional), only supports date atm
  * for type `date`: format of the date, default yyyy-MM-ddThh:mm:ss.ffffffZ
  
## Example
    
```sh
logfiler /var/log/myapp.log "{timestamp:date:yyyy-MM-ddThh:mm:ss.ffffffZ} {level:graphql-level} {source} {message}"
```
