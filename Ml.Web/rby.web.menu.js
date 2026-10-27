var mnuseason;
var mnugmDate;
var saveName;
var mnuplayerTm;
var saveDateAsString;
var mnugameId;

function LoadMenu(_keyPassData) {
	//console.log("running Menu load");
	mnuseason = _keyPassData[0];
	mnugmDate = _keyPassData[1];
	mnuplayerTm = _keyPassData[2];
	mnugameId = _keyPassData[3];
}

$(document).on({
    ajaxStart: function() { $("body").addClass("loading");    },
     ajaxStop: function() { $("body").removeClass("loading"); }    
});

function GetNextGame() {
	var serviceData = { "GMDate": mnugmDate, "Season": mnuseason, "QuickCheck": true };
	var request = { "serviceType": "TodaysGames", "serviceData": serviceData };

	if (local) {
		ActOnNextGameData("");
	}
	else {
		CallApi(request, ActOnNextGameData);
	}
}

function Nav(target, loadFunction) {
	var keyPassData = [mnuseason, mnugmDate, mnuplayerTm, mnugameId];
	//console.log(target, loadFunction);
	loadHTML(target, "navContent", () => {
		//console.log('HTML content loaded into content.');
		executeCallback(loadFunction, keyPassData);
	});
}

function Proceed() {
	console.log("Proceed")
	GetNextGame();
}

function SaveGame() {
	console.log("Save Game")
}

function ActOnNextGameData(result) {
	var data;
	if (local) {
		var data = JSON.parse(LocalTestNoNextGameData());
	}
	else {
		console.log(result);
		data = JSON.parse(result);
	}
	console.log(data);
	if (data == null) {
		$("#NextGame").html("No games left .. create new season?");
		$("#CreateSchedule").removeAttr("hidden");
	}
	else {
		//$("#NextGame").html("Next game is: " + formatDateDDMMYYYY(new Date(data)));
		var nextGameDate = new Date(data);
		var saveDate = new Date(mnugmDate);
		if (nextGameDate.getTime() != saveDate.getTime()) {
			// move saveDate on
			mnugmDate = data;
			SetSave();
		}
		// go to today's games
		var keyPassData = [mnuseason, mnugmDate, mnuplayerTm, mnugameId];
		//console.log(target, loadFunction);
		loadHTML("rby.web.todaysgames.html", "navContent", () => {
			console.log('HTML content loaded into content.');
			executeCallback(LoadTodaysGames, keyPassData);
		});
	}
}
function SetSave() {
	var serviceData = {
		"Crud": "U", "Id": "1", "TableName": "RbySave",
		"CrudData": { "Id": "1", "GmDate": mnugmDate, "Season": mnuseason, "Name": "RyanRby3", "PlayerTm": mnuplayerTm }, "Model": "Save"
	};
	var request = { "serviceType": "CRUD", "serviceData": serviceData };

	if (local) {
		UpdateSaveData("");
	}
	else {
		CallApi(request, UpdateSaveData);
	}
}

function CreateSchedule() {
	$("#NextGame").html("");
	$("#CreateSchedule").hide();

	season++;
	var newStartDate = new Date(mnugmDate);
	var year = newStartDate.getFullYear();
	const septemberFirst = new Date(year, 8, 1); // Month is 0-based: 8 = September

	// If the given date is on or after September 1st, move to next year
	if (newStartDate >= septemberFirst) {
		year++;
	}

	mnugmDate = new Date(year, 8, 1, 10, 0, 0); // set to 10am to avoid utc issues
	var serviceData = { "Operation": "Create", "Season": mnuseason, "StartDate": mnugmDate };
	var request = { "serviceType": "Schedule", "serviceData": serviceData };

	if (local) {
		SetSave("");
	}
	else {
		CallApi(request, SetSave);
	}
}

function UpdateSaveData(result) {
	console.log(result);
	if (result == "\nUpdating Save\nHttp response of write: OK") {
		// this is as a result of an update, so just update the season and save date
		saveDateAsString = formatDateDDMMYYYY(new Date(mnugmDate));
	}
	else {
		// a read so populate all variables
		var data;
		if (local) {
			var data = JSON.parse(LocalTestSaveData());
		}
		else {
			data = JSON.parse(result);
		}
		console.log(data);
		mnuseason = data["Season"];
		mnugmDate = data["GmDate"]
		saveName = data["Name"];
		mnuplayerTm = data["PlayerTm"];
		mnugameId = 0;
		saveDateAsString = data["SaveDateAsString"];
	}
	var saveDetailsString = "Name: " + saveName + " Team: " + mnuplayerTm + " Season: " + mnuseason + " Game date: " + saveDateAsString;
	$("#SaveDetails").html(saveDetailsString);
}