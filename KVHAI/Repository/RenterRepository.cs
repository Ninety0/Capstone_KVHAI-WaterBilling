using KVHAI.CustomClass;
using KVHAI.Models;
using System.Data.SqlClient;

namespace KVHAI.Repository
{
    public class RenterRepository
    {
        private readonly DBConnect _dbConnect;
        private readonly Hashing _hash;
        private readonly InputSanitize _sanitize;
        private readonly NotificationRepository _notificationRepository;
        private readonly ListRepository _listRepository;

        public RenterRepository(DBConnect dBConnect, Hashing hashing, InputSanitize inputSanitize, NotificationRepository notificationRepository, ListRepository listRepository)
        {
            _dbConnect = dBConnect;
            _hash = hashing;
            _sanitize = inputSanitize;
            _notificationRepository = notificationRepository;
            _listRepository = listRepository;
        }

        //INSERT RENTER
        public async Task<int> InsertRenter(string resident_id, Renter renter)
        {
            try
            {
                string password = _hash.HashPassword(await _sanitize.HTMLSanitizerAsync(renter.Password));
                int renterResult = 0;
                int residentAddressResult = 0;

                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            using (var command = new SqlCommand(@"INSERT INTO resident_tb
                        (lname,fname,mname,username,password, is_activated,verified_at) VALUES
                        (@l, @f, @m, @user, @pass, @is, @verify);
                        SELECT SCOPE_IDENTITY();", connection, transaction))
                            {
                                command.Parameters.AddWithValue("@l", await _sanitize.HTMLSanitizerAsync(renter.Lname));
                                command.Parameters.AddWithValue("@f", await _sanitize.HTMLSanitizerAsync(renter.Fname));
                                command.Parameters.AddWithValue("@m", await _sanitize.HTMLSanitizerAsync(renter.Mname));
                                command.Parameters.AddWithValue("@user", await _sanitize.HTMLSanitizerAsync(renter.Username));
                                command.Parameters.AddWithValue("@pass", password);
                                command.Parameters.AddWithValue("@is", 1);
                                command.Parameters.AddWithValue("@verify", DateTime.Now);

                                renterResult = Convert.ToInt32(await command.ExecuteScalarAsync());

                            }

                            if (renterResult < 1)
                            {
                                return 0;
                            }

                            using (var residentAddressCommand = new SqlCommand(@"INSERT INTO resident_address_tb    (res_id, addr_id, is_owner) VALUES
                             (@rid,@aid,@is)", connection, transaction))
                            {
                                residentAddressCommand.Parameters.AddWithValue("@rid", renterResult);
                                residentAddressCommand.Parameters.AddWithValue("@aid", renter.Address_ID);
                                residentAddressCommand.Parameters.AddWithValue("@is", 0);

                                residentAddressResult = await residentAddressCommand.ExecuteNonQueryAsync();
                            }

                            if (residentAddressResult < 1)
                            {
                                throw new Exception();
                            }

                            transaction.Commit();

                            return residentAddressResult;
                        }
                        catch (Exception)
                        {
                            transaction.Rollback();
                            return 0;
                        }
                    }

                }
            }
            catch (Exception)
            {
                return 0;
            }
        }

        //FETCH RENTER
        public async Task<List<Resident>> GetRenters(string address_id)
        {
            try
            {
                var residentAddressList = await _listRepository.ResidentAddressList();
                var residentList = await _listRepository.ResidentList();


                var renter_id = residentAddressList.Where(a => a.Address_ID.ToString() == address_id && a.Is_Owner == 0).Select(r => r.Resident_ID).FirstOrDefault();


                var listOfRenter = residentList.Where(r => r.Res_ID == renter_id.ToString()).ToList();

                return listOfRenter;

            }
            catch (Exception)
            {
                return null;
            }
        }

        //CHECK USERNAME EXIST
        public async Task<bool> IsUsernameExist(string username)
        {
            try
            {
                var residentList = await _listRepository.ResidentList();
                var user = await _sanitize.HTMLSanitizerAsync(username);

                if (residentList.Any(u => u.Username == user))
                {
                    return true;
                }

                return false;
            }
            catch (Exception)
            {
                return true;
            }
        }

        //CHECK TENANT EXIST
        public async Task<bool> IsTenantExist(string tenant_id)
        {
            try
            {
                var residentList = await _listRepository.ResidentList();

                return residentList.Any(i => i.Res_ID == tenant_id);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
