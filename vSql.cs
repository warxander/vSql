using CitizenFX.Core;
using CitizenFX.Core.Native;
using static CitizenFX.Core.Native.API;

using MySqlConnector;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace vSql
{
    public class VSql : BaseScript
    {
        private static ConcurrentQueue<Action> callbackQueue;
        private static string connectionString;
        private static bool wasInit;
        private static Task completedTask = Task.FromResult(false);

        private class DbConnection : IDisposable
        {
            public readonly MySqlConnection Connection;

            public DbConnection(string connectionString)
            { Connection = new MySqlConnection(connectionString); }

            public void Dispose()
            { Connection.Close(); }
        }

        public VSql()
        {
            callbackQueue = new ConcurrentQueue<Action>();

            Exports.Add("ready", new Action<CallbackDelegate>((callback) =>
            {
                Init();

                if (callback != null)
                {
                    callbackQueue.Enqueue(() => callback.Invoke());
                    ScheduleTick();
                }
            }));

            Exports.Add("execute_async", new Action<string, IDictionary<string, object>, CallbackDelegate>(async (query, parameters, callback) =>
            {
                Init();

                var numberOfUpdatedRows = await ExecuteAsync(query, parameters);

                if (callback != null)
                {
                    callbackQueue.Enqueue(() => callback.Invoke(numberOfUpdatedRows));
                    ScheduleTick();
                }
            }));

            Exports.Add("transaction_async", new Action<IList<object>, IDictionary<string, object>, CallbackDelegate>(async (queries, parameters, callback) =>
            {
                Init();

                var isSucceed = await TransactionAsync(queries.Select(query => query.ToString()).ToList(), parameters);

                if (callback != null)
                {
                    callbackQueue.Enqueue(() => callback.Invoke(isSucceed));
                    ScheduleTick();
                }
            }));

            Exports.Add("fetch_scalar_async", new Action<string, IDictionary<string, object>, CallbackDelegate>(async (query, parameters, callback) =>
            {
                Init();

                var result = await FetchScalarAsync(query, parameters);

                if (callback != null)
                {
                    callbackQueue.Enqueue(() => callback.Invoke(result));
                    ScheduleTick();
                }
            }));

            Exports.Add("fetch_all_async", new Action<string, IDictionary<string, object>, CallbackDelegate>(async (query, parameters, callback) =>
            {
                Init();

                var result = await FetchAllAsync(query, parameters);

                if (callback != null)
                {
                    callbackQueue.Enqueue(() => callback.Invoke(result));
                    ScheduleTick();
                }
            }));
        }

        private static void PrintException(Exception ex)
        { CitizenFX.Core.Debug.Write("^4[" + DateTime.Now + "] ^2[vSql] ^1[Error] " + ex.Message + "\n"); }

        private static void ScheduleTick()
        {
            ScheduleResourceTick("vSql");
        }

        private static async Task<int> ExecuteAsync(string query, IDictionary<string, object> parameters)
        {
            int numberOfUpdatedRows = 0;

            try
            {
                using (var db = new DbConnection(connectionString))
                {
                    await db.Connection.OpenAsync();

                    using (var command = db.Connection.CreateCommand())
                    {
                        BuildCommand(command, query, parameters);
                        numberOfUpdatedRows = await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            { PrintException(ex); }

            return numberOfUpdatedRows;
        }

        private static async Task<bool> TransactionAsync(IList<string> queries, IDictionary<string, object> parameters)
        {
            bool isSucceed = false;

            try
            {
                using (var db = new DbConnection(connectionString))
                {
                    await db.Connection.OpenAsync();

                    using (var command = db.Connection.CreateCommand())
                    {
                        foreach (var parameter in parameters ?? Enumerable.Empty<KeyValuePair<string, object>>())
                            command.Parameters.AddWithValue(parameter.Key, parameter.Value);

                        using (var transaction = await db.Connection.BeginTransactionAsync())
                        {
                            command.Transaction = transaction;

                            try
                            {
                                foreach (var query in queries)
                                {
                                    command.CommandText = query;
                                    await command.ExecuteNonQueryAsync();
                                }

                                await transaction.CommitAsync();
                                isSucceed = true;
                            }
                            catch (Exception ex)
                            {
                                PrintException(ex);

                                try
                                { await transaction.RollbackAsync(); }
                                catch (Exception rollbackEx)
                                { PrintException(rollbackEx); }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            { PrintException(ex); }

            return isSucceed;
        }

        private static async Task<object> FetchScalarAsync(string query, IDictionary<string, object> parameters)
        {
            object result = null;

            try
            {
                using (var db = new DbConnection(connectionString))
                {
                    await db.Connection.OpenAsync();

                    using (var command = db.Connection.CreateCommand())
                    {
                        BuildCommand(command, query, parameters);
                        result = await command.ExecuteScalarAsync();
                    }
                }
            }
            catch (Exception ex)
            { PrintException(ex); }

            return result;
        }

        private static async Task<List<Dictionary<string, object>>> FetchAllAsync(string query, IDictionary<string, object> parameters)
        {
            var result = new List<Dictionary<string, Object>>();

            try
            {
                using (var db = new DbConnection(connectionString))
                {
                    await db.Connection.OpenAsync();

                    using (var command = db.Connection.CreateCommand())
                    {
                        BuildCommand(command, query, parameters);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                result.Add(Enumerable.Range(0, reader.FieldCount).ToDictionary(
                                    i => reader.GetName(i),
                                    i => reader.IsDBNull(i) ? null : reader.GetValue(i)
                                ));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            { PrintException(ex); }

            return result;
        }

        private static void BuildCommand(MySqlCommand command, string query, IDictionary<string, object> parameters)
        {
            command.CommandText = query;

            foreach (var parameter in parameters ?? Enumerable.Empty<KeyValuePair<string, object>>())
                command.Parameters.AddWithValue(parameter.Key, parameter.Value);
        }

        private static void Init()
        {
            if (wasInit)
                return;

            connectionString = Function.Call<string>(Hash.GET_CONVAR, "mysql_connection_string");
            wasInit = true;
        }

        [Tick]
        private static Task OnTick()
        {
            while (callbackQueue.TryDequeue(out Action action))
                action.Invoke();

            return completedTask;
        }
    }
}
