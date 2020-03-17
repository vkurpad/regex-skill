using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using WebApiSkills.Common;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace regexskill
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            return name != null
                ? (ActionResult)new OkObjectResult($"Hello, {name}")
                : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
        }

        [FunctionName("regex")]
        public static IActionResult RunKewit(
       [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
       ILogger log, ExecutionContext executionContext)
        {
            log.LogInformation("Projection: C# HTTP trigger function processed a request.");
            string skillName = executionContext.FunctionName;
            log.LogInformation($"REQUEST: {new StreamReader(req.Body).ReadToEnd()}");
            req.Body.Position = 0;
            IEnumerable<WebApiRequestRecord> requestRecords = WebApiSkillHelpers.GetRequestRecords(req);
            if (requestRecords == null)
            {
                return new BadRequestObjectResult($"{skillName} - Invalid request record array.");
            }
            WebApiSkillResponse response = WebApiSkillHelpers.ProcessRequestRecords(skillName, requestRecords,
            (inRecord, outRecord) =>
            {
                string content = inRecord.Data["text"].ToString();
                string regex = inRecord.Data["regex"].ToString();
                if (regex == null)
                    regex = "^[A-Z]{2,5}$";
                Regex rx = new Regex(regex);
                MatchCollection matches = rx.Matches(content);
                foreach (Match match in matches)
                {
                    GroupCollection groups = match.Groups;
                    Console.WriteLine("'{0}' repeated at positions {1} and {2}",
                                      groups["word"].Value,
                                      groups[0].Index,
                                      groups[1].Index);
                }
                var list = matches.Cast<Match>().Select(match => new { match.Value, match.Index }).ToList();
                outRecord.Data["matches"] = list;
                
                return outRecord;
            });
            //log.LogInformation(JsonConvert.SerializeObject(response));
            return new OkObjectResult(response);
        }

    }
}
