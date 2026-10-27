var ftseason;
var ftgmDate;
var ftsaveName;
var ftplayerTm;
var ftsaveDateAsString;
var ftgameId;

function LoadFooter(_keyPassData) {
	//console.log("running Footer load");
	//console.log(_keyPassData);
	ftseason = _keyPassData[0];
	ftgmDate = _keyPassData[1];
	ftplayerTm = _keyPassData[2];
	ftgameId = _keyPassData[3];
	ftsaveName = _keyPassData[4];
	ftsaveDateAsString = _keyPassData[5];

	ShowSaveData();
}

$(document).on({
    ajaxStart: function() { $("body").addClass("loading");    },
     ajaxStop: function() { $("body").removeClass("loading"); }    
});

function ShowSaveData() {
	var saveDetailsString = "Name: " + ftsaveName + " Team: " + ftplayerTm + " Season: " + ftseason + " Game date: " + ftsaveDateAsString;
	$("#SaveDetails").html(saveDetailsString);
}
