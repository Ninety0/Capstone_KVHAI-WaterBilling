using KVHAI.CustomClass;
using KVHAI.Models;
using System.Data.SqlClient;

namespace KVHAI.Repository
{
    public class HubConnectionRepository
    {
        private readonly DBConnect _dbConnect;
        private readonly InputSanitize _sanitize;

        public HubConnectionRepository(DBConnect dBConnect, InputSanitize inputSanitize)
        {
            _dbConnect = dBConnect;
            _sanitize = inputSanitize;
        }

        #region RESIDENT HUB CONNECTION
        public async Task SaveHubConnection(HubConnect hubConnect)
        {
            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    //REMOVE EXISTING CONNECTION THAT AVOID POPULATING TABLES
                    using (var deleteCommand = new SqlCommand("DELETE FROM hubconnection_tb WHERE resident_id = @res_id AND username = @username", connection))
                    {
                        deleteCommand.Parameters.AddWithValue("@res_id", hubConnect.Resident_ID);
                        deleteCommand.Parameters.AddWithValue("@username", hubConnect.Username);
                        await deleteCommand.ExecuteNonQueryAsync();
                    }

                    using (var insertCommand = new SqlCommand("INSERT INTO hubconnection_tb (connection_id, resident_id,username) VALUES(@connection, @res_id, @username)", connection))
                    {
                        insertCommand.Parameters.AddWithValue("@connection", hubConnect.Connection_ID);
                        insertCommand.Parameters.AddWithValue("@res_id", hubConnect.Resident_ID);
                        insertCommand.Parameters.AddWithValue("@username", hubConnect.Username);
                        await insertCommand.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        public async Task RemoveHubConnection(string connection_id)
        {
            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand("DELETE FROM hubconnection_tb WHERE connection_id = @id", connection))
                    {
                        command.Parameters.AddWithValue("@id", connection_id);
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        public async Task<List<HubConnect>> SelectHubConnection(string res_id)
        {
            try
            {
                var hubConnectionList = new List<HubConnect>();

                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand("SELECT * FROM hubconnection_tb WHERE resident_id = @id", connection))
                    {
                        command.Parameters.AddWithValue("@id", res_id);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                hubConnectionList.Add(new HubConnect
                                {
                                    Connection_ID = reader["connection_id"].ToString(),
                                });
                            }
                        }
                    }
                }
                return hubConnectionList;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<HubConnect>> SelectHubConnection(List<int> residentIdList)
        {
            if (residentIdList == null || !residentIdList.Any())
                return new List<HubConnect>();

            try
            {
                var hubConnectionList = new List<HubConnect>();

                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                using (var command = new SqlCommand())
                {
                    command.Connection = connection;

                    // Create parameterized query for the IN clause
                    var idParameters = string.Join(", ", residentIdList.Select((id, index) => $"@id{index}"));
                    command.CommandText = $"SELECT * FROM hubconnection_tb WHERE resident_id IN ({idParameters})";

                    // Add parameters
                    for (int i = 0; i < residentIdList.Count; i++)
                    {
                        command.Parameters.AddWithValue($"@id{i}", residentIdList[i]);
                    }

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            hubConnectionList.Add(new HubConnect
                            {
                                Connection_ID = reader["connection_id"].ToString(),
                            });
                        }
                    }
                }

                return hubConnectionList;
            }
            catch (Exception ex)
            {
                // Log the exception here (e.g., using ILogger)
                Console.WriteLine(ex.Message);
                return new List<HubConnect>();
            }
        }
        #endregion

        #region EMPLOYEE HUB CONNECTION
        public async Task SaveHubConnectionEmployee(HubConnect hubConnect)
        {
            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    //REMOVE EXISTING CONNECTION THAT AVOID POPULATING TABLES
                    using (var deleteCommand = new SqlCommand("DELETE FROM hubconnection_emp_tb WHERE emp_id = @emp_id AND username = @username", connection))
                    {
                        deleteCommand.Parameters.AddWithValue("@emp_id", hubConnect.Employee_ID);
                        deleteCommand.Parameters.AddWithValue("@username", hubConnect.Username);
                        await deleteCommand.ExecuteNonQueryAsync();
                    }

                    using (var insertCommand = new SqlCommand("INSERT INTO hubconnection_emp_tb (connection_id, emp_id,username) VALUES(@connection, @emp_id, @username)", connection))
                    {
                        insertCommand.Parameters.AddWithValue("@connection", hubConnect.Connection_ID);
                        insertCommand.Parameters.AddWithValue("@emp_id", hubConnect.Employee_ID);
                        insertCommand.Parameters.AddWithValue("@username", hubConnect.Username);
                        await insertCommand.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        public async Task RemoveHubConnectionEmployee(string connection_id)
        {
            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand("DELETE FROM hubconnection_emp_tb WHERE connection_id = @id", connection))
                    {
                        command.Parameters.AddWithValue("@id", connection_id);
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        public async Task<List<HubConnect>> SelectHubConnectionEmployee(string emp_id)
        {
            try
            {
                var hubConnectionList = new List<HubConnect>();

                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand("SELECT * FROM hubconnection_emp_tb WHERE emp_id = @id", connection))
                    {
                        command.Parameters.AddWithValue("@id", emp_id);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                hubConnectionList.Add(new HubConnect
                                {
                                    Connection_ID = reader["connection_id"].ToString(),
                                });
                            }
                        }
                    }
                }
                return hubConnectionList;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<HubConnect>> SelectHubConnectionEmployee(List<string> employeeIdList)
        {
            if (employeeIdList == null || !employeeIdList.Any())
                return new List<HubConnect>();

            try
            {
                var hubConnectionList = new List<HubConnect>();

                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                using (var command = new SqlCommand())
                {
                    command.Connection = connection;

                    // Create parameterized query for the IN clause
                    var idParameters = string.Join(", ", employeeIdList.Select((id, index) => $"@id{index}"));
                    command.CommandText = $"SELECT * FROM hubconnection_emp_tb WHERE emp_id IN ({idParameters})";

                    // Add parameters
                    for (int i = 0; i < employeeIdList.Count; i++)
                    {
                        command.Parameters.AddWithValue($"@id{i}", employeeIdList[i]);
                    }

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            hubConnectionList.Add(new HubConnect
                            {
                                Connection_ID = reader["connection_id"].ToString(),
                            });
                        }
                    }
                }

                return hubConnectionList;
            }
            catch (Exception ex)
            {
                // Log the exception here (e.g., using ILogger)
                Console.WriteLine(ex.Message);
                return new List<HubConnect>();
            }
        }
        #endregion


    }
}
