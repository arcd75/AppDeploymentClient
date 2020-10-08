using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPWSAppDeploymentClient.Models
{
    public class SqlProcessor
    {
        public async Task<DataTable> GetSQL(string SqlCommand,
            Settings settings)
        {

            try
            {
                SqlConnection sqlConnection = await TestConnection(settings);
                var q = sqlConnection.CreateCommand();
                q.CommandTimeout = 120;
                q.CommandType = System.Data.CommandType.Text;
                q.CommandText = SqlCommand;
                q.ExecuteNonQuery();
                SqlDataAdapter adapter = new SqlDataAdapter(q);
                DataTable table = new DataTable();
                adapter.Fill(table);
                return table;
            }
            catch (Exception ex)
            {
                return null;
                //throw;
            }
        }

        public async Task<SqlConnection> TestConnection(Settings settings)
        {
            string connectionString = string.Format(@"Data Source={0};Initial Catalog=SPWSAppDeployment;User Id={1};Password={2}", settings.Server, settings.Username, settings.Password);
            SqlConnection sqlConnection =  new SqlConnection(connectionString);
            sqlConnection.Open();
            return sqlConnection;
        }

    }
}
