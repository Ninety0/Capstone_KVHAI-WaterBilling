using KVHAI.CustomClass;
using KVHAI.Models;
using System.Data.SqlClient;
using System.Text;

namespace KVHAI.Repository
{
    public class EmployeeRepository
    {
        private readonly DBConnect _dbConnect;
        private readonly Hashing _hash;

        public EmployeeRepository(DBConnect dbConnect, Hashing hash)
        {
            _dbConnect = dbConnect;
            _hash = hash;
        }

        //CREATE
        public async Task<int> CreateEmployee(Employee employee)
        {
            var pass = _hash.HashPassword(employee.Password);
            var dt = GetTimeDate();
            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand("INSERT INTO employee_tb (lname, fname, mname, phone, email, username, password, role, created_at) VALUES(@lname, @fname, @mname, @phone, @email, @user, @pass, @role, @create)", connection))
                    {
                        command.Parameters.AddWithValue("@lname", employee.Lname);
                        command.Parameters.AddWithValue("@fname", employee.Fname);
                        command.Parameters.AddWithValue("@mname", employee.Mname);
                        command.Parameters.AddWithValue("@phone", employee.Phone);
                        command.Parameters.AddWithValue("@email", employee.Email);
                        command.Parameters.AddWithValue("@user", employee.Username);
                        command.Parameters.AddWithValue("@pass", pass);
                        command.Parameters.AddWithValue("@role", employee.Role);
                        command.Parameters.AddWithValue("@create", dt);

                        await command.ExecuteNonQueryAsync();

                        return 1;
                    }
                }
            }
            catch (Exception)
            {
                return 0;
            }

        }

        //READ
        public async Task<List<Employee>> GetAllEmployeesAsync()
        {
            var employees = new List<Employee>();

            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                using (var command = new SqlCommand("SELECT * FROM employee_tb", connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {

                        while (await reader.ReadAsync())
                        {
                            var employee = new Employee();
                            employee.Emp_ID = reader[0]?.ToString() ?? string.Empty;
                            employee.Lname = reader[1]?.ToString() ?? string.Empty;
                            employee.Fname = reader[2]?.ToString() ?? string.Empty;
                            employee.Mname = reader[3]?.ToString() ?? string.Empty;
                            employee.Phone = reader[4]?.ToString() ?? string.Empty;
                            employee.Email = reader[5]?.ToString() ?? string.Empty;
                            employee.Username = reader[6]?.ToString() ?? string.Empty;
                            employee.Password = reader[7]?.ToString() ?? string.Empty;
                            employee.Role = reader[8]?.ToString() ?? string.Empty;
                            employee.Created_At = reader[9]?.ToString() ?? string.Empty;
                            employees.Add(employee);

                        }
                    }
                }
            }

            return employees;
        }

        //UPDATE
        public async Task<int> UpdateEmployee(Employee employee)
        {
            var hasValue = !string.IsNullOrEmpty(employee.Password);

            var query = new StringBuilder("UPDATE employee_tb SET ");
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
            query.Append("role = @role ");
            query.Append("WHERE emp_id = @id");


            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(query.ToString(), connection))
                    {
                        command.Parameters.AddWithValue("@id", employee.Emp_ID);
                        command.Parameters.AddWithValue("@lname", employee.Lname);
                        command.Parameters.AddWithValue("@fname", employee.Fname);
                        command.Parameters.AddWithValue("@mname", employee.Mname);
                        command.Parameters.AddWithValue("@phone", employee.Phone);
                        command.Parameters.AddWithValue("@email", employee.Email);
                        command.Parameters.AddWithValue("@user", employee.Username);

                        if (hasValue)
                        {
                            var hashPass = _hash.HashPassword(employee.Password);
                            command.Parameters.AddWithValue("@pass", hashPass);
                        }

                        command.Parameters.AddWithValue("@role", employee.Role);

                        int result = await command.ExecuteNonQueryAsync();
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return 0;
            }
        }

        //DELETE
        public async Task<int> DeleteEmployee(int id)
        {
            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand("DELETE FROM employee_tb WHERE emp_id=@id", connection))
                    {
                        command.Parameters.AddWithValue("@id", id);

                        int result = await command.ExecuteNonQueryAsync();
                        return result;
                    }
                }
            }
            catch (Exception)
            {
                return 0;
            }
        }

        //RETURN SINGLE EMPLOYEE
        public async Task<List<Employee>> GetSingleEmployee(string id)
        {
            var employees = new List<Employee>();

            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                using (var command = new SqlCommand("SELECT * FROM employee_tb where emp_id = @id", connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    using (var reader = await command.ExecuteReaderAsync())
                    {

                        while (await reader.ReadAsync())
                        {
                            var employee = new Employee();
                            employee.Emp_ID = reader[0]?.ToString() ?? string.Empty;
                            employee.Lname = reader[1]?.ToString() ?? string.Empty;
                            employee.Fname = reader[2]?.ToString() ?? string.Empty;
                            employee.Mname = reader[3]?.ToString() ?? string.Empty;
                            employee.Phone = reader[4]?.ToString() ?? string.Empty;
                            employee.Email = reader[5]?.ToString() ?? string.Empty;
                            employee.Username = reader[6]?.ToString() ?? string.Empty;
                            employee.Password = reader[7]?.ToString() ?? string.Empty;
                            employee.Role = reader[8]?.ToString() ?? string.Empty;
                            employee.Created_At = reader[9]?.ToString() ?? string.Empty;
                            employees.Add(employee);

                        }
                    }
                }
            }

            return employees;
        }

        //VALIDATE EXISTING USER
        public async Task<bool> UserExists(Employee employee)
        {
            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand("SELECT COUNT(*) FROM employee_tb WHERE email = @email OR username= @user", connection))
                    {
                        command.Parameters.AddWithValue("@email", employee.Email);
                        command.Parameters.AddWithValue("@user", employee.Username);
                        int count = (int)command.ExecuteScalar();
                        return count > 0;
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
        }


        public async Task<int> GetEmployeeId()
        {
            int new_id = 1;

            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                using (var command = new SqlCommand("select emp_id from employee_tb", connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var id = Convert.ToInt32(reader["emp_id"].ToString());

                            new_id = id + 1;
                        }

                        else
                        {
                            new_id = 1;
                        }
                    }
                }
            }

            return new_id;
        }

        public async Task<bool> ValidateCredentials(string user, string pass)
        {

            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                using (var command = new SqlCommand("SELECT * FROM employee_tb WHERE username=@user AND password=@pass", connection))
                {
                    command.Parameters.AddWithValue("@user", user);
                    command.Parameters.AddWithValue("@pass", pass);

                    using (var reader = await command.ExecuteReaderAsync())
                    {

                        if (await reader.ReadAsync())
                        {
                            var ps = reader[0].ToString() ?? string.Empty;
                            return _hash.VerifyPassword(ps, pass);
                        }
                    }
                }
            }

            return false;
        }

        private string GetTimeDate()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }


        //SAMPLE DATAS
        public async Task<List<Employee>> GetAllEmployeesAsync(int offset, int limit)
        {
            var employees = new List<Employee>();

            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                using (var command = new SqlCommand($"SELECT * FROM employee_tb ORDER BY emp_id OFFSET {offset} ROWS FETCH NEXT {limit} ROWS ONLY", connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {

                        while (await reader.ReadAsync())
                        {
                            var employee = new Employee();
                            employee.Emp_ID = reader[0]?.ToString() ?? string.Empty;
                            employee.Lname = reader[1]?.ToString() ?? string.Empty;
                            employee.Fname = reader[2]?.ToString() ?? string.Empty;
                            employee.Mname = reader[3]?.ToString() ?? string.Empty;
                            employee.Phone = reader[4]?.ToString() ?? string.Empty;
                            employee.Email = reader[5]?.ToString() ?? string.Empty;
                            employee.Username = reader[6]?.ToString() ?? string.Empty;
                            employee.Password = reader[7]?.ToString() ?? string.Empty;
                            employee.Role = reader[8]?.ToString() ?? string.Empty;
                            employee.Created_At = reader[9]?.ToString() ?? string.Empty;
                            employees.Add(employee);

                        }
                    }
                }
            }

            return employees;
        }

        public async Task<int> CountEmployeeData()
        {
            int result = 0;
            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                using (var command = new SqlCommand($"SELECT COUNT(*) FROM employee_tb", connection))
                {
                    result = (int)command.ExecuteScalar();

                    return result;
                }
            }
        }
    }
}
