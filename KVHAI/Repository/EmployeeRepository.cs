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
        private readonly InputSanitize _sanitize;
        private readonly ListRepository _listRepository;

        public EmployeeRepository(DBConnect dbConnect, Hashing hash, InputSanitize sanitize, ListRepository listRepository)
        {
            _dbConnect = dbConnect;
            _hash = hash;
            _sanitize = sanitize;
            _listRepository = listRepository;
        }

        public async Task<List<Employee>> EmployeeList()
        {
            try
            {
                var empList = new List<Employee>();
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand("select * from employee_tb", connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var emp = new Employee
                                {
                                    Emp_ID = reader.GetInt32(0).ToString(),
                                    Lname = reader.GetString(1),
                                    Fname = reader.GetString(2),
                                    Mname = reader.GetString(3),
                                    Phone = reader.GetString(4),
                                    Email = reader.GetString(5),
                                    Username = reader.GetString(6),
                                    Password = reader.GetString(7),
                                    Role = reader.GetString(8),
                                    Created_At = reader.GetDateTime(9).ToString("yyyy-MM-dd"),
                                };

                                empList.Add(emp);
                            }
                        }
                    }
                }

                return empList;
            }
            catch (Exception)
            {
                return null;
            }
        }


        public async Task<bool> ValidatePassword(string username, string password)
        {
            try
            {
                // Sanitize the input username
                string _user = await _sanitize.HTMLSanitizerAsync(username);
                string _pass = await _sanitize.HTMLSanitizerAsync(password);

                // Fetch the employee list
                var empList = await EmployeeList();

                // Get the password from the database associated with the username
                var passwordFromDB = empList.Where(u => u.Username == _user).Select(p => p.Password).FirstOrDefault();

                // If no password was found, return false (username does not exist or password is null)
                if (string.IsNullOrEmpty(passwordFromDB))
                {
                    return false; // Username not found or password missing
                }

                // Verify the password using the hashing service
                if (_hash.VerifyPassword(passwordFromDB, _pass))
                {
                    return true; // Password is correct
                }
                else
                {
                    return false; // Password is incorrect
                }
            }
            catch (Exception ex)
            {
                // Optionally log the exception
                // Log(ex);  // You can log the exception here if needed
                return false; // In case of any exception, return false
            }
        }

        public async Task<AuthClaims> GetEmployeeByUserandPass(string user, string pass)
        {
            try
            {
                // Sanitize the input username and password
                string _user = await _sanitize.HTMLSanitizerAsync(user);
                string _pass = await _sanitize.HTMLSanitizerAsync(pass);

                // Fetch the employee list
                var empList = await EmployeeList();

                // Get the password from the database associated with the username
                var passwordFromDB = empList.Where(u => u.Username == _user).Select(p => p.Password).FirstOrDefault();

                // If no password was found, return null (username does not exist or password is null)
                if (string.IsNullOrEmpty(passwordFromDB))
                {
                    return null; // Username not found or password missing
                }

                // Verify the password using the hashing service
                if (_hash.VerifyPassword(passwordFromDB, _pass))
                {
                    // Return employee details for the matched username and password
                    var claim = empList
                        .Where(u => u.Username == _user) // Assuming username is unique
                        .Select(e => new AuthClaims
                        {
                            ID = e.Emp_ID, // Fill in employee details
                            Role = e.Role,
                        })
                        .FirstOrDefault(); // Select the first matching employee

                    return claim;
                }
                else
                {
                    return null; // Password is incorrect
                }
            }
            catch (Exception ex)
            {
                // Handle the exception appropriately (log it, etc.)
                return null;
            }
        }




        public async Task<int> ValidateAccount(string username, string password)
        {
            try
            {
                // Sanitize the input username
                string _user = await _sanitize.HTMLSanitizerAsync(username);

                // Fetch the employee list
                var empList = await EmployeeList();

                // Get the password from the database associated with the username
                var passwordFromDB = empList.Where(u => u.Username == _user).Select(p => p.Password).FirstOrDefault();

                // If no password was found, return 0 (either username does not exist or password is null)
                if (string.IsNullOrEmpty(passwordFromDB))
                {
                    return 0; // Username not found or password missing
                }

                // Verify the password using the hashing service
                if (_hash.VerifyPassword(passwordFromDB, password))
                {
                    // Fetch the employee ID of the matched user
                    var emp_id = empList.Where(u => u.Username == _user).Select(u => u.Emp_ID).FirstOrDefault();
                    return Convert.ToInt32(emp_id);
                }
                else
                {
                    return 0; // Password is incorrect
                }
            }
            catch (Exception ex)
            {
                // Optionally, log the exception here
                throw new Exception("Error occurred during account validation", ex);
            }
        }


        //CREATE
        public async Task<int> CreateEmployee(Employee employee)
        {
            var pass = _hash.HashPassword(await _sanitize.HTMLSanitizerAsync(employee.Password));
            var dt = GetTimeDate();
            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand("INSERT INTO employee_tb (lname, fname, mname, phone, email, username, password, role, created_at) VALUES(@lname, @fname, @mname, @phone, @email, @user, @pass, @role, @create)", connection))
                    {
                        command.Parameters.AddWithValue("@lname", await _sanitize.HTMLSanitizerAsync(employee.Lname));
                        command.Parameters.AddWithValue("@fname", await _sanitize.HTMLSanitizerAsync(employee.Fname));
                        command.Parameters.AddWithValue("@mname", await _sanitize.HTMLSanitizerAsync(employee.Mname));
                        command.Parameters.AddWithValue("@phone", await _sanitize.HTMLSanitizerAsync(employee.Phone));
                        command.Parameters.AddWithValue("@email", await _sanitize.HTMLSanitizerAsync(employee.Email));
                        command.Parameters.AddWithValue("@user", await _sanitize.HTMLSanitizerAsync(employee.Username));
                        command.Parameters.AddWithValue("@pass", pass);
                        command.Parameters.AddWithValue("@role", await _sanitize.HTMLSanitizerAsync(employee.Role));
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
            var hasValue = !string.IsNullOrEmpty(await _sanitize.HTMLSanitizerAsync(employee.Password));

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
                        command.Parameters.AddWithValue("@id", await _sanitize.HTMLSanitizerAsync(employee.Emp_ID));
                        command.Parameters.AddWithValue("@lname", await _sanitize.HTMLSanitizerAsync(employee.Lname));
                        command.Parameters.AddWithValue("@fname", await _sanitize.HTMLSanitizerAsync(employee.Fname));
                        command.Parameters.AddWithValue("@mname", await _sanitize.HTMLSanitizerAsync(employee.Mname));
                        command.Parameters.AddWithValue("@phone", await _sanitize.HTMLSanitizerAsync(employee.Phone));
                        command.Parameters.AddWithValue("@email", await _sanitize.HTMLSanitizerAsync(employee.Email));
                        command.Parameters.AddWithValue("@user", await _sanitize.HTMLSanitizerAsync(employee.Username));

                        if (hasValue)
                        {
                            var hashPass = _hash.HashPassword(await _sanitize.HTMLSanitizerAsync(employee.Password));
                            command.Parameters.AddWithValue("@pass", hashPass);
                        }

                        command.Parameters.AddWithValue("@role", await _sanitize.HTMLSanitizerAsync(employee.Role));

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


        //WITHOUT SEARCH
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
                            employee.Created_At = reader.GetDateTime(9).ToString("yyyy-MM-dd");
                            employees.Add(employee);

                        }
                    }
                }
            }

            return employees;
        }

        //WITH SEARCH
        public async Task<List<Employee>> GetAllEmployeesAsync(string search, string category, int offset, int limit)//string category, 
        {
            var employees = new List<Employee>();
            string query = $"SELECT * FROM employee_tb WHERE {category} LIKE @search ORDER BY emp_id OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY";

            //if (category == "surname")
            //{
            //    query = "SELECT * FROM employee_tb WHERE lname LIKE @search ORDER BY emp_id OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY";
            //}
            //else
            //{
            //    query = "SELECT * FROM employee_tb WHERE role LIKE @search ORDER BY emp_id OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY";
            //}

            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@search", "%" + search + "%");
                    //command.Parameters.AddWithValue("@category", category.ToLower());
                    command.Parameters.AddWithValue("@offset", offset);
                    command.Parameters.AddWithValue("@limit", limit);
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
                            employee.Created_At = reader.GetDateTime(9).ToString("yyyy-MM-dd");
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

        //WITH CONDITION
        public async Task<int> CountEmployeeData(string category, string? search = "")
        {
            int result = 0;

            string query = $"SELECT COUNT(*) FROM employee_tb WHERE {category} LIKE @search";

            //if (category == "surname")
            //{
            //    query = "SELECT COUNT(*) FROM employee_tb WHERE lname LIKE @search";
            //}
            //else
            //{
            //    query = "SELECT COUNT(*) FROM employee_tb WHERE role LIKE @search";
            //}

            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@search", "%" + search + "%");

                    result = (int)command.ExecuteScalar();

                    return result;
                }
            }
        }
    }
}
