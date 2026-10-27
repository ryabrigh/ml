function GetTooltip(request) {
	if (local) {
		ShowTooltip(request);
	}
	else {
		CallApi(request, ShowTooltip);
	}
}

function ShowTooltip(result) {
	//console.log(result);
	var data = JSON.parse(result);
	//console.log(data);
	var tooltipText1 = "";
	var tooltipText2 = "";
	if (data["Sc1"] != null) {
		// Sc1 is in the data so this is game data and tooltip should be game breakdown
		tooltipText1 = data["GmDateAsString"] + "\n" + data["TeamName1"] + "\nTries: " + data["Tries1"] + "\nPens: " + data["Penalties1"] + "\nConv: "
			+ data["Conversions1"] + "\nPts: " + data["Sc1"];
		tooltipText2 = "\n" + data["TeamName2"] + "\nTries: " + data["Tries2"] + "\nPens: " + data["Penalties2"] + "\nConv: "
			+ data["Conversions2"] + "\nPts: " + data["Sc2"];
	}
	$("#toolTipBox").removeClass("hideme");
	$("#toolTipBox").addClass("showme");
	$("#toolTipTm1").html(tooltipText1.replace(/\n/g, "<br>"));
	$("#toolTipTm2").html(tooltipText2.replace(/\n/g, "<br>"));
}