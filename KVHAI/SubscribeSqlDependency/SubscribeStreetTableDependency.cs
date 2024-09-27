using KVHAI.Hubs;
using KVHAI.Models;
using TableDependency.SqlClient;

namespace KVHAI.SubscribeSqlDependency
{
    public class SubscribeStreetTableDependency : ISubscribeTableDependency
    {
        private readonly DBConnect _dBConnect;
        private readonly StreetHub _streetHub;
        SqlTableDependency<Streets> _tableDependency;

        public SubscribeStreetTableDependency(DBConnect dBConnect, StreetHub streetHub)
        {
            _dBConnect = dBConnect;
            _streetHub = streetHub;
        }

        public void SubscribeTableDependency(string connectionString)
        {
            _tableDependency = new SqlTableDependency<Streets>(connectionString);
            _tableDependency.OnChanged += _tableDependency_OnChanged;
            _tableDependency.OnError += _tableDependency_OnError;
            _tableDependency.Start();
        }

        private void _tableDependency_OnError(object sender, TableDependency.SqlClient.Base.EventArgs.ErrorEventArgs e)
        {
            Console.WriteLine($"{nameof(Streets)} SqlTableDependency error: {e.Error.Message}");
        }

        private void _tableDependency_OnChanged(object sender, TableDependency.SqlClient.Base.EventArgs.RecordChangedEventArgs<Streets> e)
        {
            if (e.ChangeType != TableDependency.SqlClient.Base.Enums.ChangeType.None)
            {
                _streetHub.GetStreets();
            }
        }

    }
}
