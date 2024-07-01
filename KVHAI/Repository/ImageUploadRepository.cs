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
