using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPWSAppDeploymentClient.Models
{
    public class Apps
    {
        [Key]
        public int AppId { get; set; }

        public string AppName { get; set; }
        public List<AppVersion> AppVersions { get;set;}
        public AppVersion AppCurrentVersion
        {
            get
            {
                return this.AppVersions.OrderByDescending(a => a.Date).FirstOrDefault();
            }
        }

        public override string ToString()
        {
            return this.AppName;
        }

        public async static Task<List<Apps>> GetAllApps(
           Settings settings = null
            )
        {
            List<Apps> apps = new List<Apps>();
            SqlProcessor sqlProcessor = new SqlProcessor();
            string sqlQuery = "Select * FROM Apps";
            DataTable results = await sqlProcessor.GetSQL(sqlQuery, settings);
            foreach (DataRow item in results.Rows)
            {
                int AppId = 0;
                int.TryParse(item.ItemArray[0].ToString(), out AppId);
                apps.Add(new Apps()
                {
                    AppId = AppId,
                    AppName = item.ItemArray[1].ToString()
                });
            }
            return apps;
        }

        
    }
}
