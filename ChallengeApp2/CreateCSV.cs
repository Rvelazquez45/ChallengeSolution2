using ChallengeApp2.Models;

using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using System.Globalization;
using System.Net;
using System.Text.Json;

namespace ChallengeApp2;

public class CreateCSV
{
    private readonly ILogger<CreateCSV> _logger;
    private readonly IConfiguration _configuration;

    public CreateCSV(ILogger<CreateCSV> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    [Function("CreateCSV")]
    public async Task<HttpResponseData> Run
    (
        [HttpTrigger(AuthorizationLevel.Function, "post")]
        HttpRequestData req
    )
    {
        _logger.LogInformation
        (
            $"Processing CreateCsv for {req.Body}"
        );

        List<PersonModel>? listOfPeople = await
            JsonSerializer.DeserializeAsync<List<PersonModel>>(req.Body);

        if (listOfPeople is null || listOfPeople.Any() is false)
        {
            _logger.LogError
            (
                "No people found in the request body"
            );

            HttpResponseData badRequestResponse =
                req.CreateResponse(HttpStatusCode.BadRequest);

            await badRequestResponse.WriteStringAsync
            (
                "Please pass a list of people in the request body"
            );

            return badRequestResponse;
        }

        #region turn input into CSV

        bool? includeHeader =
            _configuration.GetValue<bool?>(key: "includeHeader");

        if (includeHeader is null)
        {
            _logger.LogError
            (
                "No includeHeader value found in configuration"
            );

            return req.CreateResponse(HttpStatusCode.InternalServerError);
        };

        using Stream memoryStream = new MemoryStream();
        using StreamWriter streamWriter = new(stream: memoryStream);
        using CsvWriter csvWriter = new
        (
            writer: streamWriter,
            configuration: new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = (bool)includeHeader
            }
        );

        csvWriter.WriteRecords(records: listOfPeople);
        csvWriter.Flush();
        memoryStream.Seek
        (
            offset: 0,
            origin: SeekOrigin.Begin
        );

        #endregion turn input into CSV

        #region prepare response

        HttpResponseData okResponse = req.CreateResponse(HttpStatusCode.OK);

        okResponse.Headers.Add
            (name: "Content-Type", value: "text/csv");
        okResponse.Headers.Add
            (name: "Content-Disposition", value: "attachment;filename=people.csv");

        // The MemoryStream is a file but it's in memory, and we copy it to the body of our output
        await memoryStream.CopyToAsync(okResponse.Body);

        #endregion prepare response

        return okResponse;
    }
}
