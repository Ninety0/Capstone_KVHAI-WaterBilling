﻿using KVHAI.CustomClass;
using KVHAI.Models;
using System.Data.SqlClient;

namespace KVHAI.Repository
{
    public class StreetRepository
    {
        private readonly DBConnect _dbConnect;
        private readonly InputSanitize _sanitizer;
        private readonly ListRepository _listRepository;


        public StreetRepository(DBConnect dBConnect, InputSanitize sanitizer, ListRepository listRepository)
        {
            _dbConnect = dBConnect;
            _sanitizer = sanitizer;
            _listRepository = listRepository;
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

        //RETURN SINGLE StreetID
        public async Task<int> GetSingleStreetID(string name)
        {
            try
            {
                var st_list = await _listRepository.StreetList();

                var stID = st_list.Where(n => n.Street_Name == name).Select(i => i.Street_ID).FirstOrDefault() ?? "0";

                return Convert.ToInt32(stID);
            }
            catch (Exception)
            {

                return 0;
            }
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
        public async Task<List<Address>> GetStreetID(List<Address> streetList)
        {
            try
            {
                // Retrieve list of all streets once
                var st_list = await _listRepository.StreetList();
                var st_IDS = new List<Address>();
                if (st_list.Count > 0)
                {

                    foreach (var st in streetList)
                    {
                        // Find matching street ID in `st_list`
                        var st_id = st_list
                            .Where(s => s.Street_Name == st.Street_Name)
                            .Select(i => i.Street_ID)
                            .FirstOrDefault();

                        // Only add if an ID was found
                        if (!string.IsNullOrEmpty(st_id))
                        {
                            st_IDS.Add(new Address
                            {
                                Street_ID = Convert.ToInt32(st_id)
                            });
                        }
                    }
                }


                return st_IDS;
            }
            catch (Exception ex)
            {
                return null;  // Consider logging the exception for debugging purposes
            }
        }


        //RETURN ID OF STREET
        public async Task<int> GetStreetID(ResidentAddress street)
        {
            try
            {
                var result = 0;
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand("SELECT * FROM street_tb WHERE st_name like @name", connection))
                    {
                        command.Parameters.AddWithValue("@name", "%" + street.Street + "%");
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                result = Convert.ToInt32(reader["st_id"].ToString());

                            }
                        }
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                return 0;
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

        public async Task<List<ResidentAddress>> GetStreetNameList(List<Address> addressess)
        {
            try
            {
                var residentAddress = new List<ResidentAddress>();
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    foreach (var item in addressess)
                    {
                        using (var command = new SqlCommand(@"
                            select addr_id,s.st_id, block,lot, st_name  from address_tb a
                            JOIN street_tb s ON a.st_id = s.st_id
                            WHERE addr_id = @id", connection))
                        {
                            command.Parameters.AddWithValue("@id", item.Address_ID);

                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    var ra = new ResidentAddress
                                    {
                                        Address_ID = reader.GetInt32(0),
                                        Street_ID = reader.GetInt32(1),
                                        Block = reader.GetString(2),
                                        Lot = reader.GetString(3),
                                        Street = reader.GetString(4),
                                    };

                                    residentAddress.Add(ra);
                                }
                            }

                        }
                    }

                }

                return residentAddress;
            }
            catch (Exception)
            {
                return null;
            }

        }
    }
}
