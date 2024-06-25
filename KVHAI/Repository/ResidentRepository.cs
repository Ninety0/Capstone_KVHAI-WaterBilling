using KVHAI.CustomClass;
using KVHAI.Models;
using System.Data.SqlClient;
using System.Text;

namespace KVHAI.Repository
{
    public class ResidentRepository
    {
        private readonly DBConnect _dbConnect;
        private readonly Hashing _hash;

        public ResidentRepository(DBConnect dbconn, Hashing hash)
        {
            _dbConnect = dbconn;
            _hash = hash;
        }

        public async Task<List<Resident>> GetAllEmployeesAsync()
        {
            var resident = new List<Resident>();

            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                using (var command = new SqlCommand("SELECT * FROM resident_tb", connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {

                        while (await reader.ReadAsync())
                        {
                            var _resident = new Resident();
                            _resident.Res_ID = reader[0]?.ToString() ?? string.Empty;
                            _resident.Address_ID = reader[1]?.ToString() ?? string.Empty;
                            _resident.Lname = reader[2]?.ToString() ?? string.Empty;
                            _resident.Fname = reader[3]?.ToString() ?? string.Empty;
                            _resident.Mname = reader[4]?.ToString() ?? string.Empty;
                            _resident.Phone = reader[5]?.ToString() ?? string.Empty;
                            _resident.Email = reader[6]?.ToString() ?? string.Empty;
                            _resident.Username = reader[7]?.ToString() ?? string.Empty;
                            _resident.Password = reader[8]?.ToString() ?? string.Empty;
                            _resident.Date_Residency = reader[9]?.ToString() ?? string.Empty;
                            _resident.Occupancy = reader[10]?.ToString() ?? string.Empty;
                            _resident.Created_At = reader[11]?.ToString() ?? string.Empty;
                            resident.Add(_resident);

                        }
                    }
                }
            }

            return resident;
        }

        //address id need to update the code
        public async Task CreateEmployee(Resident resident)
        {
            var res_id = await GetEmployeeId();
            var pass = _hash.HashPassword(resident.Password);
            var dt = GetTimeDate();


            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand("INSERT INTO resident_tb (res_id, address_id, lname, fname, mname, phone, email, username, password, date_residency, occupancy, created_at) VALUES(@id, @addr, @lname, @fname, @mname, @phone, @email, @user, @pass, @residency, @occupy, @create)", connection))
                    {
                        command.Parameters.AddWithValue("@id", res_id);
                        command.Parameters.AddWithValue("@addr", res_id);
                        command.Parameters.AddWithValue("@lname", resident.Lname);
                        command.Parameters.AddWithValue("@fname", resident.Fname);
                        command.Parameters.AddWithValue("@mname", resident.Mname);
                        command.Parameters.AddWithValue("@phone", resident.Phone);
                        command.Parameters.AddWithValue("@email", resident.Email);
                        command.Parameters.AddWithValue("@user", resident.Username);
                        command.Parameters.AddWithValue("@pass", pass);
                        command.Parameters.AddWithValue("@pass", resident.Date_Residency);
                        command.Parameters.AddWithValue("@occupy", resident.Occupancy);
                        command.Parameters.AddWithValue("@create", dt);

                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

        }

        public async Task UpdateCategory(Resident resident)
        {
            var hasValue = !string.IsNullOrEmpty(resident.Password);

            var query = new StringBuilder("UPDATE resident_tb SET ");
            query.Append("lname = @lname, ");
            query.Append("fname = @fname, ");
            query.Append("mname = @mname, ");
            query.Append("phone = @phone, ");
            query.Append("email = @email, ");
            query.Append("username = @user, ");
            if (hasValue)
            {
                query.Append("password = @pass, ");
            }
            query.Append("occupancy = @occupy ");
            query.Append("WHERE res_id = @id");


            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(query.ToString(), connection))
                    {
                        command.Parameters.AddWithValue("@id", resident.Res_ID);
                        command.Parameters.AddWithValue("@lname", resident.Lname);
                        command.Parameters.AddWithValue("@fname", resident.Fname);
                        command.Parameters.AddWithValue("@mname", resident.Mname);
                        command.Parameters.AddWithValue("@phone", resident.Phone);
                        command.Parameters.AddWithValue("@email", resident.Email);
                        command.Parameters.AddWithValue("@user", resident.Username);

                        if (hasValue)
                        {
                            var hashPass = _hash.HashPassword(resident.Password);
                            command.Parameters.AddWithValue("@pass", hashPass);
                        }

                        command.Parameters.AddWithValue("@occupy", resident.Occupancy);

                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task DeleteCategory(int id)
        {
            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand("DELETE FROM Category WHERE res_id=@id", connection))
                    {
                        command.Parameters.AddWithValue("@id", id);

                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<String> GetEmployeeId()
        {
            string emp_id = "res_";
            string new_id = "";
            int index = 0;

            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                using (var command = new SqlCommand("select res_id from resident_tb", connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {

                            //res_0000000001
                            string myString = reader[0].ToString() ?? string.Empty;
                            for (int i = 4; i < myString.Length; i++) //i = 4 to not include <res_>
                            {
                                int number = myString[i] - '0'; //logic to return if number is zero
                                if (number > 0)
                                {
                                    index = i;
                                    break;
                                }
                            }

                            var val_str = Convert.ToInt32(myString.Substring(index));
                            val_str += 1;
                            var new_val_str = emp_id + val_str.ToString("000000000"); //the number of zero after <res_>

                            new_id = new_val_str;
                        }
                        else
                        {
                            new_id = "res_0000000001";
                        }
                    }
                }
            }

            return new_id;
        }

        private string GetTimeDate()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}
