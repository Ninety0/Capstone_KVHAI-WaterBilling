using KVHAI.Models;
using System.Data.SqlClient;

namespace KVHAI.Repository
{
    public class ImageUploadRepository
    {
        private readonly DBConnect _dbConnect;

        public ImageUploadRepository(DBConnect dBConnect)
        {
            _dbConnect = dBConnect;
        }



        public async Task<int> ImageUpload(List<IFormFile> files, string webrootPath, List<Address> addresses, SqlTransaction transaction, SqlConnection connection, string residentID)
        {
            if (files == null || files.Count == 0 || addresses == null || addresses.Count == 0)
            {
                return 0; // No files or addresses to process
            }

            try
            {
                var directoryPath = Path.Combine(webrootPath, $"proof_img", $"user{residentID}");

                // Ensure the directory is created only once
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                int insertedRows = 0;

                for (int i = 0; i < files.Count; i++)
                {
                    var fileName = Path.GetFileName(files[i].FileName);
                    var relativeFilePath = Path.Combine($"proof_img", $"user{residentID}", fileName); // Store relative path
                    var filePath = Path.Combine(directoryPath, fileName);

                    // Save the file to disk
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await files[i].CopyToAsync(fileStream);
                    }

                    // Insert the file information into the database
                    using (var command = new SqlCommand("INSERT INTO proof_img_tb (addr_id, path_file) VALUES(@id, @path)", connection, transaction))
                    {
                        command.Parameters.AddWithValue("@id", addresses[i].Address_ID); // Assuming Address class has AddrID property
                        command.Parameters.AddWithValue("@path", relativeFilePath);  // Use relative path

                        // Execute the query
                        await command.ExecuteNonQueryAsync();
                        insertedRows++;
                    }
                }

                return insertedRows; // Return the number of successfully inserted rows
            }
            catch (Exception ex)
            {
                // Log the exception and roll back the transaction
                var message = ex.Message;
                transaction.Rollback();
                throw; // Re-throw the exception after rollback
            }
        }



        public async Task<int> GetImagePathID()
        {
            int id = 1;

            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                using (var command = new SqlCommand("select img_id from proof_img_tb ORDER BY img_id DESC", connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            id = Convert.ToInt32(reader[0].ToString());
                        }
                    }
                }
            }
            return id;
        }

    }
}
/*
 
 public async Task<int> ImageUpload(IFormFile file, string webrootPath, int res_id, SqlTransaction transaction, SqlConnection connection)
        {
            if (file != null && file.Length > 0)
            {
                var fileName = Path.GetFileName(file.FileName);
                var directoryPath = Path.Combine(webrootPath, "proof_img");
                var filePath = Path.Combine(directoryPath, fileName);

                Directory.CreateDirectory(directoryPath);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                try
                {
                    using (var command = new SqlCommand("INSERT INTO proof_img_tb (res_id,path_file) VALUES(@id,@path)", connection, transaction))
                    {
                        command.Parameters.AddWithValue("@id", res_id);
                        command.Parameters.AddWithValue("@path", filePath);


                        await command.ExecuteNonQueryAsync();

                        return 1;
                    }
                }
                catch (Exception ex)
                {
                    var message = ex.Message;
                    throw;
                }
            }

            return 0;
        }
 
 */