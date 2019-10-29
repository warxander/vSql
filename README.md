# vSql
FiveM resource for connecting to MySQL database.

# Features
* **Async mode only**
* Based on latest* [Async MySQL Connector for .NET and .NET Core](https://github.com/mysql-net/MySqlConnector)
* Highly compatible with existing [MySQL Async Library](https://github.com/brouznouf/fivem-mysql-async/tree/v2.1.1)
* Nothing extra

# How to Install
* Download and put into `resources/` folder
* Add `ensure vSql` to `server.cfg`
* Add `set mysql_connection_string "server=localhost;database=db;userid=user;password=pwd"` to `server.cfg`
* Add `server_script "@vSql/vSql.lua"` to `__resource.lua`

# API
```lua
vSql.Async.execute(query, parameters, callback)
vSql.Async.transaction(queries, parameters, callback)

vSql.Async.fetchScalar(query, parameters, callback)
vSql.Async.fetchAll(query, parameters, callback)
```
