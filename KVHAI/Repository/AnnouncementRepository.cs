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
        private readonly ListRepository _listRepo;

        public AnnouncementRepository(DBConnect dBConnect, InputSanitize inputSanitize, AnnouncementImageRepository announcementImageRepository, ListRepository listRepo)
        {
            _dbConnect = dBConnect;
            _sanitize = inputSanitize;
            _announcementImageRepository = announcementImageRepository;
            _listRepo = listRepo;
        }

        public async Task<List<Announcement>> GetAnnouncements(string id = "")
        {
            try
            {
                // Retrieve the list of announcements from the repository
                var announcements = await _listRepo.AnnouncementList();
                
                if (string.IsNullOrEmpty(id))
                {
                    return announcements
                        .Select(a => new Announcement
                        {
                            Announcement_ID = a.Announcement_ID,
                            Title = a.Title,
                            Contents = a.Contents,
                            Date_Created = a.Date_Created
                        })
                        .OrderByDescending(a => a.Date_Created)
                        .ToList();
                }
                
                return announcements
                    .Where(a => a.Announcement_ID.ToString() == id)
                    .Select(a => new Announcement
                    {
                        Announcement_ID = a.Announcement_ID,
                        Title = a.Title,
                        Contents = a.Contents,
                        Date_Created = a.Date_Created
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                // Log the exception details
                // logger.LogError(ex, "Error retrieving announcements");
                throw; // Rethrow the exception to be handled by the caller
            }
        }

        public async Task<int> CountAnnouncementData()
        {
            try
            {
                var announcement = await _listRepo.AnnouncementList();

                return announcement.Count();
            }
            catch (Exception)
            {
                return 0;
            }

        }

        public async Task<List<Announcement>> GetAllAnnouncement(int offset, int limit)
        {
            var announcement = new List<Announcement>();

            var query = "SELECT * FROM announcement_tb ORDER BY date_created DESC OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY";

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
                            var announce = new Announcement
                            {
                                // Map the repository data to the Announcement model properties
                                Announcement_ID = reader.GetInt32(0),
                                Title = reader.GetString(2),
                                Contents = reader.GetString(3),
                                Date_Created = reader.GetDateTime(4).ToString("MMM dd, yyyy HH:mm:ss")
                            };
                            announcement.Add(announce);
                        }
                    }
                }
            }

            return announcement;
        }

        public async Task<int> Save(Announcement announce, List<IFormFile>? images = null)
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
                           throw new Exception();
                        }

                        // Save the images (if any)
                        if (images?.Any() == true)
                        {
                            var saveImg = await _announcementImageRepository.SaveAnnouncementImg(id, images, connection, transaction);

                            if (saveImg < 1) // Assuming non-zero result indicates success
                            {
                                throw new Exception();
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
                    INSERT INTO announcement_tb(emp_id, title, contents, date_created) 
                    VALUES(@emp_id, @title, @content, @created);
                     SELECT CAST(SCOPE_IDENTITY() AS INT);", connection, transaction))
                {
                    command.Parameters.AddWithValue("@emp_id", "1");  // Use actual employee ID
                    command.Parameters.AddWithValue("@title", await _sanitize.HTMLSanitizerAsync(announcement.Title));
                    command.Parameters.AddWithValue("@content", await _sanitize.HTMLSanitizerAsync(announcement.Contents));
                    command.Parameters.AddWithValue("@created", date);

                    int newAnnouncementId = (int)await command.ExecuteScalarAsync();
                    return newAnnouncementId;
                }
            }
            catch (Exception)
            {
                return 0; // 0 indicates failure
            }
        }

        public async Task<int> SubmitUpdateAnnouncement(Announcement announcement, SqlConnection connection, SqlTransaction transaction)
        {
            try
            {
                var date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                using (var command = new SqlCommand(@"
                    UPDATE announcement_tb set title = @title, contents = @content, date_created=@date
                    WHERE announcement_id = @id;", connection, transaction))
                {
                    command.Parameters.AddWithValue("@title", await _sanitize.HTMLSanitizerAsync(announcement.Title));
                    command.Parameters.AddWithValue("@content", await _sanitize.HTMLSanitizerAsync(announcement.Contents));
                    command.Parameters.AddWithValue("@date", date);
                    command.Parameters.AddWithValue("@id", announcement.Announcement_ID); 

                    int result = await command.ExecuteNonQueryAsync();
                    return result;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
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
                        INSERT INTO announcement_tb(emp_id, title, contents, date_created) 
                        VALUES(@emp_id, @title, @content, @created);
                        SELECT CAST(SCOPE_IDENTITY() AS INT);", connection))
                    {
                        command.Parameters.AddWithValue("@emp_id", "1");  // Use actual employee ID
                        command.Parameters.AddWithValue("@title", await _sanitize.HTMLSanitizerAsync(announcement.Title));
                        command.Parameters.AddWithValue("@content", await _sanitize.HTMLSanitizerAsync(announcement.Contents));
                        command.Parameters.AddWithValue("@created", date);

                        int newAnnouncementId = (int)await command.ExecuteScalarAsync();
                        return newAnnouncementId;
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
                var date = DateTime.Now.AddDays(-3).ToString("yyyy-MM-dd HH:mm:ss");

                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(@"
                        SELECT a.announcement_id, a.emp_id, a.title, a.contents, a.date_created, ai.image_url 
                        FROM announcement_tb a
                        LEFT JOIN announcement_img_tb ai ON a.announcement_id = ai.announcement_id
                        ORDER BY date_created DESC", connection))
                    {//WHERE a.date_created >= GETDATE() -3
                        //command.Parameters.AddWithValue("@date", date);

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
                                        Images = new List<string>() // Initialize the list to hold the images
                                    };

                                    announcementsDict[announcementId] = announcement;
                                }

                                // Check if there is an associated image for this announcement and add it to the list
                                if (!reader.IsDBNull(5))
                                {
                                    string imageUrl = reader.GetString(5);
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

        public async Task<int> UpdateAnnouncement(Announcement announce, List<IFormFile>? images = null)
        {
            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Insert the announcement first
                        var result = await SubmitUpdateAnnouncement(announce, connection, transaction);

                        if (result < 1)
                        {
                            throw new Exception();
                        }

                        // Save the images (if any)
                        if (images?.Any() == true)
                        {
                            var saveImg = await _announcementImageRepository.UpdateAnnouncementImg(announce.Announcement_ID, images, connection, transaction);

                            if (saveImg < 1) // Assuming non-zero result indicates success
                            {
                                throw new Exception();
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
    }
}
