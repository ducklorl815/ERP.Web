using ERP.Web.Models.Models.ControllerSetting;
using ERP.Web.Models.Models.Tools;
using ERP.Web.Service.ViewModels.ControllerSetting;
using ERP.Web.Service.ViewModels.Tools;
using ERP.Web.Utility.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ERP.Web.Service.Service.ControllerSetting
{
    public partial class ToolsService
    {
        public async Task<FontAwesomeViewModel_result> ImportFontJson(IFormFile JsonFile)
        {
            var result = new FontAwesomeViewModel_result();
            var FontAwesomeList = new List<FontAwesomeMainModel>();
            using (var stream = new StreamReader(JsonFile.OpenReadStream()))
            {
                var json = await stream.ReadToEndAsync();
                var jsonString = JsonConvert.DeserializeObject<List<string>>(json);

                // 拆成兩個欄位
                FontAwesomeList = jsonString.Select(x =>
                {
                    var parts = x.Split(" ");
                    return new FontAwesomeMainModel
                    {
                        IconStyle = parts[0],
                        IconClass = parts[1]
                    };
                }).ToList();
            }
            if (FontAwesomeList.Count == 0)
            {
                result.IsSuccess = false;
                result.Msg = "Json無樣式資料";
                return result;
            }
            foreach (var FontAwesomeData in FontAwesomeList)
            {
                if (string.IsNullOrEmpty(FontAwesomeData.IconClass) || string.IsNullOrEmpty(FontAwesomeData.IconStyle))
                    continue;
                bool chkInsetData = await _toolsRepo.chkInsetData(FontAwesomeData);
                if (chkInsetData)
                    continue;
                await _toolsRepo.InsertFontAwesome(FontAwesomeData);
            }


            result.IsSuccess = true;
            return result;
        }

        /// <summary>
        /// 產生密碼雜湊（對應 SQL: HASHBYTES('SHA2_256', @Password + Salt)）
        /// </summary>
        public async static Task<string> HashPassword(string password, string salt)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException(nameof(password));

            if (string.IsNullOrEmpty(salt))
                throw new ArgumentNullException(nameof(salt));

            // 密碼 + Salt
            var combined = password + salt;

            // SHA256 雜湊
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(combined);
                var hash = sha256.ComputeHash(bytes);

                // 轉成 16 進位字串（與 SQL HASHBYTES 類似）
                var sb = new StringBuilder();
                foreach (var b in hash)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }
    }
}
