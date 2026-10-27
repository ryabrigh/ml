var stdseason;
var stdoriginalSeason;

function LoadStandings(keyPassData) {
	stdseason = keyPassData[0];
	stdoriginalSeason = keyPassData[0];
	GetStandings();
}

$(document).on({
    ajaxStart: function() { $("body").addClass("loading");    },
     ajaxStop: function() { $("body").removeClass("loading"); }    
});

function GetStandings() {
	var serviceData = { "Season": stdseason };
	var request = { "serviceType": "Standings", "serviceData": serviceData };
	if (local) {
		ShowStandings("");
	}
	else {
		CallApi(request, ShowStandings);
	}
}

function ShowStandings(result) {
	console.log(result);
	var data;
	if (local) {
		var data = JSON.parse(LocalTestStandingsData());
	}
	else {
		data = JSON.parse(result);
	}
	console.log(data);

	if (data.length == 0) {
		stdseason = stdoriginalSeason;
		return;
	}
	var firstRecordYear = data[0]["Year"];
	$("#season").html("Season: " + stdseason + " - " + firstRecordYear);

	$("#dataTable").html("");
	var table = CreateTable(data, ["W5_Pos", "W10_TeamName", "W5_P", "W5_W", "W5_D", "W5_L", "W5_F", "W5_A", "W5_BP", "W5_Pts" ], "");
	table.classList.add("rbtablethin");
	document.getElementById("dataTable").appendChild(table);

	$("table td").addClass("rbcell");
	$('[id^="keyField"]').addClass("rbcellkeyfield");
	$("table th").addClass("rbheadercell");
}

function StandingsPrevious() {
	if (stdseason > 1) {
		stdseason--;
		GetStandings();
	}
}

function StandingsNext() {
	if (stdseason < stdoriginalSeason) {
		stdseason++;
		GetStandings();
	}
}