vSql = { }
vSql.__index = vSql


vSql.Async = { }


local function safeCallback(callback)
	if callback then
		assert(type(callback) == 'function', 'Callback must be a function type!')
	end

	return callback
end


local function safeQuery(query)
	assert(type(query) == 'string', 'Query must be a string type!')
	assert(query ~= '', 'Query must be a non-empty string!')

	return query
end


local function safeQueries(queries)
	assert(type(queries) == 'table', 'Queries must be in table!')
	for _, query in ipairs(queries) do
		query = safeQuery(query)
	end

	return queries
end


local function safeParameters(parameters)
	if parameters then
		assert(type(parameters) == 'table', 'Parameters must be in table!')
	end

	if not parameters or not next(parameters) then
		return { [''] = true }
	end

	return parameters
end


function vSql.ready(callback)
	assert(callback and type(callback) == 'function', 'Callback must be a function type!')

	exports.vSql:ready(callback)
end


function vSql.Async.execute(query, parameters, callback)
	exports.vSql:execute_async(safeQuery(query), safeParameters(parameters), safeCallback(callback))
end


function vSql.Async.fetchScalar(query, parameters, callback)
	exports.vSql:fetch_scalar_async(safeQuery(query), safeParameters(parameters), safeCallback(callback))
end


function vSql.Async.fetchAll(query, parameters, callback)
	exports.vSql:fetch_all_async(safeQuery(query), safeParameters(parameters), safeCallback(callback))
end


function vSql.Async.transaction(queries, parameters, callback)
	exports.vSql:transaction_async(safeQueries(queries), safeParameters(parameters), safeCallback(callback))
end
