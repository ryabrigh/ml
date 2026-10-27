var plygameId;
var plyplayerTm;
var plyseason;
var plygmDate;
var eventsString;
let countdown = null;       // Interval ID
let timeLeft = 161;   // Time in half seconds
let isRunning = false;
var resultCache = null;
var impactMinutes = [];
var teamNames = [];
var speed = 750;
var speedIncrements = 250;
var maxSpeed = 3000;
var currentEventCounter = 0;
var loggedCurrentEvent = 0;
var timeIncrement = 1;
var completeEvent = false;
var runningScore1 = 0;
var runningScore2 = 0;
var keyPassData;

function LoadPlay(_keyPassData) {
	console.log(_keyPassData);
	plyplayerTm = _keyPassData[2];
	plygameId = _keyPassData[3];
	keyPassData = _keyPassData;
	LoadGame();
}

// Function to update the display
function updateDisplay() {
	//console.log("tick");
	var halfMinuteGone = 160 - timeLeft;
	var minuteGone = Math.round(halfMinuteGone / 2);
	$("#timer").html(minuteGone);
	const slider = document.getElementById('speedslider');
	slider.value = speed;

	$("#speed").html(speed);
	if (impactMinutes.includes(halfMinuteGone)) {
		//console.log("impactHalfMinutes: " + halfMinuteGone);
		var gameEvent = Enumerable.from(resultCache["GameEvents"]).where(x => x["HalfMinute"] == halfMinuteGone).toArray()[0];
		//console.log(gameEvent);
		if (gameEvent["HalfMinute"] > loggedCurrentEvent && !completeEvent) {
			//console.log("showing event for halfminute: " + halfMinuteGone);
			//console.log(gameEvent["HalfMinute"], loggedCurrentEvent);
			// show current event instead of going through time
			timeIncrement = 0;
			$("#leftpos").width(gameEvent["PosX"] + "%");
			completeEvent = ShowCurrentEvent(gameEvent);
			//console.log(completeEvent);
			if (completeEvent) {
				//console.log("completed");
				completeEvent = false;
				loggedCurrentEvent = gameEvent["HalfMinute"];

				//console.log("checking score event");
				var scoreEvent = IsScoreEvent(gameEvent["Sc1"], gameEvent["Sc2"]);
				if (scoreEvent > 0) {
					//console.log("updating score event");
					runningScore1 = gameEvent["Sc1"];
					runningScore2 = gameEvent["Sc2"];
					var scoreEventType = "";
					switch (gameEvent["EventType"]) {
						case "OP":
							scoreEventType = "Try";
							break;
						case "PK":
							scoreEventType = "Penalty";
							break;
						case "Conv":
							scoreEventType = "Conversion";
							break;
						default:
							break;
					}
					eventsString += minuteGone.toString().padStart(2, "0") + " : " + scoreEventType + " for " + teamNames[scoreEvent] + "\n";
					$("#gameEvents").html(eventsString.replace(/\n/g, "<br>"));
					var gameDetailsString = teamNames[1] + " " + gameEvent["Sc1"] + ": " + gameEvent["Sc2"] + " " + teamNames[2];
					$("#gameScore").html(gameDetailsString);

				}
				if (gameEvent["EventType"] != "HT") {
					// go through time
					//console.log("restarting time");
					timeIncrement = 1;
				}
				else {
					timeIncrement = 1;
					pauseTimer();
				}
			}

		}
	}
}

function IsScoreEvent(eventSc1, eventSc2) {
	return eventSc1 > runningScore1 ? 1 : eventSc2 > runningScore2 ? 2 : 0;
}

function ShowCurrentEvent(gameEvent) {
	//console.log(gameEvent);
	var currentEvents = gameEvent["Description"];

	if (currentEventCounter < currentEvents.length) {
		//console.log(currentEvents[currentEventCounter]);
		$("#currentEvent").html(currentEvents[currentEventCounter]);
		currentEventCounter++;
		if (currentEventCounter >= currentEvents.length) {
			//console.log("done showing event");
			currentEventCounter = 0;
			return true;
		}
		else {
			return false;
		}
	}
}

function LoadGame() {
	var serviceData = { "Crud": "R", "Id": plygameId, "TableName": "RbyGm", "CrudData": {}, "Model": "Game" };
	var request = { "serviceType": "CRUD", "serviceData": serviceData };

	if (local) {
		ShowGame("");
	}
	else {
		CallApi(request, ShowGame);
	}
}

function ShowGame(result) {
	//console.log(result);
	var data;
	if (local) {
		var data = JSON.parse(LocalTestGameData());
	}
	else {
		data = JSON.parse(result);
	}
	plyseason = data["Season"];
	plygmDate = data["GmDate"];
	//console.log(data);
	teamNames = ["", data["TeamName1"], data["TeamName2"]];
		var gameDetailsString = teamNames[1] + " 0 : 0 " + teamNames[2];
	$("#gameScore").html(gameDetailsString);
	eventsString = "Highlights\n---------------\n";
	resultCache = null;
	$("#gameEvents").html(eventsString.replace(/\n/g, "<br>"));
	$("#speed").html(speed);
}

$(document).on({
    ajaxStart: function() { $("body").addClass("loading");    },
     ajaxStop: function() { $("body").removeClass("loading"); }    
});

function Play() {
	var serviceData = { "GameId":plygameId, "Mode":"PlayByPlay" };
	var request = { "serviceType": "PlayGame", "serviceData": serviceData };
	if (local) {
		startTimer("");
	}
	else if (resultCache != null)
	{
		//console.log("resultCache");
		startTimer("");
	}
	else {
		//console.log("calling API");
		CallApi(request, startTimer);
	}
}

function startTimer(result) {
	var data;
	if (resultCache != null) {
		//console.log("getting data from resultcache")
		data = resultCache;
	}
	else if (local) {
		//console.log("getting data from testdata")
		var data = JSON.parse(LocalTestPlayByPlayDataQuick());
		resultCache = data[0];
	}
	else {
		//console.log("getting data from service")
		data = JSON.parse(result);
		resultCache = data[0];
	}
	$("#posBar").css("background-color", resultCache["ColourHex"][1]);
	$("#leftpos").css("background-color", resultCache["ColourHex"][0]);

	if (impactMinutes.length == 0) {
		impactMinutes = Enumerable.from(resultCache["GameEvents"]).select(x => x["HalfMinute"]).toArray();
		//console.log(impactMinutes);
	}

	if (!isRunning) {
		isRunning = true;
		countdown = setInterval(() => {
			if (timeLeft >= 0) {
				timeLeft = timeLeft - timeIncrement;
				updateDisplay();
			} else {
				clearInterval(countdown);
				isRunning = false;
				$("#returnToTodaysGames").removeClass("rbinvisible");
			}
		}, speed);
		//console.log(countdown);
	}
}

// Pause the timer
function pauseTimer() {
	//console.log(countdown);
	if (isRunning) {
		clearInterval(countdown);
		isRunning = false;
	}
}

function ShowEvents(result) {

	var eventsString = "";
	for (i = 0; i < data["GameEvents"].length; i++) {
		var thisEvent = data["GameEvents"][i];
		eventsString += thisEvent["EventType"] + " for " + thisEvent["TeamName"] + "\n";
		if (thisEvent["EventType"] == "Penalty Attempt") {
			eventsString += (thisEvent["Success"] ? ".. successful" : ".. failed") + "\n";
		}
	}
	//console.log(eventsString);
	$("#gameEvents").html(eventsString.replace(/\n/g, "<br>"));
}

// Adjust speed
function adjustSpeed() {
	clearInterval(countdown);
	startTimer(); // Restart with new speed
}

function Faster() {
	if (!isRunning) {
		if (speed - speedIncrements > 0) {
			speed = speed - speedIncrements;
			adjustSpeed();
		}
		else {
			// max speed
			speed = 100;
			adjustSpeed();
		}
	}
}

function Slower() {
	if (!isRunning) {
		if (speed + speedIncrements <= 2000) {
			speed = speed + speedIncrements;
			adjustSpeed();
		}
		else {
			// min speed
			speed = 2000;
			adjustSpeed();
		}
	}
}

function ReturnToTodaysGames() {
	// empty navMenuContent
	$("#navMenuContent").empty();

	// put the original divs in navMenuContent back
	jQuery('<div>', {
		id: 'navMenu',
		class: 'col-sm-3 no-padding',
	}).appendTo('#navMenuContent');
	jQuery('<div>', {
		id: 'navContent',
		class: 'col-sm-9 rbcontent no-padding',
	}).appendTo('#navMenuContent');

	// load the menu
	loadHTML("rby.web.menu.html", "navMenu", () => {
		//console.log('HTML content loaded into div.');
		LoadMenu(keyPassData);
	});

	// load today's games
	loadHTML("rby.web.todaysgames.html", "navContent", () => {
		//console.log('HTML content loaded into div.');
		LoadTodaysGames(keyPassData);
	});

}