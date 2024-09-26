using KVHAI.CustomClass;
using KVHAI.Models;
using System.Data.SqlClient;

namespace KVHAI.Repository
{
    public class AnnouncementRepository
    {
        private readonly DBConnect _dbConnect;
        private readonly InputSanitize _sanitize;
        private readonly AnnouncementImageRepository _announcementImageRepository;

        public AnnouncementRepository(DBConnect dBConnect, InputSanitize inputSanitize, AnnouncementImageRepository announcementImageRepository)
        {
            _dbConnect = dBConnect;
            _sanitize = inputSanitize;
            _announcementImageRepository = announcementImageRepository;
        }

        public async Task<int> Save(Announcement announce, List<IFormFile> images)
        {
            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Insert the announcement first
                        var id = await SubmitAnnouncement(announce, connection, transaction);

                        if (id < 1)
                        {
                            return 0;
                        }

                        // Save the images (if any)
                        if (images != null && images.Count > 0)
                        {
                            var saveImg = await _announcementImageRepository.SaveAnnouncementImg(id, images, connection, transaction);

                            if (saveImg < 1) // Assuming non-zero result indicates success
                            {
                                return 0;
                            }
                        }

                        // Commit the transaction if everything is successful
                        transaction.Commit();
                        return 1;
                    }
                    catch (Exception ex)
                    {
                        // Rollback the transaction on error
                        transaction.Rollback();
                        return 0;
                    }
                }
            }
        }

        public async Task<int> SubmitAnnouncement(Announcement announcement, SqlConnection connection, SqlTransaction transaction)
        {
            try
            {
                var date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                using (var command = new SqlCommand(@"
                    INSERT INTO announcement_tb(emp_id, title, contents, date_created, date_expire) 
                    OUTPUT INSERTED.announcement_id 
                    VALUES(@emp_id, @title, @content, @created, @expire)", connection, transaction))
                {
                    command.Parameters.AddWithValue("@emp_id", "1");  // Use actual employee ID
                    command.Parameters.AddWithValue("@title", await _sanitize.HTMLSanitizerAsync(announcement.Title));
                    command.Parameters.AddWithValue("@content", await _sanitize.HTMLSanitizerAsync(announcement.Contents));
                    command.Parameters.AddWithValue("@created", date);
                    command.Parameters.AddWithValue("@expire", await _sanitize.HTMLSanitizerAsync(announcement.Date_Expire));

                    return (int)await command.ExecuteScalarAsync();
                }
            }
            catch (Exception)
            {
                return 0; // 0 indicates failure
            }
        }

        public async Task<int> SubmitAnnouncementNoImage(Announcement announcement)
        {
            try
            {
                var date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(@"
                    INSERT INTO announcement_tb(emp_id, title, contents, date_created, date_expire) 
                    OUTPUT INSERTED.announcement_id 
                    VALUES(@emp_id, @title, @content, @created, @expire)", connection))
                    {
                        command.Parameters.AddWithValue("@emp_id", "1");  // Use actual employee ID
                        command.Parameters.AddWithValue("@title", await _sanitize.HTMLSanitizerAsync(announcement.Title));
                        command.Parameters.AddWithValue("@content", await _sanitize.HTMLSanitizerAsync(announcement.Contents));
                        command.Parameters.AddWithValue("@created", date);
                        command.Parameters.AddWithValue("@expire", await _sanitize.HTMLSanitizerAsync(announcement.Date_Expire));

                        return (int)await command.ExecuteScalarAsync();
                    }
                }
            }
            catch (Exception)
            {
                return 0; // 0 indicates failure
            }
        }


        public async Task<List<Announcement>> ShowAnnouncement()
        {
            try
            {
                var announcementList = new List<Announcement>();
                var date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(@"
                        SELECT a.announcement_id, a.emp_id, a.title, a.contents, a.date_created, a.date_expire, ai.image_url 
                        FROM announcement_tb a
                        LEFT JOIN announcement_img_tb ai ON a.announcement_id = ai.announcement_id
                        WHERE a.date_expire >= @date
                        ORDER BY date_created DESC", connection))
                    {
                        command.Parameters.AddWithValue("@date", date);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            var announcementsDict = new Dictionary<int, Announcement>();

                            while (await reader.ReadAsync())
                            {
                                int announcementId = reader.GetInt32(0);

                                // Check if this announcement has already been added to the dictionary
                                if (!announcementsDict.ContainsKey(announcementId))
                                {
                                    // Create a new Announcement object and add it to the dictionary
                                    var announcement = new Announcement
                                    {
                                        Announcement_ID = announcementId,
                                        Employee_ID = reader.GetInt32(1),
                                        Title = reader.GetString(2),
                                        Contents = reader.GetString(3),
                                        Date_Created = reader.GetDateTime(4).ToString("yyyy-MM-dd HH:mm:ss"),
                                        Date_Expire = reader.GetDateTime(5).ToString("yyyy-MM-dd HH:mm:ss"),
                                        Images = new List<string>() // Initialize the list to hold the images
                                    };

                                    announcementsDict[announcementId] = announcement;
                                }

                                // Check if there is an associated image for this announcement and add it to the list
                                if (!reader.IsDBNull(6))
                                {
                                    string imageUrl = reader.GetString(6);
                                    announcementsDict[announcementId].Images.Add(imageUrl);
                                }
                            }

                            // Convert the dictionary values to a list of announcements
                            announcementList = announcementsDict.Values.ToList();
                        }
                    }
                }

                return announcementList;
            }
            catch (Exception ex)
            {
                // Log the exception as needed
                return null;
            }
        }


    }
}
