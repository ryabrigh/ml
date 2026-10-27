using Amazon.DynamoDBv2.Model;
using Rby.Data;
using Rby.IData;
using Rby.Models;

namespace Rby.Service
{
    public class PlayBLL(IRepository repository)
	{
        private List<GameEvent> gameEvents = [];
        private readonly Random random = new();
        private int possession;
        private int posX;
        private int halfMinute;
        private int runningScore1;
        private int runningScore2;
        private string[] teamNames = [];
        private List<string> eventOutcomes = [];
        private int[] teamIds = [];
        private bool hadHalfTime;
        private int initPossession;
        private readonly IRepository _repository = repository;

		public async Task<(Game, PutItemResponse)> Play(Game game, string mode)
        {
            gameEvents = [];
            runningScore1 = 0;
            runningScore2 = 0;
            possession = random.Next(1, 3);
            initPossession = possession;
            posX = 50;
            eventOutcomes = [];
            hadHalfTime = false;

            return mode switch
            {
                "BasicRandom" => await BasicRandomPlay(game),
                "Simple" => await FullRandomPlay(game, mode),
                "SimpleMultiple" => await FullRandomPlay(game, mode),
                "SimpleMultipleTest" => await FullRandomPlay(game, mode),
                "Verbose" => await FullRandomPlay(game, mode),
                "VerboseTest" => await FullRandomPlay(game, mode),
                "PlayByPlay" => await PlayByPlay(game, mode),
                "PlayByPlayTest" => await PlayByPlay(game, mode),
                "PlayByPlayMultiple" => await PlayByPlay(game, mode),
                "PlayByPlayMultipleTest" => await PlayByPlay(game, mode),
                "PbpBulkTest" => await PlayByPlay(game, mode),
                _ => (new Game(), new PutItemResponse())
            };
        }

        public async Task<(Game, PutItemResponse)> BasicRandomPlay(Game game)
        {
            ArgumentNullException.ThrowIfNull(game);
            (int pens, int tries, int cons) scores = GetRandomScores(random);
            game.Penalties1 = scores.pens;
            game.Tries1 = scores.tries;
            game.Conversions1 = scores.cons;
            game.Sc1 = (scores.pens * 3) + (scores.tries * 5) + (scores.cons * 2);
            scores = GetRandomScores(random);
            game.Penalties2 = scores.pens;
            game.Tries2 = scores.tries;
            game.Conversions2 = scores.cons;
            game.Sc2 = (scores.pens * 3) + (scores.tries * 5) + (scores.cons * 2);
            game.Played = true;
            PutItemResponse response = await _repository.WriteRecord<Game>("RbyGm", game);
            return (game, response);
        }

        private static (int, int, int) GetRandomScores(Random random)
        {
            int randomPens = random.Next(0, 5); // 0 to 4 pens
            int randomTries = random.Next(0, 6); // 0 to 5 tries
            int randomConversions = randomTries == 0 ? 0 : random.Next(0, randomTries + 1); // 0 to randomTries conversions
            return (randomPens, randomTries, randomConversions);
        }

        public async Task<(Game, PutItemResponse)> FullRandomPlay(Game game, string mode)
        {
            // assumptions 2.75 tries per team per game
            // 1.6 penalties attempted per team per game with a 80% success rate
            // assume 80% rate also on try conversions

            // this means over an 80 minute game, there are 8.7 events which directly impact score

            ArgumentNullException.ThrowIfNull(game);
            var runningScore1 = 0;
            var runningScore2 = 0;

            for (halfMinute = 1; halfMinute < 161; halfMinute++)
            {
                int impactEvent = random.Next(1, 801);
                if (impactEvent <= 200)
                {
                    int teamEvent = random.Next(1, 3);
                    GameEvent gameEvent = new()
                    {
                        TeamId = teamEvent == 1 ? game.Tm1 : game.Tm2,
                        TeamName = teamEvent == 1 ? game.TeamName1 ?? "" : game.TeamName2 ?? "",
                        HalfMinute = halfMinute
                    };
                    if (impactEvent <= 25)
                    {
                        // penalty attempt
                        gameEvent.EventType = "Penalty Attempt";
                        gameEvent.Success = random.Next(1, 11) <= 8;
                        runningScore1 += teamEvent == 1 ? gameEvent.Success ? 3 : 0 : 0;
                        runningScore2 += teamEvent == 2 ? gameEvent.Success ? 3 : 0 : 0;
                    }
                    else if (impactEvent <= 80)
                    {
                        // try
                        bool conversion = random.Next(1, 11) <= 8;
                        gameEvent.EventType = conversion ? "Try Converted" : "Try";
                        runningScore1 += teamEvent == 1 ? conversion ? 7 : 5 : 0;
                        runningScore2 += teamEvent == 2 ? conversion ? 7 : 5 : 0;
                    }
                    else
                    {
                        // non-scoring event
                        GetRandomNonScoringEvent(ref gameEvent);
                    }
                    gameEvent.Sc1 = runningScore1;
                    gameEvent.Sc2 = runningScore2;
                    gameEvents.Add(gameEvent);
                }
            }
            game.Penalties1 = gameEvents.Where(e => e.TeamId == game.Tm1 && e.EventType == "Penalty Attempt" && e.Success).Count();
            game.Tries1 = gameEvents.Where(e => e.TeamId == game.Tm1 && (e.EventType == "Try" || e.EventType == "Try Converted")).Count();
            game.Conversions1 = gameEvents.Where(e => e.TeamId == game.Tm1 && e.EventType == "Try Converted").Count();
            game.Sc1 = (game.Penalties1 * 3) + (game.Tries1 * 5) + (game.Conversions1 * 2);
            game.Penalties2 = gameEvents.Where(e => e.TeamId == game.Tm2 && e.EventType == "Penalty Attempt" && e.Success).Count();
            game.Tries2 = gameEvents.Where(e => e.TeamId == game.Tm2 && (e.EventType == "Try" || e.EventType == "Try Converted")).Count();
            game.Conversions2 = gameEvents.Where(e => e.TeamId == game.Tm2 && e.EventType == "Try Converted").Count();
            game.Sc2 = (game.Penalties2 * 3) + (game.Tries2 * 5) + (game.Conversions2 * 2);
            game.Played = true;
            if (mode == "Verbose" || mode == "VerboseTest")
            {
                game.GameEvents = gameEvents;
            }
            PutItemResponse response = new();
            if (mode != "VerboseTest" && mode != "SimpleMultipleTest")
            {
                response = await _repository.WriteRecord<Game>("RbyGm", game);
            }
            else
            {
                response.HttpStatusCode = System.Net.HttpStatusCode.OK;
            }
            return (game, response);
        }

        public async Task<(Game, PutItemResponse)> PlayByPlay(Game game, string mode)
        {
            ArgumentNullException.ThrowIfNull(game);
            teamNames = ["", game.TeamName1 ?? "", game.TeamName2 ?? ""];
            teamIds = [0, game.Tm1, game.Tm2];
			string nextEvent = "KO";
            string[] longEvents = ["SC", "LO", "Pen", "Conv"];
            for (halfMinute = 1; halfMinute < 161; halfMinute++)
            {
                if (halfMinute >= 80 && !hadHalfTime)
                {
                    nextEvent = "HT";
                    hadHalfTime = true;
                }
                if (longEvents.Contains(nextEvent))
                {
                    halfMinute++;
                }
                nextEvent = nextEvent switch
                {
                    "KO" => KO(),
                    "OP" => OP(),
                    "SC" => SC(),
                    "LO" => LO(),
                    "RK" => RK(),
                    "Pen" => Pen(),
                    "Conv" => Conv(),
                    "HT" => HT(),
                    _ => ""
                    // throw new Exception("nextEvent of " + nextEvent + " not recognised"), // put this error throw back in when events are completed
                };
            }
            FT();
            game.Penalties1 = gameEvents.Where(e => e.TeamId == game.Tm1 && e.EventType == "PK" && e.Success).Count();
            game.Tries1 = gameEvents.Where(e => e.TeamId == game.Tm1 && e.IsTry).Count();
            game.Conversions1 = gameEvents.Where(e => e.TeamId == game.Tm1 && e.EventType == "Conv" && e.Success).Count();
            game.Sc1 = (game.Penalties1 * 3) + (game.Tries1 * 5) + (game.Conversions1 * 2);
            game.Penalties2 = gameEvents.Where(e => e.TeamId == game.Tm2 && e.EventType == "PK" && e.Success).Count();
            game.Tries2 = gameEvents.Where(e => e.TeamId == game.Tm2 && e.IsTry).Count();
            game.Conversions2 = gameEvents.Where(e => e.TeamId == game.Tm2 && e.EventType == "Conv" && e.Success).Count();
            game.Sc2 = (game.Penalties2 * 3) + (game.Tries2 * 5) + (game.Conversions2 * 2);
            game.Played = true;
            if (mode.Contains("Verbose") || mode.Contains("PlayByPlay") || mode.Contains("Pbp"))
            {
                game.GameEvents = gameEvents;
            }
            if (mode == "PbpBulkTest")
            {
				game.EventOutcomes = eventOutcomes;
			}
            PutItemResponse response = new();
            if (!mode.Contains("Test"))
            {
                response = await _repository.WriteRecord<Game>("RbyGm", game);
            }
            else
            {
                response.HttpStatusCode = System.Net.HttpStatusCode.OK;
            }
            return (game, response);
        }

        private string KO()
        {
            GameEvent thisEvent = new()
            {
                EventType = "KO",
                HalfMinute = halfMinute,
                Possession = possession,
                PosX = posX,
                TeamId = teamIds[possession]
            };
            int yardsK = random.Next(1, 3) * 20; // either 20 or 40
            posX = possession == 1 ? posX + yardsK : posX - yardsK;
            int outcomeRand = random.Next(1, 101);
            int outcome;
            string nextEvent;
            List<string> description = [];
            description.Add(teamNames[possession] + " kick off");
            description.Add("Kick travels " + yardsK + " yards");
            if (yardsK == 20)
            {
                eventOutcomes.Add("KO:20");
                outcome = outcomeRand switch
                {
                    <= 45 => 1,// kick team catch attempt
                    <= 90 => 2,// receive team catch attempt
                    _ => 3,// straight out for a throw to receive team
                };
            }
            else // yardsK is 40
            {
                eventOutcomes.Add("KO:40");
                outcome = outcomeRand switch
                {
                    <= 15 => 1,// kick team catch attempt
                    <= 85 => 2,// receive team catch attempt
                    _ => 3,// straight out for a throw to receive team
                };
            }

            int secondaryOutcome = random.Next(1, 101);
            switch (outcome)
            {
                case 1:
                    if (secondaryOutcome <= 70)
                    {
                        // kick team successful catch
                        eventOutcomes.Add("KO:kickteamcatchsuccessful");
                        description.Add("Caught by " + teamNames[possession]);
                        nextEvent = "OP";
                    }
                    else
                    {
                        // kick team drop catch
                        eventOutcomes.Add("KO:kickteamcatchdrop");
                        description.Add("Dropped by " + teamNames[possession]);
                        description.Add("There will be a scrum for " + teamNames[possession == 1 ? 2 : 1]);
                        nextEvent = "SC";
                        SwitchPossession();
                    }
                    break;
                case 2:
                    if (secondaryOutcome <= 70)
                    {
                        // receive team successful catch
                        eventOutcomes.Add("KO:defteamcatchsuccessful");
                        description.Add("Caught by " + teamNames[possession == 1 ? 2 : 1]);
                        nextEvent = "OP";
                        SwitchPossession();
                    }
                    else
                    {
                        // receive team drop catch
                        eventOutcomes.Add("KO:defteamcatchdrop");
                        description.Add("Dropped by " + teamNames[possession == 1 ? 2 : 1]);
                        description.Add("There will be a scrum for " + teamNames[possession]);
                        nextEvent = "SC";
                    }
                    break;
                case 3:
                    eventOutcomes.Add("KO:kickoutofplay");
                    description.Add("There will be a lineout for " + teamNames[possession == 1 ? 2 : 1]);
                    nextEvent = "LO";
                    SwitchPossession();
                    break;
                default:
                    throw new Exception("KO outcome of " + outcome + " not recognised");
            }
            thisEvent.Description = [.. description];
			thisEvent.Sc1 = runningScore1;
			thisEvent.Sc2 = runningScore2;
			gameEvents.Add(thisEvent);
            return nextEvent;
        }

        private string OP()
        {
            GameEvent thisEvent = new()
            {
                EventType = "OP",
                HalfMinute = halfMinute,
                Possession = possession,
                PosX = posX,
                TeamId = teamIds[possession]
            };
            string nextEvent = "";
            List<string> description = [];
            int decisionRand = random.Next(1, 101);
            int distanceToLine = possession == 2 ? posX : 100 - posX;
            int chanceOfKick = distanceToLine - 10; // so if you're 90y away from opp line, chance is 80, but if you're 20y, then chance is 10
            if (decisionRand <= chanceOfKick)
            {
                // kick
                eventOutcomes.Add("OP:kick");
                int minKick = distanceToLine >= 20 ? 10 : Math.Min(5, distanceToLine - 1);
                int maxKick = Math.Min(50, distanceToLine);
                int kickLength = random.Next(minKick, maxKick); // not +1 as we - for now - just want to max out at kicks to the 1
                int kickOutcome = random.Next(1, 101);
                description.Add("Player kicks ...");
                switch (kickOutcome)
                {
                    case <= 10:
                        // charge down
                        eventOutcomes.Add("OP:kickchargeddown");
                        description.Add("The kick is charged down!");
                        posX += possession == 1 ? -10 : 10;
                        int chargePoss = random.Next(1, 3);
                        description.Add("Recovered by " + (possession == chargePoss ? teamNames[possession] : teamNames[possession == 1 ? 2 : 1]));
                        if (possession != chargePoss)
                        {
                            possession = chargePoss;
                        }
                        nextEvent = "OP";
                        break;
                    case <= 60:
                        eventOutcomes.Add("OP:kickfortouch");
                        // kick for touch
                        KickForTouch(ref description, ref nextEvent, distanceToLine, false);
                        break;
                    case <= 100:
                        // box kick
                        eventOutcomes.Add("OP:boxkick");
                        description.Add("A box kick for " + kickLength + " metres");
                        posX += possession == 1 ? kickLength : kickLength * -1;
                        int catchPoss = random.Next(1, 3);
                        description.Add(teamNames[catchPoss] + " go up to catch");
                        int success = random.Next(1, 4);
                        if (success <= 2)
                        {
                            // successful catch
                            eventOutcomes.Add("OP:boxkickcaught");
                            possession = catchPoss;
                            description.Add("A successful catch so " + teamNames[possession] + " have the ball at the " 
                                + PosString());
                            nextEvent = "OP";
                        }
                        else
                        {
                            // dropped catch
                            eventOutcomes.Add("OP:boxkickdrop");
                            possession = catchPoss;
                            SwitchPossession();
                            description.Add("The catch is dropped so " + teamNames[possession] + " will have a scrum at the " 
                                + PosString());
                            nextEvent = "SC";
                        }
                        break;
                    default:
                        throw new Exception("kickoutcome of " + kickOutcome + " not recognised");
                }
            }
            else
            {
                // not a kick
                int runPass = random.Next(1, 3);
                switch (runPass)
                {
                    case 1:
                        // run
                        eventOutcomes.Add("OP:run");
                        Run(ref description, ref nextEvent, ref distanceToLine);
                        break;
                    case 2:
                        // pass
                        eventOutcomes.Add("OP:pass");
                        description.Add("Player is passing from the " + distanceToLine + "..");
                        int increment = 3;
                        for (int i = distanceToLine; i > 0; i -= increment)
                        {
                            int successFailRun = random.Next(1, 5);
                            if (successFailRun <= 2)
                            {
                                // success
                                eventOutcomes.Add("OP:passsucccess");
                                posX += possession == 1 ? increment : increment * -1;
                                distanceToLine = possession == 2 ? posX : 100 - posX;
                                description.Add(".. pass out and on to the " + distanceToLine + "..");
                            }
                            else if (successFailRun == 3)
                            {
                                // fail
                                eventOutcomes.Add("OP:passknockon");
                                description.Add(".. the ball is knocked on");
                                description.Add("There will be a scrum for " + teamNames[possession == 1 ? 2 : 1] 
                                    + " at the " + PosString());
                                nextEvent = "SC";
                                SwitchPossession();
                                break;
                            }
                            else
                            {
                                nextEvent = "PassRun";
                                break;
                            }
                        }
                        if (nextEvent == "PassRun")
                        {
                            eventOutcomes.Add("OP:passrun");
                            Run(ref description, ref nextEvent, ref distanceToLine);
                        }
                        break;
                    default:
                        throw new Exception("runPass of " + runPass + " not recognised");
                }
            }

            if (IsTry())
            {
                eventOutcomes.Add("OP:try");
                ReportTry(ref description, ref nextEvent);
                thisEvent.IsTry = true;
            }

            if (OwnGoal())
            {
                eventOutcomes.Add("OP:owngoal");
                description.Add(teamNames[possession] + " forced to ground the ball in their own goal");
                SwitchPossession();
                posX = possession == 2 ? 5 : 95;
                nextEvent = "SC";
            }
            thisEvent.Description = [.. description];
            thisEvent.Sc1 = runningScore1;
            thisEvent.Sc2 = runningScore2;
            gameEvents.Add(thisEvent);
            return nextEvent;
        }

        private string SC()
        {
            GameEvent thisEvent = new()
            {
                EventType = "SC",
                HalfMinute = halfMinute,
                Possession = possession,
                PosX = posX,
                TeamId = teamIds[possession]
            };
            string nextEvent;
            List<string> description = [];
            int scOutcome = random.Next(1, 101);

            description.Add("A scrum for " + teamNames[possession] + " at the " + PosString());
            switch (scOutcome)
            {
                case <= 10:
                    // penalty for possteam
                    eventOutcomes.Add("SC:posspen");
                    description.Add(teamNames[possession == 1 ? 2 : 1] + " infringe and there is a penalty for " + teamNames[possession]);
                    nextEvent = "Pen";
                    break;
                case <= 20:
                    // penalty for defteam
                    eventOutcomes.Add("SC:defpen");
                    description.Add(teamNames[possession] + " infringe and there is a penalty for " + teamNames[possession == 1 ? 2 : 1]);
                    SwitchPossession();
                    nextEvent = "Pen";
                    break;
                case <= 90:
                    // possteam wins
                    eventOutcomes.Add("SC:posswin");
                    description.Add(teamNames[possession] + " win the scrum");
                    nextEvent = "OP";
                    break;
                case <= 100:
                    // defteam wins
                    eventOutcomes.Add("SC:defwin");
                    description.Add(teamNames[possession == 1 ? 2 : 1] + " win the scrum against the head");
                    SwitchPossession();
                    nextEvent = "OP";
                    break;
                default:
                    throw new Exception("scOutcome of " + scOutcome + " not recognised");
            }
            thisEvent.Description = [.. description];
			thisEvent.Sc1 = runningScore1;
			thisEvent.Sc2 = runningScore2;
            gameEvents.Add(thisEvent);
			return nextEvent;
        }

        private string LO()
        {
            GameEvent thisEvent = new()
            {
                EventType = "LO",
                HalfMinute = halfMinute,
                Possession = possession,
                PosX = posX,
                TeamId = teamIds[possession]
            };
            string nextEvent;
            List<string> description = [];
            int loOutcome = random.Next(1, 101);

            description.Add("A lineout for " + teamNames[possession] + " at the " + PosString());
            switch (loOutcome)
            {
                case <= 10:
                    // penalty for possteam
                    eventOutcomes.Add("LO:posspen");
                    description.Add(teamNames[possession == 1 ? 2 : 1] + " infringe and there is a penalty for " + teamNames[possession]);
                    nextEvent = "Pen";
                    break;
                case <= 20:
                    // penalty for defteam
                    eventOutcomes.Add("LO:defpen");
                    description.Add(teamNames[possession] + " infringe and there is a penalty for " + teamNames[possession == 1 ? 2 : 1]);
                    SwitchPossession();
                    nextEvent = "Pen";
                    break;
                case <= 80:
                    // possteam wins
                    eventOutcomes.Add("LO:posswin");
                    description.Add(teamNames[possession] + " win the lineout");
                    nextEvent = "OP";
                    break;
                case <= 100:
                    // defteam wins
                    eventOutcomes.Add("LO:defwin");
                    description.Add(teamNames[possession == 1 ? 2 : 1] + " win the lineout against the throw");
                    SwitchPossession();
                    nextEvent = "OP";
                    break;
                default:
                    throw new Exception("loOutcome of " + loOutcome + " not recognised");
            }
            thisEvent.Description = [.. description];
			thisEvent.Sc1 = runningScore1;
			thisEvent.Sc2 = runningScore2;
			gameEvents.Add(thisEvent);
            return nextEvent;
        }

        private string RK()
        {
            GameEvent thisEvent = new()
            {
                EventType = "RK",
                HalfMinute = halfMinute,
                Possession = possession,
                PosX = posX,
                TeamId = teamIds[possession]
            };
            string nextEvent;
            List<string> description = [];
            int rkOutcome = random.Next(1, 101);

            description.Add("A ruck at the " + PosString());
            switch (rkOutcome)
            {
                case <= 10:
                    eventOutcomes.Add("RK:posspen");
                    description.Add(teamNames[possession == 1 ? 2 : 1] + " infringe and there is a penalty for " + teamNames[possession]);
                    nextEvent = "Pen";
                    break;
                case <= 20:
                    // penalty for defteam
                    eventOutcomes.Add("RK:defpen");
                    description.Add(teamNames[possession] + " infringe and there is a penalty for " + teamNames[possession == 1 ? 2 : 1]);
                    SwitchPossession();
                    nextEvent = "Pen";
                    break;
                case <= 90:
                    // possteam wins
                    eventOutcomes.Add("RK:posswin");
                    description.Add(teamNames[possession] + " win the ruck");
                    nextEvent = "OP";
                    break;
                case <= 100:
                    // defteam wins
                    eventOutcomes.Add("RK:defwin");
                    description.Add(teamNames[possession == 1 ? 2 : 1] + " with the turnover!");
                    SwitchPossession();
                    nextEvent = "OP";
                    break;
                default:
                    throw new Exception("rkOutcome of " + rkOutcome + " not recognised");
            }
            thisEvent.Description = [.. description];
			thisEvent.Sc1 = runningScore1;
			thisEvent.Sc2 = runningScore2;
			gameEvents.Add(thisEvent);
            return nextEvent;
        }

        private string Pen()
        {
            // can include score considerations in here later

            GameEvent thisEvent = new()
            {
                EventType = "Pen",
                HalfMinute = halfMinute,
                Possession = possession,
                PosX = posX,
                TeamId = teamIds[possession]
            };
            string nextEvent = "";
            List<string> description = [];
            int distanceToLine = possession == 2 ? posX : 100 - posX;

            // TODO: score and time driver here
            // if late and close more likely to PK
            // if late and far more likely to KFT or Run

            switch (distanceToLine)
            {
                case <= 10:
                    // run or kick for touch
                    int runOrKick = random.Next(1, distanceToLine + 1);
                    if (runOrKick <= distanceToLine / 2)
                    {
                        // kick
                        eventOutcomes.Add("Pen:kickfortouch");
                        KickForTouch(ref description, ref nextEvent, distanceToLine, true);
                    }
                    else
                    {
                        // run
                        eventOutcomes.Add("Pen:run");
                        Run(ref description, ref nextEvent, ref distanceToLine);
                        if (IsTry())
                        {
                            eventOutcomes.Add("Pen:runtry");
                            ReportTry(ref description, ref nextEvent);
                            thisEvent.IsTry = true;
                        }
                    }
                    break;
                case <= 50:
                    // pk or kick for touch
                    int kickOrPk = random.Next(1, 11);
                    if (kickOrPk <= (60 - distanceToLine) / 4)
                    {
                        // pk
                        eventOutcomes.Add("Pen:PK");
                        thisEvent.EventType = "PK";
                        KickForGoal(ref thisEvent, ref description, ref nextEvent, distanceToLine);
                    }
                    else
                    {
                        // kick for touch
                        eventOutcomes.Add("Pen:kickfortouch");
                        KickForTouch(ref description, ref nextEvent, distanceToLine, true);
                    }
                    break;
                default:
                    // kick for touch
                    eventOutcomes.Add("Pen:kickfortouch");
                    KickForTouch(ref description, ref nextEvent, distanceToLine, true);
                    break;
            }

            thisEvent.Sc1 = runningScore1;
            thisEvent.Sc2 = runningScore2;
            thisEvent.Description = [.. description];
            gameEvents.Add(thisEvent);
            return nextEvent;
        }

        private string Conv()
        {
            // can include score considerations in here later

            GameEvent thisEvent = new()
            {
                EventType = "Conv",
                HalfMinute = halfMinute,
                Possession = possession,
                PosX = posX,
                TeamId = teamIds[possession]
            };
            eventOutcomes.Add("Conv");
            string nextEvent = "";
            List<string> description = [];
            int distanceToLine = 20;

            KickForGoal(ref thisEvent, ref description, ref nextEvent, distanceToLine);

            thisEvent.Sc1 = runningScore1;
            thisEvent.Sc2 = runningScore2;
            thisEvent.Description = [.. description];
            gameEvents.Add(thisEvent);
            return nextEvent;
        }

        private string HT()
        {
            possession = initPossession == 1 ? 2 : 1;
            posX = 50;
            GameEvent thisEvent = new()
            {
                EventType = "HT",
                HalfMinute = halfMinute,
                Possession = possession,
                PosX = posX,
                TeamId = teamIds[possession]
            };
            eventOutcomes.Add("HT");
            string nextEvent = "KO";
            List<string> description = [];

            description.Add(" .. and that's half time");
            description.Add("With the score as: " + teamNames[1] + " " + runningScore1 + " - " + runningScore2 + " " + teamNames[2]);

            thisEvent.Sc1 = runningScore1;
            thisEvent.Sc2 = runningScore2;
            thisEvent.Description = [.. description];
            gameEvents.Add(thisEvent);
            return nextEvent;
        }

        private void FT()
        {
            GameEvent thisEvent = new()
            {
                EventType = "FT",
                HalfMinute = halfMinute,
                Possession = possession,
                PosX = posX,
                TeamId = teamIds[possession]
            };
            eventOutcomes.Add("FT");
            List<string> description = [];

            description.Add(" .. and that's full time");
            description.Add("With the final score as: " + teamNames[1] + " " + runningScore1 + " - " + runningScore2 + " " + teamNames[2]);

            thisEvent.Sc1 = runningScore1;
            thisEvent.Sc2 = runningScore2;
            thisEvent.Description = [.. description];
            gameEvents.Add(thisEvent);
        }

        private void Run (ref List<string> description, ref string nextEvent, ref int distanceToLine)
        {
            int increment = 5;
            description.Add("Player is running from the " + distanceToLine + " ..");
            int yardsGained = 0;
            for (int i = distanceToLine; i > 0; i -= increment)
            {
                if (IsTry())
                {
                    eventOutcomes.Add("Run:Try");
                    break;
                }
                int success = random.Next(1, 4);
                if (success == 3)
                {
                    // run stopped
                    eventOutcomes.Add("Run:RK");
                    description.Add("Brought down at the " + PosString());
                    nextEvent = "RK";
                    break;
                }
                else
                {
                    // success
                    posX += possession == 1 ? increment : increment * -1;
                    yardsGained += increment;
                    if (!IsTry())
                    {
                        distanceToLine = possession == 2 ? posX : 100 - posX;
                        description.Add(".. to the " + distanceToLine + "..");
                    }
                }
            }
            eventOutcomes.Add("Run:yardsgained:" + yardsGained);
        }

        private void KickForTouch(ref List<string> description, ref string nextEvent, int distanceToLine, bool pen)
        {
            int minKick = distanceToLine >= 20 ? 10 : Math.Min(5, distanceToLine - 1);
            int maxKick = Math.Min(50, distanceToLine);
            int kickLength = random.Next(minKick, maxKick); // not +1 as we - for now - just want to max out at kicks to the 1

            description.Add("A kick for touch");
            posX += possession == 1 ? kickLength : kickLength * -1;
            int touchSuccess = random.Next(1, kickLength);
            if (touchSuccess <= 20)
            {
                // successfully made touch
                eventOutcomes.Add("Kickfortouch:success");
                description.Add("Successfully found touch");
                description.Add("A kick of " + kickLength + " metres for a lineout at the " + PosString());
                if (!pen)
                {
                    SwitchPossession();
                }
                nextEvent = "LO";
            }
            else
            {
                // missed touch
                eventOutcomes.Add("Kickfortouch:fail");
                description.Add("The kick has missed touch");
                possession = possession == 1 ? 2 : 1;
                description.Add("A kick of " + kickLength + " metres but now " + teamNames[possession]
                    + " have the ball at the " + PosString());
                nextEvent = "OP";
            }
        }

        private void KickForGoal(ref GameEvent thisEvent, ref List<string> description, ref string nextEvent, int distanceToLine)
        {
            string initDescription = teamNames[possession] + " going for a" + 
                (thisEvent.EventType == "PK" ? " penalty kick" : " conversion");
            description.Add(initDescription);
            description.Add("The kick is ...");
            int success = random.Next(1, distanceToLine);
            if (success <= 15)
            {
                // success
                eventOutcomes.Add("Kickforgoal:success");
                int points = thisEvent.EventType == "PK" ? 3 : 2;
                thisEvent.Success = true;
                description.Add("GOOD! " + points + " points for " + teamNames[possession]);
                runningScore1 += possession == 1 ? points : 0;
                runningScore2 += possession == 2 ? points : 0;
                posX = 50;
                nextEvent = "KO";
                SwitchPossession();
            }
            else
            {
                // failure
                eventOutcomes.Add("Kickforgoal:fail");
                description.Add("No good");
                nextEvent = "KO";
                SwitchPossession();
                int koYard = thisEvent.EventType == "PK" ? 20 : 50;
                posX = possession == 1 ? koYard : 100 - koYard;
            }
        }

        private bool IsTry()
        {
            return possession == 1 ? posX >= 100 : posX <= 0;
        }

        private bool OwnGoal()
        {
            return possession == 1 ? posX <= 0 : posX >= 100;
        }

        private string PosString()
        {
            return 
                posX == 50 ? "halfway line" :
                posX < 50 ? posX + " line of " + teamNames[1] :
                (100 - posX) + " line of " + teamNames[2];
        }

        private void SwitchPossession()
        {
            possession = possession == 1 ? 2 : 1;
        }

        private void ReportTry(ref List<string> description, ref string nextEvent)
        {
            description.Add("And it's a TRY for " + teamNames[possession]);
            runningScore1 += possession == 1 ? 5 : 0;
            runningScore2 += possession == 2 ? 5 : 0;
            nextEvent = "Conv";
            posX = possession == 1 ? 80 : 20;
        }

        private void GetRandomNonScoringEvent(ref GameEvent gameEvent)
        {
            int eventTypeNum = random.Next(1, 101);
            string eventType;
            if (eventTypeNum <= 20)
            {
                eventType = "Goal Line Attack";
            }
            else if (eventTypeNum <= 40)
            {
                eventType = "22 Attack";
            }
            else if (eventTypeNum <= 60)
            {
                eventType = "Midfield Attack";
            }
            else if (eventTypeNum <= 80)
            {
                eventType = "Own half kick for touch";
            }
            else 
            {
                eventType = "Turnover";
            }
            gameEvent.EventType = eventType;
        }
    }
}
