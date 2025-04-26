using MessageProcessingSystemAPI.Services;
using Shared.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace MessageProcessingSystemAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HornetController : ControllerBase
    {
        private readonly RabbitMqService _rabbitService;

        public HornetController(RabbitMqService rabbitService)
        {
            _rabbitService = rabbitService;
        }

        /// <summary>
        /// Returns the count of hashes grouped by day.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetHashCounts([FromServices] HashService hashService)
        {
            var hashes = await hashService.GetHashCountsAsync();
            return Ok(new { hashes });
        }


        /// <summary>
        /// Generates 40,000 random SHA1 hashes and sends them in parallel to RabbitMQ.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> GenerateHashes()
        {
            var splittedHashes = Enumerable.Range(0, 4).Select(_ =>
                Task.Run(async () =>
                {
                    for (int i = 0; i < 10000; i++)
                    {
                        var sha1 = Helper.ComputeSha1(Guid.NewGuid().ToString());
                        await _rabbitService.PublishAsync(sha1);
                    }
                })
            );

            await Task.WhenAll(splittedHashes);

            return Ok(new { message = "Hashes sent to queue" });
        }
    }
}
