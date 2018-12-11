# LinqPad-Queries

A collection of useful LinqPad scripts I've written

## Latest LinqPad version.linq

Although LinqPad will auto check for release updates, this script is useful seeing the latest beta build along with a description of whats in the latest beta build. It can automatically download the latest zip, and give it the name of the build. You'll need to set the variable `downloadFolder` to your download destination.

## Object Explorer.linq

I wrote this before LinqPad integrated ILSpy. This script dumps an assemblies namespaces, classes, methods and properties into the Output panel. Out the box it'll run against mscorlib. Just set the variable `path`.

## Orphan MSI cleardown.linq

This is useful for some windows configurations which don't tidy up their uupdate installer files. It scans through various *.msp files in C:\windows\Installer\ and checks if they have enteries in the registry using the Windows Installer COM libary. Out the box it will only report what it found, to delete files remove the comment block.

## SQL Search.linq

This will search a databases tables, columns and store procedures for a certain search term and list the results in the output panel.

## SQL Compare.linq
<span style="color:red">**Alpha**</span>

By setting various constants at the top of the file, you can compare the data of 2 SQL databases. This script uses Dapper. It currently doesn't fully work (in testing it kept reapplying changes) and always tries to enable key id inserts, even when there's no auto increment.
When run it will list common tables between the 2 databases and you select which tables to compare. On compare it tries to work out the tables key, if they match it'll compare every field of each row. It will then produce a SQL script of all the changes required to sync the target database.

## Window Logger\Window Logger.linq

Using a SQLite database which can be created using the script `Window Logger.sql` this application will log the currently active window at timed intervals, useful for work tracking.

## Window Logger\Window Log Report.linq

Using the data created by Window Logger this will chart out either the most used programs, or mosted used windows.
