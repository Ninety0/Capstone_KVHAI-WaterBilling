using KVHAI.CustomClass;
using KVHAI.Models;
using System.Data.SqlClient;

namespace KVHAI.Repository
{
    public class AnnouncementRepository
    {
        private readonly DBConnect _dbConnect;
        private readonly InputSanitize _sanitize;

        public AnnouncementRepository(DBConnect dBConnect, InputSanitize inputSanitize)
        {
            _dbConnect = dBConnect;
            _sanitize = inputSanitize;
        }

        public async Task<int> SubmitAnnouncement(Announcement announcement)
        {
            try
            {
                var date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand("INSERT INTO announcement_tb(emp_id,title,contents,date_created,date_expire) VALUES(@emp_id, @title, @content,@created, @expire)", connection))
                    {
                        command.Parameters.AddWithValue("@emp_id", "1");
                        command.Parameters.AddWithValue("@title", announcement.Title);
                        command.Parameters.AddWithValue("@content", announcement.Contents);
                        command.Parameters.AddWithValue("@created", date);
                        command.Parameters.AddWithValue("@expire", announcement.Date_Expire);

                        return await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception)
            {
                return 0;
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
                        SELECT * 
                        FROM announcement_tb 
                        WHERE date_expire >= @date ", connection))
                    {
                        command.Parameters.AddWithValue("@date", date);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                announcementList.Add(
                                    new Announcement
                                    {
                                        Announcement_ID = reader.GetInt32(0),
                                        Employee_ID = reader.GetInt32(1),
                                        Title = reader.GetString(2),
                                        Contents = reader.GetString(3),
                                        Date_Created = reader.GetString(4),
                                        Date_Expire = reader.GetString(5)
                                    });
                            }
                        }
                    }
                }

                return announcementList;
            }
            catch (Exception)
            {
                return null;
            }
        }

    }
}
