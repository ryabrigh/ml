var tgseason;
var tggmDate;
var tgplayerTm;
var playerGames = [];
var nonPlayerGames = [];
var todaysGames;
var tgkeyPassData;

function LoadTodaysGames(_keyPassData) {
	//console.log(keyPassData);
	tgseason = _keyPassData[0];
	tggmDate = _keyPassData[1];
	tgplayerTm = _keyPassData[2];
	tgkeyPassData = _keyPassData;
	GetTodaysGames();
}

$(document).on({
    ajaxStart: function() { $("body").addClass("loading");    },
     ajaxStop: function() { $("body").removeClass("loading"); }    
});

function GetTodaysGames() {
	var serviceData = { "GMDate": tggmDate, "Season":tgseason };
	var request = { "serviceType": "TodaysGames", "serviceData": serviceData };
	if (local) {
		ShowTodaysGames("");
	}
	else {
		CallApi(request, ShowTodaysGames);
	}
}

function ShowTodaysGames(result) {
	var data;
	if (local) {
		var data = JSON.parse(LocalTestTodaysGamesData());
	}
	else {
		//console.log(result);
		data = JSON.parse(result);
	}
	data.forEach(function (item) {
		item["TeamName1"] = item["TeamName1"] + " (" + item["Rank1"] + ")";
		item["TeamName2"] = item["TeamName2"] + " (" + item["Rank2"] + ")";
		if (!item["Played"]) {
			item["Play"] = "Play";
			if (item["Tm1"] == tgplayerTm || item["Tm2"] == tgplayerTm) {
				playerGames.push(item["Id"]);
			}
			else {
				nonPlayerGames.push(item["Id"]);
			}
			$("#playAllButton").removeAttr("hidden");
		}
		else {
			item["Play"] = "";
		}
	});
	//console.log(data);
	//console.log(tggmDate);
	//console.log(tgplayerTm);
	//console.log(playerGames);
	todaysGames = data;
	$("#GmDate").html("Games for: " + toLongDate(new Date(tggmDate)));
	CreateTodaysGamesTable(data);
}

function UpdateTodaysGames(result) {
	var data;
	if (local) {
		var data = JSON.parse(LocalTestMultipleData());
	}
	else {
		//console.log(result);
		data = JSON.parse(result);
	}
	todaysGames.forEach(function (game) {
		data.forEach(function (updatedGame) {
			if (game["Id"] == updatedGame["Id"]) {
				game["Sc1"] = updatedGame["Sc1"];
				game["Sc2"] = updatedGame["Sc2"];
				game["Played"] = true;
				game["Play"] = "";
			}
		});
	});
	//console.log(data);
	//console.log(todaysGames);
	$("#dataTable").html("");
	CreateTodaysGamesTable(todaysGames);
}

function CreateTodaysGamesTable(data) {
	$("#dataTable").html("");
	var table = CreateTable(data, ["W7_TeamName1", "W2_Sc1", "W1_", "W2_Sc2", "W7_TeamName2", "W3:U:Crbtableclick_Play"], "");
	table.classList.add("rbtable");
	document.getElementById("dataTable").appendChild(table);

	$("table td").addClass("rbcellnoborder");
	$("table tr").addClass("rbrow");
	$('[id^="keyField"]').addClass("rbcellkeyfield");
	$("table th").addClass("rbheadercellinvisible");
}

function PlayClick(id) {
	console.log("PlayClick " + id);
	if (playerGames.includes(id)) {
		tgkeyPassData[3] = id;
		loadHTML("rby.web.play.html", "navMenuContent", () => {
			console.log('HTML content loaded into content.');
			LoadPlay(tgkeyPassData);
		});
	}
	else {
		var serviceData = { "GameId": id, "Mode": "PlayByPlay" };
		var request = { "serviceType": "PlayGame", "serviceData": serviceData };
		if (local) {
			UpdateTodaysGames("");
		}
		else {
			CallApi(request, UpdateTodaysGames);
		}
	}
}

function PlayAll() {
	//console.log(todaysGames.length);
	//console.log(nonPlayerGames);
	if (nonPlayerGames.length > 0) {
		var serviceData = { "GameId": 0, "Mode": "PlayByPlayMultiple", "GameIds": nonPlayerGames };
		var request = { "serviceType": "PlayGame", "serviceData": serviceData };
		if (local) {
			UpdatePlayAllGamesOrGoToPlayerGame("");
		}
		else {
			CallApi(request, UpdatePlayAllGamesOrGoToPlayerGame);
		}
	}
	else {
		UpdatePlayAllGamesOrGoToPlayerGame(nonPlayerGames);
	}
}

function UpdatePlayAllGamesOrGoToPlayerGame(result) {
	$("#playAllButton").attr("hidden", "hidden");
	if (playerGames.length > 0) {
		console.log("Going to player game:" + playerGames[0]);
		PlayClick(playerGames[0]);
	}
	else {
		console.log("No player games so updating table");
		UpdateTodaysGames(result);
	}
}
