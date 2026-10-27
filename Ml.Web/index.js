var idxseason;
var idxgmDate;
var idxplayerTm;
var idxgameId;
var idxsaveDateAsString;
var idxsaveName;

window.onload = function () {
	console.log("local is " + local);
	Load();
}

$(document).on({
    ajaxStart: function() { $("body").addClass("loading");    },
     ajaxStop: function() { $("body").removeClass("loading"); }    
});

function Load() {
	//console.log("getting menu");
    GetSave();
}

function GetSave() {
	var serviceData = { "Crud": "R", "Id": "1", "TableName": "RbySave", "CrudData": {}, "Model": "Save" };
	var request = { "serviceType": "CRUD", "serviceData": serviceData };

	if (local) {
		SetSaveData("");
	}
	else {
		CallApi(request, SetSaveData);
	}
}

function SetSaveData(result) {
	// a read so populate all variables
	var data;
	if (local) {
		var data = JSON.parse(LocalTestSaveData());
	}
	else {
		data = JSON.parse(result);
	}
	//console.log(data);
	idxseason = data["Season"];
	idxgmDate = data["GmDate"]
	idxsaveName = data["Name"];
	idxplayerTm = data["PlayerTm"];
	idxgameId = 0;
	idxsaveDateAsString = data["SaveDateAsString"];
	var keyPassData = [idxseason, idxgmDate, idxplayerTm, idxgameId, idxsaveName, idxsaveDateAsString];

	loadHTML("rby.web.footer.html", "navFooter", () => {
		//console.log('HTML content loaded into div.');
		LoadFooter(keyPassData);
	});

	loadHTML("rby.web.menu.html", "navMenu", () => {
		//console.log('HTML content loaded into div.');
		LoadMenu(keyPassData);
	});
}
