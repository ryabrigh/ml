var schseason;
var schoriginalSeason;

function LoadSchedule(keyPassData) {
	schseason = keyPassData[0];
	schoriginalSeason = keyPassData[0];
	GetSchedule();
}

$(document).on({
    ajaxStart: function() { $("body").addClass("loading");    },
     ajaxStop: function() { $("body").removeClass("loading"); }    
});

function GetSchedule() {
	var serviceData = { "Operation": "Get", "Season": schseason };
	var request = { "serviceType": "Schedule", "serviceData": serviceData };
	if (local) {
		ShowSchedule("");
	}
	else {
		CallApi(request, ShowSchedule);
	}
}

function ShowSchedule(result) {
	//console.log(result);
	var data;
	if (local) {
		var data = JSON.parse(LocalTestScheduleData());
	}
	else {
		data = JSON.parse(result);
	}
	//console.log(data);

	if (data.length == 0) {
		schseason = schoriginalSeason;
		return;
	}
	var firstGmYear = new Date(data[0]["GmDate"]).getFullYear();
	$("#season").html("Season: " + schseason + " - " + firstGmYear);
	$("#dataTable").html("");
	var table = CreateTable(data, ["LDate:K_GmDateAsString", "W10_TeamName1", "W5_Sc1", "W2_", "W5_Sc2", "W10_TeamName2"], "GetScheduleToolTip");
	table.classList.add("rbtable");
	document.getElementById("dataTable").appendChild(table);

	$("table td").addClass("rbcelllowrow");
	$("table tr").addClass("rbrow");
	$('[id^="keyField"]').addClass("rbcellkeyfield");
	$("table th").addClass("rbheadercellinvisible");

	// clear tooltip
	$("#toolTipBox").removeClass("showme");
	$("#toolTipBox").addClass("hideme");
}

function SchedulePrevious() {
	if (schseason > 1) {
		schseason--;
		GetSchedule();
	}
}

function ScheduleNext() {
	if (schseason < schoriginalSeason) {
		schseason++;
		GetSchedule();
	}
}

function GetScheduleToolTip(id) {
	var request;
	if (local) {
		request = LocalTestGameData();
	}
	else {
		var serviceData = { "Operation": "Get", "Season": schseason };
		request = {
			"serviceType": "CRUD", "serviceData": {
				"Crud": "R", "Id": id, "TableName": "RbyGm", "CrudData": {}, "Model": "Game"
			}
		};
	}
	GetTooltip(request);
}