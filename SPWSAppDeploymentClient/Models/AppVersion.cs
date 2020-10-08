using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPWSAppDeploymentClient.Models
{
    public class AppVersion
    {
        [Key]
        public int AppVersionId { get; set; }

        public int AppId { get; set; }
        public string AppVersionName { get; set; }
        public bool isMajorRevision { get; set; }
        public DateTime Date { get; set; }
        public string StringDate
        {
            get
            {
                return Date.ToString("MM/dd/yyyy");
            }
        }

        public App App { get; set; }

        public override string ToString()
        {
            return this.AppVersionName;
        }

        public async static Task<List<AppVersion>> GetAllAppVersions(
             Settings settings = null
            )
        {
            List<AppVersion> appVersions = new List<AppVersion>();
            SqlProcessor sqlProcessor = new SqlProcessor();
            string sqlQuery = "Select * FROM AppVersions";
            DataTable results = await sqlProcessor.GetSQL(sqlQuery, settings);
            foreach (DataRow item in results.Rows)
            {
                int AppVersionId = 0;
                int.TryParse(item.ItemArray[0].ToString(), out AppVersionId);
                int AppId = 0;
                int.TryParse(item.ItemArray[1].ToString(), out AppId);
                DateTime date = DateTime.Now;
                DateTime.TryParse(item.ItemArray[4].ToString(), out date);
                appVersions.Add(new AppVersion()
                {
                    AppVersionId = AppVersionId,
                    AppId = AppId,
                    AppVersionName = item.ItemArray[2].ToString(),
                    isMajorRevision = (bool)item.ItemArray[3],
                    Date = date
                });
                //int.TryParse(item.ItemArray[0].ToString(), out AppId);
                //apps.Add(new Apps()
                //{
                //    AppId = AppId,
                //    AppName = item.ItemArray[1].ToString()
                //});
            }
            return appVersions;
        }

        public async Task<List<AppFile>> GetAllFiles(
            Settings settings)
        {
            List<AppFile> files = new List<AppFile>();
            SqlProcessor sqlProcessor = new SqlProcessor();
            string sqlQuery = string.Format("Select * from AppFiles Where AppVersionId={0}",this.AppVersionId);
            DataTable results = await sqlProcessor.GetSQL(sqlQuery, settings);
            foreach (DataRow item in results.Rows)
            {
                
                var AppFileId = 0;
                int.TryParse(item.ItemArray[0].ToString(), out AppFileId);
                var AppVersionId = 0;
                int.TryParse(item.ItemArray[1].ToString(), out AppVersionId);
                int parentFolder = 0;
                int.TryParse(item.ItemArray[6].ToString(), out parentFolder);
                DateTime lwt = DateTime.Now;
                DateTime.TryParse(item.ItemArray[7].ToString(), out lwt);
                bool isFolder = false;
                bool.TryParse(item.ItemArray[4].ToString(),out isFolder);
                files.Add(new AppFile()
                {
                    AppFileId = AppFileId,
                    AppVersionId = AppVersionId,
                    parentFolder = parentFolder,
                    LastWriteTime = lwt,
                    AppFileName = item.ItemArray[2].ToString(),
                    AppFileSize = item.ItemArray[3].ToString(),
                    isFolder = isFolder,
                    AppFileExt = item.ItemArray[4].ToString()
                });

            }

            return files;


        }
    }
}
