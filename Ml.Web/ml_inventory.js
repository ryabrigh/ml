var rowChanged = [];
var config;

window.onload = function(){
	GetConfig();
	GetAll();
}

$(document).on({
    ajaxStart: function() { $("body").addClass("loading");    },
     ajaxStop: function() { $("body").removeClass("loading"); }    
});

function GetConfig(){
	$.ajax({
		url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/config",
		type: "GET",
		success: function(result) {
			config = result[0];
			//console.log(config["inventoryDate"]);
			$("#inventoryDate").val(config["inventoryDate"]);
		},
		error: function (xhr, ajaxOptions, thrownError) {
			alert(xhr.responseText);
		}
	});
}

function GetAll(){
	rowChanged = [];
	var sortField = $("#sortField").html(); 
	var sortItems = sortField.split("_");
	var sortIcon = " <span class='glyphicon glyphicon-triangle-" + (sortItems[1] == "desc" ? "bottom" : "top") + "'></span>" 
	$.ajax({
		url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/products/sort/" + sortField,
		type: "GET",
		success: function(result) {
			$("#dataTable").html("");
			var table = document.createElement("table"), row, cellA;
			table.classList.add("mltable");
			document.getElementById("dataTable").appendChild(table);
			var header = table.createTHead();
			var headerRow = header.insertRow(0);
			cellA = document.createElement("th");
			var thisFieldName = "name";
			cellA.setAttribute('onclick', 'SetSortField("' + thisFieldName + '")');
			cellA.innerHTML = thisFieldName.toUpperCase() + (sortItems[0] == thisFieldName ? sortIcon : "");
			headerRow.appendChild(cellA);
			cellA = document.createElement("th");
			var thisFieldName = "amount";
			cellA.setAttribute('onclick', 'SetSortField("' + thisFieldName + '")');
			cellA.innerHTML = thisFieldName.toUpperCase() + (sortItems[0] == thisFieldName ? sortIcon : "");
			headerRow.appendChild(cellA);
			cellA = document.createElement("th");
			var thisFieldName = "portionName";
			cellA.setAttribute('onclick', 'SetSortField("' + thisFieldName + '")');
			cellA.innerHTML = "PORTION" + (sortItems[0] == thisFieldName ? sortIcon : "");
			headerRow.appendChild(cellA);
			cellA = document.createElement("th");
			var thisFieldName = "areaName";
			cellA.setAttribute('onclick', 'SetSortField("' + thisFieldName + '")');
			cellA.innerHTML = "AREA" + (sortItems[0] == thisFieldName ? sortIcon : "");
			headerRow.appendChild(cellA);
			
			var body = table.createTBody(0);
			for (i = 0; i < result.length; i++) {
				var thisActive = result[i]["active"];
				if (thisActive) {
					row = body.insertRow();
					var thisId;
					var thisName;
					var thisOrder;
					var thisAmount;
					var thisPortion;
					var thisArea;
					var thisStore;
					//need to manually do it rather than iterate to get the order we need
					//for (let key in result[i]) {
					thisId = result[i]["id"];
					cellA = row.insertCell();
					thisName = result[i]["name"].trim();
					cellA.innerHTML = thisName;
					cellA = row.insertCell();
					thisAmount = result[i]["amount"];
					var input = document.createElement('input');
					input.setAttribute('id', 'amount' + thisId);
					input.setAttribute('onchange', 'rowChanged.push(' + thisId + ')');
					input.style.width = "100%";
					input.value = thisAmount;
					cellA.appendChild(input);
					cellA = row.insertCell();
					thisPortion = result[i]["portion"];
					cellA.innerHTML = result[i]["portionName"];
					cellA = row.insertCell();
					thisArea = result[i]["area"];
					cellA.innerHTML = result[i]["areaName"];;
				}
			}
			$("table td").addClass("mlcell");
			$("table th").addClass("mlheadercell");
		},
		error: function (xhr, ajaxOptions, thrownError) {
			alert(xhr.responseText);
		}
	});
}

function SetSortField(field){
	var sortItems = $("#sortField").html().split("_");
	if (field == sortItems[0]) {
		var sortDir = (sortItems[1] == "asc" ? "desc" : "asc");
		//console.log(field + "_" + sortDir);
		$("#sortField").html(field + "_" + sortDir);
	} else {
		$("#sortField").html(field + "_asc");
	}

	GetAll();
}

function UpdateInventoryDate(){
	var enteredDate = $("#inventoryDate").val();
	var splitDate = enteredDate.split("/");
	if (splitDate.length != 3) { alert("Enter a valid date"); return;}
	var enteredDateAsDate = new Date(+splitDate[2], +splitDate[1], +splitDate[0]);
	//console.log(enteredDateAsDate);
	if (enteredDateAsDate == "Invalid Date") {alert("Enter a valid date"); return;}
	var selectedDateTime = enteredDateAsDate.getDate().toString().padStart(2, "0") + "/" + enteredDateAsDate.getMonth().toString().padStart(2, "0") + "/"
		+ enteredDateAsDate.getFullYear();
	//console.log(selectedDateTime);
	config["inventoryDate"] = selectedDateTime;
	var jsonData = JSON.stringify({ id: {"N": config["id"].toString()}, inventoryDate: {"S": config["inventoryDate"]}, active: {"BOOL": true} });
	$.ajax({
		url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/config",
		type: "PUT",
		data: jsonData,
		contentType: "application/json",
		success: function(result) {
			GetAll();
		},
		error: function (xhr, ajaxOptions, thrownError) {
			alert(xhr.responseText);
		}
	});
}

function Submit(){
	var data = [];
	rowChanged.forEach(id => {
		var thisAmount = $("#amount" + id).val();
		if (thisAmount == "") { thisAmount = "0";}
		var baseData = {amount: thisAmount, id: id.toString()};
		data.push(baseData);
	});
	//console.log(data);
	var jsonData = JSON.stringify(data);
	console.log(jsonData);
	$.ajax({
		url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/inventory",
		type: "PUT",
		data: jsonData,
		contentType: "application/json",
		success: function(result) {
			GetAll(); 
		},
		error: function (xhr, ajaxOptions, thrownError) {
			alert(xhr.responseText);
		}
	});	
}

function ReviseInventoryFromDate(){
	// get the dates between inventory date and now
	// reduce inventory by daily depletion for each record in depletion table
	// then reduce inventory by amounts in meals between the 2 dates (incl today)
	// just update the screen amounts and allow the user to submit
	// pop up an alert to remind the user that they still need to submit
	var splitDate = config["inventoryDate"].split("/");
	var invDateAsDate = new Date(+splitDate[2], +splitDate[1]-1, +splitDate[0]);
	var today = new Date();
	//console.log(invDateAsDate, today);

	//depletions
	var dateDiff = Math.floor((today - invDateAsDate) / 86400000);
	//console.log(dateDiff);
	var depletionlist = [];
	$.ajax({
		url: "https://i2myytdj3i.execute-api.eu-west-2.amazonaws.com/depletion",
		type: "GET",
		success: function(result) {
			//console.log(result);
			for (i = 0; i < result.length; i++) {
				var thisActive = result[i]["active"];
				if (thisActive) {
					depletionlist.push(result[i]);
				}
			}

			//console.log(depletionlist);
			depletionlist.forEach(dep => {
				dep["depletion"] = dep["depletion"] * dateDiff;
				var currAmount = $("#amount" + dep["product"]).val();
				var newAmount = Math.round((currAmount - dep["depletion"]) * 100) / 100;
				if (newAmount < 0) {newAmount = 0;}
				if (currAmount != newAmount){
					$("#amount" + dep["product"]).val(newAmount);
					var alreadyChanged = rowChanged.indexOf(dep["product"]);
					if (alreadyChanged == -1) {rowChanged.push(dep["product"]);}
				}
			});
			//console.log(depletionlist);


			//var currDate = new Date(invDateAsDate);
			//for (i = 0; i < dateDiff; i++){
			//	currDate.setDate(currDate.getDate() + 1);
				//console.log(currDate);
			//}
		},
		error: function (xhr, ajaxOptions, thrownError) {
			alert(xhr.responseText);
		}
	});
}