using KVHAI.CustomClass;
using KVHAI.Models;
using System.Data.SqlClient;

namespace KVHAI.Repository
{
    public class StreetRepository
    {
        private readonly DBConnect _dbConnect;
        private readonly InputSanitize _sanitizer;


        public StreetRepository(DBConnect dBConnect, InputSanitize sanitizer)
        {
            _dbConnect = dBConnect;
            _sanitizer = sanitizer;
        }

        //CREATE
        #region CREATE
        public async Task<int> CreateStreets(Streets street)
        {
            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand("INSERT INTO street_tb (st_name) VALUES(@st)", connection))
                    {
                        command.Parameters.AddWithValue("@st", await _sanitizer.HTMLSanitizerAsync(street.Street_Name));

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
        #endregion

        //READ
        #region READ
        public async Task<List<Streets>> GetAllStreets()
        {
            var streets = new List<Streets>();

            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                using (var command = new SqlCommand("SELECT * FROM street_tb", connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {

                        while (await reader.ReadAsync())
                        {
                            var street = new Streets();
                            street.Street_ID = reader[0]?.ToString() ?? string.Empty;
                            street.Street_Name = reader[1]?.ToString() ?? string.Empty;
                            streets.Add(street);
                        }
                    }
                }
            }

            return streets;
        }

        //RETURN SINGLE EMPLOYEE
        public async Task<List<Streets>> GetSingleStreet(string id)
        {
            var streets = new List<Streets>();

            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                using (var command = new SqlCommand("SELECT * FROM street_tb where st_id = @id", connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    using (var reader = await command.ExecuteReaderAsync())
                    {

                        while (await reader.ReadAsync())
                        {
                            var st = new Streets();
                            st.Street_ID = reader[0]?.ToString() ?? string.Empty;
                            st.Street_Name = reader[1]?.ToString() ?? string.Empty;
                            streets.Add(st);

                        }
                    }
                }
            }

            return streets;
        }

        //WITHOUT SEARCH
        public async Task<List<Streets>> GetAllStreets(int offset, int limit)
        {
            var streets = new List<Streets>();

            var query = "SELECT * FROM street_tb ORDER BY st_id OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY";

            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@offset", offset);
                    command.Parameters.AddWithValue("@limit", limit);
                    using (var reader = await command.ExecuteReaderAsync())
                    {

                        while (await reader.ReadAsync())
                        {
                            var street = new Streets();
                            street.Street_ID = reader[0]?.ToString() ?? string.Empty;
                            street.Street_Name = reader[1]?.ToString() ?? string.Empty;
                            streets.Add(street);
                        }
                    }
                }
            }

            return streets;
        }

        //WITH SEARCH
        public async Task<List<Streets>> GetAllStreets(string search, int offset, int limit)
        {
            var streets = new List<Streets>();

            var query = "SELECT * FROM street_tb WHERE st_name LIKE @search ORDER BY st_id OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY";

            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@search", "%" + await _sanitizer.HTMLSanitizerAsync(search) + "%");
                    command.Parameters.AddWithValue("@offset", offset);
                    command.Parameters.AddWithValue("@limit", limit);
                    using (var reader = await command.ExecuteReaderAsync())
                    {

                        while (await reader.ReadAsync())
                        {
                            var street = new Streets();
                            street.Street_ID = reader[0]?.ToString() ?? string.Empty;
                            street.Street_Name = reader[1]?.ToString() ?? string.Empty;
                            streets.Add(street);
                        }
                    }
                }
            }

            return streets;
        }
        #endregion

        //UPDATE
        #region UPATE
        public async Task<int> UpdateStreets(Streets street)
        {
            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand("UPDATE street_tb set st_name = @name WHERE st_id =@id", connection))
                    {
                        command.Parameters.AddWithValue("@id", street.Street_ID);
                        command.Parameters.AddWithValue("@name", await _sanitizer.HTMLSanitizerAsync(street.Street_Name));

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
        #endregion

        //DELETE
        #region DELETE
        public async Task<int> DeleteStreets(int id)
        {
            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand("DELETE FROM street_tb WHERE st_id=@id", connection))
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
        #endregion

        //COUNT WITH SEARCH
        public async Task<int> CountStreetsData()
        {

            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                using (var command = new SqlCommand("SELECT COUNT(*) FROM street_tb", connection))
                {
                    var result = await command.ExecuteScalarAsync();

                    return Convert.ToInt32(result);
                }
            }

        }
        //COUNT WITH SEARCH
        public async Task<int> CountStreetsData(string search = "")
        {

            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                using (var command = new SqlCommand("SELECT COUNT(*) FROM street_tb WHERE st_name LIKE @search", connection))
                {
                    command.Parameters.AddWithValue("@search", "%" + search + "%");
                    var result = await command.ExecuteScalarAsync();

                    return Convert.ToInt32(result);
                }
            }

        }

        //CHECK EXISTENCE
        public async Task<bool> StreetExists(Streets street)
        {
            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand("SELECT COUNT(*) FROM street_tb WHERE st_name = @name", connection))
                    {
                        command.Parameters.AddWithValue("@name", street.Street_Name);
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

        //RETURN ID OF STREET
        public async Task<List<Address>> GetStreetID(List<Address> street)
        {
            try
            {
                var result = 0;
                var st_IDS = new List<Address>();
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    foreach (var st in street)
                    {
                        using (var command = new SqlCommand("SELECT * FROM street_tb WHERE st_name like @name", connection))
                        {
                            command.Parameters.AddWithValue("@name", "%" + st.Street_Name + "%");
                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    var address = new Address
                                    {
                                        Street_ID = Convert.ToInt32(reader["st_id"].ToString())
                                    };

                                    st_IDS.Add(address);
                                }
                            }
                        }
                    }
                }
                return st_IDS;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        //RETURN STREET NAME
        public async Task<string> GetStreetName(string id)
        {

            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                using (var command = new SqlCommand("SELECT st_name FROM street_tb where st_id = @id", connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    var result = await command.ExecuteScalarAsync();

                    return result?.ToString() ?? string.Empty;
                }
            }
        }
    }
}
