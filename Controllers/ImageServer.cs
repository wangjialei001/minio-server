using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Minio;
using Minio.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WeiCloudStorageAPI.Model;

namespace WeiCloudStorageAPI.Controllers
{
    [Route("imageserve")]
    [ApiController]
    public class ImageServer : ControllerBase
    {
        private readonly IConfiguration configuration;
        public ImageServer(IConfiguration _configuration)
        {
            configuration = _configuration;
        }
        [HttpGet("{fileName}")]
        public async Task<IActionResult> Get(string fileName)
        {
            var endpoint = configuration["FileServer:Url"];
            var accessKey = configuration["FileServer:AccessKey"];
            var secretKey = configuration["FileServer:SecretKey"];
            try
            {
                var bucketName = "imageserve";
                var minioClient = new MinioClient(endpoint, accessKey, secretKey);//.WithSSL();
                // Check whether the object exists using statObject().
                // If the object is not found, statObject() throws an exception,
                // else it means that the object exists.
                // Execution is successful.
                using (var ms = new MemoryStream())
                {
                    await minioClient.GetObjectAsync(bucketName, fileName, (stream) =>
                    {
                        stream.CopyTo(ms);
                    });
                    var bytes = ms.GetBuffer();
                    string extend = fileName.Substring(fileName.LastIndexOf(".") + 1);

                    Console.WriteLine("下载文件：" + fileName);
                    //jpg、png、jpeg、bmp、gif
                    if (extend.Equals("jpg", StringComparison.InvariantCultureIgnoreCase)
                        || extend.Equals("png", StringComparison.InvariantCultureIgnoreCase)
                        || extend.Equals("jpeg", StringComparison.InvariantCultureIgnoreCase)
                        || extend.Equals("bmp", StringComparison.InvariantCultureIgnoreCase)
                        || extend.Equals("gif", StringComparison.InvariantCultureIgnoreCase))
                    {
                        return new FileContentResult(bytes, "image/jpeg");
                    }
                    else
                    {
                        return new FileContentResult(bytes, "application/octet-stream");
                    }
                }
            }
            catch (MinioException e)
            {
                Console.Out.WriteLine("Error occurred: " + e);
            }
            return NoContent();
        }
        [HttpPost]
        [Route("file")]
        public async Task<string> Post([FromForm] IFormFile file)
        {
            string fileName = file.FileName;
            MemoryStream stream = new MemoryStream();
            await file.CopyToAsync(stream);
            return await Post(stream, fileName);
        }
        [HttpPost]
        [Route("stream")]
        public async Task<string> Post([FromForm] Stream stream, [FromQuery] string fileName)
        {
            try
            {
                stream.Seek(0, SeekOrigin.Begin);
                var endpoint = configuration["FileServer:Url"];
                var accessKey = configuration["FileServer:AccessKey"];
                var secretKey = configuration["FileServer:SecretKey"];
                var bucketName = "imageserve";
                var minioClient = new MinioClient(endpoint, accessKey, secretKey);//.WithSSL();
                await minioClient.PutObjectAsync(bucketName, fileName, stream, stream.Length);
                Console.WriteLine("上传文件：" + fileName);
            }
            catch (MinioException e)
            {
                Console.Out.WriteLine("Error occurred: " + e);
                return "error:" + e.Message;
            }
            return fileName;
        }
    }
}
