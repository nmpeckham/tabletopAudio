using Amazon.DynamoDBv2;
using Extensions;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using UnityEngine;

public static class NowPlayingWebpage
{
    static AmazonDynamoDBClient client;
    static bool goodConfig = false;
    static string tableName;
    static string id;

    internal static void Init()
    {
        string accessKeyId;
        string secretAccessKey;
        Amazon.RegionEndpoint region;
        const string credFileName = "credentials.json";
        string credFolder = Camera.main.GetComponent<MainAppController>().saveDirectory;

        string fileLocation = Path.Combine(credFolder, credFileName);
        if (System.IO.File.Exists(fileLocation))
        {
            try
            {
                string contents = System.IO.File.ReadAllText(fileLocation);
                Dictionary<string, string> data = JsonSerializer.Deserialize<Dictionary<string, string>>(contents);
                accessKeyId = data["accessKeyId"];
                secretAccessKey = data["secretAccessKey"];
                region = Amazon.RegionEndpoint.GetBySystemName(data["region"]);
                tableName = data["tableName"];
                id = data["id"];
                Amazon.Runtime.CredentialManagement.CredentialProfileOptions options = new Amazon.Runtime.CredentialManagement.CredentialProfileOptions
                {
                    AccessKey = accessKeyId,
                    SecretKey = secretAccessKey
                };
                Amazon.Runtime.CredentialManagement.CredentialProfile profile = new Amazon.Runtime.CredentialManagement.CredentialProfile("default", options);
                Amazon.Runtime.AWSCredentials creds = profile.GetAWSCredentials(null);
                client = new AmazonDynamoDBClient(creds, region);
                goodConfig = true;
            }
            catch (System.Exception e)
            {
                Camera.main.GetComponent<MainAppController>().ShowErrorMessage("Couldn't read AWS config file: " + e.Message);
            }
        }
    }

    async static internal void SongChanged(Song song)
    {
        if (goodConfig)
        {
            try
            {
                Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue> item = new Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>();
                var titleAttribute = new Amazon.DynamoDBv2.Model.AttributeValue(song.title);
                var artistAttribute = new Amazon.DynamoDBv2.Model.AttributeValue(song.artist);
                var durationAttribute = new Amazon.DynamoDBv2.Model.AttributeValue();
                durationAttribute.N = song.duration.TotalSeconds.ToString("F1");    //Set as number, fixed-point with one decimal place
                var startTimeAttribute = new Amazon.DynamoDBv2.Model.AttributeValue();
                startTimeAttribute.N = System.DateTime.UtcNow.ToUnixTime().ToString("F1"); //Set as number, fixed-point with one decimal place
                var idAttribute = new Amazon.DynamoDBv2.Model.AttributeValue(id);

                item.Add("id", idAttribute);
                item.Add("s", titleAttribute);
                if (song.artist != null) item.Add("a", artistAttribute);
                item.Add("d", durationAttribute);
                item.Add("t", startTimeAttribute);
                await client.PutItemAsync("nowPlayingSong", item);

                item = new Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>();

                var typeAttribute = new Amazon.DynamoDBv2.Model.AttributeValue("Play");

                item.Add("id", idAttribute);
                item.Add("z", typeAttribute);
                item.Add("t", startTimeAttribute);

                await client.PutItemAsync("nowPlayingStatus", item);


            }
            catch (System.Exception e)
            {
                Camera.main.GetComponent<MainAppController>().ShowErrorMessage("Couldn't send AWS update: " + e.Message);
            }
        }
        return;
    }

    internal async static void SongPaused(float progress)
    {
        if (goodConfig)
        {
            var idAttribute = new Amazon.DynamoDBv2.Model.AttributeValue(id);
            Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue> item = new Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>();
            var startTimeAttribute = new Amazon.DynamoDBv2.Model.AttributeValue();
            startTimeAttribute.N = System.DateTime.UtcNow.ToUnixTime().ToString("F1"); //Set as number, fixed-point with one decimal place
            var typeAttribute = new Amazon.DynamoDBv2.Model.AttributeValue("Paused");
            var progressAttribute = new Amazon.DynamoDBv2.Model.AttributeValue();
            progressAttribute.N = progress.ToString("F1");

            item.Add("id", idAttribute);
            item.Add("t", startTimeAttribute);
            item.Add("z", typeAttribute);
            item.Add("p", progressAttribute);
            await client.PutItemAsync("nowPlayingStatus", item);
        }

    }

    internal async static void SongStopped()
    {
        if (goodConfig)
        {
            var idAttribute = new Amazon.DynamoDBv2.Model.AttributeValue(id);
            Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue> item = new Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>();
            var startTimeAttribute = new Amazon.DynamoDBv2.Model.AttributeValue();
            startTimeAttribute.N = System.DateTime.UtcNow.ToUnixTime().ToString("F1"); //Set as number, fixed-point with one decimal place
            var typeAttribute = new Amazon.DynamoDBv2.Model.AttributeValue("Stopped");

            item.Add("id", idAttribute);
            item.Add("t", startTimeAttribute);
            item.Add("z", typeAttribute);
            await client.PutItemAsync("nowPlayingStatus", item);
        }
    }

    internal async static void SongUnpaused(float progress)
    {
        if (goodConfig)
        {
            var idAttribute = new Amazon.DynamoDBv2.Model.AttributeValue(id);
            Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue> item = new Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>();
            var startTimeAttribute = new Amazon.DynamoDBv2.Model.AttributeValue();
            startTimeAttribute.N = System.DateTime.UtcNow.ToUnixTime().ToString("F1"); //Set as number, fixed-point with one decimal place
            var typeAttribute = new Amazon.DynamoDBv2.Model.AttributeValue("Unpaused");
            var progressAttribute = new Amazon.DynamoDBv2.Model.AttributeValue();
            progressAttribute.N = progress.ToString("F1");

            item.Add("id", idAttribute);
            item.Add("t", startTimeAttribute);
            item.Add("z", typeAttribute);
            item.Add("p", progressAttribute);
            await client.PutItemAsync("nowPlayingStatus", item);
        }
    }

    internal async static void SongPlaybackChanged(float progress)
    {
        if (goodConfig)
        {
            var idAttribute = new Amazon.DynamoDBv2.Model.AttributeValue(id);
            Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue> item = new Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>();
            var startTimeAttribute = new Amazon.DynamoDBv2.Model.AttributeValue();
            startTimeAttribute.N = System.DateTime.UtcNow.ToUnixTime().ToString("F1"); //Set as number, fixed-point with one decimal place
            var typeAttribute = new Amazon.DynamoDBv2.Model.AttributeValue("Seek");
            var progressAttribute = new Amazon.DynamoDBv2.Model.AttributeValue();
            progressAttribute.N = progress.ToString("F1");

            item.Add("id", idAttribute);
            item.Add("t", startTimeAttribute);
            item.Add("z", typeAttribute);
            item.Add("p", progressAttribute);
            await client.PutItemAsync("nowPlayingStatus", item);
        }
    }
}
