using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rby.Data;
using Rby.IData;
using Rby.Models;
using System.Globalization;
using System.Reflection;

namespace Rby.Service
{
    public class RbyServiceBLL(IRepository repository)
	{
		private readonly IFormatProvider ukFormat = new CultureInfo("en-GB");

        public async Task<APIGatewayProxyResponse> GetHealthCheck(JToken serviceData, ILambdaContext? context)
        {
            string _tablename = "RbyHealthCheck";
            //GenericRepository repository = new();

            APIGatewayProxyResponse response = new();
            var healthCheckRequest = serviceData.ToObject<HealthCheckRequest>();

            if (healthCheckRequest == null)
            {
                string message = "Request did not contain a proper healthcheck object";
                context?.Logger.LogInformation(message);
                response.StatusCode = 500;
                response.Body = message;
            }
            else
            {
                string responseMessage = "";
                try
                {
                    responseMessage = "requestName is " + healthCheckRequest?.RequestName;

                    responseMessage += "\nGetting first HealthCheck record ";
                    HealthCheck? firstHealthCheckRecord = await repository.GetRecordById<HealthCheck>(1, _tablename);
                    string parsedDataResponse;
                    if (firstHealthCheckRecord != null)
                    {
                        parsedDataResponse = "\nId: " + firstHealthCheckRecord.Id + " HCDate: "
                            + firstHealthCheckRecord.HCDate.ToString(ukFormat);
                        responseMessage += parsedDataResponse;
                    }
                    else
                    {
                        throw new NullReferenceException("Can't get first HealthCheck record");
                    }

                    responseMessage += "\nGetting last HealthCheck record";
                    HealthCheck? lastHealthCheckRecord = await repository.GetLatestRecord<HealthCheck>(_tablename, "HCDate");
                    parsedDataResponse = "\nId: " + lastHealthCheckRecord?.Id + " HCDate: "
                        + lastHealthCheckRecord?.HCDate.ToString(ukFormat);
                    responseMessage += parsedDataResponse;

                    responseMessage += "\nWriting new HealthCheck record";
                    int newId = lastHealthCheckRecord == null ? 1 : lastHealthCheckRecord.Id + 1;
                    HealthCheck newHealthCheck = new() { Id = newId, HCDate = DateTime.Parse(DateTime.Now.ToString(ukFormat), ukFormat) };
                    responseMessage += "\nnewHealthCheck is: " + JsonConvert.SerializeObject(newHealthCheck);
                    PutItemResponse writeResponse = await repository.WriteRecord<HealthCheck>(_tablename, newHealthCheck);
                    responseMessage += "\nWrite HTTP response: " + writeResponse.HttpStatusCode.ToString();

                    responseMessage += "\nHealthCheck is " + "complete ";

                    response.StatusCode = 200;
                    context?.Logger.LogInformation(responseMessage);
                    response.Body = responseMessage;
                }
                catch (Exception ex)
                {
                    responseMessage += "\nHealthCheck has errored: " + ex.Message;
                    context?.Logger.LogError(responseMessage);
                    response.StatusCode = 500;
                    response.Body = responseMessage;
                }
            }
            return response;
        }

        public async Task<APIGatewayProxyResponse> Crud(JToken serviceData, ILambdaContext? context)
        {
            APIGatewayProxyResponse response = new();
            CrudRequest? crudRequest = serviceData.ToObject<CrudRequest>();
            string responseMessage;

            if (crudRequest == null)
            {
                responseMessage = "Request did not contain a proper crud request object";
                context?.Logger.LogInformation(responseMessage);
                response.StatusCode = 500;
                response.Body = responseMessage;
            }
            else
            {
                try
                {
                    string typeDescription = "Rby.Models." + crudRequest.Model + ", Rby.Models";
                    Type? crudType = Type.GetType(typeDescription);
                    if (crudType == null)
                    {
                        responseMessage = "Invalid type provided";
                        context?.Logger.LogInformation(responseMessage);
                        response.StatusCode = 500;
                        response.Body = responseMessage;
                        return response;
                    }
                    MethodInfo? crudMethod = typeof(RbyServiceBLL).GetMethod("GenericCrud") ?? throw new NullReferenceException("Can't find GenericCrud method");
                    MethodInfo genericCrudMethod = crudMethod.MakeGenericMethod(crudType);
                    object? methodResponse = genericCrudMethod.Invoke(this, [crudRequest, context]);

                    if (methodResponse is Task task)
                    {
                        // Await the task dynamically and get the result
                        await task.ConfigureAwait(false);

                        // Use reflection to get the Result property of Task<T>
                        PropertyInfo? resultProperty = task.GetType().GetProperty("Result");
                        object? resultValue = resultProperty?.GetValue(task);

                        if (resultValue is null)
                        {
                            throw new NullReferenceException("Result of GenericCrud method is null");
                        }
                        else
                        {
                            response = (APIGatewayProxyResponse)resultValue;
                        }
                    }
                }
                catch (Exception ex)
                {
                    responseMessage = "Crud has errored: " + ex.Message;
                    context?.Logger.LogError(responseMessage);
                    response.StatusCode = 500;
                    response.Body = responseMessage;
                }
            }
            return response;
        }

        public async Task<APIGatewayProxyResponse> PlayGame(JToken serviceData, ILambdaContext? context)
        {
            APIGatewayProxyResponse response = new();
            PlayGameRequest? playGameRequest = serviceData.ToObject<PlayGameRequest>();
            string responseMessage = "";

            if (playGameRequest == null)
            {
                responseMessage = "Request did not contain a proper playgame request object";
                context?.Logger.LogInformation(responseMessage);
                response.StatusCode = 500;
                response.Body = responseMessage;
            }
            else
            {
                try
                {
                    int[]? gameIds;
                    if (playGameRequest.Mode.Contains("Multiple"))
                    {
                        gameIds = playGameRequest.GameIds;
                    }
                    else
                    {
                        gameIds = [playGameRequest.GameId];
                    }
                    if (playGameRequest.Mode == "PbpBulkTest")
                    {
                        int numberOfGames = playGameRequest.NumberOfGames;
                        List<int> gameIdsArray = [];
                        for (int i = 1; i <= numberOfGames; i++)
                        {
                            gameIdsArray.Add(i);
                        }
                        gameIds = [.. gameIdsArray];
                    }
                    ArgumentNullException.ThrowIfNull(gameIds);

                    bool anySuccess = false;
                    List<Game> returnedGames = [];
					PlayBLL playBLL = new(repository);
					foreach (int gameId in gameIds)
                    {
                        Game? thisGame;
                        if (playGameRequest.Mode.Contains("Test"))
                        {
                            thisGame = new Game()
                            {
                                GmDate = DateTime.UtcNow,
                                Id = gameId,
                                Played = false,
                                Season = 1,
                                TeamName1 = "Test" + gameId + "-1",
                                TeamName2 = "Test" + gameId + "-2",
                                Tm1 = 1,
                                Tm2 = 2,
                                HColour1 = "Black",
                                HColourHex1 = "#000000",
								AColour1 = "White",
								AColourHex1 = "#FFFFFF",
								HColour2 = "Black",
                                HColourHex2 = "#000000",
								AColour2 = "White",
								AColourHex2 = "#FFFFFF",
							};  
                        }
                        else
                        {
                            thisGame = await repository.GetTJoinedToX<Game, Team>(gameId, "RbyGm:RbyTm" +
                                ":Tm1/TeamName1/Id/Name:Tm2/TeamName2/Id/Name" +
								":Tm1/HColour1/Id/HColour:Tm1/HColourHex1/Id/HColourHex" +
                                ":Tm2/HColour2/Id/HColour:Tm2/HColourHex2/Id/HColourHex" +
								":Tm1/AColour1/Id/AColour:Tm1/AColourHex1/Id/AColourHex" +
								":Tm2/AColour2/Id/AColour:Tm2/AColourHex2/Id/AColourHex");
						}
						if (thisGame == null)
                        {
                            responseMessage += "Can't find game for id: " + gameId + "\n";
                        }
                        else if (thisGame.Played)
                        {
                            responseMessage += "This game has already been played id: " + gameId + "\n";
                        }
                        else
                        {
							if (thisGame != null)
							{
                                CalculateColours(ref thisGame);
							    responseMessage += "Playing game id: " + gameId + "\n";
                                (Game, PutItemResponse) updatedGameAndResponse = await playBLL.Play(thisGame, playGameRequest.Mode);
                                responseMessage += "HTTP response from game write: " + updatedGameAndResponse.Item2.HttpStatusCode.ToString() + "\n";
                                if (updatedGameAndResponse.Item2.HttpStatusCode.ToString() == "OK")
                                {
                                    anySuccess = true;
                                }
                                returnedGames.Add(updatedGameAndResponse.Item1);
							}
                        }
                    }
                    context?.Logger.LogInformation(responseMessage);
                    string returnedGamesJson = "";
                    if (returnedGames.Count > 0)
                    {
                        returnedGamesJson = JsonConvert.SerializeObject(returnedGames);
                    }
                    response.StatusCode = anySuccess ? 200 : 500;
                    if (playGameRequest.Mode != "PbpBulkTest")
                    {
                        response.Body = anySuccess ? returnedGamesJson : responseMessage + " games: " + returnedGamesJson;
                    }
                    else
                    {
                        // PbpBulkTest response
                        response.Body = JsonConvert.SerializeObject(ServiceTest.RunTests(returnedGames, gameIds.Length));
                    }
                    if (playGameRequest.Mode == "PlayByPlayTest")
                    {
                        // just a string array that you can access in debug for looking in other tools
                        List<string> eventArray = [];
                        foreach (Game game in returnedGames)
                        {
                            if (game.GameEvents != null)
                            {
                                foreach (GameEvent gameEvent in game.GameEvents)
                                {
                                    if (gameEvent.Description != null)
                                    {
                                        eventArray.Add(gameEvent.HalfMinute + " - " + String.Join("-", gameEvent.Description));
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    responseMessage = "PlayGame has errored: " + ex.Message;
                    context?.Logger.LogError(responseMessage);
                    response.StatusCode = 500;
                    response.Body = responseMessage;
                }
            }
            return response;
        }

        public async Task<APIGatewayProxyResponse> Schedule(JToken serviceData, ILambdaContext? context)
        {
            APIGatewayProxyResponse response = new();
            ScheduleRequest? scheduleRequest = serviceData.ToObject<ScheduleRequest>();
            ScheduleBLL scheduleBLL = new(repository);
            string responseMessage;

            if (scheduleRequest == null || scheduleRequest.Operation == null)
            {
                responseMessage = "Request did not contain a proper schedule request object";
                context?.Logger.LogError(responseMessage);
                response.StatusCode = 500;
                response.Body = responseMessage;
            }
            else
            {
                try
                {
                    switch(scheduleRequest.Operation)
                    {
                        case "Get":
                            context?.Logger.LogInformation("Getting schedule for season " + scheduleRequest.Season);
                            IEnumerable<Game> seasonGames = await scheduleBLL.GetScheduleForSeason(scheduleRequest.Season);
                            context?.Logger.LogInformation("Got season games. Count: " + seasonGames.Count());
                            response.StatusCode = 200;
                            response.Body = JsonConvert.SerializeObject(seasonGames);
                            return response;
                        case "Create":
                            context?.Logger.LogInformation("Creating schedule for season " + scheduleRequest.Season);
                            seasonGames = await scheduleBLL.GetScheduleForSeason(scheduleRequest.Season);
                            if (seasonGames.Any())
                            {
                                responseMessage = "Season already exists so can't create";
                                context?.Logger.LogError(responseMessage);
                                response.StatusCode = 500;
                                response.Body = responseMessage;
                                break;
                            }
                            int lastGmId = 0;
                            Game? lastGame = await repository.GetLatestRecord<Game>("RbyGm", "Id");
                            if (lastGame != null)
                            {
                                lastGmId = lastGame.Id;
                            }
                            List<Team> teams = [.. (await repository.GetAllRecords<Team>("RbyTm"))];
                            bool isEvenTeams = (teams.Count % 2 == 0);
                            int numRounds = isEvenTeams ? (teams.Count - 1) * 2 : teams.Count * 2;
                            List<Game> games = [];
                            string gameMessage = "";

                            for (int i = 0; i < teams.Count; i++)
                            {
                                Team hTeam = teams[i];
                                for (int j = 0; j < teams.Count; j++)
                                {
                                    if (i != j)
                                    {
                                        Team aTeam = teams[j];
                                        Game game = new()
                                        {
                                            Season = scheduleRequest.Season,
                                            Tm1 = hTeam.Id,
                                            Tm2 = aTeam.Id,
                                        };
                                        games.Add(game);
                                    }
                                }
                            }

                            // note only really works for even teams. if there are odd teams, then it could leave the same team twice
                            // in the last week. we could check if there are games left over and extend rounds if so

                            Random random = new();
                            Dictionary<int, List<Game>> roundGames = [];
                            List<Game> randomGames = [.. games.OrderBy(g => random.Next())];
                            for (int i = 1; i <= numRounds; i++)
                            {
                                DateTime roundDate = scheduleRequest.StartDate.AddDays(7 * (i - 1));
                                roundGames[i] = [];
                                // iterating through games backwards so i can remove while working
                                for (int j = randomGames.Count - 1; j > -1; j--)
                                {
                                    if (IsValXorYInCollectionAorB(roundGames[i].Select(r => r.Tm1), roundGames[i].Select(r => r.Tm2), randomGames[j].Tm1, randomGames[j].Tm2))
                                    {
                                        // do nothing and move to next game
                                        // as one of the teams in this game are in this round already
                                    }
                                    else
                                    {
                                        // this game has two teams not yet in this round so add to round
                                        lastGmId++;
                                        randomGames[j].Id = lastGmId;
                                        randomGames[j].Round = i;
                                        randomGames[j].GmDate = roundDate;
                                        roundGames[i].Add(randomGames[j]);
                                        gameMessage += "Round " + i + ": " + randomGames[j].Tm1 + ":" + randomGames[j].Tm2 + " / ";
                                        randomGames.RemoveAt(j);
                                    }
                                    if (roundGames[i].Count == teams.Count / 2)
                                    {
                                        break;
                                    }
                                }
                            }

                            List<Game> flattenedSchedule = [.. roundGames.SelectMany(s => s.Value)];
                            // verify total number
                            if (flattenedSchedule.Count != teams.Count * (teams.Count - 1))
                            {
                                // this will happen occasionally because the games remaining in randomGames might not contain any suitable games for this
                                // round i.e. they will all have a team who already has a game
                                // we can either rethink the above or we can resolve this by adding additional midweek rounds for these games

                                responseMessage = "Creating midweek games";
                                context?.Logger.LogError(responseMessage);

                                int midweekRound = 1;
                                while (randomGames.Count > 0 || midweekRound > 5)
                                {
                                    DateTime roundDate = scheduleRequest.StartDate.AddDays((7 * (midweekRound - 1)) + 4);
                                    int numberOfRound = numRounds + midweekRound;
                                    roundGames[numberOfRound] = [];
                                    // iterating through games backwards so i can remove while working
                                    for (int j = randomGames.Count - 1; j > -1; j--)
                                    {
                                        if (IsValXorYInCollectionAorB(roundGames[numberOfRound].Select(r => r.Tm1), roundGames[numberOfRound].Select(r => r.Tm2)
                                            , randomGames[j].Tm1, randomGames[j].Tm2))
                                        {
                                            // do nothing and move to next game
                                            // as one of the teams in this game are in this round already
                                        }
                                        else
                                        {
                                            // this game has two teams not yet in this round so add to round
                                            lastGmId++;
                                            randomGames[j].Id = lastGmId;
                                            randomGames[j].Round = numberOfRound;
                                            randomGames[j].GmDate = roundDate;
                                            roundGames[numberOfRound].Add(randomGames[j]);
                                            gameMessage += "Round " + numberOfRound + ": " + randomGames[j].Tm1 + ":" + randomGames[j].Tm2 + " / ";
                                            randomGames.RemoveAt(j);
                                        }
                                        if (roundGames[numberOfRound].Count == teams.Count / 2)
                                        {
                                            break;
                                        }
                                    }
                                    midweekRound++;
                                }

                                // try again to flatten and count
                                flattenedSchedule = [.. roundGames.SelectMany(s => s.Value)];
                                if (flattenedSchedule.Count != teams.Count * (teams.Count - 1))
                                {
                                    // if it hasn't failed this time, then report fail otherwise go ahead and save
                                    response.StatusCode = 500;
                                    response.Body = responseMessage;
                                    break;
                                }
                                else
                                {
                                    BatchWriteItemResponse writeResponse = await repository.WriteRecords("RbyGm", flattenedSchedule);
                                    response.StatusCode = 200;
                                    response.Body = gameMessage;
                                    break;
                                }
                            }
                            else
                            {
                                BatchWriteItemResponse writeResponse = await repository.WriteRecords("RbyGm", flattenedSchedule);
                                response.StatusCode = 200;
                                response.Body = gameMessage;
                                break;
                            }
                        default:
                            responseMessage = "scheduleRequest.Operation of: " + scheduleRequest.Operation + " not recognised";
                            context?.Logger.LogError(responseMessage);
                            response.StatusCode = 500;
                            response.Body = responseMessage;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    responseMessage = "Schedule has errored: " + ex.Message;
                    context?.Logger.LogError(responseMessage);
                    response.StatusCode = 500;
                    response.Body = responseMessage;
                }
            }
            return response;
        }

        public async Task<APIGatewayProxyResponse> TodaysGames(JToken serviceData, ILambdaContext? context)
        {
            APIGatewayProxyResponse response = new();
            var todaysGamesRequest = serviceData.ToObject<TodaysGamesRequest>();
            ScheduleBLL scheduleBLL = new(repository);

            if (todaysGamesRequest == null || todaysGamesRequest.Season == 0)
            {
                string message = "Request did not contain a proper todaysGamesRequest object";
                context?.Logger.LogInformation(message);
                response.StatusCode = 500;
                response.Body = message;
            }
            else
            {
                string responseMessage = "";
                try
                {
                    if (todaysGamesRequest.QuickCheck)
                    {
                        responseMessage += "\nQuick check for games for season " + todaysGamesRequest.Season + " gmdate " + todaysGamesRequest.GmDate.ToString();
                        DateTime? nextGameDate = await scheduleBLL.GetNextGameDate(todaysGamesRequest.Season, todaysGamesRequest.GmDate);
                        response.StatusCode = 200;
                        context?.Logger.LogInformation(responseMessage);
                        response.Body = JsonConvert.SerializeObject(nextGameDate);
                    }
                    else
                    {
                        responseMessage += "\nGetting games for season " + todaysGamesRequest.Season + " gmdate " + todaysGamesRequest.GmDate.ToString();
                        IEnumerable<Game> todaysGames = await scheduleBLL.GetTodaysGames(todaysGamesRequest.Season, todaysGamesRequest.GmDate) ?? throw new NullReferenceException("Can't get today's games");
                        response.StatusCode = 200;
                        context?.Logger.LogInformation(responseMessage);
                        response.Body = JsonConvert.SerializeObject(todaysGames);
                    }
                }
                catch (Exception ex)
                {
                    responseMessage += "\nTodaysGames has errored: " + ex.Message;
                    context?.Logger.LogError(responseMessage);
                    response.StatusCode = 500;
                    response.Body = responseMessage;
                }
            }
            return response;
        }

        public async Task<APIGatewayProxyResponse> Standings(JToken serviceData, ILambdaContext? context)
        {
            APIGatewayProxyResponse response = new();
            var standingsRequest = serviceData.ToObject<StandingsRequest>();
            ScheduleBLL scheduleBLL = new(repository);

            if (standingsRequest == null || standingsRequest.Season == 0)
            {
                string message = "Request did not contain a proper standingsRequest object";
                context?.Logger.LogInformation(message);
                response.StatusCode = 500;
                response.Body = message;
            }
            else
            {
                string responseMessage = "";
                try
                {
                    responseMessage += "\nGetting standings for season " + standingsRequest.Season;
                    IEnumerable<Record> standings = await scheduleBLL.GetStandings(standingsRequest.Season) ?? 
                        throw new NullReferenceException("Can't get standings for season " + standingsRequest.Season);
                    response.StatusCode = 200;
                    context?.Logger.LogInformation(responseMessage);
                    response.Body = JsonConvert.SerializeObject(standings);
                }
                catch (Exception ex)
                {
                    responseMessage += "\nStandings has errored: " + ex.Message;
                    context?.Logger.LogError(responseMessage);
                    response.StatusCode = 500;
                    response.Body = responseMessage;
                }
            }
            return response;
        }

        public async Task<APIGatewayProxyResponse> GenericCrud<T>(CrudRequest crudRequest, ILambdaContext? context)
        {
            APIGatewayProxyResponse response = new();
            string responseMessage = "";
            Type thisType = typeof(T);
            string tType = thisType.Name;

            try
            {
                switch (crudRequest.Crud)
                {
                    case "C":
                        responseMessage += "\nCreating new " + tType;
                        T? newItem = crudRequest.CrudData.ToObject<T>();
                        if (newItem == null)
                        {
                            responseMessage += "\nInvalid crud data " + crudRequest.CrudData;
                            response.StatusCode = 500;
                            break;
                        }
                        T? lastItem = await repository.GetLatestRecord<T>(crudRequest.TableName, "Id");
                        int newId = 1;
                        if (lastItem != null)
                        {
                            PropertyInfo? idProperty = thisType.GetProperty("Id");
                            if (idProperty != null)
                            {
                                object? idValue = idProperty.GetValue(lastItem);
                                if (idValue != null)
                                {
                                    bool parseInt = Int32.TryParse(idValue.ToString(), out int lastId);
                                    newId = lastId + 1;
                                }
                            }
                        }
                        thisType.GetProperty("Id")?.SetValue(newItem, newId);
                        PutItemResponse writeResponse = await repository.WriteRecord<T>(crudRequest.TableName, newItem);
                        responseMessage += "\nHttp response of write: " + writeResponse.HttpStatusCode.ToString();
                        response.StatusCode = writeResponse.HttpStatusCode.ToString() == "OK" ? 200 : 500;
                        break;
                    case "R":
                        responseMessage += "\nReading " + tType;
                        T? thisItem;
                        if (tType == "Game")
                        {
                            thisItem = await repository.GetTJoinedToX<T, Team>(crudRequest.Id, "RbyGm:RbyTm:Tm1/TeamName1/Id/Name:Tm2/TeamName2/Id/Name");
                        }
                        else
                        {
                            thisItem = await repository.GetRecordById<T>(crudRequest.Id, crudRequest.TableName);
                        }
                        if (thisItem == null)
                        {
                            responseMessage += "\nNo item found for Id: " + crudRequest.Id.ToString();
                        }
                        else
                        {
                            context?.Logger.LogInformation(responseMessage);
                            response.StatusCode = 200;
                            response.Body = JsonConvert.SerializeObject(thisItem);
                            return response;
                        }
                        break;
                    case "U":
                        responseMessage += "\nUpdating " + tType;
                        thisItem = await repository.GetRecordById<T>(crudRequest.Id, crudRequest.TableName);
                        newItem = crudRequest.CrudData.ToObject<T>();
                        if (newItem == null)
                        {
                            responseMessage += "\nInvalid crud data " + crudRequest.CrudData;
                            response.StatusCode = 500;
                            break;
                        }
                        writeResponse = await repository.WriteRecord<T>(crudRequest.TableName, newItem);
                        responseMessage += "\nHttp response of write: " + writeResponse.HttpStatusCode.ToString();
                        response.StatusCode = writeResponse.HttpStatusCode.ToString() == "OK" ? 200 : 500;
                        break;
                    case "D":
                        thisItem = await repository.GetRecordById<T>(crudRequest.Id, crudRequest.TableName);
                        if (thisItem == null)
                        {
                            responseMessage += "\nNo item found for Id: " + crudRequest.Id.ToString();
                        }
                        else
                        {
                            DeleteItemResponse deleteResponse = await repository.DeleteRecordById(crudRequest.TableName, crudRequest.Id);
                            responseMessage += "\nHttp response of delete: " + deleteResponse.HttpStatusCode.ToString();
                            response.StatusCode = deleteResponse.HttpStatusCode.ToString() == "OK" ? 200 : 500;
                        }
                        break;
                    case "RA":
                        // Read all
                        responseMessage += "\nReading all " + tType;
                        IEnumerable<T> items = await repository.GetAllRecords<T>(crudRequest.TableName);
                        if (items == null)
                        {
                            responseMessage += "\nNo items found for table: " + crudRequest.TableName;
                        }
                        else
                        {
                            context?.Logger.LogInformation(responseMessage);
                            response.StatusCode = 200;
                            response.Body = JsonConvert.SerializeObject(items);
                            return response;
                        }
                        break;
                    default:
                        responseMessage += "\nInvalid crud type " + crudRequest.Crud;
                        response.StatusCode = 500;
                        break;
                }
            }
            catch (Exception ex)
            {
                responseMessage += "\nGeneric Crud has errored: " + ex.Message;
                response.StatusCode = 500;
            }

            context?.Logger.LogInformation(responseMessage);
            response.Body = responseMessage;
            return response;
        }

        private static bool IsValXorYInCollectionAorB(IEnumerable<int> A, IEnumerable<int> B, int X, int Y)
        {
            return A.Contains(X) || A.Contains(Y) || B.Contains(X) || B.Contains(Y);
        }

        public static void CalculateColours(ref Game thisGame)
        {
			if (thisGame.HColour1 == thisGame.AColour2)
			{
				// clash of colours
				if (thisGame.HColour1 == thisGame.AColour1)
				{
					// another clash
					if (thisGame.HColour2 == thisGame.AColour2)
					{
						// another clash
						thisGame.Colours = [thisGame.HColour2, thisGame.AColour1];
						thisGame.ColourHex = [thisGame.HColourHex2, thisGame.AColourHex1];
					}
					else
					{
						thisGame.Colours = [thisGame.HColour2, thisGame.AColour2];
						thisGame.ColourHex = [thisGame.HColourHex2, thisGame.AColourHex2];
					}
				}
				else
				{
					thisGame.Colours = [thisGame.HColour1, thisGame.AColour1];
					thisGame.ColourHex = [thisGame.HColourHex1, thisGame.AColourHex1];
				}
			}
			else
			{
				// colours are fine
				thisGame.Colours = [thisGame.HColour1, thisGame.AColour2];
				thisGame.ColourHex = [thisGame.HColourHex1, thisGame.AColourHex2];
			}
		}
    }
}

