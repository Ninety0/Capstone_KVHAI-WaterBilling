using KVHAI.CustomClass;
using KVHAI.Models;
using System.Data;
using System.Data.SqlClient;

namespace KVHAI.Repository
{
    public class AnnouncementImageRepository
    {
        private readonly DBConnect _dbConnect;
        private readonly InputSanitize _sanitize;
        private readonly ListRepository _listRepository;

        public AnnouncementImageRepository(DBConnect dBConnect, InputSanitize sanitize, ListRepository listRepository )
        {
            _dbConnect = dBConnect;
            _sanitize = sanitize;
            _listRepository = listRepository;
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

       public async Task<int> DeleteImage(int announcement_id, SqlConnection connection, SqlTransaction transaction)
        {
            try
            {
                using (var command = new SqlCommand(@"
                    DELETE FROM announcement_img_tb 
                    WHERE announcement_id = @id", connection, transaction))
                {
                    command.Parameters.Add("@id", SqlDbType.Int).Value = announcement_id;
                    return await command.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                // Log exception here for debugging
                Console.WriteLine(ex.Message);
                return 0;
            }
        }

        public async Task<int> UpdateAnnouncementImg(int announcementID, List<IFormFile> images, SqlConnection connection, SqlTransaction transaction)
        {
            try
            {
                int result = 0;

                var imageAnnouncement = await _listRepository.ImageAnnouncementList();

                var isExist = imageAnnouncement.Where(a=>a.Announcement_ID == announcementID).Count();

                if(isExist > 0){
                    // Delete existing images for the announcement
                    int delResult = await DeleteImage(announcementID, connection, transaction);

                    if (delResult < 1)
                    {
                        return 0;
                    }
                }

                // Ensure the directory exists
                var directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/announcement_img");
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                // Save and insert new images
                List<string> savedFilePaths = new List<string>();

                if (images.Any())
                {
                    foreach (var image in images)
                    {
                        if (image.Length > 0)
                        {
                            // Create unique file name
                            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                            var filePath = Path.Combine(directoryPath, fileName);

                            try
                            {
                                using (var stream = new FileStream(filePath, FileMode.Create))
                                {
                                    await image.CopyToAsync(stream);
                                }

                                savedFilePaths.Add(filePath); // Track saved files in case of rollback

                                var imageUrl = "/announcement_img/" + fileName;

                                // Insert the image path into the database
                                using (var command = new SqlCommand(@"
                                    INSERT INTO announcement_img_tb (announcement_id, image_url) 
                                    VALUES (@announcement_id, @image_url)", connection, transaction))
                                {
                                    command.Parameters.Add("@announcement_id", SqlDbType.Int).Value = announcementID;
                                    command.Parameters.Add("@image_url", SqlDbType.NVarChar, 255).Value = imageUrl;

                                    result += await command.ExecuteNonQueryAsync(); // Add to result count
                                }
                            }
                            catch
                            {
                                // Cleanup saved files in case of any error
                                foreach (var savedFilePath in savedFilePaths)
                                {
                                    if (File.Exists(savedFilePath))
                                    {
                                        File.Delete(savedFilePath);
                                    }
                                }
                                throw; // Re-throw to handle higher up if necessary
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                // Log exception for debugging
                Console.WriteLine(ex.Message);
                return 0;
            }
        }
    }
}
