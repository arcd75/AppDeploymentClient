using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPWSAppDeploymentClient.Models
{
    public class AppFile
    {
        [Key]
        public int AppFileId { get; set; }

        public string AppFileName { get; set; }
        public string AppFileSize { get; set; }
        public string AppFileExt { get; set; }
        public int AppVersionId { get; set; }
        public bool isFolder { get; set; }
        public int? parentFolder { get; set; }
        public DateTime? LastWriteTime { get; set; }

        public int AppFS
        {
            get
            {
                int FS = 0;
                int.TryParse(this.AppFileSize, out FS);
                return FS;
            }
        }

        public async Task<byte[]> GetData(
            Settings settings)
        {
            byte[] data = new byte[this.AppFS];
            int filesize = 0;
            int.TryParse(this.AppFileSize, out filesize);
            string sqlQuery = string.Format("Select * From AppFileBlobs Where AppFileId={0}", this.AppFileId);
            SqlProcessor sqlProcessor = new SqlProcessor();
            DataTable result = await sqlProcessor.GetSQL(sqlQuery, settings);
            foreach (DataRow item in result.Rows)
            {
                data = (byte[])item.ItemArray[1];
            }
            return data;
        }
    }

}
