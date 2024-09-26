using KVHAI.CustomClass;
using KVHAI.Models;
using System.Data.SqlClient;

namespace KVHAI.Repository
{
    public class AnnouncementImageRepository
    {
        private readonly DBConnect _dbConnect;
        private readonly InputSanitize _sanitize;

        public AnnouncementImageRepository(DBConnect dBConnect, InputSanitize sanitize)
        {
            _dbConnect = dBConnect;
            _sanitize = sanitize;
        }

        public async Task<int> SaveAnnouncementImg(int announcementID, List<IFormFile> images, SqlConnection connection, SqlTransaction transaction)
        {
            try
            {
                int result = 0;

                if (images != null && images.Count > 0)
                {
                    foreach (var image in images)
                    {
                        if (image.Length > 0)
                        {
                            // Create unique file name
                            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);

                            // Define path to save the image
                            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/announcement_img", fileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await image.CopyToAsync(stream);
                            }

                            var imageUrl = "/announcement_img/" + fileName;

                            // Insert the image path into the database
                            using (var command = new SqlCommand(@"
                        INSERT INTO announcement_img_tb (announcement_id, image_url) 
                        VALUES (@announcement_id, @image_url)", connection, transaction))
                            {
                                command.Parameters.AddWithValue("@announcement_id", announcementID);
                                command.Parameters.AddWithValue("@image_url", imageUrl);

                                result += await command.ExecuteNonQueryAsync(); // Add to result count
                            }
                        }
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                // Handle specific exceptions if necessary
                return 0;
            }
        }
    }
}
